# THRIVTS — QA & SCALE AUDIT REPORT

This documents the quality/security/scale work already done on the codebase, and — just as
importantly — **what is still weak or unverified**. Written for the incoming dev team so you
know exactly what state each portal is in.

Validation standard used throughout: every JS block passes `node --check`, and `<div>`/`</div>`
tags are balanced, after each change.

---

## PART A — WHAT HAS BEEN HARDENED (per portal)

### Seller portal — full pass (33 fixes)
XSS escaping across all render paths; undefined `--white` CSS var fixed (was causing transparent
modals); full English/Urdu i18n + RTL; stuck-button resets; branded toasts; Esc/backdrop modal
close; `attrJSON()`/`safeUrl()` guards; payout/null fixes; Supabase-CDN-fail screen; ipify
timeout; accessibility; show-password; correct support number; landing feed capped.

### Buyer portal — full pass (40 fixes)
Duplicate form-listener bug fixed (form was firing multiple times); missing price/currency
listeners added; category weight was hardcoded, now data-driven; language toggle selector/class
bugs fixed; `esc()` applied across every render function (was zero escaping); dispute submit
button-lock + reset; "raise issue" hidden on closed/disputed deals; Lucide + Supabase CDN guards;
CSV-injection sanitisation; null qty/price guards; toast/aria/focus/dialog a11y; support links;
meta/OG tags; landing activity feed capped at 20 with bounded height.

