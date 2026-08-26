-- ============================================================================
-- THRIVTS — ADMIN ACTIVITY NOTIFICATIONS
-- Admins get a bell notification (and unread count) for the live activities they
-- need to stay on top of: new buyer signups, new seller signups, and confirmed
-- deals. Mirrors how bids/counters already notify buyers and sellers.
--
-- Safe/idempotent: re-runnable. Uses the existing push_notification writer.
-- ============================================================================

-- Helper: fan a notification out to every admin.
create or replace function public.notify_admins(
  p_kind text, p_title text, p_body text default null,
  p_link_type text default null, p_link_id uuid default null
) returns void
language plpgsql
security definer
set search_path = public
as $$
declare a record;
begin
  for a in select id from public.profiles where role = 'admin' loop
    perform public.push_notification(a.id, 'admin', p_kind, p_title, p_body, p_link_type, p_link_id);
  end loop;
end;
$$;

-- ----------------------------------------------------------------------------
-- 1) New buyer / seller signup  (fires on the profiles row created at signup)
-- ----------------------------------------------------------------------------
-- 2026-08-22 fix: NEW.role/NEW.approval_status are the native user_role/approval_status Postgres
-- enums (not text) since the .NET migration's NpgsqlEnumMapping work — Postgres has no implicit
-- enum->text cast for function-argument resolution, so notify_admins(text,...) failed to resolve
-- with "function ... does not exist" the first time a .NET-driven INSERT (buyer self-registration)
-- fired this trigger. Explicit ::text casts fix it without changing notify_admins' own signature.
create or replace function public.trg_notify_admin_new_signup()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
begin
  if NEW.role in ('buyer','seller') then
    perform public.notify_admins(
      'signup',
      'New ' || NEW.role::text || ' registered',
      'A new ' || NEW.role::text || ' account was created'
        || case when NEW.approval_status::text = 'pending'
                then ' and is awaiting approval.' else '.' end,
      NEW.role::text, NEW.id);
  end if;
  return NEW;
end;
$$;

drop trigger if exists notify_admin_new_signup on public.profiles;
create trigger notify_admin_new_signup
  after insert on public.profiles
  for each row execute function public.trg_notify_admin_new_signup();

-- ----------------------------------------------------------------------------
-- 2) Confirmed deal  (fires when buyer_accept_bid inserts into deals)
-- ----------------------------------------------------------------------------
create or replace function public.trg_notify_admin_new_deal()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
begin
  perform public.notify_admins(
    'deal_confirmed',
    'Deal confirmed — ' || coalesce(NEW.deal_number, ''),
    'A deal was confirmed for '
      || coalesce(NEW.total_quantity_pcs::text, '?') || ' pcs. Review it in Deals.',
    'deal', NEW.id);
  return NEW;
end;
$$;

drop trigger if exists notify_admin_new_deal on public.deals;
create trigger notify_admin_new_deal
  after insert on public.deals
  for each row execute function public.trg_notify_admin_new_deal();

notify pgrst, 'reload schema';
