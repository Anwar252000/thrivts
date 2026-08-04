-- ============================================================
-- THRIVTS — 3_performance_indexes.sql  (v2, schema-aware)
-- Safe to run as-is: every index checks that its table AND
-- columns actually exist first. Anything that doesn't match
-- your schema is SKIPPED with a NOTICE instead of erroring.
-- Idempotent — run as many times as you like.
-- After running, the final SELECT shows what exists.
-- ============================================================

DO $$
DECLARE
  spec RECORD;
BEGIN
  FOR spec IN
    SELECT * FROM (VALUES
      -- (index_name, table_name, required_columns, index_definition)
      ('idx_profiles_role',         'profiles',                  ARRAY['role'],                          'ON public.profiles (role)'),
      ('idx_profiles_role_created', 'profiles',                  ARRAY['role','created_at'],             'ON public.profiles (role, created_at DESC)'),
      ('idx_profiles_approval',     'profiles',                  ARRAY['approval_status'],               'ON public.profiles (approval_status)'),

      ('idx_req_buyer_created',     'requirements',              ARRAY['buyer_id','created_at'],         'ON public.requirements (buyer_id, created_at DESC)'),
      ('idx_req_buyer_status',      'requirements',              ARRAY['buyer_id','status'],             'ON public.requirements (buyer_id, status)'),
      ('idx_req_status',            'requirements',              ARRAY['status'],                        'ON public.requirements (status)'),
      ('idx_req_created',           'requirements',              ARRAY['created_at'],                    'ON public.requirements (created_at DESC)'),

      ('idx_deals_buyer_created',   'deals',                     ARRAY['buyer_id','created_at'],         'ON public.deals (buyer_id, created_at DESC)'),
      ('idx_deals_agency_created',  'deals',                     ARRAY['agency_id','created_at'],        'ON public.deals (agency_id, created_at DESC)'),
      ('idx_deals_status',          'deals',                     ARRAY['status'],                        'ON public.deals (status)'),
      ('idx_deals_settled',         'deals',                     ARRAY['status','created_at'],           'ON public.deals (status, created_at DESC) WHERE status = ''settled'''),

      ('idx_comm_agency_created',   'commissions',               ARRAY['agency_id','created_at'],        'ON public.commissions (agency_id, created_at DESC)'),
      ('idx_comm_status',           'commissions',               ARRAY['status'],                        'ON public.commissions (status)'),

      ('idx_influencers_user',      'influencers',               ARRAY['user_id'],                       'ON public.influencers (user_id)'),
      ('idx_infcomm_created',       'influencer_commissions',    ARRAY['created_at'],                    'ON public.influencer_commissions (created_at DESC)'),
      ('idx_infcomm_influencer',    'influencer_commissions',    ARRAY['influencer_id','created_at'],    'ON public.influencer_commissions (influencer_id, created_at DESC)'),

      ('idx_threads_buyer_updated', 'message_threads',           ARRAY['buyer_id','updated_at'],         'ON public.message_threads (buyer_id, updated_at DESC)'),
      ('idx_threads_participant',   'message_threads',           ARRAY['participant_id'],                'ON public.message_threads (participant_id)'),
      ('idx_threads_last_msg',      'message_threads',           ARRAY['last_message_at'],               'ON public.message_threads (last_message_at DESC NULLS LAST)'),
      ('idx_threads_unread_buyer',  'message_threads',           ARRAY['buyer_id','unread_for_buyer'],   'ON public.message_threads (buyer_id) WHERE unread_for_buyer = true'),
      ('idx_messages_thread',       'messages',                  ARRAY['thread_id','created_at'],        'ON public.messages (thread_id, created_at)'),

      ('idx_audit_created',         'audit_log',                 ARRAY['created_at'],                    'ON public.audit_log (created_at DESC)'),

      ('idx_buyers_attributed',     'buyers',                    ARRAY['attributed_to_agency','created_at'], 'ON public.buyers (attributed_to_agency, created_at DESC)'),

      ('idx_sresp_seller',          'seller_responses',          ARRAY['seller_id'],                     'ON public.seller_responses (seller_id)'),
      ('idx_rso_seller',            'requirement_seller_offers', ARRAY['seller_id','sent_at'],           'ON public.requirement_seller_offers (seller_id, sent_at DESC)'),
      ('idx_rounds_offer',          'offer_rounds',              ARRAY['offer_id','created_at'],         'ON public.offer_rounds (offer_id, created_at)'),

      ('idx_feed_created',          'public_activity_feed',      ARRAY['created_at'],                    'ON public.public_activity_feed (created_at DESC)'),

      ('idx_categories_active',     'categories',                ARRAY['is_active','display_order'],     'ON public.categories (is_active, display_order)')
    ) AS t(idx_name, tbl, req_cols, idx_def)
  LOOP
    -- table must exist as a real table (views can't be indexed)
    IF NOT EXISTS (
      SELECT 1 FROM information_schema.tables
      WHERE table_schema = 'public' AND table_name = spec.tbl AND table_type = 'BASE TABLE'
    ) THEN
      RAISE NOTICE 'SKIP %: table public.% not found (or is a view)', spec.idx_name, spec.tbl;
      CONTINUE;
    END IF;

    -- every required column must exist
    IF (
      SELECT count(*) FROM information_schema.columns
      WHERE table_schema = 'public' AND table_name = spec.tbl AND column_name = ANY (spec.req_cols)
    ) < array_length(spec.req_cols, 1) THEN
      RAISE NOTICE 'SKIP %: public.% is missing one of columns %', spec.idx_name, spec.tbl, spec.req_cols;
      CONTINUE;
    END IF;

    EXECUTE format('CREATE INDEX IF NOT EXISTS %I %s', spec.idx_name, spec.idx_def);
    RAISE NOTICE 'OK   % created (or already existed)', spec.idx_name;
  END LOOP;
END $$;

-- What actually exists now:
SELECT indexname, tablename
FROM pg_indexes
WHERE schemaname = 'public' AND indexname LIKE 'idx\_%'
ORDER BY tablename, indexname;
