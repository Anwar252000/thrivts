-- ============================================================================
-- AGENCY ATTRIBUTION v2  (project thrivts-prod)  — robust, idempotent
-- Buyers are attributed to the referring agency AT THE DATABASE LEVEL, the
-- moment the buyer row is created, by reading 'agency_ref' from the signup
-- metadata (auth.users.raw_user_meta_data). No front-end dependency, so it
-- can't be missed if a portal build is stale or an RPC call is skipped.
-- Includes a backfill that fixes buyers already created (e.g. "TEST NEW").
-- Safe to run multiple times.
-- ============================================================================

-- ---------- 1. Buyer attribution trigger (the bulletproof part) -------------
create or replace function public.attribute_buyer_from_metadata()
returns trigger
language plpgsql security definer set search_path = public as $$
declare v_ref text; v_agency uuid;
begin
  if new.attributed_to_agency is null then
    select nullif(btrim(u.raw_user_meta_data->>'agency_ref'), '')
      into v_ref
      from auth.users u
     where u.id = new.id;
    if v_ref is not null then
      select a.id into v_agency
        from public.agencies a
       where upper(a.agency_code) = upper(v_ref)
       limit 1;
      if v_agency is not null then
        new.attributed_to_agency := v_agency;
      end if;
    end if;
  end if;
  return new;
end $$;

drop trigger if exists trg_attribute_buyer on public.buyers;
create trigger trg_attribute_buyer
  before insert on public.buyers
  for each row execute function public.attribute_buyer_from_metadata();

-- ---------- 2. Backfill existing buyers (fixes TEST NEW etc.) ---------------
update public.buyers b
   set attributed_to_agency = a.id
  from auth.users u
  join public.agencies a
    on upper(a.agency_code) = upper(nullif(btrim(u.raw_user_meta_data->>'agency_ref'), ''))
 where b.id = u.id
   and b.attributed_to_agency is null;

-- ---------- 3. Front-end helper RPCs (kept; belt-and-suspenders) ------------
-- Idempotent self-attribution callable by the logged-in buyer.
create or replace function public.apply_agency_ref(p_ref text)
returns jsonb
language plpgsql security definer set search_path = public as $$
declare v_agency uuid; v_uid uuid := auth.uid();
begin
  if v_uid is null or p_ref is null or btrim(p_ref) = '' then
    return jsonb_build_object('ok', false);
  end if;
  select id into v_agency from public.agencies
   where upper(agency_code) = upper(btrim(p_ref)) limit 1;
  if v_agency is null then return jsonb_build_object('ok', false, 'reason', 'no_agency'); end if;
  update public.buyers
     set attributed_to_agency = v_agency
   where id = v_uid and attributed_to_agency is null;
  return jsonb_build_object('ok', true);
end $$;
grant execute on function public.apply_agency_ref(text) to authenticated;

-- Anon-callable public name lookup (for the signup banner).
create or replace function public.get_agency_public_name(p_code text)
returns text
language sql stable security definer set search_path = public as $$
  select agency_name from public.agencies
   where upper(agency_code) = upper(btrim(p_code)) limit 1;
$$;
grant execute on function public.get_agency_public_name(text) to anon, authenticated;

-- ---------- 4. Deal -> agency stamping (so commissions attribute) -----------
create or replace function public.stamp_deal_agency()
returns trigger
language plpgsql security definer set search_path = public as $$
declare v_agency uuid;
begin
  if new.agency_id is null and new.buyer_id is not null then
    select attributed_to_agency into v_agency from public.buyers where id = new.buyer_id;
    if v_agency is not null then new.agency_id := v_agency; end if;
  end if;
  return new;
end $$;

drop trigger if exists trg_stamp_deal_agency on public.deals;
create trigger trg_stamp_deal_agency
  before insert on public.deals
  for each row execute function public.stamp_deal_agency();

-- Backfill deals that have an attributed buyer but no agency_id yet.
update public.deals d
   set agency_id = b.attributed_to_agency
  from public.buyers b
 where d.buyer_id = b.id
   and d.agency_id is null
   and b.attributed_to_agency is not null;

-- ---------- 5. Quick check (optional) ---------------------------------------
-- select b.company_name, a.agency_name, a.agency_code
--   from public.buyers b join public.agencies a on a.id = b.attributed_to_agency
--  order by b.created_at desc;
