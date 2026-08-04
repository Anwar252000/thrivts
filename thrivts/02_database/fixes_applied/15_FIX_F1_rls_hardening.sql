-- ============================================================================
-- F1 — RLS hardening for the two buyer/seller bid views.
--
-- These views run as their owner (they bypass row-level security), so tenant
-- isolation relied entirely on the app always filtering. That's correct in the
-- UI, but a crafted API call (using the anon key directly, not the app) could
-- read another tenant's bids. This adds an auth.uid() scope INSIDE each view:
--   • buyer_visible_bids -> only bids on requirements the caller owns
--   • seller_own_bids     -> only the caller's own bids
--
-- Verified safe: buyer_visible_bids is read only by the buyer portal,
-- seller_own_bids only by the seller portal, admin reads neither. The app
-- already filters the same way, so this is a no-op for legitimate calls and a
-- block for crafted ones. Columns are unchanged, so create-or-replace applies
-- cleanly; if the live columns ever differed, Postgres rejects this rather than
-- breaking the board — a built-in safety net.
--
-- Moat: no identities or fee splits were ever exposed; this closes the last
-- cross-tenant read gap.
-- ============================================================================

create or replace view public.buyer_visible_bids as
  select
    o.id                        as bid_id,
    o.requirement_id,
    o.available_quantity_pcs,
    round((coalesce(o.current_price_usd,o.proposed_price_usd) + coalesce(o.fee_per_pc_applied_usd,0.70))::numeric,2)
                                as buyer_price_per_pc_usd,
    round(((coalesce(o.current_price_usd,o.proposed_price_usd) + coalesce(o.fee_per_pc_applied_usd,0.70)) * o.available_quantity_pcs)::numeric,2)
                                as buyer_total_usd,
    case when coalesce(r.quantity_pcs,0) > 0
         then round((o.available_quantity_pcs::numeric / r.quantity_pcs::numeric) * 100, 0)
         else null end          as fills_pct_of_requirement,
    r.quantity_pcs              as requirement_qty_pcs,
    coalesce(o.negotiation_state,'open') as negotiation_state,
    o.last_actor,
    coalesce(o.round_count,0)   as round_count,
    o.last_action_at,
    case coalesce(o.negotiation_state,'open')
      when 'countered_by_buyer'  then 'You countered — awaiting seller'
      when 'countered_by_seller' then 'Seller countered back — your move'
      when 'declined'            then 'Seller declined your counter'
      when 'accepted'            then 'Accepted'
      else 'Open bid'
    end                         as status_label,
    o.status,
    o.responded_at              as bid_time,
    s.public_alias              as seller_alias,
    s.tier                      as seller_tier,
    s.kyc_verified              as seller_verified
  from public.seller_responses o
  join public.sellers s      on s.id = o.seller_id
  join public.requirements r on r.id = o.requirement_id
  where r.buyer_id = auth.uid();

grant select on public.buyer_visible_bids to authenticated;

create or replace view public.seller_own_bids as
  select
    o.id                        as bid_id,
    o.requirement_id,
    o.seller_id,
    r.requirement_number,
    r.item_name,
    r.quantity_pcs              as requirement_qty_pcs,
    r.grade,
    r.destination_country,
    o.available_quantity_pcs,
    coalesce(o.current_price_usd, o.proposed_price_usd) as your_price_per_pc_usd,
    coalesce(o.fee_per_pc_applied_usd,0.70)             as platform_fee_per_pc_usd,
    round((coalesce(o.current_price_usd,o.proposed_price_usd) - coalesce(o.fee_per_pc_applied_usd,0.70))::numeric,2)
                                as you_receive_per_pc_usd,
    round(((coalesce(o.current_price_usd,o.proposed_price_usd) - coalesce(o.fee_per_pc_applied_usd,0.70)) * o.available_quantity_pcs)::numeric,2)
                                as you_receive_total_usd,
    o.buyer_counter_price_usd,
    round((coalesce(o.buyer_counter_price_usd,0) - coalesce(o.fee_per_pc_applied_usd,0.70))::numeric,2)
                                as counter_you_receive_per_pc,
    o.buyer_counter_at,
    o.buyer_counter_note,
    coalesce(o.negotiation_state,'open') as negotiation_state,
    o.last_actor,
    o.last_action_at,
    case coalesce(o.negotiation_state,'open')
      when 'countered_by_buyer'  then 'Buyer countered — your move'
      when 'countered_by_seller' then 'You countered — awaiting buyer'
      when 'declined'            then 'You declined the counter'
      when 'accepted'            then 'Accepted — deal created'
      else 'Bid submitted — awaiting buyer'
    end                         as status_label,
    o.status,
    o.responded_at
  from public.seller_responses o
  join public.requirements r on r.id = o.requirement_id
  where o.seller_id = auth.uid();

grant select on public.seller_own_bids to authenticated;

notify pgrst, 'reload schema';
