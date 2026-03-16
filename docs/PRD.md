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

**Important distinction:** Substitute drivers are NOT the same as partner-company coverage. A substitute is a person temporarily operating under the tenant's dispatch control. Partner coverage is a booking fulfilled by another company entirely. These need different records, permissions, and settlement logic.

---

## 11. Partner Network / Cross-Tenant Job Sharing

Taxi companies frequently need to share work — own fleet at capacity, geographic convenience, out-of-area pickups, night/weekend coverage, or reciprocal agreements.

### Partner Model
- Each tenant can register partner relationships with other tenants on the platform
- Partners have defined coverage rules and commercial terms
- Partner data is isolated — no accidental cross-tenant data leakage

### Cover Request Flow
1. Source tenant's dispatcher (or auto-escalation rule) creates a cover request
2. Target tenant receives the request with booking details and offered price
3. Target tenant accepts or declines
4. If accepted, target tenant dispatches from their own fleet
5. Source tenant retains visibility into job progress (appropriate to relationship level)
6. Settlement record generated per agreed rules

### Settlement Models
- Fixed referral fee (e.g. £5 per job)
- Percentage split (e.g. source keeps 15%, partner gets 85%)
- Net fulfilment price (partner quotes their price, source marks up to customer)
- Account customer pass-through (account rate applies, split agreed separately)

### No-Driver Escalation Path
1. Internal driver pool exhausted
2. Substitute driver pool checked (if configured)
3. Partner companies offered the job (priority order or broadcast)
4. If no partner accepts within timeout, booking flagged as unallocated exception

---

## 12. Dispatch Scoring Engine

Beyond manual dispatch, the system supports assisted and (future) automatic dispatch using a weighted scoring model.

### Candidate Scoring Inputs
- Driver availability state (gate: must be eligible)
- Vehicle compatibility (gate: must match requirements)
- Estimated distance to pickup
- Estimated time to pickup (ETA)
- Driver shift status and remaining hours
- Recent workload / fairness balancing
- Customer-driver affinity (if repeat customer has preferred driver)
- Job priority level
- Penalty for recently declined jobs
- Partner option only after internal scores fall below threshold or timeout

### Dispatch Flow
1. Booking becomes dispatchable
2. System validates booking completeness
3. Internal driver candidate pool generated
4. Scores calculated per above inputs
5. Dispatcher sees ranked recommendations (or system auto-assigns if policy permits)
6. If no viable internal driver → escalate (substitute → partner → unallocated exception)
7. Assigned driver notified
8. Booking state updated through lifecycle

For v1: manual dispatch with scored suggestions displayed. Auto-dispatch is Phase 3+.

---

## 13. White-Label Booking Websites

Each tenant can have one or more branded booking websites that feed directly into the dispatch system.

### Website → Dispatch Pipeline
1. Customer lands on tenant's branded website (SEO-optimised route/area pages)
2. Customer requests a quote or books directly
3. Booking data posts into the dispatch platform via API
4. Operator confirms (or future: automated acceptance rules)
5. Booking enters dispatch queue

### SEO Strategy
- Auto-generated landing pages per route (e.g. "Taxi from Shaftesbury to Bristol Airport")
- Local area pages for organic search
- Structured data for Google search visibility
- Each page has an embedded booking form that feeds into the platform

### Commercial Value
Combining lead generation websites + booking conversion + dispatch fulfilment + reporting creates a stronger SaaS than dispatch alone. This is a key differentiator.

For v1: API endpoint for website booking ingestion. White-label website builder is Phase 4+.

---

## 14. Customer Directory

Unlike the legacy system (which stores passenger details directly on bookings), Red Taxi introduces a proper Customer entity.

### Customer Record
- Name, phone, email
- Account type (casual, regular, business account)
- Default pickup notes / preferences
- Saved addresses
- Marketing source (how they found us)
- Booking history (derived)
- Notes

### Benefits
- Phone lookup instantly populates booking form (existing "Lookup" feature, but backed by a real customer record)
- Repeat customer recognition
- Account customer linking
- Marketing source tracking for website-generated leads
- Future: customer self-service portal, loyalty

---

## 15. WhatsApp Chatbot Booking Channel (Phase 2+)

