-- ============================================================================
-- BUG-035 FIX — "Raise an issue" fails with RLS error on 'disputes' table
-- (project thrivts-prod)  SECURITY-CRITICAL
--
-- Symptom: buyer site throws "new row violates row-level security policy
-- for table 'disputes'" when a buyer submits the Raise-an-issue form
-- (code/1_BUYER_SITE/index.html -> submitDispute(), inserts
-- {deal_id, raised_by, category, description, requested_resolution}).
--
-- Root cause: public.disputes has RLS enabled but has no working INSERT
-- policy that lets an authenticated buyer insert a dispute for THEIR OWN
-- deal. Without a matching policy, RLS denies every insert by default.
--
-- Fix: allow INSERT only when both are true —
--   1) raised_by = auth.uid()                (buyer can't raise "as" someone else)
--   2) the deal_id belongs to a deal owned by that buyer (deals.buyer_id = auth.uid())
--      (buyer can't raise a dispute against someone else's deal)
--
-- Safe to run multiple times: checks the table/columns exist first, only
-- replaces existing INSERT policies on disputes (SELECT/UPDATE policies
-- used by the admin portal are left untouched).
-- ============================================================================

DO $$
DECLARE
  pol RECORD;
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'disputes' AND table_type = 'BASE TABLE'
  ) THEN
    RAISE NOTICE 'SKIP: public.disputes not found';
    RETURN;
  END IF;

  IF (
    SELECT count(*) FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'disputes'
      AND column_name IN ('deal_id', 'raised_by')
  ) < 2 THEN
    RAISE NOTICE 'SKIP: public.disputes is missing deal_id and/or raised_by column';
    RETURN;
  END IF;

  -- Make sure RLS is actually on (no-op if already enabled).
  EXECUTE 'ALTER TABLE public.disputes ENABLE ROW LEVEL SECURITY';

  -- Drop any existing INSERT-only policies so this script is safely
  -- re-runnable and doesn't leave stale/conflicting ones behind.
  FOR pol IN
    SELECT policyname FROM pg_policies
    WHERE schemaname = 'public' AND tablename = 'disputes' AND cmd = 'INSERT'
  LOOP
    EXECUTE format('DROP POLICY %I ON public.disputes', pol.policyname);
    RAISE NOTICE 'Dropped existing INSERT policy: %', pol.policyname;
  END LOOP;

  EXECUTE $p$
    CREATE POLICY buyers_insert_own_disputes
    ON public.disputes
    FOR INSERT
    TO authenticated
    WITH CHECK (
      raised_by = auth.uid()
      AND EXISTS (
        SELECT 1 FROM public.deals d
        WHERE d.id = deal_id
          AND d.buyer_id = auth.uid()
      )
    )
  $p$;

  RAISE NOTICE 'OK: buyers_insert_own_disputes policy created on public.disputes';
END $$;

-- What actually exists now on public.disputes (verify after running):
SELECT policyname, cmd, roles, qual, with_check
FROM pg_policies
WHERE schemaname = 'public' AND tablename = 'disputes'
ORDER BY cmd, policyname;
