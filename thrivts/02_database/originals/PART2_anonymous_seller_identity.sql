-- ============================================================================
-- THRIVTS — BIDDING PIVOT · PART 2: ANONYMOUS SELLER IDENTITY (the moat)
-- ----------------------------------------------------------------------------
-- Gives every seller a stable Reddit-style pseudonym (e.g. "Seller_7fx3")
-- that is the ONLY thing buyers ever see. Real identity (company, name,
-- contact) is mapped underneath and visible ONLY to admin.
--
-- WHY THIS IS FIRST: the live bidding board (later parts) shows buyers the
-- quotes as they arrive. If a buyer can see who the seller is, they can go
-- direct and cut Thrivts out. This migration makes seller anonymity a
-- DATABASE-LEVEL boundary, not a UI choice — the buyer-facing read path
-- physically cannot return a real seller identity.
--
-- SAFETY: idempotent. Does not touch existing columns. USD/data untouched.
-- HOW TO RUN: Supabase Dashboard -> SQL Editor -> paste all -> Run.
-- ============================================================================


-- ----------------------------------------------------------------------------
-- 1. PSEUDONYM COLUMN ON sellers
-- ----------------------------------------------------------------------------
alter table public.sellers
  add column if not exists public_alias text;

comment on column public.sellers.public_alias is
  'Buyer-facing pseudonym (e.g. Seller_7fx3). The ONLY seller identifier a buyer may ever see. Real identity stays admin-only.';

-- Unique so two sellers never collide on the board.
create unique index if not exists sellers_public_alias_key
  on public.sellers (public_alias)
  where public_alias is not null;


-- ----------------------------------------------------------------------------
-- 2. ALIAS GENERATOR
-- ----------------------------------------------------------------------------
-- Deterministic-length, unguessable-ish suffix. Not a secret — just a stable,
-- non-identifying handle. Format: Seller_<4 lowercase base36 chars>.
create or replace function public.generate_seller_alias()
returns text
language plpgsql
security definer set search_path = public as $$
declare
  chars text := 'abcdefghijklmnopqrstuvwxyz0123456789';
  candidate text;
  i int;
  tries int := 0;
begin
  loop
    candidate := 'Seller_';
    for i in 1..4 loop
      candidate := candidate || substr(chars, 1 + floor(random() * length(chars))::int, 1);
    end loop;
    -- ensure uniqueness
    exit when not exists (select 1 from sellers where public_alias = candidate);
    tries := tries + 1;
    if tries > 50 then
      -- extremely unlikely; widen to 6 chars
      candidate := candidate || substr(chars, 1 + floor(random()*length(chars))::int, 1)
                              || substr(chars, 1 + floor(random()*length(chars))::int, 1);
      exit;
    end if;
  end loop;
  return candidate;
end;
$$;


-- ----------------------------------------------------------------------------
-- 3. BACKFILL existing sellers + auto-assign on new sellers
-- ----------------------------------------------------------------------------
-- Backfill anyone missing an alias.
update public.sellers
   set public_alias = generate_seller_alias()
 where public_alias is null;

-- Trigger: every new seller gets an alias at insert.
create or replace function public.trg_assign_seller_alias()
returns trigger
language plpgsql
security definer set search_path = public as $$
begin
  if NEW.public_alias is null then
    NEW.public_alias := generate_seller_alias();
  end if;
  return NEW;
end;
$$;

drop trigger if exists assign_seller_alias on public.sellers;
create trigger assign_seller_alias
  before insert on public.sellers
  for each row execute function public.trg_assign_seller_alias();


-- ----------------------------------------------------------------------------
-- 4. BUYER-FACING SAFE VIEW OF A QUOTE/BID
-- ----------------------------------------------------------------------------
-- This is the boundary. The buyer-facing read path selects from this view,
-- which exposes ONLY the pseudonym + non-identifying seller signals
-- (tier, verified badge, alias) and NEVER company_name, contact, etc.
--
-- SOURCE TABLE: seller_responses — this is where an open seller bid is stored
-- (columns: requirement_id, seller_id, available_quantity_pcs,
--  proposed_price_usd, seller_notes, status). Confirmed against the live
-- seller portal's quote-submit insert.
-- NOTE: the buyer_visible_bids view is defined in PART4_fee_inclusive_bids.sql,
-- because the buyer-facing price must be FEE-INCLUSIVE. Defining it in two
-- files caused a run-order trap (whichever ran last won, and CREATE OR REPLACE
-- cannot change view columns). PART4 is now the single owner of that view.
--
-- Run order: PART2 (this file) -> PART4 -> PART6.
--
-- The moat rule still applies and is enforced there: the buyer-facing view
-- exposes ONLY public_alias, tier, verified and the fee-inclusive price —
-- never company_name, contact, seller_id, the seller's raw price, or the fee split.


-- ----------------------------------------------------------------------------
-- 5. ADMIN-ONLY REVEAL
-- ----------------------------------------------------------------------------
-- Admin needs to map a pseudonym back to the real seller for verification,
-- moderation, and QC. This RPC is admin-gated.
create or replace function public.admin_reveal_seller(p_alias text)
returns table (
  seller_id uuid,
  public_alias text,
  company_name text,
  tier text,
  kyc_verified boolean
)
language plpgsql
security definer set search_path = public as $$
begin
  if not exists (select 1 from profiles where id = auth.uid() and role = 'admin') then
    raise exception 'not authorized';
  end if;

  return query
    select s.id, s.public_alias, s.company_name, s.tier, s.kyc_verified
      from sellers s
     where s.public_alias = p_alias;
end;
$$;
revoke execute on function public.admin_reveal_seller(text) from public, anon;
grant execute on function public.admin_reveal_seller(text) to authenticated;


-- ----------------------------------------------------------------------------
-- 6. RELOAD API
-- ----------------------------------------------------------------------------
notify pgrst, 'reload schema';

-- ============================================================================
-- AFTER RUNNING — how the parts use this:
--   * Buyer portal (Part 5 board): reads ONLY from buyer_visible_bids. Shows
--     seller_alias, seller_tier, seller_verified. Never joins to sellers.
--   * Seller portal: unchanged — a seller sees their own real account.
--   * Admin portal (Part 7): can call admin_reveal_seller('Seller_7fx3') to
--     see who it really is, for verification / QC / moderation.
--   * The fee (Part 4) will make proposed_price_usd fee-INCLUSIVE for the
--     buyer-facing number; the raw column stays the seller's entered price.
--
-- HARD RULE for every future build: nothing on the buyer side ever selects
-- company_name / contact / seller_id from sellers. If you need a new seller
-- signal on the board, add it to buyer_visible_bids deliberately — and never
-- add an identifying one.
-- ============================================================================