Customers can book taxis via WhatsApp conversation with an AI-powered chatbot.

### Flow
1. Customer messages the tenant's WhatsApp Business number
2. Chatbot greets customer, asks for pickup and destination
3. Bot extracts addresses, validates, calls pricing engine for quote
4. Customer confirms booking
5. Booking created in dispatch system as a WebBooking (pending or auto-accepted based on config)
6. Customer receives confirmation message with booking details
7. On dispatch: driver allocated notification sent via same WhatsApp thread
8. Driver arrival / journey updates sent conversationally

### Chatbot Capabilities
- Natural language address extraction
- Price quoting
- Booking creation and confirmation
- Booking status enquiry ("Where's my taxi?")
- Cancellation requests
- Handoff to human operator for complex queries

### Technical Approach
- WhatsApp Business API (existing Twilio integration)
- AI layer (Claude or similar) for conversation management
- Structured tool calls to booking/pricing APIs
- Conversation state managed per customer phone number
- Falls back to operator notification if bot confidence is low

For v1: WhatsApp remains one-way notifications only. Chatbot booking is Phase 2/3.

---

## 16. Implementation Phases

### Phase 1 — Core Dispatch (Weeks 1-4)
- Backend: Booking CRUD, pricing engine, driver availability, dispatch/allocation
- Customer directory with phone lookup
- Blazor dispatch console: booking form, diary view, availability grid, driver status
- Auth: JWT + Identity with role-based access
- Database: EF Core migrations from legacy schema + TenantId column

### Phase 2 — Accounts, Messaging & Web Portal (Weeks 5-6)
- Account management, invoicing, statements, credit notes
- SMS/WhatsApp/Push notification system (one-way)
- Payment link integration (Revolut)
- Customer web booking portal
- Substitute driver flagging and reporting

### Phase 3 — Driver App & Reporting (Weeks 7-8)
- Flutter driver app: login, jobs, accept/reject, navigation, GPS tracking, document upload
- Reporting module: earnings, profitability, booking breakdowns, VAT
- Dispatch scoring engine (assisted suggestions for operators)

### Phase 4 — SaaS & Migration (Weeks 9-12)
- Tenant import wizard (Ace SQL Server → Red Taxi)
- Tenant provisioning workflow
- Tenant-level config: branding, tariffs, messaging templates
- Subscription billing integration
- SaaS packaging: Starter / Growth / Professional / Enterprise tiers

### Phase 5 — Partner Network & Job Sharing (Weeks 13-16)
- Partner company registry and relationship management
- Cover request workflow (create, accept, decline)
- Cross-tenant job assignment with visibility controls
- Settlement engine: referral fee, percentage split, net fulfilment
- Partner job audit trail and reporting
- No-driver escalation path (internal → substitute → partner)

### Phase 6 — White-Label Websites & Booking Channels (Weeks 17-20)
- Public booking API for website ingestion
- WhatsApp chatbot booking channel (AI-powered conversational booking)
- White-label website builder (SEO route pages, area pages, embedded booking forms)
- Booking source tracking and attribution reporting

### Phase 7+ — AI & Growth (Future)
- AI voice booking agent (phone intake)
- Auto-dispatch (fully automated assignment based on scoring engine)
- Dynamic pricing engine
- Demand forecasting
- Driver monitoring/scoring
- AI operator copilot (natural language queries, anomaly detection)
- Proactive partner coverage recommendations

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

---

## 17. Master Tenant / Network Dispatch (Phase 5+)

The system will support a Master Tenant concept — a central marketplace that receives bookings from shared booking channels and distributes them to participating operators.

### Flow
1. Customer books through the marketplace (e.g. "First Taxis" brand)
2. Master tenant receives the booking
3. Job broadcast to participating operators based on rules (geographic radius, fastest acceptance, operator preference)
4. Operators accept or reject
5. Accepted operator dispatches from their fleet

First real-world use case: **First Taxis** — a network brand that distributes jobs to member taxi companies.

This extends the Partner Network model — the master tenant is essentially a specialised tenant that only distributes, never fulfils directly.

---

## 18. Telephony / Caller ID Integration