### Admin portal — full pass (45 fixes) + deeper scale pass (5)
~50 `esc()` XSS wraps across every table (buyers, sellers, agencies, requirements, deals,
commissions, messages, audit, disputes); the **dispute-badge refresh bug** (resolving a dispute
didn't clear its nav badge) fixed; `toast()` HTML-injection → safe text node; CSV formula-injection
sanitised + BOM; clipboard fallback; Supabase/Lucide CDN guards; double-submit guards on
create-agency/influencer/offer; badge-poll timer cleared on logout; Escape-close; meta
noindex/theme/description; per-section page titles; inputmode; show-password.
**Deeper scale pass:** accurate GMV that pages past the 1,000-row cap (dashboard total was
silently undercounting); "Showing X of Y" + Load-more on all 8 lists (they were silently capped);
error surfacing on 5 loaders that were swallowing failures and showing false "empty"; badge poll
pauses on hidden tab.

### Agency portal — full pass (18 fixes)
Double-`$$` on money KPIs fixed; status-pill colors (red/blue/gold classes were referenced but
never defined → everything rendered gray) added; **no mobile nav at all** (sidebar was
`display:none` under 680px with no replacement) → horizontal scroll nav; both auth+portal screens
stacked on first paint → portal hidden by default; tables now scroll on mobile; error messages
escaped (×4); session-check network failure handled; clipboard fallback; forgot-password prefill
was reading other portals' input IDs; Enter submits from email; show-password; Esc/backdrop close
+ dialog a11y; noindex/theme/description; per-page titles; WhatsApp support link; 500-row
truncation notice.

### Partners portal — full pass (14 fixes)
**Currency mismatch (major):** the public landing advertised earnings in **£ GBP** (31 places,
incl. the calculator) while the commission ledger + dashboard pay **USD** — converted all to USD.
`og:image` made absolute (relative path meant shared links showed no preview — critical for a
page built to be shared by influencers); anti-spam honeypot + min-time on the public application
form (was wide open to bot submissions into your DB); the auth-trap fixed (a non-partner who
logged in got stuck, signed-in, with no sign-out visible); 3 dashboard loaders were swallowing
errors → now surfaced; XSS escaping on buyer labels + commission status; overview RPC made
tolerant of array-vs-object shape; sign-in double-submit lock; clipboard fallback; mobile table
scroll; noindex/meta; show-password; consistent 2-dp money formatting.

### Cross-cutting scale audit (all portals) — for 100k+ users
- **No realtime/websocket subscriptions anywhere** — confirmed. (This is the #1 thing that kills
  Supabase apps at scale via connection limits; the app correctly avoids it.)
- **Buyer landing hero image (380KB) + fonts were embedded in the HTML** → every visitor
  downloaded ~400KB before render, re-downloaded on every deploy. Hero extracted to `assets/`
  (10x lighter transfer). *Fonts were later restored inline at Afnan's request so the artifact
  preview renders correctly — see note below.*
- **Buyer landing feed was polling the DB every 25s per visitor, even on hidden tabs** → changed
  to 60–80s with jitter, skips hidden tabs. (At 3k concurrent visitors that was ~120 req/s of
  pure cosmetic load.)
- **Unbounded list queries** capped across portals (Supabase silently caps at 1,000 rows).
- Admin search inputs debounced (were rebuilding whole tables on every keystroke).
- `3_performance_indexes.sql` added — indexes matching every real query pattern.

---

## PART B — WHAT IS STILL WEAK / UNVERIFIED (read carefully)

These are NOT yet fixed. Prioritise them.

1. **RLS policies are unverified from the frontend side.** The whole security model depends on
   Row-Level Security + `SECURITY DEFINER` RPCs in Postgres. The frontend audit could not test
   these. **The new team must audit every RLS policy directly in Supabase** — confirm a buyer
   cannot read another buyer's rows, a seller cannot see buyer identity, an agency only sees its
   own attributed data, etc. This is the single most important security task and it is outside
   what a frontend pass can verify.

2. **Full schema/DDL is not version-controlled.** Tables, views, RLS, and most RPC bodies live
   only in the live Supabase project. There is no migration history. If the project is lost or
   corrupted, it cannot be rebuilt from this ZIP. **Export and commit the full schema immediately.**

3. **Supabase is on the Free plan and was flagged "EXCEEDING USAGE LIMITS."** On Free, once a
   limit's grace period passes, the DB can go read-only or the API returns 402 — which at launch
   means every request fails. Upgrade to Pro ($25/mo) before driving real traffic. (Splitting
   Thrivts into its own Supabase org — separate from the Vintage Hub Ops app — reclaims a full
   quota and may clear the badge short-term, but Pro is the real fix.)

4. **`select('*')` on hot paths.** Several loaders still select all columns. Named-column selects
   would cut egress meaningfully (matters on Free). Not yet done.

5. **Anon key is shared across all portals and hardcoded.** This is *correct* for Supabase (anon
   key is public, RLS is the guard) — but it means security rests entirely on point #1. Do not
   treat the hardcoded key as a vulnerability; do treat weak RLS as one.

6. **No automated tests, no CI, no error monitoring.** No Sentry/logging. At scale you'll want
   client error reporting and uptime monitoring.

7. **No database backups on Free plan.** Set up backups (Pro gives daily; or a GitHub Action +
   external storage on Free).

8. **Business logic is spread between Postgres RPCs and frontend JS.** Some validation happens
   only client-side (e.g. form checks). Anything security-relevant must be enforced in the
   RPC/RLS layer too, never trusted from the browser. Audit for client-only enforcement.

9. **Single-file architecture** is fast to ship but hard to maintain at team scale (no components,
   no shared modules, duplicated helpers like `esc()`/`fmtUSD()` across portals). Fine for now;
   a future refactor to a component framework is a judgment call for the new team — weigh it
   against the simplicity that's currently working.

10. **Preview vs production font note:** Buyer/Admin/Agency/Seller embed fonts as base64 inline
    (kept inline deliberately so previews render). This inflates each HTML file (Admin ~344KB).
    Extracting fonts to `assets/` + `font-display:swap` would lighten transfer and enable browser
    caching — a safe optimisation the new team can make, as long as they deploy the folder and
    verify on the live URL (not a preview).

---

## PART C — SUGGESTED PRIORITY ORDER FOR THE NEW TEAM

1. Get access (Supabase, Netlify, DNS, Resend).
2. **Export full schema + RLS from Supabase; commit to Git.** (De-risks everything.)
3. **Audit RLS policies** portal-by-portal (the real security work).
4. Upgrade Supabase to Pro; set up backups + billing alerts.
5. Run `3_performance_indexes.sql`; capture its OK/SKIP notices to spot schema drift.
6. Add error monitoring (Sentry) + uptime checks.
7. Egress trims (`select('*')` → named columns) if staying cost-sensitive.
8. Decide on long-term architecture (keep single-file vs. refactor).
