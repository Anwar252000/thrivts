-- ============================================================================
-- THRIVTS — PART 8A: NOTIFICATIONS TABLE (run this FIRST, on its own)
-- ----------------------------------------------------------------------------
-- Split out so a failure later in the script can't roll back the table.
-- Safe to re-run.
-- ============================================================================

create table if not exists public.notifications (
  id           uuid primary key default gen_random_uuid(),
  user_id      uuid not null,
  audience     text not null,
  kind         text not null,
  title        text not null,
  body         text,
  link_type    text,
  link_id      uuid,
  read_at      timestamptz,
  created_at   timestamptz not null default now()
);

-- If an older/partial version exists without these columns, add them.
alter table public.notifications
  add column if not exists user_id    uuid,
  add column if not exists audience   text,
  add column if not exists kind       text,
  add column if not exists title      text,
  add column if not exists body       text,
  add column if not exists link_type  text,
  add column if not exists link_id    uuid,
  add column if not exists read_at    timestamptz,
  add column if not exists created_at timestamptz default now();

create index if not exists notifications_user_idx
  on public.notifications (user_id, read_at, created_at desc);

alter table public.notifications enable row level security;

drop policy if exists notif_select_own on public.notifications;
create policy notif_select_own on public.notifications
  for select using (user_id = auth.uid());

drop policy if exists notif_update_own on public.notifications;
create policy notif_update_own on public.notifications
  for update using (user_id = auth.uid());

grant select, update on public.notifications to authenticated;

notify pgrst, 'reload schema';

-- VERIFY — should list the columns including user_id
select column_name, data_type
from information_schema.columns
where table_schema='public' and table_name='notifications'
order by ordinal_position;
