-- ============================================================================
-- THRIVTS — ENUM FIX (QA BUG-025)  —  RUN THIS SEPARATELY / SECOND
-- ----------------------------------------------------------------------------
-- Postgres won't ADD enum values inside some multi-statement transactions.
-- If STEP2_platform_fee_engine.sql errored on the enum block, run THIS file
-- on its own (select all -> Run). It only touches REAL enum types and skips
-- plain-text status columns without error, so it's safe to run anytime.
-- ============================================================================

-- The admin settle/advance flow can surface "invalid input value for enum ..."
-- when an enum-typed status column is missing a value the app writes.
-- Some status columns are plain TEXT (no enum) — those need no fix and must be
-- skipped without error. This block finds every REAL enum type backing a
-- status/commission_status column on the money tables and adds the four states
-- the app uses, idempotently and non-destructively.
do $$
declare
  r record;
  needed text[] := array['pending','accrued','released','reversed'];
  v text;
begin
  for r in
    select distinct t.typname as enum_type
      from pg_type t
      join pg_attribute a on a.atttypid = t.oid
      join pg_class c     on c.oid = a.attrelid
      join pg_namespace n on n.oid = c.relnamespace
     where t.typtype = 'e'                       -- ONLY real enum types
       and n.nspname = 'public'
       and c.relname in ('influencer_commissions','commissions','deals','deal_allocations')
       and a.attname in ('status','commission_status')
  loop
    foreach v in array needed loop
      if not exists (
        select 1 from pg_enum e
        join pg_type t on t.oid = e.enumtypid
        where t.typname = r.enum_type and e.enumlabel = v
      ) then
        execute format('alter type public.%I add value if not exists %L', r.enum_type, v);
      end if;
    end loop;
  end loop;
end $$;


-- ----------------------------------------------------------------------------

notify pgrst, 'reload schema';
