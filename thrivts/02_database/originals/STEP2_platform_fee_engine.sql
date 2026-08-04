-- ============================================================================
-- THRIVTS — STEP 2: PLATFORM FEE ENGINE (backend)
-- ----------------------------------------------------------------------------
-- Makes the $0.70/pc seller platform fee real end-to-end (Step 1 was display).
--
-- This migration:
--   1. Creates an admin-configurable fee-rate config (global, + optional
--      per-category and per-seller overrides for future tiering).
--   2. Adds a PER-ORDER FEE SNAPSHOT to deal_allocations so historical
--      settlements never recalculate when the rate later changes.
--   3. Wires settlement so total_payout_usd is stored NET of the fee, and
--      records gross + fee alongside it for reporting.
--   4. Folds in the commission_status enum fix (QA BUG-025 root cause) — the
--      influencer_commissions.status enum is made complete so the admin
--      settle/advance flow stops throwing the raw enum error.
--
-- SAFETY: idempotent (safe to run more than once). USD is the source of truth.
-- HOW TO RUN: Supabase Dashboard -> SQL Editor -> paste all -> Run.
-- Then reload PostgREST (last line does this automatically).
-- ============================================================================


-- ----------------------------------------------------------------------------
-- 1. FEE CONFIG
-- ----------------------------------------------------------------------------
-- Single-row global config + optional override tables. Default $0.70/pc.
create table if not exists public.platform_fee_config (
  id            boolean primary key default true check (id),   -- single row
  fee_per_pc_usd numeric(10,4) not null default 0.70,
  pkr_reference  text          not null default '≈ PKR 200',
  updated_at     timestamptz   not null default now(),
  updated_by     uuid
);
insert into public.platform_fee_config (id) values (true)
  on conflict (id) do nothing;

-- Optional future tiering (unused by v1 UI, ready for when you tier by value):
create table if not exists public.platform_fee_overrides (
  id             uuid primary key default gen_random_uuid(),
  scope          text not null check (scope in ('category','seller')),
  category_id    uuid,
  seller_id      uuid,
  fee_per_pc_usd numeric(10,4) not null,
  created_at     timestamptz not null default now()
);

-- Resolver: given a seller + category, return the effective fee/pc.
-- Precedence: seller override > category override > global default.
create or replace function public.effective_fee_per_pc(
  p_seller_id uuid default null,
  p_category_id uuid default null
) returns numeric
language sql stable security definer set search_path = public as $$
  select coalesce(
    (select fee_per_pc_usd from platform_fee_overrides
       where scope = 'seller' and seller_id = p_seller_id limit 1),
    (select fee_per_pc_usd from platform_fee_overrides
       where scope = 'category' and category_id = p_category_id limit 1),
    (select fee_per_pc_usd from platform_fee_config where id = true),
    0.70
  );
$$;
grant execute on function public.effective_fee_per_pc(uuid,uuid) to authenticated;


-- ----------------------------------------------------------------------------
-- 2. PER-ORDER FEE SNAPSHOT on deal_allocations
-- ----------------------------------------------------------------------------
-- gross = qty * price ; fee = qty * fee_per_pc_applied ; net = gross - fee.
-- fee_per_pc_applied is frozen at settlement so later rate changes don't
-- retroactively alter paid deals.
alter table public.deal_allocations
  add column if not exists fee_per_pc_applied_usd numeric(10,4),
  add column if not exists platform_fee_usd       numeric(14,2),
  add column if not exists gross_payout_usd        numeric(14,2);

comment on column public.deal_allocations.fee_per_pc_applied_usd is
  'Platform fee per piece frozen at settlement time (USD). Never recalculated.';
comment on column public.deal_allocations.platform_fee_usd is
  'Total platform fee for this allocation = allocated_quantity_pcs * fee_per_pc_applied_usd.';
comment on column public.deal_allocations.gross_payout_usd is
  'Gross (pre-fee) payout = allocated_quantity_pcs * price_per_pc_usd. total_payout_usd holds NET.';


-- ----------------------------------------------------------------------------
-- 3. FEE SNAPSHOT FUNCTION — freezes fee + net onto an allocation
-- ----------------------------------------------------------------------------
-- Call this when a deal reaches settlement (or when an allocation is finalized).
-- It sets gross, fee, and NET (into total_payout_usd) using the effective rate.
create or replace function public.apply_allocation_fee(p_allocation_id uuid)
returns void
language plpgsql security definer set search_path = public as $$
declare
  a record;
  v_cat uuid;
  v_fee numeric;
  v_gross numeric;
  v_feetot numeric;
