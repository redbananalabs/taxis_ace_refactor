# Red Taxi — Claude Code Build Handoff

**Date:** 2026-03-16  
**Status:** Ready to build Phase 1

---

## 1. What Claude Code Should Do First

### Step 1: Analyse Legacy Service Files
Extract exact business logic from these god services before writing any new code:

| File | Lines | Extract |
|------|-------|---------|
| `TaxiDispatch.Lib/Services/BookingService.cs` | 2,142 | Every booking operation: create, update, cancel, complete, duplicate, merge, price calc triggers, recurrence handling |
| `TaxiDispatch.Lib/Services/DispatchService.cs` | 1,002 | Allocation flow, soft allocate, confirm, unallocate, job offer sending, turn-down recording |
| `TaxiDispatch.Lib/Services/AccountsService.cs` | 2,247 | Invoice generation, statement generation, batch pricing, credit notes, posting/unposting, HVS pricing |
| `TaxiDispatch.Lib/Services/TariffService.cs` | 709 | Price calculation (Google Distance Matrix call, tariff application, dead miles) |
| `TaxiDispatch.Lib/Services/AceMessagingService.cs` | 891 | Message template rendering, channel selection (SMS/WhatsApp/Push), send logic |
| `TaxiDispatch.Lib/Services/WebBookingService.cs` | 878 | Web portal booking creation, acceptance, rejection, amendment requests |
| `TaxiDispatch.Lib/Services/AvailabilityService.cs` | 491 | Availability CRUD, preset logic, availability grid queries |

For each service: produce a document listing every public method, what it does, which entities it touches, and which MediatR handler it should become.

### Step 2: Create the Solution Structure

The repo should be organised as follows (legacy code copied in for reference):

```
redtaxi/
├── legacy/                         ← existing Ace projects (READ-ONLY reference)
│   ├── TaxiDispatch.API/
│   ├── TaxiDispatch.Lib/
│   ├── TaxiDispatch.Tests/
│   ├── ace-admin-ui/               ← React dispatch console (if available)
│   ├── ace-sms-gateway/            ← Android SMS gateway (if available)
│   └── ace-driver-app/             ← Driver app (if available)
├── src/
│   ├── RedTaxi.sln
│   ├── RedTaxi.API/                (.NET 8 Web API + Blazor Server host)
│   ├── RedTaxi.Application/        (MediatR handlers by feature)
│   ├── RedTaxi.Domain/             (Entities, enums, events, interfaces)
│   ├── RedTaxi.Infrastructure/     (EF Core, Redis, external APIs)
│   ├── RedTaxi.Blazor/             (Dispatch console - Blazor Server + Syncfusion)
│   └── RedTaxi.WebPortal/          (Customer booking portal)
├── tests/
│   ├── RedTaxi.UnitTests/
│   ├── RedTaxi.IntegrationTests/
│   └── RedTaxi.ArchTests/
├── docs/                            ← already populated (8 docs + chatgpt-source)
├── docker-compose.yml
├── .github/workflows/deploy.yml
└── .gitignore
```

Claude Code should reference `legacy/` when extracting business logic but NEVER modify files in that folder.

### Step 3: Build Phase 1 Features
In this order:

1. **Domain entities** — port from legacy `Data/Models/` adding `TenantId`, new entities (Customer, SavedAddress)
2. **DbContext** — RedTaxiDbContext with global query filters for TenantId
3. **Auth** — ASP.NET Core Identity + JWT + roles (SuperAdmin, TenantAdmin, Dispatcher, Driver, AccountUser, WebBooker)
4. **Booking CRUD** — CreateBooking, UpdateBooking, GetBookingsToday, GetBookingById, FindBookings
5. **Pricing engine** — TariffService port: Google Distance Matrix, tariff selection by time/day, price calculation (InitialCharge + FirstMile + AdditionalMiles × Rate)
6. **Customer directory** — CreateCustomer, LookupByPhone, GetCustomer
7. **Driver availability** — SetAvailability, GetAvailability (with presets: Custom, SR AM Only, SR PM Only, SR Only, UNAVAILABLE ALL DAY)
8. **Dispatch** — AllocateBooking, SoftAllocate, ConfirmSoftAllocates, RecordTurnDown
9. **Blazor dispatch console** — booking form, diary (SfSchedule), availability grid, driver status, map tab

---

## 2. Reference Documents (all in `docs/`)

| Doc | Purpose |
|-----|---------|
| `PRD.md` | What to build (34 sections) |
| `business-rules.md` | How it works (28 sections) |
| `data-model.md` | Entity definitions and relationships |
| `api-contract.md` | 232 legacy endpoints → v2 REST routes |
| `architecture.md` | Stack decisions, project structure, ADRs |
| `module-map.md` | 72 modules for parallel development |
| `saas-packaging.md` | SaaS tiers (not needed for Phase 1) |

---

## 3. Key Technical Decisions (Do Not Override)

- **Blazor Server** for dispatch console (not WASM)
- **Syncfusion Blazor** components: SfSchedule (diary), SfDataGrid (lists/reports), SfChart (dashboard), SfTextBox/SfDropDown (forms)
- **MediatR** — one handler per use case, no service-to-service calls
- **Domain events** — messaging triggered by events (BookingAllocated → send notification), not by direct calls
- **Hangfire** for background jobs (not RabbitMQ)
- **QuestPDF** for invoice/statement PDFs
- **SignalR** for real-time dispatch updates (built into Blazor Server)
- **EF Core** with `IDbContextFactory` (same pattern as legacy)
- **Redis** for GPS cache and SignalR backplane
- **Global query filters** for tenant isolation (`TenantId` on all entities)

---

## 4. Legacy Code to Port vs Rewrite

### Port (copy logic, clean up structure)
- Tariff calculation (TariffService.GetPriceHVS — the core algorithm works)
- Booking entity model (Booking.cs — field definitions are correct)
- Enum definitions (BookingEnums.cs — all values must match for import compatibility)
- AutoMapper profiles (field mappings between entities and DTOs)

### Rewrite (logic is correct but structure is wrong)
- BookingService → split into ~15 MediatR handlers
- DispatchService → split into ~8 MediatR handlers
- AccountsService → split into ~12 MediatR handlers
- AceMessagingService → domain event handlers (not direct calls)

### Discard
- WeatherForecast.cs (template leftover)
- ATestController (test endpoint)
- Legacy migration files (new baseline migration for Red Taxi)

---

## 5. Phase 1 Definition of Done

Phase 1 is complete when an operator can:

1. Log in to the Blazor dispatch console
2. Create a booking (pickup, destination, vias, passenger, date/time, scope)
3. See the price auto-calculate using tariff rules
4. See the booking on the diary/scheduler
5. Allocate a driver to the booking
6. See the booking change colour to the driver's colour
7. Look up a customer by phone number
8. Set driver availability (using presets)
9. See driver availability on the availability grid
10. Complete or cancel a booking

This matches the core Ace operator workflow.
