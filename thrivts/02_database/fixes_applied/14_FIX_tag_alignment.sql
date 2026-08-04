-- ============================================================================
-- Tag disconnect: admin manages sellers.tags (approve_seller, editSellerTags),
-- but current_seller_tags() read sellers.manual_tags — so admin tag changes
-- never affected which requirements a seller could see. Point the reader at the
-- admin-managed column. (manual_tags is unused: handle_new_user seeds tags=[]
-- and never writes manual_tags.)
-- ============================================================================

CREATE OR REPLACE FUNCTION public.current_seller_tags()
 RETURNS text[]
 LANGUAGE sql
 STABLE SECURITY DEFINER
AS $function$
  SELECT tags FROM sellers WHERE id = auth.uid()
$function$;

notify pgrst, 'reload schema';
