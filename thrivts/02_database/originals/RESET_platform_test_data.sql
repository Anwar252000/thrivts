-- ============================================================================
-- THRIVTS — PLATFORM TEST-DATA RESET
-- ----------------------------------------------------------------------------
-- ⚠️  DESTRUCTIVE. Wipes ALL trading data so you can retest the new bidding
--     flow from a clean slate.
--
-- WHAT IT DELETES (all trading activity):
--     disputes, commissions, deal_allocations, deals,
--     seller_responses (bids), requirement_seller_offers (direct offers),
--     requirements, and related activity/audit rows.
--
-- WHAT IT KEEPS (your accounts and setup):
--     buyers, sellers, agencies, profiles, auth users, categories,
--     platform_fee_config, seller public_alias pseudonyms.
--     → You will NOT have to re-register anyone. Log-ins keep working.
--
-- HOW TO RUN: read the WHAT IT DELETES list above, make sure you accept it,
-- then paste the whole file into SQL Editor and Run.
-- ============================================================================

begin;

-- Children first (FK order), each guarded so a missing table doesn't abort.
do $$
begin
  begin delete from disputes;          exception when undefined_table then null; end;
  begin delete from commissions;       exception when undefined_table then null; end;
  begin delete from influencer_commissions; exception when undefined_table then null; end;
  begin delete from deal_allocations;  exception when undefined_table then null; end;
  begin delete from deals;             exception when undefined_table then null; end;
  begin delete from seller_responses;  exception when undefined_table then null; end;
  begin delete from requirement_seller_offers; exception when undefined_table then null; end;
  begin delete from offer_rounds;      exception when undefined_table then null; end;
  begin delete from matches;           exception when undefined_table then null; end;
  begin delete from requirement_seller_pushes; exception when undefined_table then null; end;
  begin delete from requirements;      exception when undefined_table then null; end;
  -- activity / audit noise from the old tests
  begin delete from activity_log;      exception when undefined_table then null; end;
  begin delete from audit_log;         exception when undefined_table then null; end;
  begin delete from messages;          exception when undefined_table then null; end;
end $$;

-- Reset seller counters so test stats start clean (safe if columns absent).
do $$
begin
  begin
    update sellers set total_orders_fulfilled = 0
     where total_orders_fulfilled is distinct from 0;
  exception when undefined_column then null;
  end;
end $$;

commit;

-- ----------------------------------------------------------------------------
-- VERIFY — should all return 0
-- ----------------------------------------------------------------------------
select 'requirements' as table_name, count(*) from requirements
union all select 'seller_responses', count(*) from seller_responses
union all select 'deals', count(*) from deals
union all select 'deal_allocations', count(*) from deal_allocations;

-- ----------------------------------------------------------------------------
-- CONFIRM SETUP SURVIVED — sellers keep their pseudonyms, fee config intact
-- ----------------------------------------------------------------------------
select count(*) as sellers_with_alias from sellers where public_alias is not null;
select fee_per_pc_usd, pkr_reference from platform_fee_config where id = true;
