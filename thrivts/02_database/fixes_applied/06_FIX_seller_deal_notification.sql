-- ============================================================================
-- THRIVTS — notify the SELLER when their bid is accepted (deal confirmed).
-- Sellers previously got no notification on acceptance, so the seller portal had
-- no activity signal for the My Deals tab. This adds a bell notification to the
-- seller on every new deal, linked to the deal so the portal can dot the tab.
-- Moat-safe: seller sees only their own deal number + qty, never buyer identity
-- or the spread.
-- ============================================================================

create or replace function public.trg_notify_seller_new_deal()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
begin
  perform public.push_notification(
    NEW.seller_id, 'seller', 'deal_confirmed',
    'Your bid was accepted',
    'Deal ' || coalesce(NEW.deal_number, '') || ' confirmed for '
      || coalesce(NEW.total_quantity_pcs::text, '?') || ' pcs.',
    'deal', NEW.id);
  return NEW;
end;
$$;

drop trigger if exists notify_seller_new_deal on public.deals;
create trigger notify_seller_new_deal
  after insert on public.deals
  for each row execute function public.trg_notify_seller_new_deal();

notify pgrst, 'reload schema';
