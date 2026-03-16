# SaaS Execution Plan

## Completed In This Pass
1. Project rename baseline finalized (`TaxiDispatch.*` solution/projects).
2. Secrets moved out of hardcoded source paths into configuration:
   - `GetAddress` keys
   - Google Distance Matrix key
3. Startup migration control added (`Database:ApplyMigrationsOnStartup`).
4. Naming cleanup completed:
   - Redis instance prefix: `TaxiDispatch:`
   - Sentry release: `taxidispatch-api@...`
   - log file prefix: `taxidispatch-api-*`
5. Legacy solution artifact removed.
6. Tenancy foundation executed:
   - `SingleDatabase` and `PerTenantDatabase` mode support via config.
   - Tenant header based connection resolution (`X-Tenant-Id` by default).

## Deferred By Request
1. Full endpoint authorization hardening pass (`[Authorize]` matrix remediation).

## Next Implementation Phases
1. Tenant data isolation at schema/model level (`TenantId` strategy + query guards).
2. Tenant provisioning workflow (new tenant, connection bootstrap, seed data).
3. Billing/subscription integration and tenant lifecycle states.
4. Operational hardening (rate limits, audit trails, backup/restore drills per tenant).
