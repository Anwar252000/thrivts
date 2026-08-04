# Thrivts — Fix Task List
_Compiled 1 Aug 2026 from the static audit + edge-case review. Work top-down; each item notes owner (Claude = I implement, You = a prod check/action) and how to verify it later._

## A. Code / SQL fixes — I implement these

### A1 — `timeAgo` display bug: "0mo ago" and "X ago ago" _(High, quick, all 3 portals)_
Two real bugs in `timeAgo()`:
- **7–29-day gap:** buckets jump from `d ago` (<7 days) straight to `mo ago` (÷30 days), so anything 7–29 days old renders **"0mo ago."** Needs a weeks bucket (or extend the day bucket to 30 days).
- **Double "ago":** `timeAgo` already returns e.g. "4d ago", but some call sites wrap it as `timeAgo(x) + ' ago'` → **"4d ago ago"** / **"just now ago"** (seen in the requirement detail's "Posted" line and card metas).
Fix: correct the buckets and strip the trailing "ago" wrappers across buyer/seller/admin.

### A2 — `buyer_accept_bid` swallows its final insert silently _(Medium, reliability)_
The allocation/commission guards mostly log via `raise notice`, but the last catch is `exception when others then null;` — fully silent. A deal could be created without its allocation and you'd never see it. Fix: make the last catch log too. Ships with a reconciliation query (deals lacking `deal_allocations`) so a silent miss surfaces.

### A3 — Views bypass RLS _(Medium, hardening — deliberate, needs testing)_
No view sets `security_invoker`, so isolation rests entirely on the app's `.eq` filters (which are correct today). A crafted API call could read another tenant's bids — anonymous aliases + all-in prices only, so **the two moat rules are not broken**, but it's a data-isolation gap. Fix: enable `security_invoker = true` on the buyer/seller views **with** correct underlying-table RLS, then retest that legitimate reads still work. This is the one item that can break reads if rushed, so it's done carefully and tested in isolation.

### A4 — Accept concurrency (over-allocation window) _(Low, edge)_
`buyer_accept_bid` computes remaining pcs then inserts, not atomically — two simultaneous accepts could both pass the remaining check and over-allocate. Low probability (accepts are manual/serial) but real. Fix: lock the requirement row / re-check remaining inside the transaction.

## B. Prod checks / actions — you run these (I supply exact SQL)

### B1 — Can a 2nd seller still bid after the 1st? _(High — your flagged issue)_
Run `select pg_get_viewdef('public.seller_requirements_view', true);`. If it filters to `status='posted'` only, requirements vanish from other sellers once the first bid flips status → they can't bid. If so, the fix is to include `matching` / `ready_to_order` in the view (I'll write it). **This is the most likely cause of "several sellers couldn't bid."**

### B2 — `buyer_deals_view` fee opacity _(Medium, moat verify)_
Confirm its columns include no `spread`, `seller_cost`, `avg_seller_price`, or `total_spread` (a `total_invoice_usd` is fine). Query in the audit doc. _(In progress — visible columns so far are clean.)_

### B3 — Delete the 3 old webhooks _(Ops)_
Database → Webhooks → delete `notify-offers`, `notify-rounds`, `notify-deals`. Keep `notify-all`.

### B4 — Confirm one notification email actually sends _(Ops — still unverified end-to-end)_
Trigger a counter → Edge Functions → `thrivts-notify` → Logs shows `sent:` → Resend shows Delivered. This is the one path we've never seen succeed.

## C. Confirmed healthy — no action
- **Multi-seller bidding works:** unique key is `(requirement_id, seller_id)`; accept sums committed pcs, rejects over-allocation, supports partial fills.
- **Stale-state guards:** you cannot counter or respond to an already-accepted bid (checked both sides).
- **Same-seller re-bid** now revises instead of crashing (duplicate-key fix).
- **Moat 1 & 2 pass** in every buyer-facing surface I can see.
- **Sellers don't see buyer identity; buyers don't see seller identity.**

## Suggested order
1. **B1** (run the viewdef now — settles your bid worry; may add a quick view fix).
2. **A1** (fast, visible cleanup).
3. **B2 / B3 / B4** (quick prod checks).
4. **A2** (reliability logging + reconciliation).
5. **A4**, then **A3** (hardening — the careful ones, done last and tested alone).
