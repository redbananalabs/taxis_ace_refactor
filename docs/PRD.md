# Red Taxi Platform — Product Requirements Document

**Version:** 2.0  
**Status:** Active  
**Last Updated:** 2026-03-16  
**Initial Tenant:** Ace Taxis (Dorset)

---

## 1. Product Vision

Red Taxi is a cloud-based taxi dispatch platform that replaces legacy dispatch systems and operates as a multi-tenant SaaS product. The first deployment must fully replicate the current Ace Taxis operational workflow before enabling SaaS onboarding for additional operators.

The platform provides: booking management, intelligent dispatch, driver mobile applications, real-time GPS tracking, automated customer communication, account billing, reporting/analytics, a customer web booking portal, and a foundation for future AI automation.

---

## 2. Objectives

| Priority | Objective |
|----------|-----------|
| P0 | Replicate Ace Taxis operational behaviour exactly |
| P0 | Three frontends at launch: dispatch console, driver app, customer booking portal |
| P1 | Eliminate technical debt (god services, logic in controllers, single-tenant design) |
| P1 | Tenant import wizard so Ace migrates seamlessly |
| P2 | Multi-tenant SaaS onboarding for new operators |
| P3 | AI automation features (voice booking, auto-dispatch, demand forecasting) |

---

## 3. Problems With Current System

The existing Ace system (repo: `taxis_ace_refactor`) suffers from:

- **God services**: `BookingService` (2,142 lines), `AccountsService` (2,247 lines) mix booking, pricing, messaging, dispatch, and COA logic
- **Business logic in controllers**: thin controllers call fat services but routing/validation bleeds across boundaries
- **Hard-coded Ace rules**: pricing, messaging templates, and company config are not tenant-configurable
- **Inconsistent DTOs**: 175KB of DTOs with naming inconsistencies and unused types
- **Single-tenant design**: tenancy scaffolding exists (header-based `X-Tenant-Id`, per-tenant DB mode) but no data isolation, no provisioning workflow
- **Minimal testing**: only structural/guardrail tests exist, no business logic coverage
- **No CI/CD pipeline**: manual deployment via IIS

---

## 4. Core Modules

### 4.1 Booking Management
Create, edit, duplicate, cancel, and complete bookings. Supports single bookings, return bookings, repeat bookings (daily/weekly/fortnightly with day selection and end date), and ASAP bookings.

### 4.2 Dispatch & Driver Allocation
Manual allocation (operator selects driver from list), soft allocation (tentative assignment), drag-and-drop diary allocation. School run merge: operators can drag school bookings onto each other when they share the same destination + account — the merged booking's pickup becomes a via stop.

### 4.3 Pricing & Tariff Engine
Auto-calculates price on any booking field change. Supports standard tariffs, night tariffs, airport tariffs, and per-account custom tariffs. Uses Google Distance Matrix for mileage/duration (averages A→B and B→A).

### 4.4 Driver Availability
Drivers declare availability via: app login, pre-declared shift schedules, calling dispatch, or operator manual override. States: Offline, Available, Busy, On Job, Break, Unavailable. Availability grid shows all drivers with time blocks.

### 4.5 Corporate Accounts
Business accounts with custom tariffs, monthly invoicing, passenger lists, and web booking access. Account jobs track two prices: `PriceAccount` (invoice amount) and `Price` (driver payout).

### 4.6 Messaging & Notifications
SMS (Twilio), WhatsApp, push notifications (FCM). Automated messages on: booking confirmation, driver allocation, driver arrival, journey completion, payment links. Operator can send custom/scheduled messages.

### 4.7 Payment Links
Revolut integration for sending payment links to customers. Track payment status, resend requests, send receipts. Future: Stripe, GoCardless.

### 4.8 Reporting & Settlement
Driver earnings, account invoicing, driver statements, profitability reports, booking breakdowns by scope/period, VAT outputs, credit notes. Driver commission is calculated at settlement, not booking level.

### 4.9 Driver Mobile Application
Receive job notifications, accept/reject, navigate, update status, upload compliance documents, GPS tracking in background. Must support reliable background GPS and push notifications.

### 4.10 Customer Web Booking Portal
Public-facing booking interface for cash customers and authenticated account customers. Address autocomplete, price quote, booking submission, amendment requests, cancellation requests.

### 4.11 Multi-Tenant SaaS
Each operator is a tenant with isolated data, custom tariffs, drivers, branding, and integrations. Tenant import wizard for migrating from existing systems. Pricing tiers based on drivers, volume, and enabled modules.

---

## 5. Operator Dispatch Interface

The dispatch console has four primary tabs (bottom navigation):

### Availability
- Grid view: hours on Y-axis, drivers on X-axis
- Colour-coded blocks showing declared availability per driver
- Shows driver name, number, vehicle type (Saloon/Estate/SUV/MPV)
- "Use Old Driver Availability" toggle for copying previous day's pattern
- Click driver to set/edit availability

### Driver Status
- Same grid layout as availability but shows allocated bookings overlaid on availability
- Colour-coded per driver (each driver has a unique RGB colour)
- Hatched pattern = completed jobs
- Booking cards show pickup → destination text

### Booking
- Full booking form: date/time, ASAP toggle, return toggle, repeat booking config
- Pickup address + postcode with autocomplete (Ideal Postcodes + address lookup)
- Destination address + postcode with autocomplete
- Swap addresses button
- Booking details (free text)
- Arrive By toggle + Calculate Pickup button
- Add VIA stops (modal: address + postcode per via, orderable)
- Driver Price (£) — auto-calculated, manually overridable
- Manually Priced checkbox
- Passengers count
- All Day checkbox
- Reset Price button
- Name, Hours, Minutes (journey duration)
- Email, Phone + Lookup button (finds customer by phone)
- Confirmation status dropdown
- Scope dropdown: Cash, Account, Rank, Card
- Account number (shown when scope = Account)
- Allocate Driver button
- Action buttons: Cancel On Arrival, Send Quote, Cancel, Create

