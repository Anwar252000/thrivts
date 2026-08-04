# Thrivts — Flow Verification Matrix
_The pathway to prove every built flow works. Run each flow's steps (via Claude for Chrome or by hand), check the expected UI, then run the `[DB]` query to confirm the data actually changed. A flow only "passes" when both the UI and the DB agree._

**Legend:** `[UI]` = what you should see. `[DB]` = paste into the Supabase SQL editor as `postgres`. ✅ = expected pass condition.

**Accounts to prepare:** 1 admin, 2 buyers (B1, B2), 2 sellers (S1, S2). Two of each so cross-tenant isolation and multi-seller fills can be tested.

---

## 1. Onboarding & approval

**1.1 Buyer signup**
- Steps: sign up as a new buyer. `[UI]` "check your email" screen → confirm email → land in dashboard as pending.
- ✅ `[DB]` `select role, approval_status, is_active from profiles where email='<b1>';` → `buyer / pending / true`. And `select 1 from buyers where id=(select id from profiles where email='<b1>');` returns a row.
- ✅ Admin bell shows "New buyer registered."

**1.2 Seller invite + signup**
- Steps: admin creates a seller invite → seller uses the token to sign up.
- ✅ `[DB]` `select * from seller_invites order by created_at desc limit 1;` shows the invite; after signup `used_at` is set. `select role,approval_status from profiles where email='<s1>';` → `seller / pending`. `select public_alias from sellers where id=(...);` → a `Seller_xxxx` alias exists.
- ✅ Admin bell shows "New seller registered."

**1.3 Admin approve seller (+ tags)**
- Steps: admin approves S1 with tags.
- ✅ `[DB]` `select approval_status from profiles where id='<s1>';` → `approved`. `select tags from sellers where id='<s1>';` → the tags you set.
- ✅ S1 can now see Open Requirements (was blocked before).

**1.4 Approve / reject / block / unblock (buyer & seller)**
- For each of `approve_seller`, `reject_seller`, `block_seller`, `unblock_seller`, `admin_block_buyer`, `admin_unblock_buyer`: click it, then
- ✅ `[DB]` `select approval_status,is_active from profiles where id='<target>';` reflects the action (approved/rejected/suspended, is_active true/false).
- ✅ Security: while logged in as a **non-admin**, none of these are callable (button absent or call returns "Admin only").

**1.5 KYC verify**
- Steps: admin verifies S1's KYC.
- ✅ `[DB]` `select kyc_verified from sellers where id='<s1>';` → `true`.

**1.6 Privilege lock (security)**
- Steps: as buyer B1, attempt (via console) to set your own `role='admin'` or `approval_status='approved'`.
- ✅ It silently no-ops — `[DB]` `select role,approval_status from profiles where id='<b1>';` unchanged. (Enforced by `lock_profile_privileged_cols`.)

---

## 2. Requirements

**2.1 Post requirement (B1)** → `[UI]` appears in My Requirements as "Posted". ✅ `[DB]` `select status from requirements where requirement_number='<REQ>';` → `posted`. Admin bell shows "requirement posted" (if wired).

**2.2 Edit / delete requirement** → deleting a requirement with no deals removes it; ✅ `[DB]` row gone. Deleting one **with** deals should be blocked or cascade per `admin_delete_requirement`.

**2.3 Cross-tenant isolation** → as B2, you must NOT see B1's requirements in My Requirements. ✅ `[DB]` `select count(*) from requirements where buyer_id='<b2>';` matches only B2's list.

---

## 3. Bidding & negotiation (the core loop)

