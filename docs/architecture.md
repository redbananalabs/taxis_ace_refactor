# Red Taxi Platform — Architecture

**Version:** 2.0  
**Last Updated:** 2026-03-16

---

## 1. Architecture Style

**Modular Monolith with Vertical Slice Architecture**

Single deployable .NET 8 API with features organised by bounded context. Each feature contains its own commands, queries, handlers, DTOs, and endpoints. No microservices for v1 — complexity is not justified at this scale.

MediatR handles command/query dispatch. One handler per use case. Domain events drive cross-cutting concerns (messaging, notifications, audit logging) so features don't call each other directly.

---

## 2. Technology Stack

| Layer | Technology | Rationale |
|-------|-----------|-----------|
| **Backend API** | .NET 8, ASP.NET Core | Team expertise, legacy compatibility |
| **CQRS / Handlers** | MediatR | Breaks god services into single-purpose handlers |
| **ORM** | EF Core 8 | Existing migration history, `IDbContextFactory` for scoped contexts |
| **Database** | SQL Server | Legacy compatibility, import wizard needs schema parity |
| **Cache / Real-time state** | Redis | GPS positions, driver status, dispatch notifications |
| **Real-time** | SignalR | Blazor Server built-in; diary/dispatch live updates |
| **Push notifications** | FCM (Firebase) | Existing driver app integration |
| **SMS** | Twilio | Existing integration |
| **WhatsApp** | Twilio / WhatsApp Business API | Existing integration |
| **Payments** | Revolut API | Existing integration; Stripe planned for Phase 4+ |
| **Address lookup** | Ideal Postcodes + Google Places | Existing integration |
| **Distance/duration** | Google Distance Matrix API | Existing pricing dependency |
| **PDF generation** | QuestPDF | Existing; invoices, statements, credit notes |
| **Dispatch console** | Blazor Server + Syncfusion | C# full-stack, real-time via SignalR built-in, Syncfusion Scheduler/Grid/Charts |
| **Driver app** | Flutter | Reliable background GPS + push; Dart syntax close to C# |
| **Customer portal** | Blazor WASM or lightweight SPA | Small surface area; decision deferred |
| **Hosting** | Self-hosted VPS + Docker Compose | v1 target |
| **Reverse proxy** | Nginx | SSL termination, static files, load balancing |

---

## 3. Project Structure

