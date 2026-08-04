# THRIVTS — DEPLOYMENT & INFRASTRUCTURE

Everything about where things live and how to ship. No secrets in this file — request access
credentials from Afnan.

---

## HOSTING MAP

| Portal (folder) | Netlify site | Live domain | Must deploy as folder? |
|-----------------|--------------|-------------|------------------------|
| `1_BUYER_SITE` | (buyer site) | `buyers.thrivts.com` | **Yes** — has `_redirects` + `assets/img/hero.jpg` |
| `2_ADMIN_SITE` | (admin site) | `vhq-backstage-7k2.thrivts.com` | Single file OK (folder fine) |
| `3_AGENCY_SITE` | (agency site) | `agency.thrivts.com` | Single file OK |
| `4_PARTNERS_SITE` | (partners site) | `partners.thrivts.com` | **Yes** — `_redirects`, `login.html`, `assets/` |
| `5_SELLER_SITE` | (seller site) | `sellers.thrivts.com` | Single file OK |

- **Static hosting only.** No server, no build step. Deploy = upload the folder.
- The admin domain (`vhq-backstage-7k2...`) is deliberately obscure — it's a staff console.
  The portal also sets `noindex,nofollow` so it stays out of search engines.

## `_redirects` files
- **Buyer** and **Partners** each ship a Netlify `_redirects` file. Partners routes the login
  path to `login.html`. If you deploy only the HTML and forget `_redirects`, routing breaks.

## DNS
- `thrivts.com` DNS via **AWS Route 53**, pointing each subdomain (CNAME) at its Netlify site.

---

## BACKEND — SUPABASE (`thrivts-prod`)

| Item | Value |
|------|-------|
| URL | `https://wdtzsmvciysevswqnklo.supabase.co` |
| Ref | `wdtzsmvciysevswqnklo` |
| Region | `eu-west-1` (Ireland) |
| Plan | Free (upgrade to Pro before launch — see AUDIT_REPORT §B3) |

- **Anon key**: public by design, hardcoded in each portal's CONFIG block. Fine — RLS is the guard.
- **service_role key**: NOT in any frontend file. Must never be. Use only in server-side/edge
  contexts (e.g. a future admin automation running with elevated rights).
- **Auth emails** sent via **Resend** SMTP from `noreply@thrivts.com`. Verify SPF/DKIM.
- **Confirm-email setting**: was configured OFF to support the session-based RLS flow — verify
  this is still the intended behaviour.

## RUNNING THE SQL MIGRATIONS

In Supabase → SQL Editor, run in order (all idempotent):
1. `SQL_RUN_IN_SUPABASE/1_partner_applications.sql`
2. `SQL_RUN_IN_SUPABASE/2_agency_attribution_v2.sql`
3. `SQL_RUN_IN_SUPABASE/3_performance_indexes.sql` — after running, read the NOTICES: it prints
   `OK <index>` for each created and `SKIP <index>: ...` for any table/column that didn't match.
   Any SKIP tells you where the live schema differs from what the code expects — investigate those.

---

## LOCAL / DEV NOTES

- To edit: open the portal HTML in any editor. No `npm install`, no build.
- To test: **must be on a real HTTPS URL** (Netlify deploy or `netlify dev` / a static server on
  https). Supabase calls fail inside sandboxed preview iframes and from `file://`.
- Recommended: set up a **staging Supabase project** + Netlify branch/preview deploys so you
  never test against production data.

## DEPLOY VERIFICATION TIP
After deploying, confirm the new build is live (Netlify deploy log / a build-stamp in the footer)
rather than trusting drag-drop feedback — stale caches have bitten this project before.

---

## RECOMMENDED NEXT INFRA STEPS (from KNOWN_GAPS)
1. Upgrade Supabase to Pro + enable backups + billing alerts.
2. Move Thrivts to its own Supabase org (separate quota from the Vintage Hub Ops app).
3. Git repo + Netlify Git integration (auto-deploy per portal).
4. Staging environment.
5. Error monitoring (Sentry) + uptime checks.
