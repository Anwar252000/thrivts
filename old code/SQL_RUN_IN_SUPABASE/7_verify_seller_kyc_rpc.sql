-- ============================================================================
-- BUG-002 FIX — Mark KYC Verified fails: RPC missing (verify_seller_kyc)
-- (project thrivts-prod)
--
-- Symptom: admin clicking "Mark KYC Verified" (code/2_ADMIN_SITE/index.html
-- -> verifySellerKyc() -> rpc('verify_seller_kyc', { p_seller_id, p_notes }))
-- gets a PostgREST "function not found in schema cache" error — the RPC was
-- never deployed to this Supabase project, so admins cannot verify sellers.
--
-- Fix: create the RPC. Admin-only (re-checks role server-side per
-- SCHEMA_AND_DATA_MODEL.md's rule that every admin_* / admin-triggered RPC
-- must not rely solely on the UI gating it). Sets sellers.kyc_verified = true
-- and stores the optional internal note in sellers.kyc_notes (both columns
-- already read by the admin + seller portals). Also stamps
-- kyc_verified_at/kyc_verified_by if those columns exist on sellers, without
-- failing if they don't.
--
-- Safe to run multiple times (CREATE OR REPLACE).
-- ============================================================================

CREATE OR REPLACE FUNCTION public.verify_seller_kyc(p_seller_id uuid, p_notes text DEFAULT NULL)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  has_verified_at boolean;
  has_verified_by boolean;
  set_clause text := 'kyc_verified = true, kyc_notes = COALESCE($2, kyc_notes)';
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM public.profiles WHERE id = auth.uid() AND role = 'admin'
  ) THEN
    RAISE EXCEPTION 'Not authorized';
  END IF;

  SELECT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'sellers' AND column_name = 'kyc_verified_at'
  ) INTO has_verified_at;

  SELECT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'sellers' AND column_name = 'kyc_verified_by'
  ) INTO has_verified_by;

  IF has_verified_at THEN
    set_clause := set_clause || ', kyc_verified_at = now()';
  END IF;
  IF has_verified_by THEN
    set_clause := set_clause || ', kyc_verified_by = auth.uid()';
  END IF;

  EXECUTE format('UPDATE public.sellers SET %s WHERE id = $1', set_clause)
  USING p_seller_id, p_notes;

  IF NOT FOUND THEN
    RAISE EXCEPTION 'Seller not found: %', p_seller_id;
  END IF;
END;
$$;

GRANT EXECUTE ON FUNCTION public.verify_seller_kyc(uuid, text) TO authenticated;

-- Force PostgREST to pick up the new function immediately (otherwise it can
-- take a few minutes to appear, still showing "not in schema cache").
NOTIFY pgrst, 'reload schema';

-- Verify it now exists:
SELECT proname, pronargs FROM pg_proc
WHERE proname = 'verify_seller_kyc' AND pronamespace = 'public'::regnamespace;
