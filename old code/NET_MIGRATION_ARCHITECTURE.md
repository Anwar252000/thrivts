# THRIVTS — .NET 10 + REACT/TS MIGRATION ARCHITECTURE GUIDE

**Purpose of this document:** Thrivts is moving from "5 static HTML portals talking directly to
Supabase" to a proper 3-tier system: **React + TypeScript** frontend(s) → **.NET 10 Web API**
(Clean Architecture) → **Supabase Postgres** (kept as the database only). This document is the
single reference for that migration — architecture, folder structure, security model, DTO rules,
EF Core setup, and a phased rollout plan. Any developer joining the project should be able to read
this end-to-end and know exactly what to build and how.

Read alongside (existing docs, still valid for business context):
`HANDOVER_README.md`, `SCHEMA_AND_DATA_MODEL.md`, `AUDIT_REPORT.md`, `KNOWN_GAPS_AND_TODO.md`.

---

## 1. WHY THIS SHAPE (decision summary)

| Decision | Choice | Reason |
|---|---|---|
| Database | **Keep Supabase (Postgres)** | Schema, RLS, and all 19+ tables already live there. Rewriting to SQL Server means re-deriving the entire data model for no functional gain. |
| Backend | **.NET 10 Web API, Clean Architecture** | Business logic currently lives half in Postgres RPCs, half in frontend JS. Centralising it in a typed, testable backend fixes both the "unauditable RLS" and "client-only validation" risks flagged in `AUDIT_REPORT.md` (§B1, §B8). |
| DB access | **EF Core + Npgsql provider** | Postgres works fine from .NET; no need for SQL Server-only tooling. |
| Frontend | **React + TypeScript**, one app (or a small monorepo of 5 role-scoped apps) | Replaces the 5 hand-rolled single-file HTML portals with shared components, typed API contracts, and no more copy-pasted `esc()`/`fmtUSD()` helpers (tech-debt item in `KNOWN_GAPS_AND_TODO.md` §D). |
| Browser ↔ Supabase direct calls | **Removed entirely** | The React apps will talk **only** to the .NET API. Supabase's URL/anon key no longer ship to the browser. This is the single biggest architectural change — see §6 (Security). |

---

## 2. HIGH-LEVEL ARCHITECTURE

```
┌─────────────────────────────┐
│  React + TypeScript SPA(s)  │   Buyer / Seller / Agency / Partner / Admin
│  (Vite, role-based routing) │
└───────────────┬─────────────┘
                 │ HTTPS, JSON, Bearer JWT
                 ▼
┌───────────────────────────────────────────────────────────┐
│                  .NET 10 Web API (Clean Architecture)      │
│  ┌────────────┐  ┌───────────────┐  ┌────────────────────┐│
│  │ Presentation│→│  Application   │→│    Domain           ││
│  │ (Controllers,│ │ (Use cases,   │ │ (Entities, enums,   ││
│  │  DTOs, auth  │ │  DTOs, valid- │ │  deal state machine,││
│  │  middleware) │ │  ation, rules)│ │  no external deps)  ││
│  └────────────┘  └───────┬───────┘  └────────────────────┘│
│                           │ interfaces (IRepository, etc.) │
│                  ┌────────▼────────┐                       │
│                  │  Infrastructure  │                       │
│                  │ (EF Core+Npgsql, │                       │
│                  │  Supabase Auth   │                       │
│                  │  JWT validation, │                       │
│                  │  email, storage) │                       │
│                  └────────┬────────┘                       │
└───────────────────────────┼─────────────────────────────────┘
                             │ Postgres wire protocol (pooled via Supavisor)
                             ▼
                   ┌───────────────────┐
                   │ Supabase Postgres  │  DB only now.
                   │ (thrivts-prod)     │  Auth = still Supabase GoTrue
                   │ RLS: defense-in-   │  (issues the JWT the API verifies).
                   │ depth, not primary │  RPCs/views retired — logic moves
                   │ gate any more      │  into the Application layer.
                   └───────────────────┘
```

**Key shift:** today the browser *is* the client of Supabase (enforced by RLS). After migration,
the .NET API is the *only* client of Supabase, and the API becomes the enforcement point. RLS stays
on as a safety net, but you must not rely on it as the primary guard any more — see §6.2.

---

## 3. SOLUTION / FOLDER STRUCTURE

