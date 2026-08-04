-- ============================================================================
-- THRIVTS — PART 8: FULL NEGOTIATION LOOP + NOTIFICATIONS
-- ----------------------------------------------------------------------------
-- Closes the loop that was left half-built:
--   * Buyer counters a bid  -> seller SEES it and can accept / decline / re-counter
--   * Seller re-counters    -> buyer SEES it and can accept / decline / re-counter
--   * Unlimited rounds, always showing CURRENT STATUS on both sides
--   * Every action writes a NOTIFICATION for the other party (bell feed)
--
-- Run AFTER: PART2, PART4, FIX_accept_bid_and_admin_board, FIX2.
-- Idempotent. Safe to re-run.
-- ============================================================================


-- ----------------------------------------------------------------------------
-- 1. NEGOTIATION STATE ON THE BID
-- ----------------------------------------------------------------------------
alter table public.seller_responses
  add column if not exists negotiation_state text default 'open',
  add column if not exists last_actor        text,
  add column if not exists current_price_usd numeric(12,4),
  add column if not exists round_count       integer default 0,
  add column if not exists last_action_at    timestamptz;

comment on column public.seller_responses.negotiation_state is
  'open | countered_by_buyer | countered_by_seller | accepted | declined. Drives the status text both sides see.';
comment on column public.seller_responses.current_price_usd is
  'The live SELLER-SIDE price on the table right now (fee added on top for buyer display).';

-- Seed existing rows so nothing shows blank.
update public.seller_responses
   set current_price_usd = coalesce(current_price_usd, proposed_price_usd),
       negotiation_state = coalesce(negotiation_state, 'open'),
       round_count       = coalesce(round_count, 0)
 where current_price_usd is null or negotiation_state is null;


-- ----------------------------------------------------------------------------
-- 2. NOTIFICATIONS TABLE
-- ----------------------------------------------------------------------------
-- MOVED to PART8A_notifications_table.sql — run that FIRST, on its own.
-- Splitting it out means a failure here can't roll back the table creation.

-- Helper: write a notification (used by all the RPCs below).
create or replace function public.push_notification(
  p_user_id uuid, p_audience text, p_kind text,
  p_title text, p_body text default null,
  p_link_type text default null, p_link_id uuid default null
) returns void
language plpgsql security definer set search_path = public as $$
declare
  v_col text;
begin
  if p_user_id is null then return; end if;

  -- The notifications table may already exist with a different owner column
  -- (recipient_id) from the original build. Detect it rather than assume.
  select column_name into v_col
    from information_schema.columns
   where table_schema='public' and table_name='notifications'
     and column_name in ('recipient_id','user_id')
   order by case column_name when 'recipient_id' then 1 else 2 end
   limit 1;

  if v_col is null then return; end if;   -- no compatible table: skip silently

  begin
    execute format(
      'insert into public.notifications (%I, title, body) values ($1,$2,$3)',
      v_col
    ) using p_user_id, p_title, p_body;
  exception when others then
    -- NEVER let a notification failure break bidding / counters / accepts.
    raise notice 'notification skipped: %', sqlerrm;
  end;
end;
$$;

-- Mark notifications read.
create or replace function public.mark_notifications_read(p_ids uuid[] default null)
returns void
language plpgsql security definer set search_path = public as $$
begin
  if p_ids is null then
    update notifications set read_at = now()
     where user_id = auth.uid() and read_at is null;
  else
    update notifications set read_at = now()
     where user_id = auth.uid() and id = any(p_ids);
  end if;
end;
$$;
grant execute on function public.mark_notifications_read(uuid[]) to authenticated;


-- ----------------------------------------------------------------------------
-- 3. BUYER COUNTERS  (replaces the earlier stub)
-- ----------------------------------------------------------------------------
create or replace function public.buyer_counter_bid(
  p_bid_id uuid,
  p_counter_buyer_price_usd numeric,      -- fee-INCLUSIVE price the buyer will pay
  p_note text default null
) returns jsonb
language plpgsql security definer set search_path = public as $$
declare
  v_bid record; v_req record; v_fee numeric; v_seller_side numeric; v_seller_uid uuid;
begin
  select * into v_bid from seller_responses where id = p_bid_id;
  if not found then raise exception 'Bid not found'; end if;

  select * into v_req from requirements where id = v_bid.requirement_id;
  if v_req.buyer_id is distinct from auth.uid() then raise exception 'not authorized'; end if;
  if lower(coalesce(v_bid.status::text,'')) = 'accepted'
     or v_bid.negotiation_state = 'accepted' then
    raise exception 'This bid is already accepted';
  end if;
  if p_counter_buyer_price_usd is null or p_counter_buyer_price_usd <= 0 then
    raise exception 'Enter a valid counter price';
  end if;

  v_fee := coalesce(v_bid.fee_per_pc_applied_usd, 0.70);
  v_seller_side := round((p_counter_buyer_price_usd - v_fee)::numeric, 4);
  if v_seller_side <= 0 then raise exception 'Counter must be more than the platform fee'; end if;

  update seller_responses
     set buyer_counter_price_usd = v_seller_side,
         buyer_counter_at        = now(),
         buyer_counter_note      = p_note,
         current_price_usd       = v_seller_side,
         negotiation_state       = 'countered_by_buyer',
         last_actor              = 'buyer',
         round_count             = coalesce(round_count,0) + 1,
         last_action_at          = now()
   where id = p_bid_id;

  -- notify the seller (sellers.id IS the auth user id in this schema)
  v_seller_uid := v_bid.seller_id;
  perform push_notification(
    v_seller_uid, 'seller', 'counter',
    'Buyer countered your bid',
    'New offer: $' || to_char(v_seller_side + v_fee, 'FM999999990.00') ||
      '/pc on ' || coalesce(v_req.requirement_number,'a requirement'),
    'requirement', v_bid.requirement_id);

  return jsonb_build_object('ok', true, 'state','countered_by_buyer',
                            'counter_buyer_price', p_counter_buyer_price_usd);
