-- ============================================================================
-- CANCEL CASCADE (+ reason) + finalize notifications
--
-- Bug: advance_deal_status('cancelled') only flipped deals.status + reversed
-- commission. It never un-accepted the bid, so the seller kept seeing "Deal
-- confirmed", the committed quantity never freed, and the requirement never
-- reverted.
--
-- This trigger reacts to a deal becoming 'cancelled' and:
--   1. frees the source bid(s) back to a live 'pending'/'open' state,
--   2. reverts the requirement to 'matching' if nothing is still committed,
--   3. notifies BOTH the seller and the buyer, WITH the cancellation reason,
--   4. gives admins a dot on cancel — and also on settle (finalize).
-- The deal row itself stays status='cancelled' so it shows as cancelled in
-- both the seller's My Deals and the buyer's deals view.
-- ============================================================================

create or replace function public.trg_deal_terminal_cascade()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
declare
  v_reason text;
  v_reqnum text;
begin
  -- ---------- CANCELLED ----------
  if NEW.status = 'cancelled' and (OLD.status is distinct from 'cancelled') then

    -- pull the reason the admin entered (advance_deal_status appends
    -- 'Cancelled <ts>: <reason>' to admin_notes)
    v_reason := coalesce(
      nullif(btrim(substring(coalesce(NEW.admin_notes,'') from 'Cancelled[^:]*:\s*(.*)$')), ''),
      'No reason given');
    select requirement_number into v_reqnum from public.requirements where id = NEW.requirement_id;

    -- 1) free the bid(s) so quantity releases and the seller stops seeing "Deal confirmed"
    update public.seller_responses sr
       set status            = 'pending'::seller_response_status,
           negotiation_state = 'open',
           deal_id           = null,
           last_action_at    = now()
     where sr.id = NEW.source_response_id
        or sr.id in (
             select seller_response_id from public.deal_allocations
              where deal_id = NEW.id and seller_response_id is not null
           );

    -- 2) revert the requirement if nothing is still committed
    if NEW.requirement_id is not null then
      update public.requirements r
         set status = 'matching'
       where r.id = NEW.requirement_id
         and r.status::text not in ('settled','cancelled')
         and not exists (
               select 1 from public.seller_responses s
                where s.requirement_id = NEW.requirement_id
                  and lower(s.status::text) = 'accepted'
             );
    end if;

    -- 3) notify seller + buyer WITH the reason
    perform public.push_notification(
      NEW.seller_id, 'seller', 'deal_cancelled',
      'Deal cancelled — ' || coalesce(NEW.deal_number,''),
      'Reason: ' || v_reason || '. Your quote is live again if the requirement is still open.',
      'requirement', NEW.requirement_id);

    perform public.push_notification(
      NEW.buyer_id, 'buyer', 'deal_cancelled',
      'Deal cancelled — ' || coalesce(NEW.deal_number,''),
      'Your deal on ' || coalesce(v_reqnum,'your requirement') || ' was cancelled. Reason: ' || v_reason || '.',
      'requirement', NEW.requirement_id);

    -- 4) admin dot
    perform public.notify_admins(
      'deal_cancelled',
      'Deal cancelled — ' || coalesce(NEW.deal_number,''),
      'Reason: ' || v_reason,
      'deal', NEW.id);

  -- ---------- SETTLED (finalized) ----------
  elsif NEW.status = 'settled' and (OLD.status is distinct from 'settled') then
    perform public.notify_admins(
      'deal_settled',
      'Deal settled — ' || coalesce(NEW.deal_number,''),
      'Deal ' || coalesce(NEW.deal_number,'') || ' is fully settled.',
      'deal', NEW.id);
  end if;

  return NEW;
end;
$$;

drop trigger if exists deal_terminal_cascade on public.deals;
create trigger deal_terminal_cascade
  after update of status on public.deals
  for each row execute function public.trg_deal_terminal_cascade();

notify pgrst, 'reload schema';
