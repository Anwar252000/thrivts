-- ============================================================================
-- BUG-025 FIX — Settle deal throws enum error 'commission_status: accrued'
-- (project thrivts-prod)  BREAKS MONEY FLOW AT THE LAST STEP
--
-- Symptom: admin clicking "Mark Settled" on a deal (code/2_ADMIN_SITE/index.html
-- -> submitAdvance() -> rpc('advance_deal_status', { p_new_status: 'settled' }))
-- fails with:
--   invalid input value for enum commission_status: "accrued"
--
-- Root cause: SCHEMA_AND_DATA_MODEL.md documents commission timing as keying
-- off `delivered` (accrue) and `settled` (release) — i.e. advance_deal_status
-- (or a trigger it calls) needs to set a commissions-related row's status to
-- 'accrued' as part of settling. The public.commission_status enum type was
-- never given that label, so the write fails and the whole settle transaction
-- rolls back.
--
-- Fix: add the missing label to the enum. This is additive/non-destructive —
-- existing rows and values are untouched, this only makes 'accrued' a legal
-- value going forward.
--
-- Safe to run multiple times (checks the value doesn't already exist).
-- ============================================================================

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_type WHERE typname = 'commission_status' AND typnamespace = 'public'::regnamespace
  ) THEN
    RAISE NOTICE 'SKIP: enum type public.commission_status not found';
    RETURN;
  END IF;

  IF EXISTS (
    SELECT 1 FROM pg_enum e
    JOIN pg_type t ON t.oid = e.enumtypid
    WHERE t.typname = 'commission_status' AND t.typnamespace = 'public'::regnamespace
      AND e.enumlabel = 'accrued'
  ) THEN
    RAISE NOTICE 'SKIP: commission_status already has the "accrued" label';
    RETURN;
  END IF;

  ALTER TYPE public.commission_status ADD VALUE IF NOT EXISTS 'accrued';
  RAISE NOTICE 'OK: added "accrued" to public.commission_status';
END $$;

-- Verify — should now include accrued alongside pending/ready_to_release/released/cancelled:
SELECT e.enumlabel, e.enumsortorder
FROM pg_enum e
JOIN pg_type t ON t.oid = e.enumtypid
WHERE t.typname = 'commission_status' AND t.typnamespace = 'public'::regnamespace
ORDER BY e.enumsortorder;