**3.1 Seller places bid (S1 on B1's REQ)**
- ✅ `[DB]` `select status,negotiation_state,fee_per_pc_applied_usd from seller_responses where requirement_id='<req>' and seller_id='<s1>';` → `pending / open`, fee frozen (non-null).
- ✅ Buyer B1's My Requirements row gets a **red dot**; buyer bell shows "New bid"; seller sees "awaiting buyer" in My Quotes.

**3.2 Second seller (S2) bids same REQ**
- ✅ Both S1 and S2 appear on B1's bidding board. ✅ `[DB]` `select seller_id,available_quantity_pcs from seller_responses where requirement_id='<req>';` → two rows. (Confirms multiple sellers can bid.)

**3.3 Seller revises a bid** → open the quote (should read "Update quote", prefilled), change price.
- ✅ No `duplicate key` error. ✅ `[DB]` `current_price_usd` updated, `negotiation_state='countered_by_seller'`. Buyer's board shows the new all-in price.

**3.4 Buyer counters (B1)** → ✅ `[DB]` `negotiation_state='countered_by_buyer'`, `buyer_counter_price_usd` set. Seller S1: My Quotes tab **red dot** + card dot + "Buyer countered — your move"; seller bell shows it; clicking clears the dot.

**3.5 Seller counters back** → buyer sees "Seller countered back" + new price; ✅ `[DB]` `negotiation_state='countered_by_seller'`.

**3.6 Seller ACCEPTS the buyer's counter → DEAL CREATED (no re-confirm)**
- This is the unified flow. ✅ A deal is created **immediately** — buyer does NOT need to accept again.
- ✅ `[DB]` `select deal_number,status from deals where source_response_id='<bid_id>';` → one `confirmed` deal. `select status from seller_responses where id='<bid_id>';` → `accepted`, `deal_id` set.
- ✅ Seller My Quotes shows "Accepted — Deal created"; My Deals tab dot; admin bell "Deal confirmed".

**3.7 Buyer ACCEPTS a bid → DEAL CREATED**
- ✅ Same as 3.6 via the buyer path. `[DB]` deal exists, bid `accepted`, requirement → `matching` (partial) or `ready_to_order` (full).

**3.8 Multi-fill accounting** → accept a partial bid (e.g. 500 of 1000), then check the board shows "500/1000 committed"; a second bid can fill the rest; a bid exceeding the remainder is rejected with "exceeds the remaining quantity".
- ✅ `[DB]` `select coalesce(sum(available_quantity_pcs),0) from seller_responses where requirement_id='<req>' and status='accepted';` never exceeds `quantity_pcs`.

**3.9 Concurrency (optional)** → two rapid accepts on the same requirement must not over-allocate (row lock serializes them). ✅ `[DB]` sum of accepted ≤ requirement qty.

---

## 4. Deal lifecycle (admin)

**4.1 Advance deal statuses** → as admin, move a deal `paid → in_fulfillment → dispatched → delivered → settled`, filling the modal fields at each step.
- ✅ `[DB]` after each: `select status,paid_at,dispatched_at,delivered_at,settled_at from deals where deal_number='<d>';` reflects the transition.
- ✅ On `delivered` with an agency deal: `select status,release_due_at from commissions where deal_id='<id>';` → `accrued`, due = delivered + 20 days.
- ✅ On `settled`: requirement → `settled`; agency commission → `released`; **admin bell shows "Deal settled"**.

**4.2 Cancel a deal (with reason)**
- ✅ Deal shows **CANCELLED** in the admin table (should read as a danger/red state). ✅ `[DB]` `select status from deals where deal_number='<d>';` → `cancelled`.
- ✅ The bid frees: `select status from seller_responses where id='<bid>';` → `pending` (not accepted); requirement reverts to `matching` if nothing else is committed.
- ✅ **Seller AND buyer both get a "Deal cancelled — <reason>" notification.** Admin bell shows it too.
- ✅ Committed quantity is released — a new bid can now fill it.

**4.3 Known gap to verify — influencer commissions** → if the influencer program is live, on `settled` the influencer commission should release and on `cancelled` reverse. Currently `advance_deal_status` does NOT call `release_influencer_commission` / `reverse_influencer_commission`. ✅ `[DB]` `select status from influencer_commissions where deal_id='<id>';` — if it stays `accrued` after settle, that's the open bug (#3).

---

## 5. Direct offers (admin → seller)

**5.1 Admin pushes an offer / posts a round** (`post_offer_round`, `admin_accept_offer`, `admin_decline_offer`) and seller responds (`accept_seller_offer`, `post_offer_round` as seller).
- ✅ `[DB]` `select status,current_price_per_pc from requirement_seller_offers where id='<offer>';` tracks countered/accepted/declined.
- ⚠️ Note: accepting an offer marks it accepted but does **not** itself create a deal — admin still runs `create_deal_from_match`. Confirm that's the intended two-step, or flag it.

---

## 6. Notifications & emails (every event)

For each event below, confirm: recipient's **bell** shows it, the **dot** appears where relevant, and — once B4 is confirmed — an **email** sends.
- New bid → buyer. Buyer counter → seller. Seller counter/accept/decline → buyer. Deal confirmed → seller + admin. Deal cancelled → seller + buyer + admin. Deal settled → admin. New signup → admin.
- ✅ `[DB]` `select kind,audience,recipient_id,read_at from notifications order by created_at desc limit 20;` — each event produced a row for the right recipient. Opening the item sets `read_at`.
- ✅ Email: after any event, Edge Functions → `thrivts-notify` → Logs shows `sent:`, Resend shows Delivered.

---

## 7. Moat spot-checks (must pass — non-negotiable)

**7.1 Seller anonymity** → as B1, open any bid. `[UI]` you see only `Seller_xxxx` + tier, never a company name/email. ✅ `[DB]` `select column_name from information_schema.columns where table_name='buyer_visible_bids';` has no company/email/seller_id.
**7.2 Fee opacity** → `[UI]` each bid shows one all-in price, no fee/spread line. ✅ same column list has no fee/spread/seller_cost/proposed_price. Repeat for `buyer_deals_view`.
**7.3 Admin can de-anonymize** → `admin_reveal_seller('<alias>')` returns the real company only for admin; a non-admin call errors.

---

## 8. Agency & influencer portals (if live)

If these programs are on: verify `get_agency_dashboard` totals, commission accrue/release, `apply_referral_code`/`apply_agency_ref` attribution, and the influencer overview. If they're **not** live yet, mark this whole section "deferred" so it's not mistaken for a pass.

---

## How to drive this with Claude for Chrome
1. Give Chrome one section at a time ("run section 3, tell me what you see at each step").
2. After each flow, you (or I) run the `[DB]` check to confirm the data — the UI can look right while the data is wrong (that's how the notification bug hid for days).
3. Log each flow as **pass / fail / blocked**. Anything that fails, send me the step + the `[DB]` output and I fix the function.
4. When every section is green in both UI and DB, that's your launch gate — not before.

## Current known-open items (fix before final pass)
- #2 deal-number race — fixed by the unified finalize (uses the sequence).
- #3 influencer commission release/reverse on settle/cancel — still open (section 4.3).
- Two deal-creation conventions: buyer/seller path (`DEAL-…`, unified) vs admin match (`create_deal_from_match`, `DEL-…`, sets `confirmed`) — unify if the admin match path is used.
- CANCELLED pill color in the admin deals table (cosmetic).
