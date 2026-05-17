# CX Platform v2 — Phase 0

Customer-experience system for the General Authority for Competition (GAC).
This repository is a clean rewrite of the React pilot, aligned with the
GAC-mandated stack:

- **Frontend**: Angular 17 standalone components, SCSS, GAC visual identity
- **Backend**: ASP.NET Core 8 Web API, Clean-Architecture (Api / Application
  / Domain / Infrastructure)
- **Database**: MySQL 8 via EF Core (Pomelo provider)
- **API Gateway**: YARP reverse proxy in front of the API
- **Compose**: `docker-compose.yml` with `mysql`, `api`, `gateway`

It tracks GAC Reference Architecture v0.1 and the Integration Patterns
catalogue (Oct 2024). The React pilot at `cx-platform/` stays live unchanged.

## What's in Phase 0

Working, end-to-end:

1. **Login** — JWT issue + refresh, role permissions returned with token,
   demo accounts panel.
2. **Dashboard** — 9 KPIs (Strategic KPIs Excel + Monafasah+ API source pills)
   plus complaints-by-category CSS bar chart.
3. **Complaints** — All / Down Journey tabs, full table, drawer with
   resolve / reopen / note actions, **Close Date** column, server stamps it
   when status moves to `Resolved`/`Closed` and clears on reopen.
4. **Inbox** — Email / WhatsApp / Chat threads, channel + status filter
   pills, reply drawer that calls the channel adapter (mock 600–1200ms
   latency + 95 % success) and persists the reply.
5. **Admin** — Role × Page matrix (admin row locked true) and Contact
   Channels editor (WhatsApp / Info email / Support hours).

The remaining 12 pages from the React pilot are out of scope for Phase 0
and will be added in later phases.

## Architecture

```
            ┌──────────────────────────────────────────────────────────────┐
            │                       Browser                                 │
            │  Angular 17 SPA — gold/navy GAC identity, bilingual EN/AR     │
            └─────────────────────────┬────────────────────────────────────┘
                                      │ http://localhost:5000
              ┌───────────────────────▼────────────────────────┐
              │      YARP gateway (CxPlatform.Gateway)         │
              │  /api/*  → API   |   /*  → Angular (dev/prod)  │
              │  CSP, HSTS, frame-deny, no-sniff, referrer     │
              └───────────────────────┬────────────────────────┘
                                      │ http://localhost:5001
              ┌───────────────────────▼────────────────────────┐
              │   ASP.NET Core 8 API (CxPlatform.Api)          │
              │   • JWT bearer + refresh                        │
              │   • Rate limit 60 req/min/IP on /api/*         │
              │   • Audit middleware → hash-chained audit_events │
              │   • Application / Domain / Infrastructure      │
              └───────────────────────┬────────────────────────┘
                                      │ EF Core (Pomelo MySQL)
              ┌───────────────────────▼────────────────────────┐
              │              MySQL 8                            │
              └────────────────────────────────────────────────┘
```

Full diagram: [`docs/architecture.md`](./docs/architecture.md). API endpoints:
[`docs/api-contract.md`](./docs/api-contract.md).

## Repository layout

```
src/
├── api/                          # ASP.NET Core 8 solution
│   ├── CxPlatform.sln
│   ├── CxPlatform.Api/           # Controllers, Program.cs, JWT, audit
│   ├── CxPlatform.Application/   # DTOs (Phase 1 will add use cases)
│   ├── CxPlatform.Domain/        # Entities, enums
│   └── CxPlatform.Infrastructure/# AppDbContext, channel adapters, hash chain, seed, migrations
├── gateway/CxPlatform.Gateway/   # YARP
└── web/                          # Angular 17
db/                               # MySQL init scripts
docs/                             # architecture.md, api-contract.md
docker-compose.yml
```

## Running locally

Two paths. Pick one.

### A) Docker (full stack)

