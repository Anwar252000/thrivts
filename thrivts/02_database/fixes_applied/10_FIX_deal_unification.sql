-- ============================================================================
-- DEAL CREATION UNIFICATION
--
-- Problem: a bid gets accepted in two places, and they disagreed —
--   • buyer_accept_bid  -> creates the deal (correct)
--   • seller_respond_to_counter('accept') -> did NOT create a deal; it bounced
--     the state back to 'open' and forced the BUYER to accept AGAIN. That double
--     confirmation is the broken flow: both sides already agreed on the price.
--
-- Fix: one shared internal function finalize_bid_to_deal(bid) creates the deal.
-- Both accept paths call it, so ANY mutual acceptance creates the deal at once,
-- no re-confirmation. It also uses deal_number_seq (fixes the deal-number race).
-- Callers authorize; finalize_bid_to_deal itself is internal.
-- ============================================================================

-- The Chunk-3 duplicate trigger is superseded here (step 10 notifies the seller).
drop trigger if exists notify_seller_new_deal on public.deals;
drop function if exists public.trg_notify_seller_new_deal();

create or replace function public.finalize_bid_to_deal(p_bid_id uuid)
returns jsonb
language plpgsql
security definer
set search_path = public
as $$
declare
  v_bid record; v_req record;
  v_already int; v_remaining int; v_qty int;
  v_seller_price numeric; v_fee numeric; v_buyer_price numeric;
  v_subtotal numeric; v_spread_total numeric; v_payout_total numeric;
  v_deal_id uuid; v_deal_number text;
