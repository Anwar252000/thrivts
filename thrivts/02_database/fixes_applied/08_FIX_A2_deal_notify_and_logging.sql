-- ============================================================================
-- A2 — deal-notification dedupe + kill the last silent catch in buyer_accept_bid
--
-- (1) buyer_accept_bid step 10 ALREADY notifies the seller (link_type='deal'),
--     so the notify_seller_new_deal trigger added earlier was a DUPLICATE —
--     sellers were getting two "your bid was accepted" notifications. Drop it.
--     The admin deal trigger and the seller's My Deals dot both still work
--     (the dot keys on link_type='deal', which step 10 already provides).
-- (2) Recreate buyer_accept_bid with step 10's silent catch changed to log,
--     so a failed seller notification is visible instead of swallowed.
-- ============================================================================

drop trigger if exists notify_seller_new_deal on public.deals;
drop function if exists public.trg_notify_seller_new_deal();

-- ============================================================================
-- THRIVTS — FIX 4: buyer_accept_bid (written against the VERIFIED deals schema)
-- ----------------------------------------------------------------------------
-- Previous attempts failed because I guessed column names. This version is
-- written against the actual information_schema output for public.deals.
--
-- REQUIRED (NOT NULL, no default) columns on deals — all supplied here:
--   deal_number, requirement_id, buyer_id, total_quantity_pcs,
--   buyer_price_per_pc_usd, avg_seller_price_per_pc_usd, spread_per_pc_usd,
--   subtotal_usd, total_invoice_usd, total_spread_usd, total_seller_payout_usd
--
-- MONEY MODEL under the flat fee:
--   buyer_price_per_pc      = seller price + $0.70 platform fee
--   avg_seller_price_per_pc = the seller's own price
--   spread_per_pc           = the platform fee ($0.70)  <- the fee IS the spread
--   subtotal / total_invoice = buyer_price_per_pc * qty
--   total_spread            = fee * qty          (Thrivts revenue)
--   total_seller_payout     = seller_price * qty (what the seller is owed)
--
-- Idempotent. Run in SQL Editor.
-- ============================================================================

create or replace function public.buyer_accept_bid(p_bid_id uuid)
returns jsonb
language plpgsql
security definer set search_path = public as $$
declare
  v_bid          record;
  v_req          record;
  v_buyer_uid    uuid := auth.uid();
  v_already      integer;
  v_remaining    integer;
  v_status_txt   text;
  v_qty          integer;
  v_seller_price numeric;
  v_fee          numeric;
  v_buyer_price  numeric;
  v_subtotal     numeric;
  v_spread_total numeric;
  v_payout_total numeric;
  v_deal_id      uuid;
  v_deal_number  text;
  v_seq          integer;
