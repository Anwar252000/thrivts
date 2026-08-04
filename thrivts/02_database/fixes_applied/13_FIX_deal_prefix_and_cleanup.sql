-- ============================================================================
-- (E) Deal-number prefix: create_deal_from_match issued 'DEL-…' while every
-- other path issues 'DEAL-…'. This recreates it (verbatim from your backend)
-- with the prefix corrected to 'DEAL-'. Only that one line changed.
-- ============================================================================

CREATE OR REPLACE FUNCTION public.create_deal_from_match(p_requirement_id uuid, p_seller_id uuid, p_final_qty integer, p_buyer_price_per_pc numeric, p_seller_cost_per_pc numeric, p_shipping_cost_usd numeric DEFAULT 0, p_source_response_id uuid DEFAULT NULL::uuid, p_source_offer_id uuid DEFAULT NULL::uuid, p_estimated_dispatch date DEFAULT NULL::date, p_admin_notes text DEFAULT NULL::text)
 RETURNS json
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'public', 'pg_temp'
AS $function$
DECLARE
  v_actor      uuid := auth.uid();
  v_role       text;
  v_buyer_id   uuid;
  v_agency_id  uuid;
  v_deal_id    uuid;
  v_deal_num   text;
  v_lot_num    text;
  v_spread_pc  numeric;
  v_subtotal   numeric;
  v_invoice    numeric;
  v_spread_tot numeric;
  v_payout     numeric;
  v_net_spread numeric;
  v_commission numeric;
BEGIN
  SELECT role INTO v_role FROM public.profiles WHERE id = v_actor;
  IF v_role IS DISTINCT FROM 'admin' THEN
    RAISE EXCEPTION 'Only admin can create deals';
  END IF;

  SELECT buyer_id INTO v_buyer_id FROM public.requirements WHERE id = p_requirement_id;
  IF v_buyer_id IS NULL THEN
    RAISE EXCEPTION 'Requirement % not found', p_requirement_id;
  END IF;

  BEGIN
    SELECT attributed_to_agency INTO v_agency_id FROM public.buyers WHERE id = v_buyer_id;
  EXCEPTION WHEN undefined_column THEN
    v_agency_id := NULL;
  END;

  v_deal_num   := 'DEAL-' || EXTRACT(YEAR FROM now())::text || '-' ||
                  lpad(nextval('public.deal_number_seq')::text, 5, '0');
  v_lot_num    := 'LOT-' || EXTRACT(YEAR FROM now())::text || '-' ||
                  lpad(nextval('public.deal_number_seq')::text, 5, '0');
  v_spread_pc  := p_buyer_price_per_pc - p_seller_cost_per_pc;
  v_subtotal   := p_buyer_price_per_pc * p_final_qty;
  v_invoice    := v_subtotal + COALESCE(p_shipping_cost_usd, 0);
  v_payout     := p_seller_cost_per_pc * p_final_qty;
  v_spread_tot := v_invoice - v_payout;
  v_commission := GREATEST(v_spread_tot, 0) * 0.30;
  v_net_spread := v_spread_tot - v_commission;

  INSERT INTO public.deals (
    deal_number, requirement_id, buyer_id, seller_id, agency_id,
    total_quantity_pcs,
    buyer_price_per_pc_usd, avg_seller_price_per_pc_usd, spread_per_pc_usd,
    subtotal_usd, shipping_cost_usd, total_invoice_usd,
    total_seller_payout_usd, total_spread_usd, net_settled_spread_usd,
    status, confirmed_at,
    source_response_id, source_offer_id,
    estimated_dispatch_date, admin_notes, created_by
  ) VALUES (
    v_deal_num, p_requirement_id, v_buyer_id, p_seller_id, v_agency_id,
    p_final_qty,
    p_buyer_price_per_pc, p_seller_cost_per_pc, v_spread_pc,
    v_subtotal, COALESCE(p_shipping_cost_usd, 0), v_invoice,
    v_payout, v_spread_tot, v_net_spread,
    'confirmed'::deal_status, now(),
    p_source_response_id, p_source_offer_id,
    p_estimated_dispatch, p_admin_notes, v_actor
  )
  RETURNING id INTO v_deal_id;

  INSERT INTO public.deal_allocations (
    deal_id, seller_id, seller_response_id,
    lot_number,
    allocated_quantity_pcs, price_per_pc_usd, total_payout_usd,
    source_response_id, source_offer_id
  ) VALUES (
    v_deal_id, p_seller_id, p_source_response_id,
    v_lot_num,
    p_final_qty, p_seller_cost_per_pc, v_payout,
    p_source_response_id, p_source_offer_id
  );

  IF p_source_response_id IS NOT NULL THEN
    UPDATE public.seller_responses SET deal_id = v_deal_id WHERE id = p_source_response_id;
  END IF;
  IF p_source_offer_id IS NOT NULL THEN
    UPDATE public.requirement_seller_offers SET deal_id = v_deal_id WHERE id = p_source_offer_id;
  END IF;

  UPDATE public.requirements
  SET status = 'confirmed'
  WHERE id = p_requirement_id
    AND status::text NOT IN ('confirmed','settled','cancelled');

  RETURN json_build_object('ok', true, 'deal_id', v_deal_id, 'deal_number', v_deal_num);
END $function$;

-- ============================================================================
-- (D) Clean up the leftover QA test data that's leaking into the buyer UI.
-- Review the SELECT first, then run the DELETE. Adjust names if needed.
-- ============================================================================
-- see what would be removed:
-- select id, name from categories where name ilike '%QA TEST%' or name ilike '%delete me%';

-- delete the QA test category (only if no requirements reference it):
delete from public.categories
 where (name ilike '%QA TEST%' or name ilike '%delete me%')
   and id not in (select category_id from public.requirements where category_id is not null);

notify pgrst, 'reload schema';
