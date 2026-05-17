# Runbook (Phase 0 stub)

## Build verification

```bash
export PATH=$HOME/.dotnet:$PATH
export DOTNET_ROOT=$HOME/.dotnet
cd src/api && dotnet build CxPlatform.sln
cd ../web && npm install && npx ng build
```

Both should finish with no errors.

## Adding an EF migration

```bash
cd src/api
dotnet ef migrations add <Name> -p CxPlatform.Infrastructure -s CxPlatform.Api
```

The migration files commit to
`src/api/CxPlatform.Infrastructure/Migrations/`.

## Reset the dev database

```sql
DROP DATABASE cx_platform; CREATE DATABASE cx_platform;
```

Next startup re-runs migrations and `Seed.RunAsync`.

## Health probe

```bash
curl -sS http://localhost:5001/api/healthz
```

Expected: `{"ok":true,"version":"v2-phase0-2026.05.17","timestamp":"..."}`.

## Audit chain spot-check

```sql
SELECT id, kind, prev_hash, entry_hash, at FROM audit_events ORDER BY id DESC LIMIT 10;
```

Each row's `prev_hash` should equal the previous row's `entry_hash`. The
first row's `prev_hash` is 64 zeroes.
