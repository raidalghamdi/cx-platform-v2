# API contract — Phase 0

Versioned at `/api/v1/*`. Health check is the only un-versioned route.
All endpoints return JSON; mutating routes accept JSON ≤ 1 MB.
Bearer auth required except `auth/login`, `auth/refresh`, `auth/logout`,
and `healthz`.

## Health

| Method | Path | Description |
|---|---|---|
| GET | `/api/healthz` | `{ ok: true, version: "v2-phase0-...", timestamp }` |

## Auth

| Method | Path | Body | Returns |
|---|---|---|---|
| POST | `/api/v1/auth/login` | `{ email, password }` | `LoginResponse` |
| POST | `/api/v1/auth/refresh` | `{ refreshToken }` | `LoginResponse` |
| POST | `/api/v1/auth/logout` | `{ refreshToken }` | 204 |

`LoginResponse` = `{ accessToken, refreshToken, user: UserDto, permissions: RolePermissionDto[] }`.

## KPIs

| Method | Path | Description |
|---|---|---|
| GET | `/api/v1/kpis` | All KPIs visible to the caller's role |

`KpiDto` = `{ key, nameEn, nameAr, value, unit, delta, target?, source, lastSyncAt }`.

## Complaints

| Method | Path | Notes |
|---|---|---|
| GET | `/api/v1/complaints` | Query: `?downJourney=true&status=InProgress` |
| GET | `/api/v1/complaints/by-category` | `[{ category, count }]` |
| GET | `/api/v1/complaints/{id}` | Full `ComplaintDto` |
| PATCH | `/api/v1/complaints/{id}/status` | `{ status }` — stamps/clears `closedAt` |
| POST | `/api/v1/complaints/{id}/notes` | `{ note }` |
| PATCH | `/api/v1/complaints/{id}/assign` | `{ userId? }` |

`ComplaintStatus` enum: `New | InProgress | Resolved | Closed`.

## Inbox

| Method | Path | Notes |
|---|---|---|
| GET | `/api/v1/inbox/threads` | Query: `?channel=Email&status=New` |
| GET | `/api/v1/inbox/threads/{id}` | Full thread |
| POST | `/api/v1/inbox/threads/{id}/reply` | `{ body, subject? }` — calls channel adapter |
| PATCH | `/api/v1/inbox/threads/{id}/status` | `{ status }` |

`InboxChannel`: `Email | WhatsApp | Chat`. `InboxStatus`: `New | Open | Replied | Closed`.

## Admin (role: admin)

| Method | Path | Notes |
|---|---|---|
| GET | `/api/v1/admin/role-permissions` | Full matrix |
| PATCH | `/api/v1/admin/role-permissions` | `{ items: RolePermissionDto[] }` (server forces admin row to all-true) |
| GET | `/api/v1/admin/contact-channels` | `[{ key, value }]` |
| PATCH | `/api/v1/admin/contact-channels/{key}` | `{ value }` |

Valid contact-channel keys: `whatsapp`, `info_email`, `support_hours`.

## Notifications

| Method | Path | Notes |
|---|---|---|
| GET | `/api/v1/notifications` | Caller's recent notifications (max 50) |
| PATCH | `/api/v1/notifications/{id}/read` | Marks read |

## Error model

Errors return `{ error: string }` with the appropriate HTTP status:
`400` validation, `401` unauthorized, `403` forbidden, `404` not found,
`429` rate limited, `502` upstream adapter error, `500` unexpected.
