-- ============================================================================
-- THRIVTS — BIDDING PIVOT · PART 4: FEE-INCLUSIVE BIDS
-- ----------------------------------------------------------------------------
-- Makes every buyer-facing bid include the $0.70/pc platform fee, so the
-- buyer compares apples-to-apples on the live board. The seller still enters
-- (and sees) their own price; the buyer sees seller_price + fee.
--
-- Depends on:
--   * PART2_anonymous_seller_identity.sql  (buyer_visible_bids view + aliases)
--   * STEP2_platform_fee_engine.sql        (platform_fee_config, effective_fee_per_pc)
--
-- SAFETY: idempotent. Does not change what sellers see or store. Adds a
-- fee-inclusive number to the buyer-facing read path only.
-- HOW TO RUN: SQL Editor -> paste all -> Run. (Run PART2 + STEP2 first.)
-- ============================================================================


-- ----------------------------------------------------------------------------
-- 1. FEE SNAPSHOT ON THE BID (seller_responses)
-- ----------------------------------------------------------------------------
-- Freeze the fee rate onto each bid the moment it's placed, so a later rate
-- change never retroactively alters a bid already on the board.
alter table public.seller_responses
  add column if not exists fee_per_pc_applied_usd numeric(10,4);

comment on column public.seller_responses.fee_per_pc_applied_usd is
  'Platform fee/pc frozen when this bid was placed. Buyer-facing price = proposed_price_usd + this. Never recalculated.';

-- Backfill existing bids with the current GLOBAL flat fee.
-- (Category/seller fee tiering is a future feature; launch uses one flat rate,
--  so we read platform_fee_config directly and avoid any category_id type
--  dependency. When tiering ships, swap this for effective_fee_per_pc.)
update public.seller_responses sr
   set fee_per_pc_applied_usd = coalesce(
         (select fee_per_pc_usd from platform_fee_config where id = true),
         0.70)
 where fee_per_pc_applied_usd is null;

-- Trigger: freeze the fee on every new bid at insert.
create or replace function public.trg_freeze_bid_fee()
returns trigger
language plpgsql security definer set search_path = public as $$
begin
  if NEW.fee_per_pc_applied_usd is null then
    NEW.fee_per_pc_applied_usd := coalesce(
      (select fee_per_pc_usd from platform_fee_config where id = true),
      0.70);
  end if;
  return NEW;
end;
$$;

drop trigger if exists freeze_bid_fee on public.seller_responses;
create trigger freeze_bid_fee
  before insert on public.seller_responses
  for each row execute function public.trg_freeze_bid_fee();


-- ----------------------------------------------------------------------------
-- 2. REBUILD buyer_visible_bids WITH THE FEE-INCLUSIVE NUMBER
-- ----------------------------------------------------------------------------
-- buyer_price_per_pc_usd  = what the buyer sees & compares (fee included)
-- buyer_total_usd         = buyer_price_per_pc * available_quantity_pcs
-- The seller's own proposed_price_usd is NOT exposed to the buyer here —
-- only the fee-inclusive number, the alias, tier, and verified badge.
-- Part 2 created this view with different column names; CREATE OR REPLACE can't
-- rename view columns, so drop then recreate.
drop view if exists public.buyer_visible_bids;
create view public.buyer_visible_bids as
  select
    o.id                                                          as bid_id,
    o.requirement_id,
    o.available_quantity_pcs,
    round((o.proposed_price_usd + coalesce(o.fee_per_pc_applied_usd, 0.70))::numeric, 2)
                                                                  as buyer_price_per_pc_usd,
    round(((o.proposed_price_usd + coalesce(o.fee_per_pc_applied_usd, 0.70)) * o.available_quantity_pcs)::numeric, 2)
                                                                  as buyer_total_usd,
    o.status,
    o.responded_at                                                as bid_time,
    s.public_alias                                                as seller_alias,
    s.tier                                                        as seller_tier,
    s.kyc_verified                                                as seller_verified
    -- DELIBERATELY OMITTED: proposed_price_usd (the seller's raw number),
    -- fee_per_pc_applied_usd (the split), company_name, phone, whatsapp,
    -- email, reference_contact, seller_id. The buyer sees ONE all-in price.
  from public.seller_responses o
  join public.sellers s on s.id = o.seller_id;

comment on view public.buyer_visible_bids is
  'ONLY quote source for the buyer portal. Exposes fee-INCLUSIVE price, alias, tier, verified. Never the seller''s raw price, the fee split, or real identity.';

grant select on public.buyer_visible_bids to authenticated;


-- ----------------------------------------------------------------------------
-- 3. SELLER-SIDE HELPER (unchanged intent, exposed for the seller portal)
-- ----------------------------------------------------------------------------
-- Sellers see their own economics: their price, the fee, and buyer-facing
-- price. This view is scoped by RLS to the seller's own rows in the app.
drop view if exists public.seller_own_bids;
create view public.seller_own_bids as
  select
    o.id                       as bid_id,
    o.requirement_id,
    o.seller_id,
    o.available_quantity_pcs,
    o.proposed_price_usd        as your_price_per_pc_usd,
    coalesce(o.fee_per_pc_applied_usd, 0.70) as platform_fee_per_pc_usd,
    round((o.proposed_price_usd + coalesce(o.fee_per_pc_applied_usd,0.70))::numeric,2) as buyer_sees_per_pc_usd,
    o.status,
    o.responded_at
  from public.seller_responses o;

grant select on public.seller_own_bids to authenticated;


-- ----------------------------------------------------------------------------
-- 4. RELOAD API
-- ----------------------------------------------------------------------------
notify pgrst, 'reload schema';

-- ============================================================================
-- HOW THE PORTALS USE THIS:
--   * Buyer board (Part 5): reads buyer_visible_bids -> shows buyer_price_per_pc_usd
--     and buyer_total_usd. Every bid already includes the fee. One clean number.
--   * Seller portal: the live fee breakdown we built in Step 1 already shows
--     the seller their net vs buyer-sees; seller_own_bids backs it server-side.
--   * The seller's raw price and the fee split NEVER cross to the buyer side.
--
-- WORKED EXAMPLE: seller bids $4.30/pc on 5,000 pcs, fee $0.70.
--   Seller sees: your price $4.30, fee $0.70, buyer sees $5.00/pc.
--   Buyer board shows: $5.00/pc · $25,000 total · Seller_7fx3 · Gold · Verified.
--   Buyer never sees $4.30 or the $0.70 split.
-- ============================================================================