begin
  -- 1) Load bid + requirement, verify ownership
  select sr.* into v_bid from seller_responses sr where sr.id = p_bid_id;
  if not found then raise exception 'Bid not found'; end if;

  select r.* into v_req from requirements r where r.id = v_bid.requirement_id;
  if not found then raise exception 'Requirement not found'; end if;
  if v_req.buyer_id is distinct from v_buyer_uid then
    raise exception 'not authorized';
  end if;

  -- 2) Bid must be live
  v_status_txt := lower(coalesce(v_bid.status::text, ''));
  if v_status_txt = 'accepted' then raise exception 'This bid is already accepted'; end if;
  if v_status_txt in ('rejected','withdrawn','expired','cancelled','declined') then
    raise exception 'This bid can no longer be accepted (%)', v_status_txt;
  end if;

  -- 3) Multi-seller fill guard
  select coalesce(sum(available_quantity_pcs),0) into v_already
    from seller_responses
   where requirement_id = v_bid.requirement_id
     and lower(status::text) = 'accepted';

  v_remaining := coalesce(v_req.quantity_pcs,0) - v_already;
  if v_remaining <= 0 then raise exception 'This requirement is already fully committed'; end if;
  if v_bid.available_quantity_pcs > v_remaining then
    raise exception 'This bid (% pcs) exceeds the remaining quantity (% pcs).',
      v_bid.available_quantity_pcs, v_remaining;
  end if;

  -- 4) Money (flat-fee model: the fee IS the spread)
  v_qty          := v_bid.available_quantity_pcs;
  v_seller_price := round(coalesce(v_bid.current_price_usd, v_bid.proposed_price_usd)::numeric, 4);
  v_fee          := round(coalesce(v_bid.fee_per_pc_applied_usd, 0.70)::numeric, 4);
  v_buyer_price  := round((v_seller_price + v_fee)::numeric, 4);
  v_subtotal     := round((v_buyer_price  * v_qty)::numeric, 2);
  v_spread_total := round((v_fee          * v_qty)::numeric, 2);
  v_payout_total := round((v_seller_price * v_qty)::numeric, 2);

  -- 5) Deal number
  begin
    select coalesce(max(substring(deal_number from '[0-9]+$')::int), 0) + 1
      into v_seq from deals
     where deal_number like 'DEAL-' || to_char(now(),'YYYY') || '-%';
  exception when others then v_seq := 1;
  end;
  v_deal_number := 'DEAL-' || to_char(now(),'YYYY') || '-' || lpad(coalesce(v_seq,1)::text, 5, '0');

  -- 6) Create the deal — every NOT NULL column supplied
  insert into deals (
    deal_number,
    requirement_id,
    buyer_id,
    seller_id,
    total_quantity_pcs,
    buyer_price_per_pc_usd,
    avg_seller_price_per_pc_usd,
    seller_cost_per_pc_usd,
    spread_per_pc_usd,
    subtotal_usd,
    total_invoice_usd,
    total_spread_usd,
    total_seller_payout_usd,
    total_seller_cost_usd,
    shipping_cost_usd,
    destination_country,
    source_response_id,
    status,
    admin_notes
  ) values (
    v_deal_number,
    v_bid.requirement_id,
    v_req.buyer_id,
    v_bid.seller_id,
    v_qty,
    v_buyer_price,
    v_seller_price,
    v_seller_price,
    v_fee,
    v_subtotal,
    v_subtotal,
    v_spread_total,
    v_payout_total,
    v_payout_total,
    0,
    v_req.destination_country,
    p_bid_id,
    'confirmed'::deal_status,
    'Buyer-accepted via live bidding board'
  )
  returning id into v_deal_id;

  -- 7) Seller allocation (fee snapshot frozen). Guarded so a column mismatch
  --    here cannot undo the deal.
  begin
    insert into deal_allocations (
      deal_id, seller_id, allocated_quantity_pcs, price_per_pc_usd
    ) values (
      v_deal_id, v_bid.seller_id, v_qty, v_seller_price
    );
    begin
      update deal_allocations
         set fee_per_pc_applied_usd = v_fee,
             gross_payout_usd       = v_payout_total,
             platform_fee_usd       = v_spread_total,
             total_payout_usd       = round((v_payout_total - v_spread_total)::numeric,2)
       where deal_id = v_deal_id and seller_id = v_bid.seller_id;
    exception when undefined_column then null;
    end;
  exception when others then
    raise notice 'allocation note: %', sqlerrm;
  end;

  -- 8) Mark the bid accepted
  update seller_responses
     set status            = 'accepted'::seller_response_status,
         negotiation_state = 'accepted',
         last_action_at    = now()
   where id = p_bid_id;

  -- 9) Advance the requirement through the lifecycle:
  --    partially filled -> 'matching', fully committed -> 'ready_to_order'
  begin
    if (v_remaining - v_qty) <= 0 then
      update requirements set status = 'ready_to_order' where id = v_bid.requirement_id;
    else
      update requirements set status = 'matching'
       where id = v_bid.requirement_id
         and lower(status::text) not in ('ready_to_order','in_fulfillment','completed','cancelled');
    end if;
  exception when others then
    raise notice 'status advance note: %', sqlerrm;
  end;

  -- 10) Notify the seller (never blocks)
  begin
    perform push_notification(
      v_bid.seller_id, 'seller', 'accepted',
      'Your bid was accepted',
      'Deal ' || v_deal_number || ' — ' || v_qty || ' pcs',
      'deal', v_deal_id);
  exception when others then raise notice 'seller notify note: %', sqlerrm;
  end;

  return jsonb_build_object(
    'ok', true,
    'deal_id', v_deal_id,
    'deal_number', v_deal_number,
    'remaining_after', v_remaining - v_qty,
    'fully_filled', (v_remaining - v_qty) <= 0
  );
end;
$$;

revoke execute on function public.buyer_accept_bid(uuid) from public, anon;
grant execute on function public.buyer_accept_bid(uuid) to authenticated;

notify pgrst, 'reload schema';


-- ============================================================================
-- RECONCILIATION — run anytime to catch a deal created without its allocation.
-- The allocation insert is guarded (can't undo the deal); this surfaces a silent
-- miss instead of it hiding. Expect zero rows.
-- ============================================================================
-- select d.deal_number, d.created_at
-- from deals d
-- left join deal_allocations a on a.deal_id = d.id
-- where a.id is null
-- order by d.created_at desc;