```
Thrivts.sln
src/
  Thrivts.Domain/                 # No NuGet deps except maybe base validation attrs
    Entities/
      Profile.cs, Buyer.cs, Seller.cs, Agency.cs, Influencer.cs,
      Requirement.cs, Deal.cs, DealAllocation.cs, Commission.cs,
      InfluencerCommission.cs, Dispute.cs, SellerResponse.cs,
      RequirementSellerOffer.cs, OfferRound.cs, MessageThread.cs,
      Category.cs, AuditLogEntry.cs, ExchangeRate.cs, ShippingRate.cs
    Enums/
      UserRole.cs, DealStatus.cs, CommissionStatus.cs,
      InfluencerCommissionStatus.cs, SellerApprovalStatus.cs
    ValueObjects/  (Money, if you want currency-safe arithmetic)
    DomainServices/
      DealStateMachine.cs         # match→confirmed→paid→...→settled, cancelled/disputed off-ramps
      CommissionCalculator.cs     # agency 30% spread, influencer 5%/12mo — see §9

  Thrivts.Application/
    Common/
      Interfaces/ (IAppDbContext, ICurrentUser, IDateTimeProvider, IEmailSender)
      Behaviours/ (ValidationBehaviour, AuthorizationBehaviour, LoggingBehaviour — if using MediatR)
      Exceptions/ (NotFoundException, ForbiddenException, ValidationException)
    Buyers/
      Commands/CreateBuyer, UpdateBuyer, ...
      Queries/GetBuyerDeals, GetBuyerDashboard, ...
      Dtos/ (BuyerDto, BuyerDealDto, ...)
      Validators/ (CreateBuyerValidator : AbstractValidator<CreateBuyerCommand>)
    Sellers/  Agencies/  Influencers/  Requirements/  Deals/  Disputes/
    Admin/
      (approve_seller, verify_seller_kyc, block/unblock, advance_deal_status, etc.
       — every current *admin_* RPC becomes a Command handler here)
    Public/
      (get_public_activity, submit_partner_application, apply_referral_code — anonymous-allowed)

  Thrivts.Infrastructure/
    Persistence/
      ThrivtsDbContext.cs
      Configurations/ (EntityTypeConfiguration per entity — Fluent API, not attributes)
      Migrations/
      Repositories/ (if not using generic repo — one per aggregate)
    Auth/
      SupabaseJwtValidation.cs   # validates tokens issued by Supabase GoTrue
    Email/  (Resend integration, replaces Supabase Auth's built-in email trigger if needed)
    DependencyInjection.cs        # AddInfrastructure(IServiceCollection, IConfiguration)

  Thrivts.Api/
    Controllers/  (or Minimal API endpoint groups)
      BuyersController.cs, SellersController.cs, AgenciesController.cs,
      InfluencersController.cs, AdminController.cs, PublicController.cs
    Middleware/ (ExceptionHandlingMiddleware, AuditLoggingMiddleware)
    Filters/ (ApiExceptionFilter)
    Program.cs                     # composition root: auth, CORS, Swagger, rate limiting
    appsettings.json / appsettings.Development.json

tests/
  Thrivts.Domain.Tests/
  Thrivts.Application.Tests/       # handler logic, no DB — mock IAppDbContext
  Thrivts.Infrastructure.Tests/    # EF Core against a real Postgres (Testcontainers)
  Thrivts.Api.Tests/               # WebApplicationFactory integration tests

frontend/
  apps/
    admin/  buyer/  seller/  agency/  partners/     # or one app w/ role-based routing — see §11
  packages/
    api-client/     # generated from the .NET OpenAPI/Swagger spec (openapi-typescript / NSwag)
    ui/             # shared components (replaces copy-pasted esc()/fmtUSD() etc.)
```

**Dependency rule (Clean Architecture, non-negotiable):** `Domain` depends on nothing.
`Application` depends only on `Domain`. `Infrastructure` and `Api` depend on `Application` +
`Domain`. Nothing outside `Domain`/`Application` is ever referenced *from* them — this is what
keeps business logic (deal state transitions, commission math, the invisible-broker rule) testable
without spinning up a database.

---

## 4. DOMAIN LAYER

Map 1:1 from `SCHEMA_AND_DATA_MODEL.md` §CORE ENTITIES. A few rules:

