-- ============================================================================
-- FIX: in-app notification bell shows nothing on buyer / seller / admin.
-- Cause: the bell relies on RLS to scope rows, but the RLS SELECT/UPDATE
--        policies (and mark_notifications_read) filter on `user_id`, while
--        push_notification writes rows to `recipient_id`. Policy matches
--        nothing -> every user sees "No notifications yet".
-- This migration is self-detecting: it targets whichever recipient column
-- actually exists on the table (recipient_id preferred; coalesces if both).
-- ============================================================================

alter table public.notifications enable row level security;

do $$
declare
  has_recipient boolean;
  has_user      boolean;
  v_expr        text;
begin
  select exists (select 1 from information_schema.columns
                 where table_schema='public' and table_name='notifications'
                   and column_name='recipient_id') into has_recipient;
  select exists (select 1 from information_schema.columns
                 where table_schema='public' and table_name='notifications'
                   and column_name='user_id') into has_user;

  if has_recipient and has_user then
    v_expr := 'coalesce(recipient_id, user_id) = auth.uid()';
  elsif has_recipient then
    v_expr := 'recipient_id = auth.uid()';
  else
    v_expr := 'user_id = auth.uid()';
  end if;

  -- SELECT policy (what the bell reads through)
  execute 'drop policy if exists notif_select_own on public.notifications';
  execute format(
    'create policy notif_select_own on public.notifications for select using (%s)', v_expr);

  -- UPDATE policy (mark-as-read)
  execute 'drop policy if exists notif_update_own on public.notifications';
  execute format(
    'create policy notif_update_own on public.notifications for update using (%s)', v_expr);

  -- read RPC re-pointed at the same expression
  execute format($f$
    create or replace function public.mark_notifications_read(p_ids uuid[] default null)
    returns void
    language plpgsql
    security definer
    set search_path = public
    as $body$
    begin
      if p_ids is null then
        update notifications set read_at = now() where %s and read_at is null;
      else
        update notifications set read_at = now() where %s and id = any(p_ids);
      end if;
    end;
    $body$;
  $f$, v_expr, v_expr);

  raise notice 'notifications RLS + read RPC now scoped by: %', v_expr;
end $$;

grant execute on function public.mark_notifications_read(uuid[]) to authenticated;
notify pgrst, 'reload schema';