### Diary
- Calendar timeline view (day view)
- Columns per driver showing their bookings as blocks
- Click booking to open detail popup
- Show Allocated / Show Completed toggles
- Month navigation

### Booking Detail Popup
Shows: booking number, scope badge (CASH/ACCOUNT), pickup details, destination, passenger info, journey details (type, allocated driver, price, time, distance with dead miles vs trip miles breakdown), payment status with Send Payment Link / Resend buttons, confirmation status, booked by + date.

Action buttons: Soft Allocate, Allocate Booking, Edit Booking, Duplicate Booking, Driver Arrived, Complete Booking, Cancel Booking.

### Menu
Booking, Diary, Merge Mode toggle, Logs, COA Entries, Search, No (turn-downs), Text Message, Ticket Raise, Logout.

---

## 6. Booking Lifecycle

```
Created → Allocated → Accepted → Driver En Route → Driver Arrived → Passenger Onboard → Completed
                                                                                      ↘ Cancelled
                                                                                      ↘ No Show / COA
```

Each transition triggers: booking status update, driver availability update, optional messaging (configurable per event), UI notification to dispatch console.

---

## 7. Pricing Model

### Cash / Rank / Card Jobs
- Single price calculated = driver price
- Company earns via **commission** charged to driver at settlement time
- Commission rate is a driver-level or company-level config

### Account Jobs
- Two prices on every booking:
  - `PriceAccount` — what the account is invoiced
  - `Price` — what the driver is paid
- Account may have custom tariffs overriding default rates
- Changing account number triggers price recalculation

### Price Calculation
- Uses Google Distance Matrix API
- Averages A→B and B→A distances for fairness
- Tariff applied: base fare + (miles × per-mile rate) + (minutes × per-minute rate)
- Recalculates on any change to: pickup, destination, vias, passengers, vehicle type, account number
- Manual override available (sets `ManuallyPriced = true`, locks auto-recalc)

### Tariff Components
- Base fare
- Per mile rate
- Per minute rate  
- Minimum fare
- Waiting time charge
- Passenger surcharge
- Airport surcharge
- Dead miles tracking (total miles = dead miles + trip miles)

---

## 8. COA (Cancel on Arrival) Rules

COA is triggered when a passenger doesn't show up after the driver has arrived.

- Account jobs: account may be billed the **full fare** (they booked, their passenger no-showed)
- Driver: paid a **reduced amount** (didn't complete the full journey)
- COA records are tracked separately (`COARecord` table)
- COA pricing is per-booking: operator can adjust the account charge and driver payout independently

---

## 9. School Run Behaviour

School bookings are regular bookings with a school-run tag/flag.

**Special dispatch behaviour:** When multiple school bookings share the same destination AND the same account, operators can drag one booking onto another in the diary view to **merge** them. The merged booking's pickup address becomes a via stop on the target booking. This is a dispatch UI feature, not a backend auto-merge.

Drivers may have availability patterns like "AM School Only" or "PM School Run" indicating they only work school run slots.

---

## 10. Substitute Drivers

Drivers from other taxi companies who take Ace jobs. Same as normal drivers in the system but flagged as substitutes. Same payment rules, same dispatch workflow. The flag enables separate reporting and filtering.

---

## 11. Implementation Phases

### Phase 1 — Core Dispatch (Weeks 1-4)
- Backend: Booking CRUD, pricing engine, driver availability, dispatch/allocation
- Blazor dispatch console: booking form, diary view, availability grid, driver status
- Auth: JWT + Identity with role-based access
- Database: EF Core migrations from legacy schema + TenantId column

### Phase 2 — Accounts & Messaging (Weeks 5-6)
- Account management, invoicing, statements, credit notes
- SMS/WhatsApp/Push notification system
- Payment link integration (Revolut)
- Customer web booking portal

### Phase 3 — Driver App & Reporting (Weeks 7-8)
- Flutter driver app: login, jobs, accept/reject, navigation, GPS tracking, document upload
- Reporting module: earnings, profitability, booking breakdowns, VAT

### Phase 4 — SaaS & Migration (Weeks 9-12)
- Tenant import wizard (Ace SQL Server → Red Taxi)
- Tenant provisioning workflow
- Tenant-level config: branding, tariffs, messaging templates
- Subscription billing integration

### Phase 5+ — AI & Growth (Future)
- AI voice booking agent
- AI dispatch suggestions
- Dynamic pricing engine
- Demand forecasting
- Driver monitoring/scoring

---

## 12. Performance Requirements

- Support 100+ drivers per tenant
- 1,000+ daily bookings per tenant
- Real-time diary updates < 500ms latency
- GPS tracking updates every 5-10 seconds
- System uptime target: 99.9%

---

## 13. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | Exact tariff rate values for Ace (base fare, per-mile, etc.) | Pending — can extract from legacy `Tariff` table |
| 2 | Commission rate structure (flat % or tiered?) | Pending |
| 3 | Web booking portal auth flow for account customers | Pending |
| 4 | Revolut API version and webhook setup details | Can extract from legacy code |
| 5 | Flight tracking integration for airport pickups | Pending — future phase? |
| 6 | Live driver map timing — Phase 1 or later? | Pending |
