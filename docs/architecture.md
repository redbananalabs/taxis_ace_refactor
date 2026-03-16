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

## 5. Deployment Architecture (v1 — VPS)

```
VPS (Docker Compose)
├── nginx                    # Reverse proxy, SSL termination
├── redtaxi-api              # .NET 8 API (serves both API + Blazor Server)
├── redtaxi-portal           # Customer booking portal (could be same container or separate)
├── sqlserver                # SQL Server 2022 container
├── redis                    # Redis 7 container
└── (optional) seq/grafana   # Logging/monitoring
```

### Ports
- 443 → nginx → redtaxi-api (Blazor + API on same host)
- 443/portal → nginx → redtaxi-portal
- 1433 internal only (SQL Server)
- 6379 internal only (Redis)

### CI/CD
GitHub Actions: build → test → Docker image → push to container registry → SSH deploy to VPS. Blue-green deployment via nginx upstream switching.

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