```
src/
├── RedTaxi.API/                    # ASP.NET Core host, controllers (thin), middleware, Program.cs
│   ├── Controllers/                # Thin REST controllers — delegate to MediatR
│   ├── Middleware/                  # Tenant resolution, JWT, error handling
│   ├── Hubs/                       # SignalR hubs (dispatch, GPS)
│   └── Configuration/              # DI registration, options
│
├── RedTaxi.Application/            # Use cases organised by feature
│   ├── Bookings/
│   │   ├── Commands/               # CreateBooking, UpdateBooking, CancelBooking, AllocateDriver, CompleteBooking, MergeSchoolRuns
│   │   ├── Queries/                # GetBookingsToday, GetBookingById, FindBookings, GetBookingsByDriver
│   │   ├── Events/                 # BookingCreated, BookingAllocated, BookingCompleted, BookingCancelled
│   │   └── DTOs/
│   ├── Dispatch/
│   │   ├── Commands/               # AllocateBooking, SoftAllocate, ConfirmSoftAllocates, RecordTurnDown
│   │   ├── Queries/                # GetDriverList, GetDiaryView
│   │   └── Events/                 # DriverAllocated, DriverUnallocated
│   ├── Pricing/
│   │   ├── Commands/               # RecalculatePrice, ManualPriceUpdate, ResetPrice
│   │   ├── Queries/                # GetPrice, GetTariffs, GetAccountTariffs, GetZonePrices
│   │   └── Services/               # TariffCalculator, DistanceMatrixClient
│   ├── Fleet/
│   │   ├── Commands/               # SetAvailability, UpdateGPS, DriverShift, UploadDocument
│   │   ├── Queries/                # GetAvailability, GetDriverStatus, GetAllGPS, GetDriverExpirys
│   │   └── Events/                 # DriverWentAvailable, DriverWentOffline, GPSUpdated
│   ├── Accounts/
│   │   ├── Commands/               # CreateInvoice, CreditInvoice, MarkPaid, PostJobs, PriceBulk
│   │   ├── Queries/                # GetAccounts, GetInvoices, GetStatements, GetChargableJobs, VATOutputs
│   │   └── Events/                 # InvoiceCreated, StatementCreated
│   ├── Messaging/
│   │   ├── Commands/               # SendSMS, SendWhatsApp, SendPush, SendPaymentLink, ScheduleMessage
│   │   ├── Queries/                # GetMessageConfig, GetMessageHistory
│   │   └── Handlers/               # Event handlers that react to domain events (BookingAllocated → send driver notification)
│   ├── Reporting/
│   │   ├── Queries/                # RevenueByMonth, DriverEarnings, Profitability, BookingScopeBreakdown, TopCustomers
│   │   └── DTOs/
│   ├── WebBooking/
│   │   ├── Commands/               # CreateWebBooking, AcceptWebBooking, RejectWebBooking, RequestAmendment
│   │   ├── Queries/                # GetWebBookings, GetAccountActiveBookings
│   │   └── Events/                 # WebBookingSubmitted, WebBookingAccepted
│   ├── Partners/
│   │   ├── Commands/               # RegisterPartner, CreateCoverRequest, AcceptCover, DeclineCover, CreateSettlement
│   │   ├── Queries/                # GetPartners, GetCoverRequests, GetPartnerJobs, GetSettlements
│   │   └── Events/                 # CoverRequested, CoverAccepted, CoverDeclined, SettlementCreated
│   ├── Customers/
│   │   ├── Commands/               # CreateCustomer, UpdateCustomer, SaveAddress
│   │   ├── Queries/                # LookupByPhone, GetCustomer, GetSavedAddresses
│   │   └── DTOs/
│   ├── WhatsAppBot/
│   │   ├── Commands/               # ProcessInboundMessage, CreateBotBooking, HandoffToOperator
│   │   ├── Queries/                # GetConversationState
│   │   └── Services/               # ConversationManager, AddressExtractor, BotResponseGenerator
│   ├── Identity/
│   │   ├── Commands/               # Register, Login, RefreshToken, ResetPassword
│   │   └── Queries/                # GetUser, ListUsers
│   └── Tenancy/
│       ├── Commands/               # ProvisionTenant, ImportTenantData, UpdateTenantConfig
│       └── Queries/                # GetTenantConfig, GetCompanyConfig
│
├── RedTaxi.Domain/                 # Entities, enums, value objects, domain events, interfaces
│   ├── Entities/                   # Booking, Driver, Account, Tariff, etc.
│   ├── Enums/                      # BookingStatus, BookingScope, VehicleType, DriverStatus, etc.
│   ├── Events/                     # Domain event definitions
│   └── Interfaces/                 # Repository interfaces, service interfaces
│
├── RedTaxi.Infrastructure/         # EF Core, Redis, external APIs, file storage
│   ├── Persistence/
│   │   ├── RedTaxiDbContext.cs
│   │   ├── Configurations/         # EF entity configurations (fluent API)
│   │   └── Migrations/
│   ├── Redis/                      # GPS cache, driver state, SignalR backplane
│   ├── ExternalServices/           # Google Maps, Ideal Postcodes, Revolut, Twilio, FCM
│   └── Tenancy/                    # Tenant connection resolver, tenant middleware
│
├── RedTaxi.Blazor/                 # Dispatch console (Blazor Server)
│   ├── Pages/                      # Booking, Diary, Availability, DriverStatus, Accounts, Reports
│   ├── Components/                 # BookingForm, BookingDetailPopup, DriverList, ViaManager, etc.
│   ├── Shared/                     # Layout, NavMenu
│   └── wwwroot/
│
├── RedTaxi.WebPortal/              # Customer booking portal
│
└── tests/
    ├── RedTaxi.UnitTests/
    ├── RedTaxi.IntegrationTests/
    └── RedTaxi.ArchTests/          # Architecture guardrails (handler isolation, no cross-feature refs)
```

---

## 4. Key Architecture Decisions

### ADR-001: MediatR over direct service injection
God services exist because services call each other. MediatR handlers are isolated by design — one handler per use case. Cross-cutting concerns (messaging after allocation, audit logging) are handled by domain event handlers, not direct calls.

### ADR-002: Blazor Server over Blazor WASM for dispatch console
Dispatch operators need instant load time and real-time updates. Blazor Server gives us: zero WASM download, built-in SignalR for live diary/dispatch, full server-side rendering, and Syncfusion Blazor components (Scheduler, DataGrid, Charts) which are most mature on Blazor Server.

