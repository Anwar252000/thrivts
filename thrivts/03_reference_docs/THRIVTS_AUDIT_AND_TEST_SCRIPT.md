# Thrivts — Static Audit + Live-Test Script
_Audit date: 30 Jul 2026 · Basis: the uploaded `buyer.html`, `seller.html`, `admin.html`, and the SQL set._

**Scope caveat:** this audits the files you gave me. It is only as accurate as those files match what's actually deployed on Netlify/Supabase right now. Where a view or table lives in your base schema (not in the uploaded SQL), it's flagged as "verify on prod."

---

## Moat compliance — both rules PASS (in everything visible to me)

**Rule 1 — Seller anonymity (buyer must never identify a seller): PASS.**
The only buyer-facing bid source, `buyer_visible_bids`, exposes `seller_alias` (the anonymous `public_alias`), `seller_tier`, and `seller_verified` — and nothing else about the seller. No `company_name`, email, real name, or `seller_id` appears in any buyer-facing view or in `buyer.html`. The bidding board renders only the alias + a tier badge. Buyer-facing notifications use the anonymous alias or generic copy, never a seller's real identity.

**Rule 2 — Fee opacity (buyer sees one all-in price, never the split): PASS in visible surfaces.**
`buyer_visible_bids` carries only the all-in `buyer_price_per_pc_usd` / `buyer_total_usd`. No `fee_per_pc`, `spread`, `seller_cost`, or raw `proposed_price` reaches a buyer-facing view, and `buyer.html` renders no split (the "spread"/"split" strings in it are CSS class names). **One verify item:** `buyer_deals_view` is defined in your base schema, not the uploaded SQL — confirm it excludes `spread`, `seller_cost`, and `avg_seller_price` (see F3).

---

## Findings (ranked)

### F1 — Views bypass RLS; isolation rests on app-side filters _(Medium — hardening)_
No view sets `security_invoker`, so every view (`buyer_visible_bids`, `seller_own_bids`, the deals/requirements views) runs as its owner and **bypasses row-level security**. Today the app filters correctly — `renderBids` scopes `buyer_visible_bids` by `requirement_id`, seller queries scope by `seller_id`, buyer deal queries by `buyer_id`. But because the DB doesn't enforce it, a crafted API call (someone using your anon key directly, not the UI) could read another tenant's rows: e.g. bids on a requirement that isn't theirs.

Important: what would leak is **anonymous aliases + all-in prices** — *not* seller real identity and *not* the fee split. So the two moat rules themselves are not broken by this. It's a competitive-data isolation gap, worth hardening.

_Fix direction:_ enable `security_invoker = true` on these views **and** ensure the underlying tables have RLS policies that scope buyer/seller correctly — or add explicit scoping predicates. This needs testing (a wrong RLS policy will blank out legitimate reads), so it's a deliberate task, not a one-liner. Flagging, not silently changing.

### F2 — `buyer_accept_bid` swallows its allocation/commission inserts _(Medium — reliability)_
The deal-creation function guards the `deal_allocations`, commission, and status-advance inserts in `exception when others` blocks so a hiccup there "can't undo the deal." Most of them log via `raise notice` (good), but the final guard is `exception when others then null;` — fully silent. Risk: a deal row can exist while its allocation or commission silently didn't get created. This is the same silent-failure class that hid the notifications bug for days.

_Fix direction:_ make the last catch `raise notice` too, and run a periodic reconciliation — deals with no matching `deal_allocations` row — so a silent miss surfaces instead of hiding.

### F3 — Verify `buyer_deals_view` excludes the spread _(Verify)_
Its definition isn't in the uploaded SQL. Run on prod:
```sql
select column_name from information_schema.columns
where table_schema='public' and table_name='buyer_deals_view'
order by ordinal_position;
```
Confirm there's no `spread`, `seller_cost`, `avg_seller_price`, or `total_spread` column. If any exist, that's a Rule-2 leak to fix.

### F4 — Confirm the three old webhooks are deleted _(Ops)_
`notify-offers`, `notify-rounds`, `notify-deals` pointed the old function at the old tables. They don't block anything, but they fire junk on offer/round/deal changes. Database → Webhooks → delete all three; keep only `notify-all`.