- Entities are **plain C# classes**, no EF Core attributes, no Supabase types. EF Core mapping
  lives entirely in `Infrastructure/Persistence/Configurations/*.cs` (Fluent API), so the Domain
  project never references `Microsoft.EntityFrameworkCore`.
- `Deal.cs` owns the state machine transition logic (`match → confirmed → paid → in_fulfillment →
  dispatched → delivered → settled`, off-ramps `cancelled`/`disputed`) as methods, e.g.
  `deal.AdvanceTo(DealStatus.Confirmed)` — invalid transitions throw a domain exception, not a
  silent no-op. This directly replaces the `advance_deal_status` RPC.
- Commission timing rules (accrue on `delivered`, release on `settled`, agency 30%-of-spread,
  influencer 5%/12 months) belong in `DomainServices/CommissionCalculator.cs` — confirm exact
  numbers with Afnan per `KNOWN_GAPS_AND_TODO.md` §E before finalizing.
- Enums (`DealStatus`, `CommissionStatus` = pending/ready_to_release/released/cancelled,
  `InfluencerCommissionStatus` = accrued/released/reversed) are C# enums, mapped to Postgres text
  via a value converter (§7.3) — don't use Postgres native enum types from EF Core, they're painful
  to evolve.

---

## 5. APPLICATION LAYER

This is where every current Postgres RPC function gets a new home. Use either **MediatR**
(Command/Query + Handler per use case — recommended, keeps each use case in its own file and
easy to unit-test) or plain application services if the team prefers less ceremony — pick one and
stay consistent.

### 5.1 RPC → Application-layer mapping

| Old Postgres RPC | New Application handler | Layer note |
|---|---|---|
| `get_public_activity` | `GetPublicActivityQuery` | Anonymous-allowed, rate-limited (§6.6) |
| `submit_partner_application` | `SubmitPartnerApplicationCommand` | Anonymous, honeypot + min-time check ported from frontend (`AUDIT_REPORT.md` Partners section) into the handler, not just the UI |
| `apply_referral_code` / `apply_agency_ref` | `ApplyReferralCodeCommand` / `ApplyAgencyRefCommand` | |
| `get_agency_public_name` | `GetAgencyPublicNameQuery` | |
| `accept_seller_offer` / `accept_seller_quote` | `AcceptSellerOfferCommand` | Seller-authenticated |
| `post_offer_round` | `PostOfferRoundCommand` | Seller or admin caller — check role inside handler |
| `get_agency_dashboard` | `GetAgencyDashboardQuery` | Scoped to `currentUser.AgencyId` — never trust a client-supplied agency id |
| `get_influencer_overview` / `get_influencer_buyers` | `GetInfluencerOverviewQuery` / `GetInfluencerBuyersQuery` | Same scoping rule |
| `approve_seller`, `verify_seller_kyc`, `unverify_seller_kyc`, `block_seller`, `unblock_seller`, `delete_seller` | one Command each under `Admin/Sellers/` | Require `[Authorize(Policy = "AdminOnly")]` at the controller **and** re-check role in the handler (defense in depth — mirrors the audit note that admin RPCs must not trust the caller) |
| `admin_block_buyer`, `admin_unblock_buyer`, `admin_delete_buyer` | `Admin/Buyers/*` commands | same |
| `create_deal_from_match`, `advance_deal_status`, `admin_accept_offer`, `admin_decline_offer` | `Admin/Deals/*` commands, calling into `Deal.AdvanceTo(...)` | |
| `admin_create_agency`, `admin_create_influencer` | `Admin/Onboarding/*` commands | |
| `admin_list_partner_applications`, `admin_set_partner_application_status` | `Admin/Partners/*` | |

### 5.2 DTOs — see §9, they are not optional and not 1:1 with entities.

### 5.3 Validation

Use **FluentValidation**. One validator per Command/Query, registered via
`services.AddValidatorsFromAssembly(...)`. This replaces the client-only form checks flagged in
`AUDIT_REPORT.md` §B8 — **every** rule that matters for correctness or security must exist here,
even if the React form also checks it for UX. Never trust the frontend as the only validator.

---

## 6. SECURITY (read this section carefully — it's the part that changes most)

### 6.1 Authentication

Keep **Supabase Auth (GoTrue)** as the identity provider — no need to rebuild login/signup/
password-reset. The flow becomes:

1. React app calls Supabase Auth directly (`supabase-js`, auth-only — no DB calls) to sign in →
   gets back a JWT.
2. React sends that JWT as `Authorization: Bearer <token>` on every call to the .NET API.
3. .NET validates the JWT using **Supabase's JWT secret** (Settings → API → JWT Secret) with
   standard ASP.NET Core JWT Bearer middleware — no custom crypto needed:

```csharp
// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"{supabaseUrl}/auth/v1",
            ValidateAudience = true,
            ValidAudience = "authenticated",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(supabaseJwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
```

The `sub` claim on the token is the Supabase `auth.uid()` — use it to look up `profiles.role` on
every request (cache per-request via `ICurrentUserService`, don't re-query per handler call).

### 6.2 Authorization — the most important change in this whole migration

Today, security is enforced by **RLS at the database**, because the browser talks to Postgres
directly. After migration, the .NET API becomes a **trusted intermediary** — it will typically
connect to Postgres with a single application role (not a per-user RLS-scoped connection), because
threading the end-user's JWT through to Postgres per-request (Supabase's `set_config('request.jwt.claims', ...)`
trick) is possible but adds real complexity for a .NET/Npgsql stack and most teams don't do it.

**Therefore: authorization must be fully re-implemented in the Application layer. RLS becomes a
defense-in-depth backstop, not the primary gate.** This is non-negotiable given Thrivts' core
guarantee (buyer and seller must never see each other's identity — the "invisible broker" model).
Concretely:

- Every query handler must filter by the **current authenticated user's** id/role — e.g.
  `GetBuyerDealsQuery` must scope to `currentUser.BuyerId`, never accept a buyer id from the
  client and trust it.
- Use `[Authorize(Policy = "BuyerOnly")]` / `"SellerOnly"` / `"AgencyOnly"` / `"AdminOnly"` /
  `"InfluencerOnly"` policies (mapped from the `role` claim/DB lookup) at the controller level as
  the first gate, then re-check inside the handler for anything sensitive (mirrors the existing
  audit note about admin RPCs).
- Keep RLS **enabled** on the Supabase tables as a second layer — if the API's DB role is scoped
  down as much as practical (not superuser), a bug in the Application layer is less likely to be
  catastrophic. Don't disable RLS just because the API "should" be the only caller.
- **Do not** ship the Supabase anon key or URL to the React apps any more — they never call
  Supabase directly except for the Auth endpoints (sign in/up/reset), which is safe by design
  (that's what the anon key is for).

### 6.3 DTOs enforce the identity-hiding rule (replaces the old views)

`buyer_deals_view`, `seller_deals_view`, `seller_requirements_view` existed purely to stop one
side from seeing the other's identity. In the new architecture, **that job moves to DTOs**:

- `BuyerDealDto` — never includes seller/allocation identity fields.
- `SellerDealDto` — never includes buyer identity or buyer-side pricing.
- `AdminDealDto` — full detail, only ever returned to `AdminOnly` endpoints.

Never return a Domain entity directly from a controller. Always map to a role-specific DTO
explicitly (see §9) — this is your enforcement point, treat any missing/incorrect DTO mapping here
as a security bug, not a style issue.

### 6.4 Secrets

- Supabase **service-role key** (if the API ever needs elevated DB access) and the **JWT secret**
  live in `dotnet user-secrets` locally, and in your host's secret store in prod (Azure Key Vault /
  AWS Secrets Manager / Netlify-equivalent env vars on whatever host you pick for the API) —
  **never in appsettings.json committed to git**, never in frontend code.
- Connection string uses **Supabase's pooled connection (Supavisor)**, not the direct DB port —
  see §7.1.

### 6.5 Input validation & injection

- EF Core parameterizes all queries by default — do not build raw SQL by string concatenation
  anywhere. If you need raw SQL (rare), use `FromSqlInterpolated`, never `FromSqlRaw` with
  concatenated strings.
- FluentValidation at the Application boundary catches malformed input before it reaches EF Core.
- React output-escaping: this problem mostly disappears once you move off innerHTML-style manual
  rendering (the old portals needed a hand-rolled `esc()` everywhere because they built HTML
  strings — React escapes by default). Still sanitize anything rendered via `dangerouslySetInnerHTML`
  (should be essentially never).

### 6.6 Rate limiting & abuse

Public/anonymous endpoints (`GetPublicActivity`, `SubmitPartnerApplication`, referral code apply)
are the ones currently open to anonymous callers — use ASP.NET Core's built-in rate limiter
(`AddRateLimiter`) on these specifically. Port the existing honeypot + min-time-to-submit anti-bot
check from the Partners portal into `SubmitPartnerApplicationCommandValidator`.

### 6.7 Transport & headers

HTTPS/HSTS enforced (`app.UseHsts()`, `app.UseHttpsRedirection()`), CORS locked to the known React
app origins (no wildcard), standard security headers (`X-Content-Type-Options`,
`Referrer-Policy`, CSP if feasible for the SPA hosting).

### 6.8 Audit logging

`audit_log` table already exists for admin actions. Keep writing to it — either an EF Core insert
inside every Admin command handler, or a MediatR pipeline behaviour that logs any
`Admin/*` command automatically (recommended — one place, can't be forgotten per-handler).

---

## 7. INFRASTRUCTURE LAYER — EF CORE + SUPABASE POSTGRES

### 7.1 Connection & pooling

Supabase gives you two connection modes via **Supavisor** (its pooler):

| Mode | Port | Use for |
|---|---|---|
| Transaction mode | 6543 | Your API's normal runtime traffic — short-lived connections, high concurrency. Use this as the default connection string. |
| Session mode | 5432 (direct) | EF Core **migrations** only (`dotnet ef database update`), since migrations need session-level features transaction mode doesn't support. |

```json
// appsettings.json (values from user-secrets/env in real environments)
"ConnectionStrings": {
  "Default": "Host=aws-0-eu-west-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.wdtzsmvciysevswqnklo;Password=***;SSL Mode=Require;Trust Server Certificate=true;Pooling=true"
}
```

### 7.2 Packages

```
Npgsql.EntityFrameworkCore.PostgreSQL
EFCore.NamingConventions        # translates Postgres snake_case columns <-> C# PascalCase properties
```

```csharp
services.AddDbContext<ThrivtsDbContext>(options =>
    options.UseNpgsql(config.GetConnectionString("Default"))
           .UseSnakeCaseNamingConvention());
```

### 7.3 Schema authority — decide this explicitly before writing code

Today the schema exists **only** in the live Supabase project (`KNOWN_GAPS_AND_TODO.md` §A —
still the #1 unresolved risk). Before building the Infrastructure layer:

1. **Export the full schema first**, regardless of what you decide next:
   `supabase db dump --schema public -f schema.sql`. Commit it. This alone de-risks the whole
   migration — do it in week one.
2. Then pick one of:
   - **(A) Scaffold once, hand-own after** (recommended): `dotnet ef dbcontext scaffold` against
     the live DB to generate a starting point for entities + `DbContext`, then delete the
     scaffolding tool's output structure and hand-place the mapping into
     `Infrastructure/Persistence/Configurations/*.cs` as Fluent API config. From this point,
     **EF Core migrations become the source of truth going forward** — future schema changes are
     authored as EF Core migrations, not ad-hoc SQL in Supabase's SQL editor.
   - **(B) Supabase SQL stays authoritative**, EF Core is told `NOT` to manage migrations
     (`modelBuilder` mapped by hand, no `Migrations/` folder used against prod). Only pick this if
     you expect to keep editing schema by hand in Supabase's dashboard — not recommended long-term,
     it's exactly the "no version control on schema" problem you're trying to fix.

   Recommendation: **(A)**. It gives you the git history and repeatable migrations the audit report
   says you're missing, and it's the natural EF Core workflow.

### 7.4 Enum mapping example

```csharp
// Configurations/DealConfiguration.cs
public class DealConfiguration : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> b)
    {
        b.ToTable("deals");
        b.Property(d => d.Status)
         .HasConversion<string>()   // stores enum as text, matches existing column type
         .HasColumnName("status");
        b.HasMany(d => d.Allocations)
         .WithOne()
         .HasForeignKey(a => a.DealId);
    }
}
```

### 7.5 Query performance

- `AsNoTracking()` on every read-only query (all the dashboard/list queries).
- Explicit `.Select(...)` projections straight into DTOs where possible — avoids the `select('*')`
  problem called out in `AUDIT_REPORT.md` §B4 by construction (EF Core only fetches columns you
  project).
- Reuse the indexes already defined in `SQL_RUN_IN_SUPABASE/3_performance_indexes.sql` — confirm
  they still match query patterns once queries move into LINQ.
- Pagination via `Skip`/`Take` (or keyset pagination for the larger admin lists) — replaces the
  silent 1,000-row Supabase cap and the ad hoc "Load more" buttons.

---

## 8. API / PRESENTATION LAYER

- One controller (or Minimal API group) per portal domain: `BuyersController`,
  `SellersController`, `AgenciesController`, `InfluencersController`, `AdminController`,
  `PublicController`.
- Controllers are thin: bind route/query → build a Command/Query → `await _mediator.Send(...)` →
  map result to a DTO if the handler didn't already → return `Ok(dto)`. No business logic here.
- Global exception handling middleware maps `NotFoundException → 404`,
  `ForbiddenException → 403`, `ValidationException → 400` with a consistent error-body shape.
- API versioning from day one (`api/v1/...`) even with one consumer — avoids breaking the 5
  frontends together on future changes.
- Swagger/OpenAPI (`Microsoft.AspNetCore.OpenApi` + Swashbuckle or NSwag) — this spec is also what
  generates the typed TypeScript client for the React apps (§11), so keep DTOs Swagger-clean
  (no circular refs, explicit nullability).

---

## 9. DTO CONVENTIONS

Rules, not suggestions:

1. **Never expose a Domain entity across the API boundary.** Always a DTO.
2. **DTOs are role-shaped, not entity-shaped.** One entity can produce multiple DTOs
   (`BuyerDealDto`, `SellerDealDto`, `AdminDealDto` from `Deal`) — this is what enforces the
   invisible-broker rule now that the Postgres views are gone. When adding a field to a DTO, ask
   "should this role see this?" every time, not just "does the entity have this field?".
3. Naming: `{Entity}Dto` for read models, `Create{Entity}Command` / `Update{Entity}Command` for
   writes (these double as the "input DTO" when using MediatR — no separate `CreateBuyerDto` needed
   unless the API shape must differ from the command shape).
4. Mapping: use **Mapster** or **AutoMapper** for the mechanical entity→DTO mapping, but write
   role-scoped `Select()` projections by hand for anything identity-sensitive (§6.3) rather than
   trusting an automapper profile not to leak a field.
5. Validation attributes/rules live on Commands (FluentValidation), not on DTOs used for reads.
6. Money fields: keep currency + amount together in DTOs (e.g. `decimal AmountUsd`, explicit `Usd`
   suffix per the existing convention — the currency-mismatch bug in `AUDIT_REPORT.md`'s Partners
   section happened exactly because currency wasn't explicit everywhere).

---

## 10. FRONTEND — REACT + TYPESCRIPT

- **Typed API client**: generate from the .NET Swagger/OpenAPI spec (`openapi-typescript` or
  `NSwag`) — regenerate on every backend contract change. This is the biggest correctness win over
  the old hand-written `supabase-js` calls scattered through 5 HTML files.
- **Auth**: `supabase-js` used *only* for `signInWithPassword` / `signUp` / password reset —
  store the resulting JWT (short-lived access token + refresh token) and attach it as a Bearer
  token on API calls. Refresh via Supabase's refresh-token flow on expiry.
- **Structure**: either 5 separate Vite apps (mirrors current 5-portal deploy model, keeps blast
  radius small per role) or 1 app with role-based routing + code-splitting per role — either is
  reasonable; 5 apps is more consistent with the current Netlify-per-subdomain hosting, 1 app is
  less duplication. This is a team call, not a correctness issue.
- **Shared package** (`packages/ui`): the things that were copy-pasted per portal before —
  currency formatting, date formatting, toast/dialog components, the CSV-export helper — become
  one shared package instead of 5 copies (`KNOWN_GAPS_AND_TODO.md` §D1).
- Server state via **TanStack Query** (React Query) against the typed client — gives you caching,
  request de-dupe, and polling-with-backoff for anything replacing the old landing-page activity
  feed poll, without hand-rolling visibility checks again.

---

## 11. MIGRATION ROADMAP (phased)

Ordered to front-load risk reduction, matching `KNOWN_GAPS_AND_TODO.md` §A priorities:

1. **Export & commit the Supabase schema** (`supabase db dump`) — do this before writing any .NET
   code, migration or not. De-risks everything downstream.
2. **Audit existing RLS policies** while they're still the only guard, to know the *intended*
   authorization rules — this becomes your spec for the Application-layer authorization you're
   about to (re)write in §6.2. Don't skip this by assuming the RLS as-built is correct; audit it,
   then translate the *intended* rules.
3. Scaffold `Thrivts.Domain` + `Thrivts.Infrastructure` against the exported schema (§7.3 option A).
4. Stand up auth: Supabase JWT validation in .NET (§6.1), role policies (§6.2), a minimal
   `/api/v1/me` endpoint — get one authenticated round-trip working end to end before building
   features.
5. Port RPCs to Application handlers one portal at a time, **starting with Admin** (highest
   security stakes, smallest user base to coordinate cutover with) — table in §5.1 is your
   checklist.
6. Build the corresponding Controllers + DTOs per portal, generate the TS client, wire up one React
   app at a time against the new API — Admin first, then Seller/Agency (lower traffic), Buyer and
   Partners last (highest traffic / most public-facing).
7. Run both stacks in parallel per portal during cutover (old static HTML on its existing Netlify
   URL, new React app on a staging URL) until each portal is verified, then flip DNS/redeploy.
8. Once every portal is off direct Supabase access, **lock down the Supabase project**: rotate the
   anon key (no longer needed by browsers), confirm RLS is still enabled as backstop, and restrict
   network access to the API's egress if your host supports IP allowlisting.
9. Retire the old single-file portals from the `code/` folder once cutover is confirmed stable.

---

## 12. TESTING STRATEGY

| Layer | Approach |
|---|---|
| `Domain` | Pure unit tests — deal state machine transitions, commission calculation, no mocks needed. |
| `Application` | Unit tests per handler, mock `IAppDbContext`/repositories. This is where you test authorization scoping (§6.2) — e.g. "GetBuyerDealsQuery for buyer A never returns buyer B's deals" as an explicit test case. |
| `Infrastructure` | Integration tests against a real Postgres via **Testcontainers** — verifies EF Core mappings, migrations, and query correctness against actual Postgres semantics (not SQLite-in-memory, which hides real bugs). |
| `Api` | `WebApplicationFactory<Program>` integration tests — auth required/rejected, DTO shape, status codes. |
| Security-specific | Explicit test suite for the invisible-broker guarantee: assert seller-facing DTOs never contain buyer PII fields and vice versa, at the type level if possible (e.g. a test that reflects over `SellerDealDto` and fails if a banned field name ever gets added). |

---

## 13. CI/CD & DEPLOYMENT NOTES

- `dotnet ef migrations` run as part of CI/CD against Supabase's **session-mode** port (§7.1), not
  transaction mode.
- Host the .NET API wherever fits the team (Azure App Service, AWS, Fly.io, Railway, etc.) — no
  Supabase-specific constraint here beyond outbound Postgres connectivity to Supavisor.
  React apps stay on Netlify (current hosting works fine for static SPA builds).
- Keep a **staging Supabase project** (already recommended in `DEPLOYMENT_AND_INFRA.md`) and a
  matching staging .NET API + staging React build — never test schema or authorization changes
  against `thrivts-prod`.
- Add error monitoring (Sentry, both API and React side) and structured logging (Serilog →
  wherever the team centralizes logs) — carried over from `KNOWN_GAPS_AND_TODO.md` §C, still
  applies in the new stack.

---

## 14. QUICK CHECKLIST FOR ANY NEW FEATURE

- [ ] Domain: does this need a new entity/enum, or a change to the deal state machine / commission rules?
- [ ] Application: Command or Query? Handler + FluentValidation validator + unit test for authorization scoping.
- [ ] DTO: which role(s) can see this? Does an existing DTO need a *new* role-specific variant instead of adding a field to an existing one?
- [ ] Infrastructure: EF Core mapping/migration if schema changed; confirm indexes still cover the new query.
- [ ] Api: controller action, `[Authorize(Policy=...)]`, Swagger doc, regenerate TS client.
- [ ] Frontend: consume via the typed client + TanStack Query; no direct `supabase-js` DB calls, ever.
- [ ] Security: if this touches buyer/seller identity, disputes, commissions, or admin actions — add an explicit authorization test before merging.