### ADR-003: Flutter for driver app
Background GPS and push notifications must be reliable on both iOS and Android. Flutter's `geolocator` and `firebase_messaging` packages are production-proven. .NET MAUI was considered (C# consistency) but background services on iOS are still unreliable. React Native background location is notoriously flaky. Dart is syntactically similar to C#.

### ADR-004: Single database with TenantId for v1
Per-tenant databases add operational complexity. For v1 with one tenant (Ace), a single database with `TenantId` on all tables is sufficient. The legacy already has `TenantId` scaffolding. Global query filters in EF Core enforce isolation automatically. Switch to per-tenant DB later if needed — the tenant resolver pattern already supports both modes.

### ADR-005: Redis for real-time state
GPS positions, driver on-shift status, and dispatch notifications change frequently and must be fast. Redis stores ephemeral state; SQL Server stores durable records. SignalR uses Redis as backplane for horizontal scaling.

### ADR-006: Domain events for messaging
When a booking is allocated, the `AllocateBookingHandler` publishes a `BookingAllocated` domain event. A separate `SendDriverNotificationHandler` reacts to that event and sends SMS/WhatsApp/Push. This decouples dispatch from messaging — the exact problem the legacy BookingService has.

---

## 5. Deployment Architecture (v1 — Hetzner VPS)

### Server Spec
- **Provider:** Hetzner Cloud
- **Plan:** CX32 (4 vCPU, 8GB RAM, 80GB SSD) — ~€7/month
- **OS:** Ubuntu 24.04 LTS
- **Location:** Falkenstein (EU) or Ashburn (US) — EU preferred for UK data residency
- **Extras:** Hetzner Firewall (free), automated backups (€1.20/month)

### Docker Compose Stack
```yaml
# docker-compose.yml
services:
  nginx:
    image: nginx:alpine
    ports: ["80:80", "443:443"]
    volumes: [./nginx.conf:/etc/nginx/nginx.conf, ./certs:/etc/ssl]
    depends_on: [redtaxi-api]

  redtaxi-api:
    image: ghcr.io/redbananalabs/redtaxi:latest
    environment:
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=RedTaxi;...
      - Redis__ConnectionString=redis:6379
    depends_on: [sqlserver, redis]

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=${SQL_PASSWORD}
    volumes: [sqldata:/var/opt/mssql]

  redis:
    image: redis:7-alpine
    volumes: [redisdata:/data]

volumes:
  sqldata:
  redisdata:
```

### Domains
- `dispatch.redtaxi.io` → nginx → redtaxi-api (Blazor Server dispatch console)
- `api.redtaxi.io` → nginx → redtaxi-api (REST API)
- `book.acetaxisdorset.co.uk` → nginx → redtaxi-api (customer portal, tenant-branded)

SSL via Let's Encrypt (certbot auto-renewal).

### CI/CD Pipeline (GitHub Actions)
```
Push to main
  → Build .NET 8
  → Run tests
  → Build Docker image
  → Push to GitHub Container Registry (ghcr.io)
  → SSH to Hetzner VPS
  → docker compose pull && docker compose up -d
```

Single YAML file: `.github/workflows/deploy.yml`. No Kubernetes, no Terraform. Blue-green deployment via nginx upstream port switching.

### Backup Strategy
- Hetzner automated server snapshots (daily, €1.20/month)
- SQL Server backup job via Hangfire (nightly, to Hetzner Object Storage)
- Redis AOF persistence (survives container restart)

---

## 6. Authentication & Authorisation

- ASP.NET Core Identity with JWT bearer tokens
- Roles: SuperAdmin, TenantAdmin, Dispatcher, Driver, AccountUser, WebBooker
- Tenant resolution via `X-Tenant-Id` header (API) or subdomain (Blazor/portal)
- Refresh token rotation with Redis-backed revocation
- Driver app: JWT + FCM device registration
- Web portal: JWT for account users, anonymous with rate limiting for cash bookings

---

## 7. Real-Time Communication

| Channel | Technology | Use |
|---------|-----------|-----|
| Dispatch console live updates | SignalR (built-in Blazor Server) | Diary changes, new bookings, driver status |
| Driver app job offers | FCM push + SignalR fallback | New job notification, status updates |
| GPS tracking | SignalR → Redis | Driver location updates every 5-10s |
| Customer tracking | SignalR or polling | "Driver is X minutes away" |

---

## 8. API Versioning