end;
$$;
revoke execute on function public.buyer_counter_bid(uuid,numeric,text) from public, anon;
grant execute on function public.buyer_counter_bid(uuid,numeric,text) to authenticated;


-- ----------------------------------------------------------------------------
-- 4. SELLER RESPONDS — accept / decline / re-counter
-- ----------------------------------------------------------------------------
create or replace function public.seller_respond_to_counter(
  p_bid_id uuid,
  p_action text,                      -- 'accept' | 'decline' | 'counter'
  p_new_price_usd numeric default null,  -- SELLER-side price when countering
  p_note text default null
) returns jsonb
language plpgsql security definer set search_path = public as $$
declare
  v_bid record; v_req record; v_seller record; v_fee numeric;
begin
  select * into v_bid from seller_responses where id = p_bid_id;
  if not found then raise exception 'Bid not found'; end if;

  select * into v_seller from sellers where id = v_bid.seller_id;
  if not found then raise exception 'Seller not found'; end if;
  -- sellers.id IS the auth user id in this schema
  if v_seller.id is distinct from auth.uid() then raise exception 'not authorized'; end if;

  select * into v_req from requirements where id = v_bid.requirement_id;
  v_fee := coalesce(v_bid.fee_per_pc_applied_usd, 0.70);

  if v_bid.negotiation_state = 'accepted' then
    raise exception 'This bid is already accepted';
  end if;

  if p_action = 'accept' then
    -- Seller accepts the buyer's counter: that price becomes the live price.
    update seller_responses
       set current_price_usd  = coalesce(buyer_counter_price_usd, current_price_usd, proposed_price_usd),
           proposed_price_usd = coalesce(buyer_counter_price_usd, proposed_price_usd),
           negotiation_state  = 'open',      -- back to open at the agreed price
           last_actor         = 'seller',
           round_count        = coalesce(round_count,0) + 1,
           last_action_at     = now()
     where id = p_bid_id;

    perform push_notification(
      v_req.buyer_id, 'buyer', 'counter_accepted',
      'Seller accepted your counter',
      'You can now accept the bid at your price on ' || coalesce(v_req.requirement_number,'your requirement'),
      'requirement', v_bid.requirement_id);

    return jsonb_build_object('ok', true, 'state','open','message','Counter accepted');

  elsif p_action = 'decline' then
    update seller_responses
       set negotiation_state = 'declined',
           last_actor        = 'seller',
           last_action_at    = now()
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
revoke execute on function public.seller_respond_to_counter(uuid,text,numeric,text) from public, anon;
grant execute on function public.seller_respond_to_counter(uuid,text,numeric,text) to authenticated;


-- ----------------------------------------------------------------------------
-- 5. NOTIFY ON NEW BID + ON REQUIREMENT GOING LIVE
-- ----------------------------------------------------------------------------
create or replace function public.trg_notify_new_bid()
returns trigger language plpgsql security definer set search_path = public as $$
declare v_req record; v_alias text;
begin
  select * into v_req from requirements where id = NEW.requirement_id;
  select public_alias into v_alias from sellers where id = NEW.seller_id;
  perform push_notification(
    v_req.buyer_id, 'buyer', 'bid_placed',
    'New bid on ' || coalesce(v_req.requirement_number,'your requirement'),
    coalesce(v_alias,'A seller') || ' bid on ' || NEW.available_quantity_pcs || ' pcs',
    'requirement', NEW.requirement_id);
  return NEW;
end;
$$;
drop trigger if exists notify_new_bid on public.seller_responses;
create trigger notify_new_bid
  after insert on public.seller_responses
  for each row execute function public.trg_notify_new_bid();


-- ----------------------------------------------------------------------------
-- 6. REBUILD BUYER VIEW WITH NEGOTIATION STATUS
-- ----------------------------------------------------------------------------
drop view if exists public.buyer_visible_bids;
create view public.buyer_visible_bids as
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
    -- human-readable current status for the buyer's screen
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
  join public.requirements r on r.id = o.requirement_id;

grant select on public.buyer_visible_bids to authenticated;


-- ----------------------------------------------------------------------------
-- 7. SELLER'S OWN BID BOARD (what was completely missing)
-- ----------------------------------------------------------------------------
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
