-- ============================================================================
-- BUG-032 FIX — Pending-review requirements publicly visible on Home
-- (project thrivts-prod)  DATA LEAK / TRUST ISSUE
--
-- Symptom: unapproved (status = 'pending_review') buyer requirements were
-- appearing in the public "live activity" feed on the Buyer and Seller home
-- pages, before an admin reviewed/approved them ("Push Live" in the admin
-- portal, which sets requirements.status='posted' + public_display=true).
--
-- Root cause: the frontend called RPC get_public_activity() first (which is
-- expected to filter to public_display=true / posted-only rows), but had a
-- fallback that queried public.public_activity_feed DIRECTLY with no filter
-- whenever the RPC returned zero rows. If anon/authenticated roles had
-- SELECT on it, that fallback leaked every row regardless of review status.
--
-- Frontend fix (already applied): removed the unfiltered direct fallback in
-- code/1_BUYER_SITE/index.html and code/5_SELLER_SITE/index.html — the feed
-- now relies solely on get_public_activity().
--
-- This script is the defense-in-depth half: it revokes direct read access so
-- public_activity_feed can ONLY be read through the RPC, even if some future
-- code adds a direct query again.
--
-- public_activity_feed turned out to be a VIEW, not a base table (confirmed
-- via a failed "ENABLE ROW LEVEL SECURITY" — that ALTER is table-only in
-- Postgres). Views can't hold RLS policies, but GRANT/REVOKE still applies to
-- them normally, so REVOKE SELECT is still the correct, effective lock here.
--
-- Safe to run multiple times.
-- ============================================================================

DO $$
DECLARE
  obj_kind text;
BEGIN
  SELECT c.relkind INTO obj_kind
  FROM pg_class c
  JOIN pg_namespace n ON n.oid = c.relnamespace
  WHERE n.nspname = 'public' AND c.relname = 'public_activity_feed';

  IF obj_kind IS NULL THEN
    RAISE NOTICE 'SKIP: public.public_activity_feed not found';
    RETURN;
  END IF;

  IF obj_kind = 'r' THEN
    -- Plain base table: full lockdown, RLS included.
    EXECUTE 'ALTER TABLE public.public_activity_feed ENABLE ROW LEVEL SECURITY';

    DECLARE pol RECORD;
    BEGIN
      FOR pol IN
        SELECT policyname FROM pg_policies
        WHERE schemaname = 'public' AND tablename = 'public_activity_feed' AND cmd = 'SELECT'
      LOOP
        EXECUTE format('DROP POLICY %I ON public.public_activity_feed', pol.policyname);
        RAISE NOTICE 'Dropped SELECT policy: %', pol.policyname;
      END LOOP;
    END;

    EXECUTE 'REVOKE SELECT ON public.public_activity_feed FROM anon, authenticated';
    RAISE NOTICE 'OK: public.public_activity_feed (table) locked down — reads must go through get_public_activity() RPC';

  ELSIF obj_kind = 'v' THEN
    -- View: RLS doesn't apply to views, but REVOKE SELECT still blocks
    -- anon/authenticated from querying it directly.
    EXECUTE 'REVOKE SELECT ON public.public_activity_feed FROM anon, authenticated';
    RAISE NOTICE 'OK: public.public_activity_feed (view) — direct SELECT revoked for anon/authenticated. Reads must go through get_public_activity() RPC. Check the view definition below to confirm it does NOT itself expose pending_review rows.';

  ELSE
    RAISE NOTICE 'SKIP: public.public_activity_feed is neither a table nor a view (relkind=%)', obj_kind;
  END IF;
END $$;

-- If it's a view, this shows exactly what it selects — confirm it filters to
-- posted/public_display=true and does NOT expose pending_review rows itself.
SELECT definition FROM pg_views
WHERE schemaname = 'public' AND viewname = 'public_activity_feed';

-- IMPORTANT — verify get_public_activity() itself is SECURITY DEFINER and
-- filters to approved rows only (e.g. requirements.status <> 'pending_review'
-- / public_display = true). This script cannot inspect or fix the function
-- body from here; check it in Database → Functions in the Supabase dashboard.
SELECT p.proname, p.prosecdef AS is_security_definer
FROM pg_proc p
JOIN pg_namespace n ON n.oid = p.pronamespace
WHERE n.nspname = 'public' AND p.proname = 'get_public_activity';

-- Confirm anon/authenticated no longer have direct SELECT on the object:
SELECT grantee, privilege_type
FROM information_schema.role_table_grants
WHERE table_schema = 'public' AND table_name = 'public_activity_feed'
  AND grantee IN ('anon', 'authenticated');
