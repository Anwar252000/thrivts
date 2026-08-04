-- ============================================================================
-- unverify_seller_kyc — the admin "Unverify KYC" button already calls this RPC,
-- but the function was never created (only verify_seller_kyc existed), so the
-- button errored with "This feature isn't fully set up yet." This adds it.
-- Admin-gated, mirrors verify_seller_kyc in reverse.
-- ============================================================================

create or replace function public.unverify_seller_kyc(p_seller_id uuid, p_notes text default null)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  has_verified_at boolean;
  has_verified_by boolean;
  set_clause text := 'kyc_verified = false, kyc_notes = COALESCE($2, kyc_notes)';
begin
  if not exists (select 1 from public.profiles where id = auth.uid() and role = 'admin') then
    raise exception 'Not authorized';
  end if;

  -- clear the verified_at / verified_by stamps if those columns exist
  select exists (select 1 from information_schema.columns
    where table_schema='public' and table_name='sellers' and column_name='kyc_verified_at') into has_verified_at;
  select exists (select 1 from information_schema.columns
    where table_schema='public' and table_name='sellers' and column_name='kyc_verified_by') into has_verified_by;
  if has_verified_at then set_clause := set_clause || ', kyc_verified_at = null'; end if;
  if has_verified_by then set_clause := set_clause || ', kyc_verified_by = null'; end if;

  execute format('update public.sellers set %s where id = $1', set_clause)
    using p_seller_id, p_notes;

  if not found then raise exception 'Seller not found: %', p_seller_id; end if;
end;
$$;

grant execute on function public.unverify_seller_kyc(uuid, text) to authenticated;
notify pgrst, 'reload schema';
