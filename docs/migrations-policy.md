# EF Migration Policy

## Purpose
Keep schema changes safe, reviewable, and consistent across environments.

## Rules
1. All schema changes must go through Entity Framework migrations.
2. Do not manually edit `__EFMigrationsHistory` outside one-off local recovery tasks.
3. Do not run ad-hoc DDL directly against shared environments.
4. Every migration must build and run locally with `dotnet ef database update`.
5. Migration files and model snapshot must be committed together.
6. Use one migration PR for one logical schema change.

## Local Workflow
1. Create migration:
   `dotnet ef migrations add <Name> --context TaxiDispatchContext --project "TaxiDispatch.Lib/TaxiDispatch.Lib.csproj" --startup-project "TaxiDispatch.API/TaxiDispatch.API.csproj"`
2. Apply migration:
   `dotnet ef database update --context TaxiDispatchContext --project "TaxiDispatch.Lib/TaxiDispatch.Lib.csproj" --startup-project "TaxiDispatch.API/TaxiDispatch.API.csproj"`
3. Validate by running the API and key booking/account flows.

## Deployment Workflow
1. Backup database before applying migrations.
2. Apply migrations during a maintenance window when needed:
   `powershell -ExecutionPolicy Bypass -File .\scripts\apply-migrations.ps1 -Environment Production`
3. Verify migration history after deploy:
   `SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;`
4. Keep rollback plan for each release (DB backup restore or hotfix migration).

## Baseline
Current baseline migration in this repository:
- `20260304223115_InitialBaseline_20260304`

