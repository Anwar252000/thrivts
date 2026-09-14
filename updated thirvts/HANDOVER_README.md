# THRIVTS — Developer Handover

## ⚠️ READ THIS FIRST — status of these files

These are the **current working files as of this handover**, but note:

- The **HTML files** (buyer/seller/admin) are the last versions produced in the
  build sessions. **Verify against what's actually deployed on Netlify** before
  treating them as source of truth — open each live portal, open the browser
  console, and check the build marker matches:
    - buyer.html  → console: `PO panel v7-UI`
    - seller.html → console: `THRIVTS SELLER BUILD v11-PILLNAV`
    - admin.html  → console: `THRIVTS ADMIN BUILD v10-DISPATCH-DIRECT`
  If the deployed markers differ, the deployed version is newer — pull from Netlify.

- The **SQL files** are the migrations/fixes applied to the Supabase database
  over time. Several are **diagnostics** (GET_*, DIAGNOSE_*, TEST_*) — those were
  read-only investigation, NOT schema changes. The ones that changed the DB are
  listed under "Applied migrations" below.

- **These static HTML files are the current production platform.** A separate
  full rewrite (.NET/React) is reportedly in progress by the dev team — this
  handover is the existing platform, for reference/parity.

---

## STACK
- **Backend:** Supabase (project `wdtzsmvciysevswqnklo`, prod branch `thrivts-prod`)
- **Frontend:** three single-file static HTML apps on Netlify
    - buyers.thrivts.com  (buyer portal)
    - sellers.thrivts.com (seller portal)
    - vhq-backstage-7k2.thrivts.com (admin)
- **Email:** Resend (via `thrivts-notify` Edge Function + `notify-all` DB webhook)
- Marketing site thrivts.com is separate.

## CORE BUSINESS RULES (do not break)
- **Seller anonymity:** buyers never see seller identity.
- **Fee model (Model A):** buyer pays quoted price P; seller receives P − $0.70;
  platform keeps $0.70; buyer never sees the fee.
  `buyer_price = coalesce(current_price_usd, proposed_price_usd)`.
- **Bank details (invoices/PO):** THE THRIVTS MARKETPL / Acct 1660007766470 USD /
  SWIFT BMRIIDJA / Bank Mandiri.

## KEY SCHEMA NOTES
- Bidding lives in `requirement_seller_offers`, `seller_responses`, `offer_rounds`
  (there is NO `bids` table).
- Deal id is exposed as `deal_id` in seller/buyer views (not `id`).
- `buyers.approval_status` (text) tracks approval — NOT a `status` column.
- `profiles` has no `status` column; it has `approval_status`, `role`, `language_pref`.
- `language_pref` enum = only `en` / `fr`.
- `shipping_rates.countries` is a Postgres text[] (not jsonb).
- Signup path: buyer form calls `auth.signUp()` only; DB trigger
  `on_auth_user_created → handle_new_user()` fans metadata into profiles + buyers.

## THE FULFILMENT FLOW (buyer↔seller↔admin, all connected)
Bid accepted → deal `confirmed` → PO auto-issued → `awaiting_payment` →
admin "Mark payment received" → `paid` (both notified) → seller sees countdown
+ "Mark order ready" → admin "Ready to pick" filter → Mark In Fulfillment →
Dispatch (tracking form: number/courier/link/vessel/container/ETA) → `dispatched`
(both notified, buyer sees dated timeline + tracking) → Delivered → Settled.
AfterShip live-tracking is scaffolded but not wired (delivered set manually).

## APPLIED DB MIGRATIONS (the ones that changed the database)
Run in this order on a fresh DB to reproduce current state:
- PO_1_table_and_issue.sql .. PO_6_admin_set_status.sql  (purchase order system)
- PO_7_COMBINED.sql            (seller fulfilment: paid-notifies-both, order-ready RPC, seller view)
- PO_8_buyer_view.sql          (buyer deadline fields on buyer_deals_view)
- PO_9_auto_issue.sql          (auto-issue PO on deal creation)
- PO_10_no_buyer_settled_notif.sql (buyer skipped on 'settled' notification)
- PO_11_categories.sql         (16 authoritative categories)
- POPULATE_shipping_rates.sql  (UK 4.1 / EU 5.5 / US 7.5 / RoW 5.5 GBP air; sea $0.60/kg flat in app)
- CLEANUP_old_categories.sql   (removes 6 legacy categories → clean 16)
- FIX_delete_buyer_v3.sql      (admin cascade delete of a buyer)
- Signup trigger fix (applied live, see "Known fixes" below)

## DIAGNOSTIC-ONLY FILES (safe to ignore — read-only investigation)
GET_*, DIAGNOSE_*, TEST_*, and superseded versions (FIX_delete_buyer.sql /_v2,
PO_7_fulfillment_flow.sql, PO_7a/7b, RESET_go_live.sql). Kept for history only.

## KNOWN FIXES APPLIED LATE (in DB, not in these files)
1. **Signup trigger bug (critical):** `handle_new_user()` was failing silently on
   invalid `language_pref` values (`En` capital-E from the buyer form, `ur` from
   seller). Patched to normalise language (`lower()`, non en/fr → `en`) and to
   log failures to a new `signup_failures` table instead of swallowing them.
2. **partner_applications RLS:** table had RLS ON with zero policies; added
   insert(anon)/select(admin)/update(admin). NOTE: the *buyer* form does NOT use
   this table — it's the influencer/partner path. Policies are correct for that.
3. **Go-live reset:** RESET_go_live_v2.sql wiped test transactional data +
   non-admin profiles; kept admin, categories, shipping_rates, config.
   NOTE: it does NOT delete auth.users — those were cleared manually in the
   Supabase dashboard.

## OUTSTANDING BEFORE REAL LAUNCH
1. **Custom SMTP via Resend for Auth emails** — Supabase built-in email is capped
   ~4/hour and WILL fail real signups. Set Authentication → Email → SMTP → Resend.
2. Re-enable "Confirm email" if toggled off during testing.
3. Password-reset redirect → set Auth URL Configuration Site URL to
   https://buyers.thrivts.com (remove any localhost).
4. Optional: admin-notify trigger on new buyer application (Step D — NOT installed).
5. Rotate/confirm NOTIFY_SECRET (webhook header `x-notify-secret` must equal the
   Edge Function `NOTIFY_SECRET` env value).
6. Full end-to-end test on clean DB: signup → approve → post requirement → bid →
   accept → pay → fulfil → dispatch → delivered.
