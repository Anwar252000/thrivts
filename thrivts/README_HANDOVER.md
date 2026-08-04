# Thrivts — Platform Handover

Everything needed to (a) deploy the current live platform and (b) hand a clean starting point to the developer for the .NET/React migration.

**Stack:** Supabase (Postgres + Edge Functions + Auth) + static HTML portals hosted on Netlify. Supabase project ref: `wdtzsmvciysevswqnklo`.

**Two non-negotiable business rules (the "moat") — must survive the migration:**
1. **Seller anonymity** — a buyer must never be able to identify a seller (only an anonymous `Seller_xxxx` alias + tier). Enforced at the database level today.
2. **Fee opacity** — a buyer sees one all-in price only; the seller/platform fee split is never exposed.

---

## 1. Deploy to Netlify (`01_deploy_to_netlify/`)

These are the **latest** portal files — every fix through the final QA pass is in them.

- `buyer.html` → deploy to `buyers.thrivts.com`
- `seller.html` → deploy to `sellers.thrivts.com`
- `admin.html` → deploy to the admin URL

**How:** upload/replace each file in its Netlify site (or push to the connected repo and let Netlify build). After deploying, hard-refresh each portal and confirm the version is live. Nothing here needs a build step — they're self-contained HTML.

> Note: I could not deploy these for you (no hosting access). Whoever owns Netlify must upload them. Several past QA "bugs" were already-fixed issues still live on old builds — so **deploy these first**, then re-verify.

---

## 2. Database (`02_database/`)

**The live Supabase database is the source of truth.** Its schema and functions were built and then repeatedly fixed in place. Before the migration, **export the live schema + functions to version control** (this is step 1 of the dev's own migration plan, and it closes a real risk — right now the model lives only inside the live project).

Suggested export (run locally):
```
pg_dump --schema-only "postgresql://postgres:<pwd>@db.wdtzsmvciysevswqnklo.supabase.co:5432/postgres" > thrivts_schema.sql
```
Or use the Supabase dashboard's schema export. The complete set of ~90 backend functions was also dumped during the audit (ask Afnan for `functions_dump.csv`).

### `originals/` — the base build (reference, already applied)
`PART2` (anonymous seller identity), `PART4` (fee-inclusive bids + buyer/seller views), `PART8A/8B` (notifications + negotiation), `STEP2` (platform fee engine), `FIX4` (accept-bid/deal creation), `SECURITY_hardening`, plus admin-delete and commission-enum fixes. `RESET_platform_test_data.sql` wipes test data — **do not run in production.**

### `fixes_applied/` — changes made during the QA/hardening pass (already applied to prod)
Numbered in the order applied. This is a **changelog**, not a replay script — the live DB already has them. Read them to understand current behavior. Key ones and supersessions:

| # | File | What it does |
|---|------|--------------|
| 01 | seller_own_bids | Rebuilt seller bid view to expose negotiation columns (buyer couldn't-see-counters bug). |
| 02–04 | notifications_rls / push_notification / notifications_notnull | Fixed the notification chain: correct RLS scoping, writer populates all required columns, relaxed hidden NOT NULLs. This is why the bell + emails work. |
| 05–06 | admin_notifications / seller_deal_notification | Admin bell for signups/deals. **06 superseded by #10** (dropped the duplicate seller-deal trigger). |
| 07 | seller_revise_bid | Seller re-quote no longer crashes on duplicate key; revises in place. |
| 08–09 | A2 / A4 | Deal-notify dedupe + logging; accept-concurrency row lock. **Both superseded by #10.** |
| **10** | **deal_unification** | **Current accept/deal logic.** One shared `finalize_bid_to_deal` used by buyer-accept AND seller-accepts-counter (no double confirmation), sequence-based deal numbers. Supersedes 06/08/09. |
| 11 | cancel_cascade | Cancelling a deal frees the bid, reverts the requirement, notifies seller+buyer **with reason**, admin dot on cancel/settle. |
| 12 | unverify_kyc | Adds the missing `unverify_seller_kyc` RPC. |
| 13 | deal_prefix_and_cleanup | Unifies deal numbers to `DEAL-`; QA test-data cleanup. |
| 14 | tag_alignment | `current_seller_tags()` now reads the admin-managed `tags` column (controls seller requirement visibility). |
| **15** | **F1_rls_hardening** | **Moat hardening.** Scopes `buyer_visible_bids` / `seller_own_bids` to `auth.uid()` so a crafted API call can't cross tenants. |

### `thrivts-notify-index.ts`
The Supabase Edge Function that turns a `notifications` row into an email via Resend. Deployed as the `thrivts-notify` function; the `notify-all` database webhook (on `notifications` INSERT) triggers it. Requires secrets `RESEND_API_KEY`, `RESEND_FROM`, and `NOTIFY_SECRET` (must match the webhook's `x-notify-secret` header).

---

## 3. Reference docs for the migration (`03_reference_docs/`)

- **THRIVTS_FLOW_MATRIX.md** — every flow (onboarding → bid → negotiate → deal → settle/cancel, offers, notifications) with steps + the DB check that proves each works. Use this as the acceptance-test spec for the rebuild: the new system must pass the same matrix.
- **THRIVTS_AUDIT_AND_TEST_SCRIPT.md** — security/architecture audit; documents the moat status, the RLS/views situation, and known reliability gaps. The migration should encode the moat as typed, role-specific API responses (the dev's plan already does this).
- **THRIVTS_FIX_TASKLIST.md** — the running issue list and what's resolved.

---

## 4. Outstanding items

**Infra (Afnan):**
- Get **off NANO** compute (Micro is free on Pro; Small for launch headroom) — this was behind the load-time 503s.
- Confirm **one notification email sends end-to-end** (Edge Function logs show `sent:`, Resend shows Delivered).
- **Export the schema to version control** (see §2) — do before/at the start of the migration.

**Post-launch fast-follows (small, non-blocking):**
- Chip-based tag editor (comma-separated input corrupts tags that contain commas).
- Audit-log "Kyc" → "KYC"; Premium button label updating in place.
- Finish or hide the partial French/Urdu localization.
- Buyer/Seller profile-edit flow (profiles are currently read-only).
- Category edit/delete UI (currently add-only).

**For the migration specifically:**
- Preserve the two moat rules — bake them into role-specific API DTOs and add explicit tests that a buyer response never contains seller identity and vice versa.
- Reuse **THRIVTS_FLOW_MATRIX.md** as the cutover acceptance test, portal by portal.
- The current platform should be **frozen to bug-fixes only** while the rebuild proceeds; new features go into the new stack once it owns a given portal, cut over with a parallel run.

---

_Current status: full QA pass across all three portals with no HIGH-severity blockers; core lifecycle verified end-to-end; the moat is enforced at the database level. Launch-ready pending the infra items above._