```bash
docker compose up -d mysql api gateway
# Angular dev server (HMR):
cd src/web && npm install && npx ng serve
# → http://localhost:4200 (proxies /api → :5001) or
# → http://localhost:5000 via the gateway
```

> The gateway's `web` cluster is wired to the API in `docker-compose.yml`
> as a placeholder. For production, build the Angular bundle with
> `ng build` and serve it as static files from the gateway.

### B) No Docker — manual

```bash
# 1) Start MySQL however you like (Docker, brew, etc.) — ensure database
#    `cx_platform` exists and user `cx`/`cx` has full privileges, or
#    set ConnectionStrings:Default appropriately.

# 2) Run migrations + seed automatically on startup
cd src/api
export PATH=$HOME/.dotnet:$PATH
dotnet run --project CxPlatform.Api    # listens on :5001

# 3) (Optional) YARP gateway
dotnet run --project ../gateway/CxPlatform.Gateway    # listens on :5000

# 4) Angular dev server
cd ../web
npm install
npx ng serve                            # listens on :4200, proxies /api
```

The API runs database migrations and seeds the demo data on first launch
(idempotent thereafter).

## Demo accounts

All accounts share the password **`demo`**. Bcrypt-hashed at seed time.

| Email | Role | Lands on |
|---|---|---|
| admin@cx.gov.sa | admin (Noor Al Noor / نور النور) | `/admin` |
| supervisor@cx.gov.sa | supervisor | `/dashboard` |
| agent@cx.gov.sa | agent | `/inbox` |
| quality@cx.gov.sa | quality | `/complaints` |
| customer@cx.gov.sa | customer | `/dashboard` |
| executive@cx.gov.sa | executive | `/dashboard` |

The demo panel is hidden in production via the Angular
`environment.showDemoAccounts` flag.

## Security posture (PT-aware)

- JWT bearer (HS256 dev, RS256-ready). Refresh tokens rotated on each use.
- Tokens stored **in memory only** on the client (no `localStorage`).
- Per-IP rate limit (60 req/min) on `/api/*` at both the API and the YARP
  gateway.
- Body size cap 1 MB at Kestrel.
- Helmet-equivalent headers via `SecurityHeadersMiddleware` on the API and
  inline middleware on the gateway: CSP (self + Google Fonts), HSTS in prod,
  `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`,
  `Referrer-Policy: strict-origin-when-cross-origin`. `Server` /
  `X-Powered-By` removed.
- CORS allow-list: `http://localhost:4200`, `http://localhost:5000`,
  `http://localhost:5173`.
- Hash-chained audit log — every mutating call writes an `audit_events` row
  with SHA-256 over `prevHash || "|" || payload`. Genesis is 64 zeroes.

## Known gaps & deferred items

- **Microsoft Material** components were intentionally not added — the GAC
  identity is implemented with raw SCSS tokens so we keep full visual
  control. Phase 1 may swap in Material for complex widgets (date pickers,
  data tables) if needed.
- **Docker Compose** has not been live-tested in this sandbox (no Docker
  daemon). Files validated by inspection.
- **Refresh-token cookie** — Phase 0 returns the refresh token in the JSON
  body. Production should return it as an HttpOnly cookie issued by the
  gateway.
- **GE SS Two** Arabic webfont is proprietary; the SCSS falls back to
  `tahoma`. License + asset is a follow-up.
- **OpenAPI client codegen** is not wired; the Angular `types.ts` mirrors
  the DTOs manually for Phase 0.

## Conventions

- Bilingual EN+AR on every visible string — server returns both fields,
  client picks based on `I18nService.lang()`.
- Direction follows the language: `dir="rtl"` on the `<html>` element when
  Arabic is active.
- New entities go in `CxPlatform.Domain/Entities/` and get a config block
  in `AppDbContext.OnModelCreating`.
- Migrations live with the Infrastructure project; run with
  `dotnet ef migrations add <name> -p CxPlatform.Infrastructure -s CxPlatform.Api`.
