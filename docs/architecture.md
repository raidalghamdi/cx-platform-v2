# Architecture — CX Platform v2 (Phase 0)

This document captures the moving parts as they exist after Phase 0. It
will grow as we add the remaining pages.

## Tiers

```mermaid
flowchart TB
  browser["Browser<br/>Angular 17 SPA"]
  gateway["YARP gateway<br/>:5000<br/>(CSP / HSTS / rate-limit)"]
  api["ASP.NET Core 8 API<br/>:5001<br/>JWT · audit chain"]
  audit["audit_events (hash-chained)"]
  mysql[(MySQL 8)]

  browser -->|/api/* and /*| gateway
  gateway -->|/api/*| api
  gateway -->|/* (web)| browser
  api --> mysql
  api -.->|on every POST/PUT/PATCH/DELETE| audit
  audit --- mysql
```

## Folder map

```
src/
├── api/CxPlatform.sln
│   ├── CxPlatform.Api/                # entry point
│   │   ├── Controllers/               # AuthController, KpisController, ComplaintsController, InboxController, AdminController, NotificationsController, HealthController
│   │   ├── Auth/                      # JwtSettings, TokenService (HS256 dev / RS256-ready)
│   │   ├── Middleware/                # SecurityHeadersMiddleware, AuditMiddleware
│   │   ├── Mappers/                   # entity → DTO
│   │   └── Program.cs                 # services, pipeline, migrate+seed on startup
│   ├── CxPlatform.Application/
│   │   └── Dtos/                      # records mirrored by the SPA
│   ├── CxPlatform.Domain/
│   │   ├── Entities/                  # POCOs (User, Complaint, InboxThread, AuditEvent, …)
│   │   └── Enums/                     # ComplaintStatus, Priority, InboxChannel, InboxStatus
│   └── CxPlatform.Infrastructure/
│       ├── Persistence/AppDbContext.cs        # EF Core model + indexes
│       ├── Persistence/Seed.cs                # 6 users, 9 KPIs, 8 complaints, 8 threads, 5 notifications, genesis audit
│       ├── Persistence/DesignTimeDbContextFactory.cs
│       ├── Migrations/                        # initial migration committed
│       ├── Security/HashChain.cs              # SHA-256 chain util
│       └── Channels/                          # IChannelAdapter + Email/WhatsApp/Chat mocks (95% success, 600-1200ms delay)
└── gateway/CxPlatform.Gateway/        # YARP — /api → api:5001, /* → web (dev :4200, prod static)
```

## Auth flow

1. `POST /api/v1/auth/login` accepts `{ email, password }`. On success
   returns `{ accessToken, refreshToken, user, permissions }`.
2. Access token: HS256 JWT (RS256 to come), claims include `sub`, `email`,
   `role`, `name_en`, `name_ar`, `landing`. Lifetime 60 min.
3. Refresh token: 48-byte random, stored as SHA-256 hash in
   `refresh_tokens`. Rotated on each `POST /api/v1/auth/refresh`.
4. Logout revokes the refresh token's row by setting `RevokedAt`.

The SPA keeps both tokens in memory only — never `localStorage`.

## Audit chain

Every mutating `/api/*` request triggers `AuditMiddleware`, which:

1. Reads the latest `entry_hash` in `audit_events` (genesis = 64 zeroes).
2. Builds a JSON payload `{ method, path, statusCode, at, actor }`.
3. Computes `entry_hash = SHA256(prevHash || "|" || payload)`.
4. Inserts a row with `prev_hash`, `entry_hash`, `payload_json`.

A `SemaphoreSlim` guards concurrent writes so the chain doesn't fork. Hash
verification runs forward-only — recompute hashes from genesis and confirm
they match each row's `entry_hash`.

## Channel adapters

`IChannelAdapter.SendAsync(threadId, payload)` returns `SendResult { Ok,
ExternalId, Error }`. Phase 0 ships mock implementations that simulate
600–1200 ms latency and 95 % success. Replace with SMTP / WhatsApp
Business / web-socket clients via DI when production adapters land.

## RBAC

`role_permissions` rows are `(role, page_key, allowed)`. The login response
includes the active user's slice. Angular's `roleGuard` checks page access
on every route activation; the admin row is always all-true and the server
enforces it regardless of payload.

## Schema cheat-sheet

See [`db/init/01_db.sql`](../db/init/01_db.sql) for database bootstrap and
the migration files under
`src/api/CxPlatform.Infrastructure/Migrations/` for the live schema.

Phase 0 tables: `users`, `refresh_tokens`, `role_permissions`, `kpis`,
`complaints`, `complaint_events`, `inbox_threads`, `audit_events`,
`contact_channels`, `notifications`.
