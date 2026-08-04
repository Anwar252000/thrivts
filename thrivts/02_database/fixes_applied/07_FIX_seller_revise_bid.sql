-- ============================================================================
-- FIX: "duplicate key ... seller_responses_requirement_id_seller_id_key" when a
--      seller re-submits a quote on a requirement they already bid on.
--
-- The seller form only ever INSERTed, so re-quoting collided with the
-- one-bid-per-requirement constraint. A seller can only change their standing
-- quote through this form (there's no other control while awaiting the buyer),
-- so re-submitting must REVISE the existing bid, not insert a new row.
--
-- Design note: NEW bids still go through the client insert (which keeps whatever
-- seller-approval / RLS gate the base schema enforces). This function only
-- handles the REVISE case for a bid the seller already owns, so no gate is
-- bypassed. It updates current_price_usd (what the buyer's board actually reads)
-- and resets the row to a fresh seller offer awaiting the buyer.
-- ============================================================================

create or replace function public.seller_revise_bid(
  p_requirement_id uuid,
  p_qty            integer,
  p_price          numeric,
  p_notes          text default null
) returns uuid
language plpgsql
security definer
set search_path = public
as $$
declare
  v_seller uuid;
  v_bid    record;
  v_req    record;
  v_alias  text;
begin
  -- caller must be a registered seller (sellers.id = auth user id)
  select id into v_seller from public.sellers where id = auth.uid();
  if v_seller is null then
    raise exception 'Only a registered seller can revise a quote.';
  end if;

  if p_qty   is null or p_qty   < 1  then raise exception 'Quantity must be at least 1.'; end if;
  if p_price is null or p_price <= 0 then raise exception 'Price must be greater than 0.'; end if;

  -- must already own a bid on this requirement
  select * into v_bid from public.seller_responses
   where requirement_id = p_requirement_id and seller_id = v_seller;
  if v_bid.id is null then
    raise exception 'No existing bid to revise on this requirement.';
  end if;
  if v_bid.negotiation_state = 'accepted' then
    raise exception 'This bid was already accepted and can no longer be changed.';
  end if;

  -- revise in place. current_price_usd is what buyer_visible_bids reads, so it
  -- must move for the buyer to see the new number. Reset to a fresh seller offer
  -- and clear any stale buyer counter. fee_per_pc_applied_usd stays frozen
  -- (its trigger is INSERT-only), so the split is untouched.
  update public.seller_responses
     set available_quantity_pcs = p_qty,
         proposed_price_usd      = p_price,
         current_price_usd       = p_price,
         seller_notes            = p_notes,
         status                  = 'pending',
         negotiation_state       = 'countered_by_seller',
         last_actor              = 'seller',
         buyer_counter_price_usd = null,
         buyer_counter_at        = null,
         buyer_counter_note      = null,
         round_count             = coalesce(round_count, 0) + 1,
         last_action_at          = now()
   where id = v_bid.id;

  -- notify the buyer. Moat-safe: anonymous alias + qty only — never the seller's
  -- raw price or the fee split. The buyer sees the all-in number on their board.
  select * into v_req from public.requirements where id = p_requirement_id;
  select public_alias into v_alias from public.sellers where id = v_seller;
  perform public.push_notification(
    v_req.buyer_id, 'buyer', 'bid_revised',
    'Updated bid on ' || coalesce(v_req.requirement_number, 'your requirement'),
    coalesce(v_alias, 'A seller') || ' revised their quote on ' || p_qty || ' pcs',
    'requirement', p_requirement_id);

  return v_bid.id;
end;
$$;

grant execute on function public.seller_revise_bid(uuid, integer, numeric, text) to authenticated;
notify pgrst, 'reload schema';
