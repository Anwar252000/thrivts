-- ============================================================================
-- THRIVTS — SECURITY HARDENING
-- ----------------------------------------------------------------------------
-- Addresses the Supabase Security Advisor findings that actually matter:
--
--   1. 61 SECURITY DEFINER functions are executable by ANON (logged-out
--      internet users). Most have an internal admin check, so they are not
--      known to be exploitable — but admin operations should never be
--      reachable by anon at all. We revoke anon EXECUTE.
--
--   2. admin_bid_board and platform_fee_revenue expose real seller identity
--      and your fee revenue. They are locked to admins only.
--
-- DELIBERATELY LEFT ANON-CALLABLE (revoking these WOULD break the site):
--   * get_public_activity  — powers the public/logged-out activity feed
--   * handle_new_user      — runs during signup, before a session exists
--   * submit_partner_application — public partner form
--   * validate_seller_invite     — invite link checked before login
--
-- NOT TOUCHED (deliberately):
--   * authenticated EXECUTE — left intact so every portal keeps working
--   * trigger functions (trg_*) — triggers don't use EXECUTE grants
--   * the always-true RLS on ops_* / stock_* / wash_* tables — those belong
--     to the VH Operations app, not Thrivts. Changing them could break that
--     app; treat as a separate decision.
--
-- SAFETY: only REVOKEs from anon. Nothing your logged-in users do changes.
-- Idempotent. Run in SQL Editor.
-- ============================================================================

do $$
declare
  r record;
  keep_anon text[] := array[
    'get_public_activity',
    'handle_new_user',
    'submit_partner_application',
    'validate_seller_invite'
  ];
begin
  for r in
    select p.oid,
           p.proname,
           pg_get_function_identity_arguments(p.oid) as args
      from pg_proc p
      join pg_namespace n on n.oid = p.pronamespace
     where n.nspname = 'public'
       and p.prosecdef                          -- SECURITY DEFINER only
       and p.proname <> all(keep_anon)          -- keep the public ones
       and p.proname not like 'trg\_%'          -- triggers need no grant
  loop
    begin
      execute format('revoke execute on function public.%I(%s) from anon;',
                     r.proname, r.args);
    exception when others then
      -- never let one odd signature abort the whole pass
      raise notice 'skipped %(%): %', r.proname, r.args, sqlerrm;
    end;
  end loop;
end $$;


-- ----------------------------------------------------------------------------
-- 2. LOCK THE ADMIN-ONLY VIEWS
-- ----------------------------------------------------------------------------
-- These expose real seller identity / your fee revenue. Only admins may read.
revoke select on public.admin_bid_board       from anon, authenticated;
revoke select on public.platform_fee_revenue  from anon, authenticated;

-- Admin access goes through SECURITY DEFINER functions that check the role,
-- so the admin portal keeps working without granting the tables broadly.
create or replace function public.get_admin_bid_board(p_requirement_id uuid)
returns setof public.admin_bid_board
language plpgsql stable security definer set search_path = public as $$
begin
  if not exists (select 1 from profiles where id = auth.uid() and role = 'admin') then
    raise exception 'not authorized';
  end if;
  return query select * from admin_bid_board where requirement_id = p_requirement_id;
end;
$$;
revoke execute on function public.get_admin_bid_board(uuid) from anon;
grant  execute on function public.get_admin_bid_board(uuid) to authenticated;

create or replace function public.get_platform_fee_revenue()
returns setof public.platform_fee_revenue
language plpgsql stable security definer set search_path = public as $$
begin
  if not exists (select 1 from profiles where id = auth.uid() and role = 'admin') then
    raise exception 'not authorized';
  end if;
  return query select * from platform_fee_revenue;
end;
$$;
revoke execute on function public.get_platform_fee_revenue() from anon;
grant  execute on function public.get_platform_fee_revenue() to authenticated;

notify pgrst, 'reload schema';

-- ----------------------------------------------------------------------------
-- VERIFY — how many SECURITY DEFINER functions anon can still execute.
-- Expect only the four intentional public ones.
-- ----------------------------------------------------------------------------
select p.proname
from pg_proc p
join pg_namespace n on n.oid = p.pronamespace
where n.nspname = 'public'
  and p.prosecdef
  and has_function_privilege('anon', p.oid, 'execute')
order by 1;
