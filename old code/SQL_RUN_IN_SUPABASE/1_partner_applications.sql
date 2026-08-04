-- ============================================================================
-- PARTNER (INFLUENCER) APPLICATIONS  (project thrivts-prod)
-- Captures applications from the partner landing page and lets the admin
-- review/approve them. Anon can ONLY submit (via RPC) — never read the table.
-- Admin RPCs are role-guarded (profiles.role = 'admin'). Safe + idempotent.
-- ============================================================================

create table if not exists public.partner_applications (
  id            uuid primary key default gen_random_uuid(),
  full_name     text not null,
  email         text not null,
  instagram     text,
  tiktok        text,
  experience    text,
  platform      text,
  profile_link  text,
  about         text,
  status        text not null default 'pending',   -- pending | approved | rejected
  influencer_id uuid,                                -- set when approved & created
  created_at    timestamptz not null default now(),
  reviewed_at   timestamptz
);

alter table public.partner_applications enable row level security;
-- No table policies: all access goes through the SECURITY DEFINER RPCs below.

-- Public submit (anon-callable). Inserts one application, returns ok.
create or replace function public.submit_partner_application(
  p_full_name text, p_email text,
  p_instagram text default null, p_tiktok text default null,
  p_experience text default null, p_platform text default null,
  p_profile_link text default null, p_about text default null
) returns jsonb
language plpgsql security definer set search_path = public as $$
begin
  if p_full_name is null or btrim(p_full_name) = '' or p_email is null or btrim(p_email) = '' then
    return jsonb_build_object('ok', false, 'reason', 'missing_required');
  end if;
  insert into public.partner_applications
    (full_name, email, instagram, tiktok, experience, platform, profile_link, about)
  values
    (btrim(p_full_name), btrim(p_email), p_instagram, p_tiktok, p_experience, p_platform, p_profile_link, p_about);
  return jsonb_build_object('ok', true);
end; $$;
grant execute on function public.submit_partner_application(text,text,text,text,text,text,text,text) to anon, authenticated;

-- Admin: list applications (role-guarded).
create or replace function public.admin_list_partner_applications()
returns setof public.partner_applications
language plpgsql stable security definer set search_path = public as $$
begin
  if not exists (select 1 from public.profiles where id = auth.uid() and role = 'admin') then
    raise exception 'not authorized';
  end if;
  return query select * from public.partner_applications order by created_at desc;
end; $$;
grant execute on function public.admin_list_partner_applications() to authenticated;

-- Admin: set status (approved/rejected), optionally link the created influencer.
create or replace function public.admin_set_partner_application_status(
  p_id uuid, p_status text, p_influencer_id uuid default null
) returns jsonb
language plpgsql security definer set search_path = public as $$
begin
  if not exists (select 1 from public.profiles where id = auth.uid() and role = 'admin') then
    raise exception 'not authorized';
  end if;
  if p_status not in ('pending','approved','rejected') then
    return jsonb_build_object('ok', false, 'reason', 'bad_status');
  end if;
  update public.partner_applications
     set status = p_status,
         influencer_id = coalesce(p_influencer_id, influencer_id),
         reviewed_at = now()
   where id = p_id;
  return jsonb_build_object('ok', true);
end; $$;
grant execute on function public.admin_set_partner_application_status(uuid,text,uuid) to authenticated;
