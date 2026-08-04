-- ============================================================================
-- FIX: every notification insert fails on a hidden NOT NULL column.
--
-- The prod notifications table gained extra NOT NULL columns after the original
-- build (channel, and others per the failing-row dump). push_notification only
-- writes the core fields, so every insert violated one of these constraints and
-- died inside push_notification's silent exception handler. Result: zero rows,
-- empty bells, and no notification emails.
--
-- This relaxes NOT NULL on ANY notifications column that the writer does not
-- populate and that has no default of its own — so a core insert always
-- succeeds, regardless of how many extra columns exist. Columns that already
-- have a default (id, created_at, etc.) are left untouched.
-- ============================================================================

do $$
declare
  r record;
  -- columns push_notification DOES populate — never touch these
  v_provided text[] := array[
    'id','recipient_id','user_id','audience','kind','title','body',
    'link_type','link_id','created_at','read_at'
  ];
begin
  for r in
    select column_name
      from information_schema.columns
     where table_schema = 'public'
       and table_name   = 'notifications'
       and is_nullable  = 'NO'
       and column_default is null
       and column_name <> all (v_provided)
  loop
    execute format('alter table public.notifications alter column %I drop not null', r.column_name);
    raise notice 'relaxed NOT NULL on notifications.%', r.column_name;
  end loop;
end $$;

-- If a `channel` column exists, give it a sensible default too, so future rows
-- are tidy rather than null.
do $$
begin
  if exists (select 1 from information_schema.columns
             where table_schema='public' and table_name='notifications'
               and column_name='channel') then
    execute 'alter table public.notifications alter column channel set default ''in_app''';
  end if;
end $$;

notify pgrst, 'reload schema';
