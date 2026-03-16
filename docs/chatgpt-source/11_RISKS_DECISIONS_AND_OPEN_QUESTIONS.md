# Risks, Decisions, and Open Questions

## Confirmed / preferred decisions
- Modular monolith first.
- Vertical slice structure in .NET.
- Avoid heavy microservice complexity in v1.
- Strong support for substitute drivers.
- Strong support for company-to-company job sharing.
- Commercial packaging should reflect driver count and booking volume.
- White-label booking website integration is strategic.

## Main risks
### 1. Overbuilding v1
Risk of trying to build marketplace, AI autonomy, deep finance, and full mobile ecosystem too early.

### 2. Real-time complexity
Live maps and realtime dispatch can become technically heavy. Need pragmatic v1 choices.

### 3. Settlement complexity
Inter-company cover economics can become messy unless rules are explicit.

### 4. Permission leakage across tenants
Partner workflows must not accidentally expose too much data.

### 5. Packaging mistakes
Wrong pricing model can alienate small operators or underprice heavy usage customers.

## Open design questions for Claude
1. Should driver app v1 be mobile web or native?
2. What is the minimum viable realtime model for operator board?
3. How should settlement rules be represented: rules engine or simple templates?
4. How much of pricing should be configurable in v1?
5. What is the cleanest boundary between substitute drivers and partner-company fulfilment?
6. What should the event model look like in a modular monolith?
7. Which modules are essential for first customer launch vs later SaaS hardening?