```
/api/v1/*    — Legacy Ace endpoints (compatibility during migration)
/api/v2/*    — New Red Taxi endpoints (clean contract)
```

The v1 surface exists only for the transition period while the Ace frontend migrates to Blazor. Once migration is complete, v1 is deprecated.

---

## 9. Background Job Processing

### Decision: Hangfire over RabbitMQ for v1

The legacy system uses RabbitMQ for message queuing (e.g. SMS gateway polling). For Red Taxi v1, **Hangfire** is recommended instead:

- Simpler to operate (no separate message broker to manage)
- Built-in dashboard for monitoring jobs
- SQL Server backed (no additional infrastructure)
- Supports delayed/scheduled jobs natively
- Good enough for v1 scale (100+ drivers, 1000+ daily bookings per tenant)

RabbitMQ or Azure Service Bus can be introduced later if throughput demands exceed what Hangfire handles.

### Job Types
- Scheduled SMS/WhatsApp sending
- Job offer retry logic (configurable delay between attempts)
- Invoice batch generation
- Driver document expiry alerts
- GPS history cleanup
- Reporting pre-computation

---

## 10. OpenAPI Contract

The API contract should be committed to the repo as the source of truth:

- `docs/openapi/v2.json` — generated from ASP.NET Core Swagger
- Auto-generated on build via CI
- Used for: client code generation (Flutter, customer portal), regression testing, documentation
- Smoke/parity tests can compare v1 and v2 endpoint responses using HAR recordings from the legacy system

---

## 11. Reporting Strategy

### Decision: Syncfusion Blazor Components over Bold Reports for v1

Bold Reports is overkill for v1. Every report on the list is a filtered data grid or chart — not a pixel-perfect paginated report. Use what's already in the Blazor Syncfusion stack:

| Need | Tool | Notes |
|------|------|-------|
| Tabular reports | `SfDataGrid` | Sorting, filtering, grouping, column templates, export to Excel/CSV |
| Charts (revenue, profitability, growth) | `SfChart` | Bar, line, pie, area charts with drill-down |
| Dashboard KPI cards | Blazor components | Simple card components with real-time SignalR updates |
| Invoice/Statement PDFs | QuestPDF | Already used in legacy, proven |
| Credit note PDFs | QuestPDF | Same template engine |
| CSV export | Simple API endpoint | Streaming CSV from EF query |

Bold Reports (Community License) can be added in Phase 4+ if tenants want a custom report designer for their own reports. For v1, all 18 reports (3 driver, 9 booking, 6 financial) are built as Blazor pages with `SfDataGrid` + `SfChart`.

---

## 12. Custom Dispatch UI Components (Blazor)

These require custom Blazor component development beyond standard Syncfusion controls:

### Drag-and-Drop Booking Reallocation
- Operator drags a booking block from one driver column to another on the Scheduler/Diary
- Drop triggers `AllocateBooking` command (MediatR) with new driver ID
- Previous driver receives unallocation notification
- New driver receives allocation notification
- Syncfusion `SfSchedule` supports drag-and-drop events natively — hook into `OnActionCompleted`

### School Run Merge (Drag to Combine)
- When Merge Mode is enabled (toggle in top bar), dragging booking A onto booking B triggers merge logic
- Preconditions validated: both school-run tagged, same destination, same account
- Booking A's pickup becomes a via on booking B
- Booking A marked as merged
- Price recalculates on booking B
- This is CUSTOM logic on top of `SfSchedule` — needs a custom drag handler that detects overlap and shows a merge confirmation dialog

### Real-Time SignalR Updates
- All dispatch console users see live updates without page refresh
- Events pushed via SignalR hub: BookingCreated, BookingAllocated, BookingCancelled, BookingCompleted, DriverStatusChanged, DriverGPSUpdated
- Blazor Server has built-in SignalR — no extra wiring needed
- `SfSchedule` data source is refreshed on each SignalR event
- Dashboard KPI cards update in real-time
- Tracking map updates driver positions every 5-10 seconds

### Booking Form ↔ Map/Price Panel Interaction
- Right-hand panel (Map tab) updates live as operator types in the booking form
- Entering pickup + destination triggers: Google Distance Matrix call → price calculation → route display on map
- Price panel shows: Journey Cost, Charge From Base, Journey Time, Journey Mileage (dead + trip), Tariff applied
- This is a reactive Blazor component pattern — booking form state drives the price panel via shared service/state
