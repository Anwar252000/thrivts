# Vintage Hub — staff & owner apps

Two React apps on separate domains, one API (`Backend/`). Both keep the legacy Vintage Hub look:
their stylesheets are the legacy CSS ported verbatim (`apps/*/src/styles/legacy.css`).

```
Frontend/
  shared/src/     API client, PIN login, session, live updates (SignalR), formatting, toast
  apps/staff/     staff app  — gate, lots, inventory, SKU, wash, GM screens   (port 5173)
  apps/owner/     owner app  — floor, hisab, clients, approvals, team         (port 5174)
```

## Run locally

```bash
cp .env.example .env          # VITE_API_BASE_URL=http://localhost:5275
npm install
npm run dev:staff             # http://localhost:5173
npm run dev:owner             # http://localhost:5174
```

Start the API first (`dotnet run --project Backend/src/Thrivts.Api`). Its development CORS
settings already allow ports 5173 and 5174.

## Checks

```bash
npm run typecheck   # both apps + shared
npm run lint
npm run build       # apps/staff/dist and apps/owner/dist
```

## Deploy (Vercel — one project per app)

| Setting          | Staff app                 | Owner app                 |
|------------------|---------------------------|---------------------------|
| Root directory   | `Frontend`                | `Frontend`                |
| Build command    | `npm run build:staff`     | `npm run build:owner`     |
| Output directory | `apps/staff/dist`         | `apps/owner/dist`         |
| Env var          | `VITE_API_BASE_URL` = API URL | same                  |

`vercel.json` (SPA fallback) applies to both. Add both app URLs to the API's
`Cors__AllowedOrigins__0/1` on Render.

## How the apps talk to the API

- **Login**: pick your name → PIN. The API checks the PIN and locks after repeated wrong tries.
  The access token lives in memory; the rotating refresh token in `localStorage` (per app).
- **Writes** always carry an `Idempotency-Key` and are retried on network failure, so a double
  tap or a flaky connection never creates two lots, dispatches or payments.
- **Live updates**: the API pushes "this changed" signals over SignalR and the visible screens
  refetch — no "Sync now" buttons or polling. The server never pushes data itself.
- **Prices** only exist in the owner app; the staff API never returns them.
