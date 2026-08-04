-- ============================================================================
-- THRIVTS — admin_delete_requirement
-- Fixes: "update or delete on table 'requirements' violates foreign key
--         constraint 'deals_requirement_id_fkey'"
--
-- What it does: deletes a requirement AND everything hanging off it, in the
-- correct FK order (deal children -> deals -> quotes/pushes -> requirement).
-- Admin-only: any non-admin caller gets 'not authorized'.
--
-- HOW TO RUN: Supabase Dashboard -> SQL Editor -> paste all -> Run.
-- Safe to run multiple times (CREATE OR REPLACE).
-- ============================================================================

create or replace function public.admin_delete_requirement(p_req_id uuid)
returns void
language plpgsql
security definer
set search_path = public
as $$
begin
  -- 1) Only admins may call this
  if not exists (
    select 1 from profiles
    where id = auth.uid() and role = 'admin'
  ) then
    raise exception 'not authorized';
  end if;

  -- 2) Children of the deals under this requirement.
  --    Each block is guarded so a table/column that doesn't exist in this
  --    schema is simply skipped instead of breaking the whole delete.
  begin
    delete from disputes
    where deal_id in (select id from deals where requirement_id = p_req_id);
  exception when undefined_table or undefined_column then null;
  end;

  begin
    delete from commissions
    where deal_id in (select id from deals where requirement_id = p_req_id);
  exception when undefined_table or undefined_column then null;
  end;

  begin
    delete from deal_allocations
    where deal_id in (select id from deals where requirement_id = p_req_id);
  exception when undefined_table or undefined_column then null;
  end;

  -- 3) The deals themselves
  begin
    delete from deals where requirement_id = p_req_id;
  exception when undefined_table or undefined_column then null;
  end;

  -- 4) Quotes / offers / pushes / matches tied to the requirement
  begin
    delete from requirement_seller_offers where requirement_id = p_req_id;
  exception when undefined_table or undefined_column then null;
  end;

  begin
    delete from requirement_seller_pushes where requirement_id = p_req_id;
  exception when undefined_table or undefined_column then null;
  end;

  begin
    delete from matches where requirement_id = p_req_id;
  exception when undefined_table or undefined_column then null;
  end;

  -- 5) Finally the requirement itself
  delete from requirements where id = p_req_id;
end
$$;

-- Lock it down: only authenticated users can even call it,
-- and the role check inside restricts it to admins.
revoke execute on function public.admin_delete_requirement(uuid) from public, anon;
grant execute on function public.admin_delete_requirement(uuid) to authenticated;

-- Ask PostgREST to reload so the RPC is visible immediately
notify pgrst, 'reload schema';