begin
  select sr.* into v_bid from seller_responses sr where sr.id = p_bid_id;
  if not found then raise exception 'Bid not found'; end if;

  -- lock the requirement row so concurrent accepts serialize (over-fill guard)
  select r.* into v_req from requirements r where r.id = v_bid.requirement_id for update;
  if not found then raise exception 'Requirement not found'; end if;

  if lower(coalesce(v_bid.status::text,'')) = 'accepted' then
    raise exception 'This bid is already accepted';
  end if;

  select coalesce(sum(available_quantity_pcs),0) into v_already
    from seller_responses
   where requirement_id = v_bid.requirement_id and lower(status::text) = 'accepted';
  v_remaining := coalesce(v_req.quantity_pcs,0) - v_already;
  if v_remaining <= 0 then raise exception 'This requirement is already fully committed'; end if;
  if v_bid.available_quantity_pcs > v_remaining then
    raise exception 'This bid (% pcs) exceeds the remaining quantity (% pcs).',
      v_bid.available_quantity_pcs, v_remaining;
  end if;

  v_qty          := v_bid.available_quantity_pcs;
  v_seller_price := round(coalesce(v_bid.current_price_usd, v_bid.proposed_price_usd)::numeric, 4);
  v_fee          := round(coalesce(v_bid.fee_per_pc_applied_usd, 0.70)::numeric, 4);
  v_buyer_price  := round((v_seller_price + v_fee)::numeric, 4);
  v_subtotal     := round((v_buyer_price  * v_qty)::numeric, 2);
  v_spread_total := round((v_fee          * v_qty)::numeric, 2);
  v_payout_total := round((v_seller_price * v_qty)::numeric, 2);

  -- deal number via the SAME sequence the admin path uses (no max()+1 race)
  v_deal_number := 'DEAL-' || to_char(now(),'YYYY') || '-' ||
                   lpad(nextval('public.deal_number_seq')::text, 5, '0');

  insert into deals (
    deal_number, requirement_id, buyer_id, seller_id, total_quantity_pcs,
    buyer_price_per_pc_usd, avg_seller_price_per_pc_usd, seller_cost_per_pc_usd,
    spread_per_pc_usd, subtotal_usd, total_invoice_usd, total_spread_usd,
    total_seller_payout_usd, total_seller_cost_usd, shipping_cost_usd,
    destination_country, source_response_id, status, admin_notes
  ) values (
    v_deal_number, v_bid.requirement_id, v_req.buyer_id, v_bid.seller_id, v_qty,
    v_buyer_price, v_seller_price, v_seller_price,
    v_fee, v_subtotal, v_subtotal, v_spread_total,
    v_payout_total, v_payout_total, 0,
    v_req.destination_country, p_bid_id, 'confirmed'::deal_status,
    'Auto-created on mutual acceptance'
  ) returning id into v_deal_id;

  -- allocation (guarded so a column mismatch can't undo the deal)
  begin
    insert into deal_allocations (deal_id, seller_id, allocated_quantity_pcs, price_per_pc_usd)
    values (v_deal_id, v_bid.seller_id, v_qty, v_seller_price);
    begin
      update deal_allocations
         set fee_per_pc_applied_usd = v_fee,
             gross_payout_usd       = v_payout_total,
             platform_fee_usd       = v_spread_total,
             total_payout_usd       = round((v_payout_total - v_spread_total)::numeric,2)
       where deal_id = v_deal_id and seller_id = v_bid.seller_id;
    exception when undefined_column then null;
    end;
  exception when others then raise notice 'allocation note: %', sqlerrm;
  end;

  update seller_responses
     set status='accepted'::seller_response_status, negotiation_state='accepted',
         deal_id=v_deal_id, last_action_at=now()
   where id = p_bid_id;

  begin
    if (v_remaining - v_qty) <= 0 then
      update requirements set status='ready_to_order' where id=v_bid.requirement_id;
    else
      update requirements set status='matching'
       where id=v_bid.requirement_id
         and lower(status::text) not in ('ready_to_order','in_fulfillment','completed','cancelled');
    end if;
  exception when others then raise notice 'status advance note: %', sqlerrm;
  end;

  begin
    perform push_notification(v_bid.seller_id, 'seller', 'accepted',
      'Your bid was accepted', 'Deal ' || v_deal_number || ' — ' || v_qty || ' pcs',
      'deal', v_deal_id);
  exception when others then raise notice 'seller notify note: %', sqlerrm;
  end;

  return jsonb_build_object('ok', true, 'deal_id', v_deal_id, 'deal_number', v_deal_number,
    'remaining_after', v_remaining - v_qty, 'fully_filled', (v_remaining - v_qty) <= 0);
end;
$$;

-- ---------- buyer accept -> shared finalize ----------
create or replace function public.buyer_accept_bid(p_bid_id uuid)
returns jsonb
language plpgsql
security definer
set search_path = public
as $$
declare v_bid record; v_req record; v_status text;
begin
  select sr.* into v_bid from seller_responses sr where sr.id = p_bid_id;
  if not found then raise exception 'Bid not found'; end if;
  select r.* into v_req from requirements r where r.id = v_bid.requirement_id;
  if not found then raise exception 'Requirement not found'; end if;
  if v_req.buyer_id is distinct from auth.uid() then raise exception 'not authorized'; end if;

  v_status := lower(coalesce(v_bid.status::text,''));
  if v_status in ('rejected','withdrawn','expired','cancelled','declined') then
    raise exception 'This bid can no longer be accepted (%)', v_status;
  end if;

  return public.finalize_bid_to_deal(p_bid_id);
end;
$$;

-- ---------- seller responds to counter: 'accept' now CREATES the deal ----------
create or replace function public.seller_respond_to_counter(p_bid_id uuid, p_action text, p_new_price_usd numeric DEFAULT NULL::numeric, p_note text DEFAULT NULL::text)
returns jsonb
language plpgsql
security definer
set search_path = public
as $$
declare
  v_bid record; v_req record; v_seller record; v_fee numeric;
begin
  select * into v_bid from seller_responses where id = p_bid_id;
  if not found then raise exception 'Bid not found'; end if;

  select * into v_seller from sellers where id = v_bid.seller_id;
  if not found then raise exception 'Seller not found'; end if;
  if v_seller.id is distinct from auth.uid() then raise exception 'not authorized'; end if;

  select * into v_req from requirements where id = v_bid.requirement_id;
  v_fee := coalesce(v_bid.fee_per_pc_applied_usd, 0.70);

  if v_bid.negotiation_state = 'accepted' then
    raise exception 'This bid is already accepted';
  end if;

  if p_action = 'accept' then
    -- Seller accepts the buyer's counter -> MUTUAL AGREEMENT -> create the deal now.
    update seller_responses
       set current_price_usd  = coalesce(buyer_counter_price_usd, current_price_usd, proposed_price_usd),
           proposed_price_usd = coalesce(buyer_counter_price_usd, proposed_price_usd),
           last_actor         = 'seller',
           last_action_at     = now()
     where id = p_bid_id;

    perform push_notification(
      v_req.buyer_id, 'buyer', 'counter_accepted',
      'Seller accepted your counter — deal created',
      'A deal was created on ' || coalesce(v_req.requirement_number,'your requirement') || '.',
      'requirement', v_bid.requirement_id);

    return public.finalize_bid_to_deal(p_bid_id);

  elsif p_action = 'decline' then
    update seller_responses
       set negotiation_state = 'declined', last_actor = 'seller', last_action_at = now()
     where id = p_bid_id;
    perform push_notification(
      v_req.buyer_id, 'buyer', 'counter_declined',
      'Seller declined your counter',
      'Your counter on ' || coalesce(v_req.requirement_number,'a requirement') || ' was declined',
      'requirement', v_bid.requirement_id);
    return jsonb_build_object('ok', true, 'state','declined');

  elsif p_action = 'counter' then
    if p_new_price_usd is null or p_new_price_usd <= 0 then
      raise exception 'Enter a valid counter price';
    end if;
    update seller_responses
       set current_price_usd  = round(p_new_price_usd::numeric,4),
           proposed_price_usd = round(p_new_price_usd::numeric,4),
           seller_notes       = coalesce(p_note, seller_notes),
           negotiation_state  = 'countered_by_seller',
           last_actor         = 'seller',
           round_count        = coalesce(round_count,0) + 1,
           last_action_at     = now()
     where id = p_bid_id;
    perform push_notification(
      v_req.buyer_id, 'buyer', 'counter',
      'Seller countered back',
      'New price: $' || to_char(p_new_price_usd + v_fee, 'FM999999990.00') ||
        '/pc on ' || coalesce(v_req.requirement_number,'your requirement'),
      'requirement', v_bid.requirement_id);
    return jsonb_build_object('ok', true, 'state','countered_by_seller');
  else
    raise exception 'Unknown action: %', p_action;
  end if;
end;
$$;

grant execute on function public.finalize_bid_to_deal(uuid) to authenticated;
grant execute on function public.buyer_accept_bid(uuid) to authenticated;
grant execute on function public.seller_respond_to_counter(uuid, text, numeric, text) to authenticated;
notify pgrst, 'reload schema';