begin
  select da.*, d.requirement_id
    into a
    from deal_allocations da
    join deals d on d.id = da.deal_id
   where da.id = p_allocation_id;
  if not found then return; end if;

  -- resolve category via the requirement, if present
  begin
    select category_id into v_cat from requirements where id = a.requirement_id;
  exception when undefined_column then v_cat := null;
  end;

  v_fee   := effective_fee_per_pc(a.seller_id, v_cat);
  v_gross := coalesce(a.allocated_quantity_pcs,0) * coalesce(a.price_per_pc_usd,0);
  v_feetot := coalesce(a.allocated_quantity_pcs,0) * v_fee;

  update deal_allocations
     set gross_payout_usd        = v_gross,
         fee_per_pc_applied_usd   = v_fee,
         platform_fee_usd         = v_feetot,
         total_payout_usd         = greatest(v_gross - v_feetot, 0)  -- NET
   where id = p_allocation_id;
end;
$$;
grant execute on function public.apply_allocation_fee(uuid) to authenticated;

-- Convenience: apply the fee to every allocation on a deal (called at settle).
create or replace function public.apply_deal_fees(p_deal_id uuid)
returns void
language plpgsql security definer set search_path = public as $$
declare r record;
begin
  for r in select id from deal_allocations where deal_id = p_deal_id loop
    perform apply_allocation_fee(r.id);
  end loop;
end;
$$;
grant execute on function public.apply_deal_fees(uuid) to authenticated;


-- ----------------------------------------------------------------------------
-- 4. HOOK INTO SETTLEMENT
-- ----------------------------------------------------------------------------
-- The admin portal advances deals via advance_deal_status(p_deal_id,...).
-- Rather than rewrite that RPC blindly, freeze fees whenever an allocation's
-- payout is marked paid, via a trigger. This is safe regardless of how the
-- status flow calls in.
create or replace function public.trg_freeze_fee_on_payout()
returns trigger
language plpgsql security definer set search_path = public as $$
begin
  -- when payout_paid_at transitions to non-null and fee not yet frozen
  if NEW.payout_paid_at is not null and NEW.fee_per_pc_applied_usd is null then
    perform apply_allocation_fee(NEW.id);
  end if;
  return NEW;
end;
$$;

drop trigger if exists freeze_fee_on_payout on public.deal_allocations;
create trigger freeze_fee_on_payout
  after update of payout_paid_at on public.deal_allocations
  for each row execute function public.trg_freeze_fee_on_payout();


-- ----------------------------------------------------------------------------
-- 5. commission_status ENUM FIX  (QA BUG-025 root cause)
-- ----------------------------------------------------------------------------
-- MOVED OUT: enum value additions can't always run inside this multi-statement
-- script. Run FIX_commission_enum_BUG025.sql SEPARATELY (it's safe & idempotent).
-- Everything else in THIS file runs together fine.


-- 6. REPORTING VIEW — fee revenue (for the admin portal, Step 3)
-- ----------------------------------------------------------------------------
create or replace view public.platform_fee_revenue as
  select
    date_trunc('month', coalesce(da.payout_paid_at, now())) as month,
    count(*)                              as allocations,
    sum(coalesce(da.allocated_quantity_pcs,0)) as pcs,
    sum(coalesce(da.platform_fee_usd,0))  as fee_revenue_usd,
    sum(coalesce(da.gross_payout_usd,0))  as gross_paid_usd,
    sum(coalesce(da.total_payout_usd,0))  as net_paid_to_sellers_usd
  from deal_allocations da
  where da.payout_paid_at is not null
  group by 1
  order by 1 desc;


-- ----------------------------------------------------------------------------
-- 7. RELOAD API
-- ----------------------------------------------------------------------------
notify pgrst, 'reload schema';

-- ============================================================================
-- POST-RUN NOTES
--  * Existing/legacy paid allocations keep their old total_payout_usd (gross)
--    and show fee columns as NULL — they are NOT retro-charged. Only new
--    payouts freeze a fee. If you WANT to backfill test data, run:
--        select apply_allocation_fee(id) from deal_allocations
--          where payout_paid_at is not null and fee_per_pc_applied_usd is null;
--    (Do NOT run that against real historical settlements you don't intend to
--    re-net.)
--  * total_spread_usd on deals and the agencies `commissions` table are the OLD
--    30%-spread model — left intact for legacy/agency use, untouched here.
--  * Admin rate control + fee-revenue UI = Step 3.
-- ============================================================================