### F5 — Notification email delivery still unconfirmed end-to-end _(Open)_
Rows now write and `notify-all` should fire the function, but we never saw a `sent:` line or a delivered notification email. Close this with the email test below.

---

## What's healthy (confirmed)

- `notifications` RLS is correct: `coalesce(recipient_id, user_id) = auth.uid()`.
- `push_notification` now populates every required column — no more silent NOT NULL failures.
- `seller_own_bids` is current (exposes the negotiation columns).
- Seller queries scoped by `seller_id`; buyer queries by `buyer_id`; `renderBids` by `requirement_id`.
- Sellers don't see buyer identity; buyers don't see seller identity.
- Fee freeze is INSERT-only, so revising a bid preserves the frozen split.
- The activity-dot system is RLS-scoped per user — no cross-account dots.

---

## Live-test script (run these yourself, click by click)

Use three browser profiles/accounts: **Buyer**, **Seller**, **Admin**. `[DB]` steps are queries to paste in the Supabase SQL editor as `postgres`.

### A. Full deal loop
1. **Buyer** → Post a new requirement (note its REQ number).
2. **Admin** → bell should show **"New requirement posted"** (if you wired it) and **buyer/seller signups** appear for any new test accounts. Bell count ticks; clicking routes to the right section.
3. **Seller** → Open Requirements → Submit Quote on that REQ. Expect: no error; quote appears under **My Quotes** as "Bid submitted — awaiting buyer."
4. **Buyer** → My Requirements → the row shows a **red dot** and "Needs response" filter includes it. Open it → **New bid** visible; dot clears.
5. **Buyer** → **Counter** the bid.
6. **Seller** → **My Quotes** tab shows a **red dot**, and the card shows a dot + "Buyer countered — your move." Bell shows "Buyer countered your bid," clickable → jumps to the card. Clicking the card clears its dot.
7. **Seller** → **Counter back** (or accept). `[DB]` optional: `select negotiation_state, current_price_usd from seller_responses where requirement_id = '<REQ id>';`
8. **Buyer** → open the requirement → **Accept** a bid to confirm the deal.
9. **Seller** → **My Deals** tab shows a **red dot**; card shows the deal at "Confirmed." Opening the tab clears the dot.
10. **Admin** → bell shows **"Deal confirmed — <deal#>"**, clickable → Deals section.
11. `[DB]` reconciliation (catches F2): 
```sql
select d.deal_number
from deals d
left join deal_allocations a on a.deal_id = d.id
where a.id is null;
```
Expect **no rows**. Any row = a deal created without its allocation (silent failure).

### B. Re-quote / revise (the duplicate-key fix)
1. **Seller** → on a requirement you've already bid, open the quote — it should read **"Update quote"** and be **prefilled**.
2. Change the price → submit. Expect: **no `duplicate key` error**; buyer's board shows the new price; buyer sees "Seller countered back."

### C. Notifications: in-app + email
1. Do any counter (step A5/A6). 
2. `[DB]` `select count(*) from notifications;` → increases.
3. Recipient's **bell** shows it, count correct, clicking opens the right screen, "Mark all read" clears.
4. **Email:** Edge Functions → `thrivts-notify` → Logs → newest entry should read `sent:`. Then Resend → Emails → the message shows **Delivered**. If it's `401` → secret mismatch; if `Resend error` → read the reason; if no invocation → the `notify-all` webhook isn't firing.

### D. Moat spot-checks (do these as a paranoid buyer)
1. **Buyer** → open a bid on your requirement. Confirm you see only an alias like `Seller_xxxx` + tier — never a company name, email, or a fee/spread line. One number per bid.
2. `[DB]` confirm the buyer view can't expose more:
```sql
select column_name from information_schema.columns
where table_schema='public' and table_name='buyer_visible_bids' order by ordinal_position;
```
Expect no `fee_*`, `spread`, `seller_cost`, `proposed_price`, `company_name`, `email`, or `seller_id`.

---

## Suggested order of operations
1. Run the F3 and D2 column checks (2 minutes, confirms the moat at the DB level).
2. Close F5 (email) with test C4.
3. Delete the old webhooks (F4).
4. Schedule F1 (view RLS hardening) and F2 (accept-bid logging + reconciliation) as their own tasks — they're not fires, but they're the two things most likely to bite silently later.
