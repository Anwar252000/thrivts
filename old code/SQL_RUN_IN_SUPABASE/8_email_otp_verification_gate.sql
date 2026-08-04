-- ============================================================================
-- FEATURE — Email OTP verification gate for Buyer + Seller signup
-- (project thrivts-prod)
--
-- Goal: a new signup should NOT create a profiles/buyers/sellers row (and
-- therefore should NOT appear in the Admin "Approvals" queue) until the user
-- has verified their email with the 6-digit code Supabase emails them.
--
-- Current behavior: handle_new_user() is attached to auth.users as an
-- AFTER INSERT trigger, so it fires the instant signUp() is called — before
-- any email verification. That's why unverified/fake-email signups reach
-- the admin queue today.
--
-- Fix: move the SAME function to fire on UPDATE of auth.users instead, and
-- only when email_confirmed_at transitions from NULL to a real timestamp
-- (i.e. exactly the moment Supabase confirms the emailed OTP code via
-- supabase.auth.verifyOtp()). The function body itself needs NO changes —
-- it only reads NEW.id / NEW.email / NEW.raw_user_meta_data, all of which
-- are identical whether the row arrived via INSERT or UPDATE.
--
-- Requires (done separately, in the Supabase Dashboard, not by this script):
--   1) Authentication -> Providers -> Email -> "Confirm email" turned ON.
--   2) Authentication -> Email Templates -> "Confirm signup" -> template
--      body changed to send {{ .Token }} (the 6-digit code) instead of the
--      default {{ .ConfirmationURL }} magic link.
--
-- Safe to run multiple times.
-- ============================================================================

DO $$
DECLARE
  trg RECORD;
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid = p.pronamespace
    WHERE n.nspname = 'public' AND p.proname = 'handle_new_user'
  ) THEN
    RAISE NOTICE 'SKIP: public.handle_new_user() not found';
    RETURN;
  END IF;

  -- Drop every existing trigger on auth.users that calls handle_new_user(),
  -- regardless of what it's named or timed on, so this script is safely
  -- re-runnable and never ends up with two triggers double-firing the insert.
  FOR trg IN
    SELECT t.tgname
    FROM pg_trigger t
    JOIN pg_proc p ON p.oid = t.tgfoid
    JOIN pg_namespace pn ON pn.oid = p.pronamespace
    WHERE t.tgrelid = 'auth.users'::regclass
      AND NOT t.tgisinternal
      AND pn.nspname = 'public'
      AND p.proname = 'handle_new_user'
  LOOP
    EXECUTE format('DROP TRIGGER %I ON auth.users', trg.tgname);
    RAISE NOTICE 'Dropped trigger: %', trg.tgname;
  END LOOP;

  -- Re-create it to fire only once, only on the confirm-email transition.
  EXECUTE $trig$
    CREATE TRIGGER on_auth_user_email_confirmed
    AFTER UPDATE OF email_confirmed_at ON auth.users
    FOR EACH ROW
    WHEN (OLD.email_confirmed_at IS NULL AND NEW.email_confirmed_at IS NOT NULL)
    EXECUTE FUNCTION public.handle_new_user()
  $trig$;

  RAISE NOTICE 'OK: on_auth_user_email_confirmed created — profiles/buyers/sellers rows now created only after OTP verification';
END $$;

-- Verify — should show exactly one trigger, timed AFTER UPDATE OF email_confirmed_at:
SELECT tgname, pg_get_triggerdef(oid) AS definition
FROM pg_trigger
WHERE tgrelid = 'auth.users'::regclass AND NOT tgisinternal;