### Caller ID Booking Pop
When an inbound call is answered, the system:
1. Detects the caller's phone number
2. Looks up customer in the Customer Directory
3. If found: displays customer details + current bookings + previous bookings
4. Operator can select a previous booking and click "Confirm" to repeat it instantly
5. If not found: opens a new booking form pre-populated with the phone number

### VoIP Integration
The platform integrates with external VoIP providers (not hosting its own):
- 3CX
- V4Voip
- Other SIP-based providers

Requirements: detect inbound caller number, trigger booking pop, identify which operator answered.

Must work on both desktop and mobile dispatch interfaces.

---

## 19. Multi-Mode Dispatch

The platform supports three dispatch modes to suit different operator types:

### Grid Dispatch
Traditional dispatch board showing open jobs, driver status, and assignments. Used by busy dispatch offices with multiple operators.

### Diary / Scheduler Dispatch
Calendar-based dispatch (the current Ace system's primary mode). Used for airport bookings, pre-bookings, school contracts, account journeys. Split-screen: booking form left, scheduler/map right.

### Mobile Dispatch
Designed for rural operators and owner-drivers who answer calls while driving. Features: fast booking creation, minimal typing, caller ID pop, repeat booking shortcuts, view active bookings. This is a simplified mobile-first interface, not the full dispatch console.

---

## 20. AI Website Builder & Marketing Automation (Phase 6+)

### AI Website Builder
Each tenant can generate a branded booking website automatically, powered by AI tools (e.g. Lovable). Capabilities: automatic design using tenant branding, embedded booking engine, SEO-optimised structure. This is a paid module.

### SEO Content Automation
The system generates ongoing SEO content: service pages, location pages, airport transfer route pages (e.g. "Taxi Bath to Heathrow", "Taxi Gillingham to Bristol Airport"). Content generation runs on configurable schedule (weekly/monthly).

### Social Media Automation
AI-generated social media posts for Facebook, Instagram, and X. Configurable posting frequency (weekly/bi-weekly/monthly). Content based on routes served, testimonials, seasonal messaging.

---

## 21. External Booking Channel Integrations (Phase 6+)

The platform integrates with third-party booking sources:
- Booking.com
- Hotel booking systems
- Travel platforms
- Airport transfer aggregators

Bookings flow directly into dispatch. Can be used by master tenant or individual tenants.

---

## 22. Provider Configuration System

Tenants can either use their own provider accounts or platform-bundled services:

| Service | Own Provider | Platform Bundled |
|---------|-------------|-----------------|
| SMS | TextLocal, own Twilio | Platform SMS bundle |
| Address lookups | Own Google Maps key, own postcode API | Platform address service |
| Email | Own SendGrid | Platform email service |
| Payments | Own Stripe/Revolut | Platform payment processing |
| Maps | Own Google Maps | Platform maps |

This enables the SaaS to offer bundled services at margin while letting larger operators use their own accounts.

---

## 23. Keyboard Shortcuts (Dispatch Console)

The desktop dispatch console supports keyboard shortcuts for speed:

| Key | Action |
|-----|--------|
| F1 | Switch to Map view |
| F2 | Switch to Scheduler view |
| F3 | Open Messages |

Additional shortcuts TBD based on operator feedback.

---

## 24. Dispatch Console Desktop Layout

Split-screen layout:
- **Left panel**: Booking form (new booking / edit booking)
- **Right panel**: Tabbed view with MAP, SCHEDULER, LOGS, COA ENTRIES

### Map Tab
Shows Google Maps route A→B with journey cost panel displaying:
- Charge From Base (Yes/No)
- Journey Time
- Journey Mileage with dead miles vs trip miles breakdown
- Applied tariff name

### Scheduler Tab
Day view calendar with driver columns, booking blocks, COA banners at top. Toggles: Show Allocated, Show Completed, Merge Mode. Views: Day / Agenda.

### Logs Tab
Two sub-tabs: Booking Logs (action history) and Action Logs. Paginated table: DateTime, Booking#, User, Action. Actions tracked: Booking Created, Allocated, Updated, Marked as Completed, Driver Arrived (Sent).

### COA Entries Tab
Table of cancel-on-arrival records: COA Date/Time, Journey Date/Time, Pickup Address, Passenger, Account number.

### Top Bar
Ticket Raise, NO (turn-down), Text Message, Search, notification bell, Logout. Confirm SA (Confirm Soft Allocates) button.

---

## 25. Complete Booking Sources

All channels through which bookings can enter the system:

| # | Source | Phase | Description |
|---|--------|-------|-------------|
| 1 | Operator phone entry | 1 | Dispatcher creates booking during/after phone call |
| 2 | Dispatch console direct | 1 | Operator creates booking proactively |
| 3 | Customer web portal | 2 | Account customers submit via branded portal |
| 4 | Cash web booking | 2 | Public customers via website booking form |
| 5 | Repeat from phone lookup | 1 | Operator selects previous booking from caller ID popup |
| 6 | Rank pickup (driver-created) | 1 | Driver records rank job after pickup |
| 7 | Recurring booking engine | 1 | System auto-generates from repeat booking rules |
| 8 | White-label website | 6 | SEO landing page booking forms |
| 9 | External API integration | 6 | Booking.com, hotel systems, travel platforms |
| 10 | WhatsApp AI chatbot | 6 | Customer messages WhatsApp, AI creates booking |
| 11 | AI voice agent (phone) | 7+ | AI answers phone call, captures booking details |
| 12 | CMAC integration | Future | Corporate taxi network partner bookings |
| 13 | Mobile app (customer) | Future | Customer mobile app direct booking |
| 14 | Driver-entered booking | 3 | Driver creates booking via driver app |

---

## 26. Address Geolocation

All addresses in the system should store latitude and longitude alongside the text address and postcode.

### Uses
- Dispatch accuracy (distance-to-pickup calculation)
- Demand heatmaps and analytics
- Route visualisation on map
- ETA calculation
- Geofence matching (airport zones, coverage areas)

### Implementation
- Geocode on address entry (via Google Places / Ideal Postcodes)
- Store lat/lng on Booking (pickup + destination), BookingVia, LocalPOI, SavedAddress
- Legacy `DriverLocationHistory` already stores lat/lng for GPS tracking

---

## 27. Naming Convention: "Scope" → "Work Type"

The legacy system uses "Scope" for the booking payment type (Cash/Account/Rank/Card). This is unclear terminology.

**Recommendation for Red Taxi:** Rename to **Work Type** or **Booking Type** in all new UI and API contracts. The underlying enum values remain the same for backward compatibility with the import wizard. The Blazor dispatch console should display "Work Type" in the UI dropdown (currently shows "scope:").

---

## 28. AI Route Intelligence (Phase 7+)

AI analyses booking data and demand patterns to suggest:
- High-demand routes that should have landing pages
- Profitable destinations for targeted marketing
- New SEO pages to auto-generate
- Pricing optimisation opportunities (routes where competitors charge more)
- Time-of-day demand patterns for driver shift planning

---

## 29. Admin Navigation Structure (from desktop screenshots)

The full left sidebar navigation of the legacy dispatch console:

```
Dashboards
Booking & Dispatch          ← main dispatch screen (split-screen layout)
Tracking                    ← live GPS map + driver list
Availability                ← driver availability grid with presets
Availability Logs           ← audit trail of availability changes
Local POIs                  ← points of interest (451 in Ace)
Bookings ▸
  ├── Web Bookings          ← pending web portal bookings
  ├── Global Search         ← advanced multi-field booking search
  ├── Audit View            ← booking action history
  ├── Card Bookings         ← bookings paid by card
  ├── Cancel Range          ← bulk cancel bookings in date range
  └── Cancel Range Report   ← report on cancelled bookings
Accounts                    ← corporate account list (49 in Ace)
Driver ▸
  ├── Driver List           ← all drivers with details
  └── Expiry's              ← document expiry tracking
Tariffs                     ← tariff configuration (3 tariffs in Ace)
Account Tariffs             ← per-account tariff overrides
Billing & Payments ▸        ← invoices, statements, payment tracking
Reports ▸                   ← all reporting modules
Company Settings            ← tenant configuration
Message Settings            ← messaging template and channel config
Utilities                   ← miscellaneous tools
```

---

## 30. Dashboard KPIs

The dashboard shows real-time operational metrics:

### Driver Earnings Tables
- **Today's Totals**: per-driver table with Jobs, Cash, Acc, Rank, Cash Comms, Rank Comms, Total, Total Comms. Searchable. Shows Total Earnings and Total Commissions at bottom.
- **Week's Totals**: same structure for current week

### Booking Stats
- Table: Booked By (operator name), Cash Jobs Booked, Account Jobs Booked, Rank Jobs Booked, Total Booked. Searchable.

### KPI Cards (real-time counts)
| Card | Description | Example |
|------|-------------|---------|
| Today Bookings | Total bookings for today | 69 |
| Jobs Booked Today | New jobs created today | 30 |
| Drivers | Active driver count | 22 |
| POIs | Total points of interest | 451 |
| Today Unallocated | Bookings without driver | 4 |
| Day New Customer | New customers today | 29 |
| Week New Customer | New customers this week | 136 |
| Month New Customer | New customers this month | 455 |
| Day Returning Customer | Returning customers today | 11 |
| Week Returning Customer | Returning customers this week | 47 |
| Month Returning Customer | Returning customers this month | 176 |

### Top Bar Actions
- Direct Message button → send to selected drivers
- Global Message button → broadcast to all drivers
- SMS Heartbeat indicator (shows last SMS gateway ping time — monitors Android SMS device health)

---

## 31. Live Tracking Page

Full-page view with:
- Google Map showing live driver positions (each driver has unique icon/colour marker)
- Map/Satellite toggle
- F12 for fullscreen mode
- Driver List table below map: #, Name, Reg No, Last Updated (timestamp), Speed (mph)
- Drivers update position every 5-10 seconds
- Speed calculated from GPS data

---

## 32. Reports Navigation (from screenshots)

Complete reports menu structure:

```
Reports
├── Driver Reports
│   ├── Availability Report
│   ├── Expenses Report
│   └── Earning Report
├── Bookings
│   ├── Turndown History
│   ├── Airport Runs
│   ├── Duplicate Bookings
│   ├── Count By Scope
│   ├── Top Customer
│   ├── Pickups By Postcode
│   ├── By Vehicle Type
│   ├── Average Duration
│   └── Growth By Period
└── Financial
    ├── Payouts By Month
    ├── Revenue By Month
    ├── Profitability On Invoice
    ├── Total Profitability By Period
    ├── Profitability By DateRange
    └── QR Code Adverts
```

### Airport Runs Report
Shows airport journeys with period filter (Last 1/3/6/12 Months). Table: Driver, Date, Pickup, Destination, Price. Below the summary table: "Airport Journeys - 1 Month" detail grouped by driver (expandable), showing Date, Journey, Price.

Example prices visible: Heathrow Terminal 3 = £255, Heathrow Terminal 5 = £235, Bristol Airport = £128-£156, Bournemouth Airport = £110. These are clearly fixed/zone prices for airport routes.

---

## 33. User Settings Menu

Profile dropdown (top-right, click avatar) shows:
- User name + "Pro" badge (role indicator)
- **Dark Mode** toggle
- **Dark Sidebar** toggle
- **Notifications** toggle
- **Mute Notifications** toggle
- **Ticket Raise** link
- **Logout**

The dispatch console supports dark mode — the Blazor rebuild must also support light/dark theme switching.

---

## 34. Import Wizard — Ace Data to Migrate

Based on all screenshots, the import wizard must migrate:

| Data | Count (Ace) | Priority |
|------|-------------|----------|
| Accounts | 49 | P0 |
| Account Tariffs | 3 | P0 |
| Standard Tariffs | 3 | P0 |
| Drivers + Profiles | ~22 active | P0 |
| Local POIs | 451 | P0 |
| Bookings (historical) | 135,000+ (based on booking IDs) | P1 |
| Booking Vias | Unknown | P1 |
| Account Passengers | Unknown | P0 |
| Invoices + Statements | Unknown | P1 |
| Credit Notes | Unknown | P1 |
| Message Settings | 1 config set | P0 |
| Company Settings | 1 config set | P0 |
| Driver Availability templates | Recurring patterns | P1 |
| Users (operator logins) | ~5-10 | P0 |
