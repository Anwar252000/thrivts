# THRIVTS — DEVELOPER HANDOVER

**Prepared for:** incoming development team
**Product owner:** Afnan (Vintage Hub / Thrivts)
**Package date:** July 2026

> Read this file first. It is the map to everything else in this ZIP.
> Then read `AUDIT_REPORT.md` (what's been hardened + what's still weak) and
> `KNOWN_GAPS_AND_TODO.md` (what's missing / what to build next).

---

## 1. WHAT THRIVTS IS

Thrivts is a **B2B wholesale marketplace for vintage / secondhand clothing**. It connects
international **buyers** (European resellers, importers) with vetted **suppliers/sellers**
(grading houses) using an **invisible-broker model**:

- Buyers post **requirements** (what they want: category, grade, quantity, target price).
- Sellers (invitation-only, KYC-verified) submit **offers** — but never see buyer identity.
- Thrivts sits in the middle, matches them, and takes **30% of the spread** (the margin
  between buyer price and seller price). Buyer and seller identities never cross.
- **Agencies** and **influencers/partners** refer buyers and earn commissions.

**Deal lifecycle:** `match → confirmed → paid → in_fulfillment → dispatched → delivered → settled`
(with `cancelled` / `disputed` as off-ramps).

---

## 2. THE FIVE PORTALS (+ landing) — WHAT'S IN THIS ZIP

Every portal is a **single self-contained HTML file** (HTML + CSS + JS inline, no build step).
They talk directly to Supabase via the JS SDK loaded from CDN.

| # | Folder | File(s) | Who uses it | Deployed to |
|---|--------|---------|-------------|-------------|
| 1 | `1_BUYER_SITE/` | `index.html` + `_redirects` + `assets/img/hero.jpg` | Buyers | `buyers.thrivts.com` |
| 2 | `2_ADMIN_SITE/` | `index.html` | Thrivts staff (admin) | `vhq-backstage-7k2.thrivts.com` |
| 3 | `3_AGENCY_SITE/` | `index.html` | Agencies (referrers) | `agency.thrivts.com` |
| 4 | `4_PARTNERS_SITE/` | `index.html` (public landing + apply), `login.html` (dashboard), `_redirects`, `assets/` | Influencers/partners | `partners.thrivts.com` |
| 5 | `5_SELLER_SITE/` | `index.html` | Sellers/suppliers | `sellers.thrivts.com` |

Line counts (approx): Buyer 3,436 · Admin 4,883 · Seller 1,738 · Agency 851 · Partners landing 771 + dashboard 308.

**Note on the Partners portal:** `index.html` is the *public marketing + application* page.
`login.html` is the *authenticated partner dashboard*. The `_redirects` file routes `/login`
to the dashboard. This split is intentional.

---

## 3. TECH STACK (exact)

| Layer | Technology |
|-------|-----------|
| Frontend | **Vanilla HTML/CSS/JavaScript** — no framework, no build, no bundler. Each portal is one file. |
| Fonts | Buyer/Admin/Agency/Seller use **Power Grotesk** embedded as base64 woff2 inside the HTML. Partners uses Google Fonts (Space Grotesk / Inter / Space Mono). |
| Icons | Lucide (CDN, with a no-op shim fallback so a CDN miss doesn't crash the page). |
| Backend | **Supabase** (hosted Postgres + Auth + PostgREST auto-API + RPC functions). No custom server. |
| Auth | Supabase Auth (email + password). Sessions via the Supabase JS SDK. |
| DB access | Direct from browser via `@supabase/supabase-js@2` (CDN). Secured by **Row-Level Security (RLS)** + `SECURITY DEFINER` RPCs. |
| Hosting | **Netlify** (static hosting, one site per portal, drag-and-drop or Git deploy). |
| Custom domains | `*.thrivts.com` via Route 53 → Netlify. |
| Transactional email | **Resend** SMTP, sending from `noreply@thrivts.com` (used by Supabase Auth for password resets / confirmations). |

**There is no Node/Express/Python backend.** All business logic lives in Postgres
(RPC functions + RLS policies) inside Supabase. This is the single most important
thing for the new team to internalise.

---

## 4. SUPABASE PROJECT

| | |
|---|---|
| Project name | `thrivts-prod` |
| Project ref/ID | `wdtzsmvciysevswqnklo` |
| URL | `https://wdtzsmvciysevswqnklo.supabase.co` |
| Region | `eu-west-1` (West EU, Ireland) |
| Plan | **Free** (see AUDIT_REPORT — this is a launch risk) |
| Org | Being moved to a dedicated "Thrivts" org so its usage quota isn't shared with the Vintage Hub Ops app |

The **anon key** is hardcoded in each portal's `<script>` CONFIG block (this is normal for
Supabase — the anon key is public by design; security comes from RLS, not from hiding the key).
The **service_role key must NEVER be in frontend code** — it is not in any of these files, and must stay that way.

### Database surface actually used by the code

**Tables:** `profiles`, `buyers`, `sellers`, `agencies`, `influencers`, `requirements`,
`deals`, `deal_allocations`, `commissions`, `influencer_commissions`, `disputes`,
`seller_responses`, `requirement_seller_offers`, `offer_rounds`, `message_threads`,
`categories`, `audit_log`, `public_activity_feed`, `exchange_rates`, `shipping_rates`.

**Views:** `buyer_deals_view`, `seller_deals_view`, `seller_requirements_view`
(these exist to expose data to one side without leaking the other side's identity).

**RPC functions (business logic — all in Postgres):**
`get_public_activity`, `apply_referral_code`, `apply_agency_ref`, `get_agency_public_name`,
`accept_seller_offer`, `accept_seller_quote`, `post_offer_round`, `create_deal_from_match`,
`advance_deal_status`, `approve_seller`, `verify_seller_kyc`, `unverify_seller_kyc`,
`block_seller`, `unblock_seller`, `delete_seller`, `admin_accept_offer`, `admin_decline_offer`,
`admin_create_agency`, `admin_create_influencer`, `admin_block_buyer`, `admin_unblock_buyer`,
`admin_delete_buyer`, `admin_list_partner_applications`, `admin_set_partner_application_status`,
`get_agency_dashboard`, `get_influencer_overview`, `get_influencer_buyers`,
`submit_partner_application`.

⚠️ **The full DDL for these tables, views, RLS policies, and most RPCs is NOT in this ZIP** —
it lives in the live Supabase project. Only 3 SQL migration files are included (see §6).
**First task for the new team:** export the full schema from Supabase
(`supabase db dump` via CLI, or Dashboard → Database) and commit it to version control.
See `KNOWN_GAPS_AND_TODO.md`.

See `SCHEMA_AND_DATA_MODEL.md` for the per-portal table/RPC map.

---

## 5. WHICH PORTAL USES WHAT (quick reference)

- **Buyer:** profiles, buyers, requirements, buyer_deals_view, disputes, message_threads,
  categories, agencies, public_activity_feed, exchange_rates · RPCs: apply_referral_code,
  apply_agency_ref, get_agency_public_name, get_public_activity.
- **Admin:** profiles, buyers, sellers, agencies, deals, deal_allocations, commissions,
  requirements, requirement_seller_offers, offer_rounds, disputes, seller_responses,
  message_threads, categories, audit_log, shipping_rates, exchange_rates · 19 admin_* / seller /
  deal RPCs.
- **Agency:** agencies, buyers, deals, commissions, profiles · RPC: get_agency_dashboard.
- **Seller:** sellers, profiles, categories, seller_deals_view, seller_requirements_view,
  seller_responses, requirement_seller_offers, offer_rounds, public_activity_feed · RPCs:
  accept_seller_offer, post_offer_round, get_public_activity.
- **Partners:** influencers, influencer_commissions · RPCs: submit_partner_application,
  get_influencer_overview, get_influencer_buyers.

---

## 6. SQL MIGRATIONS IN THIS ZIP (`SQL_RUN_IN_SUPABASE/`)

Run these in the Supabase SQL editor **in order**. All are idempotent (safe to re-run).

| Order | File | What it does | Status |
|-------|------|--------------|--------|
| 1 | `1_partner_applications.sql` | Creates the `partner_applications` table + `submit_partner_application()` RPC for the public partner apply form. | Run once before Partners portal goes live |
| 2 | `2_agency_attribution_v2.sql` | Agency referral attribution (the `/a/CODE` link → buyer tracking). | Run once |
| 3 | `3_performance_indexes.sql` | **Schema-aware** performance indexes matching every query the portals run. Checks each table/column exists before creating; skips (with a NOTICE) anything that doesn't match. | Run before scaling; safe anytime |

These are the ONLY migrations captured here. The rest of the schema predates this handover
and must be dumped from the live project (see §4 warning).

---

## 7. DEPLOYMENT — HOW TO SHIP A CHANGE

Each portal folder maps to one Netlify site.

1. Edit the portal's HTML file.
2. Deploy its **entire folder** to the matching Netlify site (drag-and-drop the folder, or connect Git).
   - **Buyer** and **Partners MUST be deployed as folders** — they contain `_redirects` and an `assets/` folder. Deploying only `index.html` breaks routing and images.
   - Admin / Agency / Seller are single files (fonts are embedded), but deploying the folder is still fine.
3. Because everything is static + inline, there is **no build step**. What you deploy is what runs.

**Testing note:** these portals talk to Supabase, which is blocked inside sandboxed
preview iframes. **Always test on the real Netlify URL**, not a local `file://` or an
embedded preview.

---

## 8. ACCOUNTS / CREDENTIALS THE NEW TEAM WILL NEED (request from Afnan)

Not included in this ZIP for security. The team needs access to:
- **Supabase** (`thrivts-prod`) — owner/admin invite.
- **Netlify** — the 5 sites.
- **Route 53 / DNS** for `thrivts.com`.
- **Resend** account (email sending).
- Admin login for the Admin portal (an `afnan@thrivts.com`-type account with `profiles.role='admin'`).

---

## 9. FILE INDEX OF THIS ZIP

```
THRIVTS_HANDOVER/
├── HANDOVER_README.md            ← you are here (start here)
├── AUDIT_REPORT.md               ← full QA audit: what's fixed, what's weak
├── KNOWN_GAPS_AND_TODO.md        ← what's missing / build-next / risks
├── SCHEMA_AND_DATA_MODEL.md      ← tables, views, RPCs, per-portal map
├── DEPLOYMENT_AND_INFRA.md       ← domains, hosting, email, DNS, env
├── code/                         ← ALL portal source (the 5 portals)
│   ├── 1_BUYER_SITE/
│   ├── 2_ADMIN_SITE/
│   ├── 3_AGENCY_SITE/
│   ├── 4_PARTNERS_SITE/
│   └── 5_SELLER_SITE/
└── SQL_RUN_IN_SUPABASE/          ← the 3 idempotent migrations
```
