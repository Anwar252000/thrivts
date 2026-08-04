# THRIVTS — SCHEMA & DATA MODEL

Reconstructed from the queries the portals actually run. **This is the usage surface, not the
authoritative DDL** — the authoritative schema must be exported from the live Supabase project
(`thrivts-prod`). Use this as a map while you do that export.

---

## CORE ENTITIES

| Table | Purpose | Key relationships |
|-------|---------|-------------------|
| `profiles` | One row per auth user. Holds `role` (buyer/seller/agency/admin), name, email, approval_status, is_active. `profiles.id = auth.uid()`. | 1:1 with auth.users; drives all role gating |
| `buyers` | Buyer company records | `attributed_to_agency` → agencies; referral link → influencers |
| `sellers` | Supplier/grading-house records (KYC, tiers, categories) | invitation-only, approval-gated |
| `agencies` | Referral agencies | `agency_code`, `commission_rate`, `agency_code` used in `/a/CODE` links |
| `influencers` | Partner/influencer accounts | `referral_code`, `influencer_code`, `commission_rate` (default 0.05) |
| `requirements` | Buyer demand posts | → buyers, categories; matched to seller offers |
| `deals` | A matched, in-progress or completed transaction | → buyers, agencies, requirements; has `status`, `total_invoice_usd`, `total_spread_usd`, `total_quantity_pcs` |
| `deal_allocations` | Line-item / seller allocations within a deal | → deals, sellers |
| `commissions` | Agency commission ledger | → agencies, deals; `commission_rate`, `commission_amount_usd`, `status` (pending/ready_to_release/released/cancelled), `release_due_at`, `released_at`, `paid_at` |
| `influencer_commissions` | Influencer/partner commission ledger | → influencers; `order_value_usd`, `rate`, `amount_usd`, `status` (accrued/released/reversed) |
| `disputes` | Buyer-raised issues on deals | → deals, buyers |
| `seller_responses` | Seller replies to requirements | → sellers, requirements |
| `requirement_seller_offers` | Offers sent to sellers for a requirement | → requirements, sellers |
| `offer_rounds` | Negotiation rounds on an offer | → offers |
| `message_threads` | Buyer↔admin messaging | `buyer_id`, `participant_id`, `unread_for_buyer`, `unread_for_admin`, `last_message_at` |
| `categories` | Product category taxonomy | `is_active`, `display_order`, `name`, `name_fr` |
| `audit_log` | Admin action audit trail | → profiles (actor) |
| `public_activity_feed` | Anonymised recent-activity feed for landing pages | powers the marketing "live activity" |
| `exchange_rates` | FX reference | used for currency display |
| `shipping_rates` | Shipping cost reference | used in deal/quote math |

## VIEWS (identity-protecting projections)

| View | Exposes to | Deliberately hides |
|------|-----------|--------------------|
| `buyer_deals_view` | Buyers — their own deals | seller identity / spread internals |
| `seller_deals_view` | Sellers — their own deals | **buyer identity** and buyer-side pricing |
| `seller_requirements_view` | Sellers — open requirements they can bid on | buyer identity |

> The invisible-broker guarantee (buyer ↔ seller never see each other) is enforced HERE and in
> RLS. Any change to these views is security-critical.

---

## RPC FUNCTIONS (business logic lives in Postgres)

### Public / buyer-facing
- `get_public_activity(p_limit)` — anonymised landing feed (hottest anonymous endpoint).
- `submit_partner_application(...)` — public partner apply form (anon-executable).
- `apply_referral_code(...)` / `apply_agency_ref(...)` — attach a buyer to an influencer/agency at signup.
- `get_agency_public_name(...)` — resolve an agency code to a display name for the `/a/CODE` landing.

### Seller-facing
- `accept_seller_offer(...)`, `accept_seller_quote(...)` — seller accepts an offer.
- `post_offer_round(...)` — submit a negotiation round.

### Agency / influencer dashboards
- `get_agency_dashboard()` — agency KPIs (buyers, deals, commission buckets).
- `get_influencer_overview()` — partner KPIs (referred buyers, orders, earned/pending/paid).
- `get_influencer_buyers()` — partner's referred buyers (anonymised labels).

### Admin-only (should be guarded by role='admin' inside the function)
- Seller lifecycle: `approve_seller`, `verify_seller_kyc`, `unverify_seller_kyc`, `block_seller`,
  `unblock_seller`, `delete_seller`.
- Buyer lifecycle: `admin_block_buyer`, `admin_unblock_buyer`, `admin_delete_buyer`.
- Deal/offer flow: `create_deal_from_match`, `advance_deal_status`, `admin_accept_offer`,
  `admin_decline_offer`, `post_offer_round`.
- Onboarding: `admin_create_agency`, `admin_create_influencer`.
- Partner apps: `admin_list_partner_applications`, `admin_set_partner_application_status`.

> **Verify:** each `admin_*` RPC (and the seller/deal RPCs called from admin) must re-check the
> caller's role server-side. Never rely on the admin UI being the only caller.

---

## AUTH & ROLES

- Auth = Supabase email/password. `profiles.role` ∈ {buyer, seller, agency, admin}.
- Each portal's `checkSession()` loads the session, reads `profiles`, and signs the user out if
  the role doesn't match that portal (e.g. a buyer trying the agency portal is rejected).
- Admin portal additionally requires `role='admin'` + approved + active.

## DEAL STATE MACHINE

`match → confirmed → paid → in_fulfillment → dispatched → delivered → settled`
Off-ramps: `cancelled`, `disputed`.
Commission timing keys off `delivered` (accrue) and `settled` (release).
