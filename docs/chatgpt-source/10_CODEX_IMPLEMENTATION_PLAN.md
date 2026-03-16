# Codex Implementation Plan

## Build philosophy
Keep the system simple enough to ship.
Use modular monolith + vertical slices.
Build in phases.
Do not overengineer.

## Proposed repository structure
- Taxi.Api
- Taxi.App
- Taxi.Data
- optional Taxi.Domain
- tests

## Feature slice examples
- Features/Companies/Companies.cs
- Features/Users/Users.cs
- Features/Drivers/Drivers.cs
- Features/Vehicles/Vehicles.cs
- Features/Customers/Customers.cs
- Features/Bookings/Bookings.cs
- Features/Pricing/Pricing.cs
- Features/Dispatch/Dispatch.cs
- Features/Partners/Partners.cs
- Features/Reports/Reports.cs

## Suggested phase plan
### Phase 0 — Foundations
- solution setup
- auth baseline
- tenant model
- role model
- environment config
- logging / audit baseline

### Phase 1 — Core master data
- companies
- users
- drivers
- vehicles
- customers
- addresses

### Phase 2 — Booking core
- create/edit/view bookings
- booking validation
- booking status lifecycle
- pricing quote basis
- scheduled bookings

### Phase 3 — Dispatch core
- driver availability
- candidate selection
- assignment and reassignment
- dispatch timeline
- operator board

### Phase 4 — Driver operations
- driver login
- status changes
- job list
- progress updates
- location ping ingestion

### Phase 5 — Partner / substitute coverage
- substitute drivers
- partner relationships
- cover requests
- cross-tenant fulfilment audit

### Phase 6 — Reporting / finance support
- booking reports
- driver reports
- partner reports
- settlement records

### Phase 7 — Website/API integration
- public booking API
- widget/web form ingestion
- webhooks/events

### Phase 8 — AI layers
- AI call notes summarisation
- quote assist
- dispatch assist
- demand insights

## Engineering discipline requirement
Codex should maintain:
- living architecture notes,
- implementation log,
- key decisions,
- migration history,
- open issues list.
