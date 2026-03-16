# Taxi Dispatch SaaS — Full PRD

## 1. Product Summary
A cloud-based taxi dispatch and booking management platform for small to medium operators, designed to support:
- direct bookings,
- operator-entered bookings,
- automated and manual dispatch,
- driver management,
- partner-company job sharing,
- substitute driver workflows,
- future AI automation.

## 2. Target Customers
### Primary
- small rural taxi companies with roughly 5–20 active drivers,
- local/regional operators with 20–100 drivers,
- airport transfer operators,
- owner-managed firms replacing legacy dispatch tools.

### Secondary
- larger multi-depot regional fleets,
- private hire groups sharing overflow work,
- dispatch centers serving multiple companies.

## 3. Core Personas
### Company Owner / Admin
Needs visibility into operations, driver performance, pricing, revenue, and setup.

### Dispatcher / Operator
Needs fast booking entry, job monitoring, reassignment tools, live fleet view, and exception handling.

### Driver
Needs clear jobs, status management, navigation, proof of work, and earnings visibility.

### Partner Operator
Needs controlled access when covering jobs for another company.

### Finance / Back Office
Needs booking status, settlement, partner billing, and exportable reports.

## 4. Main Product Goals
- reduce manual dispatch effort,
- increase driver utilisation,
- improve response times,
- support overflow coverage,
- centralise booking + dispatch + reporting,
- provide a SaaS path that scales commercially.

## 5. Scope of v1
### In scope
- tenant/company setup,
- user roles,
- driver and vehicle records,
- booking management,
- manual and assisted dispatch,
- driver status updates,
- basic live map / location handling,
- substitute / external driver handling,
- partner-company job allocation,
- fare/pricing rules,
- notifications,
- reporting basics,
- API-ready architecture.

### Out of scope for initial v1 launch
- fully autonomous AI dispatch,
- deep financial accounting,
- marketplace-style open network bidding,
- highly advanced route optimisation,
- full customer self-service ecosystem.

## 6. Booking Types
- immediate ASAP bookings,
- scheduled bookings,
- airport transfers,
- local town journeys,
- return trips,
- account / business bookings,
- partner-covered jobs.

## 7. Booking Sources
- operator manual entry,
- company website forms,
- route/landing pages,
- mobile booking app (future),
- API ingestion,
- AI phone/voice booking (future).

## 8. Core Functional Requirements
### 8.1 Booking Management
System must support:
- customer details,
- pickup/dropoff,
- via stops,
- booking time,
- passenger count,
- luggage/special notes,
- wheelchair / accessibility flags,
- vehicle class requirements,
- quoted fare and pricing basis,
- booking status lifecycle,
- assigned company and assigned driver.

### 8.2 Dispatch
System must allow:
- manual assignment,
- assisted suggestions,
- reallocation,
- no-driver exception handling,
- partner-company escalation,
- external driver assignment,
- dispatch audit history.

### 8.3 Driver Operations
Drivers must be able to:
- go online/offline,
- mark available/busy/on-break,
- receive jobs,
- accept/decline jobs where allowed,
- update arrival/passenger onboard/completed,
- send notes/issues,
- share live location.

### 8.4 Partner & External Coverage
The system must distinguish:
- own-fleet drivers,
- substitute drivers,
- partner-company drivers.

Each category needs different permissions, economics, and visibility.

### 8.5 Pricing
System should support:
- fixed route pricing,
- area-based pricing,
- meter-estimate/manual price,
- surcharge rules,
- airport extras,
- time-window adjustments,
- partner fulfilment settlement rules.

### 8.6 Reporting
At minimum:
- bookings by day/week/month,
- completed vs cancelled,
- driver activity,
- partner jobs sent/received,
- revenue estimates,
- dispatch performance.

## 9. Non-Functional Requirements
- responsive web dashboard,
- mobile-first driver UI,
- clear audit trail,
- tenant isolation,
- role-based permissions,
- support for phased modular expansion,
- observability and logs,
- reliable background job handling.

## 10. Success Metrics
- time to enter booking,
- time to assign booking,
- percentage auto/assisted assigned,
- driver utilisation,
- cancellation rate,
- partner cover rate,
- operator hours saved,
- booking growth from web channels.

## 11. Commercial Packaging Requirement
Packages must support different business sizes. Key entitlement dimensions:
- active drivers,
- monthly bookings,
- number of operator users,
- advanced features (API, partner network, analytics, AI tools).

This is important because a rural operator with ~20 drivers and ~100 daily bookings has very different needs from a larger regional firm.

## 12. Key Domain Distinctions
The model must explicitly support:
- internal drivers,
- substitute drivers,
- partner tenants,
- company-to-company handoff,
- tenant-owned customers vs shared fulfilment data.

## 13. Product Expansion Path
### Phase 1
Core dispatch and operator workflows.

### Phase 2
Web booking intake, mobile driver operations, partner coverage.

### Phase 3
AI-assisted quoting, forecasting, automation, and white-label multi-tenant rollout.
