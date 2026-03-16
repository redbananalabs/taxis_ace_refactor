# Tenancy Modes

## Current Default
- `Tenancy:Mode = SingleDatabase`
- All requests use `ConnectionStrings:DefaultConnection`.

## Switch To Per-Tenant Database
Update `TaxiDispatch.API/appsettings.json`:

```json
"Tenancy": {
  "Mode": "PerTenantDatabase",
  "TenantHeaderName": "X-Tenant-Id",
  "RequireTenantHeader": true,
  "DefaultTenantId": "default",
  "TenantConnections": {
    "default": "Data Source=...;Initial Catalog=TaxiDispatch_Default;...",
    "tenant-a": "Data Source=...;Initial Catalog=TaxiDispatch_TenantA;..."
  }
}
```

## Behavior
- In `SingleDatabase` mode, tenant header is ignored for DB connection selection.
- In `PerTenantDatabase` mode:
  - Tenant id is read from request header (default: `X-Tenant-Id`).
  - Connection string is selected from `Tenancy:TenantConnections`.
  - If `RequireTenantHeader = true`, missing/unknown tenant ids fail fast.
  - If `RequireTenantHeader = false`, unknown tenant ids fall back to `DefaultConnection`.

## Startup Migrations
- Controlled by `Database:ApplyMigrationsOnStartup`.
- Default is `true` in this repo right now for local validation.
- For production rollout, run migrations from CI/CD and set startup migration to `false`.
