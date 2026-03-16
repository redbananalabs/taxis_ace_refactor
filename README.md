# Red Taxi Platform

Modern taxi dispatch SaaS platform. Replaces the legacy Ace Taxis dispatch system and scales to multi-tenant.

## Repo Structure

```
├── docs/                   ← PRD, architecture, business rules, data model, API contract (source of truth)
├── legacy/                 ← Ace Taxis legacy codebase (read-only reference)
│   ├── TaxiDispatch.API/
│   ├── TaxiDispatch.Lib/
│   └── TaxiDispatch.Tests/
├── src/                    ← New Red Taxi solution
├── tests/                  ← Test projects
├── docker-compose.yml      ← Local dev and production stack
└── .github/workflows/      ← CI/CD pipeline
```

## Getting Started

1. Read `docs/claude-code-handoff.md` for the build plan
2. Read `docs/PRD.md` for what we're building
3. Read `docs/architecture.md` for how we're building it

## Stack

- **Backend:** .NET 8, EF Core, MediatR, SQL Server, Redis, Hangfire
- **Dispatch Console:** Blazor Server + Syncfusion
- **Driver App:** Flutter
- **Customer Portal:** TBD (Blazor WASM or lightweight SPA)
- **Hosting:** Hetzner CX32, Docker Compose, GitHub Actions CI/CD

## Docs

| Document | Description |
|----------|-------------|
| [PRD](docs/PRD.md) | Product requirements (34 sections) |
| [Business Rules](docs/business-rules.md) | Operational logic (28 sections) |
| [Architecture](docs/architecture.md) | Stack, ADRs, deployment |
| [Data Model](docs/data-model.md) | Entity definitions |
| [API Contract](docs/api-contract.md) | 232 endpoints mapped to v2 |
| [Module Map](docs/module-map.md) | 72 modules for parallel dev |
| [SaaS Packaging](docs/saas-packaging.md) | Tier definitions |
| [Claude Code Handoff](docs/claude-code-handoff.md) | Build instructions |
