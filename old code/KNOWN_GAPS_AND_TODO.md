# THRIVTS — KNOWN GAPS & TODO

What is missing, incomplete, or needs building — so the new team isn't surprised.

---

## A. CRITICAL / DO FIRST (de-risk)

- [ ] **Export the full database schema from Supabase and put it in version control.**
      Tables, views, RLS policies, RPC function bodies, triggers, enums. Use
      `supabase db dump --schema public` (roles → schema → data) or the Dashboard.
      *Right now the schema exists ONLY in the live project. This ZIP has just 3 migrations.*
- [ ] **Audit all Row-Level Security (RLS) policies** — this is the security backbone and is
      unverified. Confirm cross-tenant isolation (buyer↔buyer, seller can't see buyer identity,
      agency sees only own data, influencer sees only own commissions, admin gated by
      `profiles.role='admin'`).
- [ ] **Upgrade Supabase to Pro** before launch traffic; enable **backups** and **billing alerts**.
- [ ] Put all portal code + SQL into a **Git repository** (currently hand-managed files).

---

## B. KNOWN INCOMPLETE FEATURES

These were noted as unfinished during development:

- [ ] **Admin dashboard live data** — some admin analytics/dashboard tiles may still use
      placeholder or partial data rather than fully live Supabase aggregates. Verify each tile.
- [ ] **AI agent** — referenced as a planned admin feature; not built.
- [ ] **Dedicated client portals for specific buyers (Frankie / Ethan)** — these exist as separate
      hardcoded files *outside this Thrivts package* (part of the Vintage Hub side). Not included
      here; mentioned so you know they exist if referenced.
- [ ] **Analytics with real data** across portals — some charts/【KPIs may need wiring to live queries.
- [ ] **Messaging** (`message_threads`) — buyer↔admin messaging exists; confirm the full
      send/receive/unread flow and whether sellers/agencies have messaging.
- [ ] **Notifications** — no automated email/push on deal status changes was confirmed built. A
      `thrivts-notify` edge function was discussed but treat as NOT present unless found in Supabase.

---

## C. INFRASTRUCTURE / OPS TODO

- [ ] Add **error monitoring** (e.g. Sentry) to all portals.
- [ ] Add **uptime monitoring** on the 5 live URLs + the Supabase health.
- [ ] Set up **staging** (a second Supabase project + Netlify branch deploys) so changes aren't
      tested in production.
- [ ] **CI/CD** — even a simple GitHub → Netlify auto-deploy per portal.
- [ ] Confirm **Resend** domain verification + SPF/DKIM for `thrivts.com` so auth emails don't
      land in spam.
- [ ] Confirm Supabase Auth **"Confirm email"** setting matches the intended flow (was noted as
      needing to be OFF for the session-based RLS flow — verify this is still correct).

---

## D. CODE-QUALITY / TECH-DEBT TODO (non-blocking)

- [ ] Deduplicate shared helpers (`esc()`, `fmtUSD()`, date formatters, clipboard, reset-password
      overlay) — currently copy-pasted across portals.
- [ ] Replace remaining `select('*')` with named columns on hot queries (egress + clarity).
- [ ] Extract embedded base64 fonts to `assets/` + `font-display:swap` (lighter, cacheable) —
      deploy as folders and verify on live URLs.
- [ ] Consider whether the single-file-per-portal approach should become a component-based build.
      It's currently a strength (zero build, easy deploy); only refactor if team velocity demands it.
- [ ] Add pagination UI consistently (admin has "Load more"; other portals cap silently).

---

## E. PRODUCT / BUSINESS-LOGIC TO CONFIRM WITH AFNAN

- [ ] Exact **seller payment timeline** (the seller intro says "paid on QC + dispatch" — confirm
      this matches what the deal lifecycle + RPCs actually enforce).
- [ ] **Commission rules**: agency 30% display vs 0.30 decimal; influencer 5% recurring for 12
      months; release timing (20–30 days post-delivery). Confirm the DB enforces these, not just
      the UI copy.
- [ ] **Currency**: base is USD. The partners landing was just converted GBP→USD. Confirm every
      portal + every RPC consistently uses USD (there are `exchange_rates` and `shipping_rates`
      tables — understand how/where they're applied).
- [ ] Partner referral: buyer gets 5% off first order, partner earns 5% for 12 months from first
      order — confirm the `apply_referral_code` RPC implements exactly this.
