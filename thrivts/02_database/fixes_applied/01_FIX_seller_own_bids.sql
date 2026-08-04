drop view if exists public.seller_own_bids;
create view public.seller_own_bids as
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
    -- the buyer's counter, if one is on the table (seller-side number)
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
  join public.requirements r on r.id = o.requirement_id;

comment on view public.seller_own_bids is
  'Seller-facing bid board. RLS on seller_responses scopes rows to the seller. Shows their price, fee, net, buyer counters, and current negotiation status.';

grant select on public.seller_own_bids to authenticated;

notify pgrst, 'reload schema';
