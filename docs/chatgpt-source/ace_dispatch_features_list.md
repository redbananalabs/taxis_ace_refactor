# Ace Dispatch → SaaS: Features List (Discussed)

## A) Core Dispatch & Booking (MVP)
- **Create / amend / cancel bookings** (addresses, pax, datetime, notes)
- **Price calculation (live)** recalculates on journey edits and **account number changes** (account tariffs)
- **Driver allocation / dispatch workflow**
  - manual allocation
  - job status updates (accepted/rejected/arrived/picked-up/completed/no-show)
- **Driver availability**
  - drivers can pre-submit availability ahead of time
  - support multiple operational processes (some drivers log on via app; some call in / go straight to pre-allocated jobs)
- **Accounts / corporate bookings**
  - account pricing rules and billing-ready journey records
- **Geo support**
  - store **lat/long** for pickup/drop-off (and optionally intermediate stops) to improve dispatch accuracy

## B) Messaging, Comms & Automation
- **Scheduled local SMS** (cost-saving) for confirmations/reminders
- **Operator-scheduled messages** (custom templates and scheduling)
- **WhatsApp marketing** option (tenant-configurable) vs SMS
- **Detect call/reject behaviour** for job offers and log outcomes (hooks for later automation)

## C) AI & “Improvements” (Enhancement Pack)
- **AI voice agent** for chasing unresponsive drivers / job acceptance follow-ups (optional module)
- **AI-assisted dispatch** (suggest best driver based on proximity/availability/history) *(future)*
- **AI summaries** for operator: “what’s happening now”, anomalies, missed jobs *(future)*

## D) Integrations Pack (Discussed)
- **HMRC Making Tax Digital (MTD)** integration readiness (accounts/export pipeline)
- **CMAC** integration (partner dispatch / transfers workflow)
- **TextLocal** (SMS provider)
- **Twilio** (SMS/WhatsApp provider option)
- **WhatsApp** (direct or via Twilio)
- **Booking.com** integration (booking ingestion / partner channel)
- **Auto-dispatch** rules engine (optional automation layer, can start as “suggestions” then “auto”)

## E) SaaS / Multi-Tenant Platform Features
- **Multi-tenant** support (start with “Ace as Tenant 1”, evolve to shared-db + TenantId)
- **Tenant settings**
  - replace hard-coded account numbers, ignored phone numbers, and bespoke Ace outputs with tenant-configured rules
  - per-tenant branding and message templates (“Ace Taxis” → tenant name)
- **Roles & permissions**
  - Admin, Dispatcher, Driver, Accountant/Reports (minimal set)
- **Tenant onboarding**
  - create tenant, set defaults, configure messaging providers, templates, tariffs
- **Audit trail**
  - key actions logged (price overrides, cancellations, manual edits)

## F) Engineering / Production Readiness (Required)
- **Thin controllers** (no business logic in controllers)
- **Service/handler-based design** (vertical slices; feature folders)
- **Logging & observability**
  - structured logs (Serilog recommended), correlation IDs, consistent error shape
  - health checks + basic metrics
- **Testing safety net**
  - smoke/parity tests for v1 vs v2 endpoints (generated from Swagger/HAR)
  - expand unit tests around extracted pure logic over time
- **Security & config hygiene**
  - remove secrets from repo; use env vars/user-secrets; example configs committed
- **CI pipeline**
  - build + test on every PR; staging deploy readiness

## G) API Evolution / Migration Plan Features
- **Side-by-side v1 + v2 endpoints**
  - v2 under `/api/v2/...` to allow controller-by-controller migration
- **OpenAPI contract as source of truth**
  - commit Swagger JSON to repo; use for client generation and regression checks

---
Generated for “features list we discussed” (dispatch SaaS + integrations + AI improvements).
