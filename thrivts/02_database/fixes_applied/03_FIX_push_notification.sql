-- ============================================================================
-- FIX: notifications are never written -> every bell is empty AND no notif
--      emails ever send.
--
-- Root cause: the notifications table requires audience, kind, title and a
-- legacy user_id (all NOT NULL), but push_notification only inserted
-- (recipient_id, title, body). Every insert violated a NOT NULL constraint and
-- died inside push_notification's own silent exception handler, so the table
-- stayed empty. (This is also why the notify-all webhook never fired an email:
-- no row was ever inserted to fire on.)
--
-- This rewrites the writer to populate every required column. It already
-- RECEIVES p_audience / p_kind / p_link_* — it just wasn't inserting them.
-- Both recipient columns are filled when present, which satisfies the legacy
-- user_id NOT NULL while keeping recipient_id (the canonical one) populated.
-- ============================================================================

-- 1. Guarantee every column the writer touches exists (no-op if already there).
alter table public.notifications
  add column if not exists recipient_id uuid,
  add column if not exists user_id      uuid,
  add column if not exists audience     text,
  add column if not exists kind         text,
  add column if not exists title        text,
  add column if not exists body         text,
  add column if not exists link_type    text,
  add column if not exists link_id      uuid,
  add column if not exists read_at      timestamptz,
  add column if not exists created_at   timestamptz default now();

-- 2. Rewrite the writer to fill everything the table requires.
create or replace function public.push_notification(
  p_user_id  uuid, p_audience text, p_kind text,
  p_title    text, p_body text default null,
  p_link_type text default null, p_link_id uuid default null
) returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  has_recipient boolean;
  has_user      boolean;
begin
  if p_user_id is null then return; end if;

  select exists (select 1 from information_schema.columns
                 where table_schema='public' and table_name='notifications'
                   and column_name='recipient_id') into has_recipient;
  select exists (select 1 from information_schema.columns
                 where table_schema='public' and table_name='notifications'
                   and column_name='user_id') into has_user;

  begin
    if has_recipient and has_user then
      insert into public.notifications
        (recipient_id, user_id, audience, kind, title, body, link_type, link_id)
      values
        (p_user_id, p_user_id, coalesce(p_audience,'general'),
         coalesce(p_kind,'general'), p_title, p_body, p_link_type, p_link_id);
    elsif has_recipient then
      insert into public.notifications
        (recipient_id, audience, kind, title, body, link_type, link_id)
      values
        (p_user_id, coalesce(p_audience,'general'),
         coalesce(p_kind,'general'), p_title, p_body, p_link_type, p_link_id);
    else
      insert into public.notifications
        (user_id, audience, kind, title, body, link_type, link_id)
      values
        (p_user_id, coalesce(p_audience,'general'),
         coalesce(p_kind,'general'), p_title, p_body, p_link_type, p_link_id);
    end if;
  exception when others then
    -- A notification failure must NEVER break bidding / counters / accepts.
    raise notice 'notification skipped: %', sqlerrm;
  end;
end;
$$;

notify pgrst, 'reload schema';
