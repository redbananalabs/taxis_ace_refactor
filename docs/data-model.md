# Red Taxi Platform — Data Model

**Version:** 2.0  
**Last Updated:** 2026-03-16

---

## 1. Overview

The data model carries forward the proven legacy schema with the addition of `TenantId` on all tenant-scoped tables and normalisation of a few denormalised fields. The legacy `TaxiDispatchContext` has 36 entity types — all are retained and enhanced.

---

## 2. Core Entities

### Tenant (NEW)

| Column | Type | Notes |
|--------|------|-------|
| Id | GUID | Primary key |
| Name | nvarchar(250) | Company name |
| Slug | nvarchar(100) | URL-safe identifier, unique |
| ContactEmail | nvarchar(250) | |
| ContactPhone | nvarchar(20) | |
| BrandingConfig | nvarchar(max) | JSON: logo URL, primary colour, etc. |
| SubscriptionTier | int | Enum: Starter, Growth, Professional, Enterprise |
| IsActive | bit | Soft disable |
| CreatedAt | datetime2 | |

### Booking (ENHANCED from legacy)

Carried forward from legacy `Booking` entity (182 lines). Key fields:

| Column | Type | Notes |
|--------|------|-------|
| Id | int | Identity PK |
| TenantId | GUID | **NEW** — FK to Tenant |
| PickupAddress | nvarchar(250) | |
| PickupPostCode | nvarchar(9) | |
| DestinationAddress | nvarchar(250) | |
| DestinationPostCode | nvarchar(9) | |
| Details | nvarchar(2000) | Free text notes |
| PassengerName | nvarchar(250) | |
| Passengers | int | Default 1 |
| PhoneNumber | nvarchar(20) | |
| Email | nvarchar(250) | |
| PickupDateTime | datetime2 | |
| ArriveBy | datetime2? | Nullable — "arrive by" mode |
| IsAllDay | bit | |
| DurationMinutes | int | Default 15 |
| RecurrenceRule | nvarchar(max) | Repeat booking config |
| RecurrenceID | int? | Parent booking for recurrence |
| RecurrenceException | nvarchar(max) | |
| BookedByName | nvarchar(250) | |
| ConfirmationStatus | int? | Enum |
| PaymentStatus | int? | Enum |
| Scope | int? | Cash=0, Account=1, Rank=2, All=3, Card=4 |
| AccountNumber | int? | FK-like to Account |
| InvoiceNumber | int? | FK to AccountInvoice |
| Price | decimal(18,2) | Driver payout |
| PriceAccount | decimal(18,2) | Account invoice amount (account jobs only) |
| Tip | decimal(18,2) | |
| ManuallyPriced | bit | Locks auto-recalculation |
| Mileage | decimal(18,2)? | Total miles |
| MileageText | nvarchar(max) | Display string |
| DurationText | nvarchar(max) | Display string |
| ChargeFromBase | bit | |
| Cancelled | bit | |
| CancelledOnArrival | bit | COA flag |
| UserId | int? | FK to UserProfile (assigned driver) |
| SuggestedUserId | int? | Soft-allocated driver |
| Status | int? | BookingStatus enum |
| VehicleType | int | Enum: Unknown, Saloon, Estate, MPV, MPVPlus, SUV |
| WaitingTimeMinutes | int | |
| WaitingTimePriceDriver | decimal(18,2) | |
| WaitingTimePriceAccount | decimal(18,2) | |
| ParkingCharge | decimal(18,2) | |
| IsASAP | bit | |
| VatAmountAdded | decimal(18,2) | |
| PaymentOrderId | nvarchar(max) | Revolut order ID |
| PaymentLink | nvarchar(max) | |
| PaymentLinkSentBy | nvarchar(max) | |
| PaymentLinkSentOn | datetime2? | |
| PaymentReceiptSent | bit | |
| PostedForInvoicing | bit | Ready for account invoice batch |
| PostedForStatement | bit | Ready for driver statement batch |
| AllocatedAt | datetime2? | |
| AllocatedById | int? | Operator who allocated |
| StatementId | int? | FK to DriverInvoiceStatement |
| IsSchoolRun | bit | **NEW** — school run tag |
| MergedIntoBookingId | int? | **NEW** — if merged, points to target booking |
| DateCreated | datetime2 | |
| DateUpdated | datetime2? | |
| UpdatedByName | nvarchar(250) | |
| CancelledByName | nvarchar(250) | |
| ActionByUserId | int | |

### BookingVia (unchanged)

| Column | Type | Notes |
|--------|------|-------|
| Id | int | Identity PK |
| BookingId | int | FK to Booking |
| Address | nvarchar(250) | |
| PostCode | nvarchar(9) | |
| ViaSequence | int | Order of stop |
| TenantId | GUID | **NEW** |

### UserProfile (Driver — enhanced)

| Column | Type | Notes |
|--------|------|-------|
| Id | int | Identity PK |
| TenantId | GUID | **NEW** |
| UserId | int | FK to AppUser (Identity) |
| ColorCodeRGB | nvarchar(10) | Diary display colour |
| RegNo | nvarchar(20) | Vehicle registration |
| VehicleType | int | Enum |
| IsSubstitute | bit | Substitute driver flag |
| CommissionRate | decimal(5,2)? | **NEW** — driver-level commission override |
| IsActive | bit | |
| ... | | Other existing fields carried forward |

### Account (unchanged + TenantId)

| Column | Type | Notes |
|--------|------|-------|
| Id | int | Identity PK |
| TenantId | GUID | **NEW** |
| AccountNumber | int | Unique per tenant |
| CompanyName | nvarchar(250) | |
| ContactName | nvarchar(250) | |
| ContactPhone | nvarchar(20) | |
| ContactEmail | nvarchar(250) | |
| DefaultTariffId | int? | Custom tariff |
| BillingCycle | int? | Monthly default |
| IsActive | bit | |

### Tariff (unchanged + TenantId)

| Column | Type | Notes |
|--------|------|-------|
| Id | int | Identity PK |
| TenantId | GUID | **NEW** |
| Name | nvarchar(100) | |
| BaseFare | decimal(18,2) | |
| PerMileRate | decimal(18,2) | |
| PerMinuteRate | decimal(18,2) | |
| MinimumFare | decimal(18,2) | |
| WaitingTimeCharge | decimal(18,2) | Per minute |
| PassengerSurcharge | decimal(18,2) | |
| AirportSurcharge | decimal(18,2) | |
| IsDefault | bit | |
| IsActive | bit | |

### COARecord (unchanged + TenantId)

| Column | Type | Notes |
|--------|------|-------|
| Id | int | Identity PK |
| TenantId | GUID | **NEW** |
| BookingId | int | FK to Booking |
| AccountCharge | decimal(18,2) | What account is billed (may be full fare) |
| DriverPayout | decimal(18,2) | Reduced amount for driver |
| Reason | nvarchar(500) | |
| CreatedAt | datetime2 | |
| CreatedByName | nvarchar(250) | |

---

## 3. New Entities (from ChatGPT pack merge)

### Customer (NEW — replaces inline passenger details)

| Column | Type | Notes |
|--------|------|-------|
| Id | int | Identity PK |
| TenantId | GUID | |
| Name | nvarchar(250) | |
| Phone | nvarchar(20) | Primary lookup key |
| Email | nvarchar(250) | |
| AccountType | int | Enum: Casual, Regular, BusinessAccount |
| DefaultAccountId | int? | FK to Account (for business customers) |
| DefaultPickupNotes | nvarchar(500) | |
| MarketingSource | nvarchar(100) | How they found us (web, phone, referral, WhatsApp) |
| Notes | nvarchar(2000) | |
| IsActive | bit | |
| CreatedAt | datetime2 | |

Booking.CustomerId (NEW nullable FK) links to this. Legacy bookings keep inline passenger fields; new bookings populate both.

### PartnerRelationship (NEW)

| Column | Type | Notes |
|--------|------|-------|
| Id | int | Identity PK |
| TenantId | GUID | Source tenant |
| PartnerTenantId | GUID | Partner tenant |
| Status | int | Enum: Pending, Active, Suspended, Terminated |
| CoverageRules | nvarchar(max) | JSON: areas, times, vehicle types covered |
| CommercialTerms | nvarchar(max) | JSON: settlement model, rates |
| Priority | int | Escalation order (1 = first choice partner) |
| CreatedAt | datetime2 | |

### CoverRequest (NEW)

| Column | Type | Notes |
|--------|------|-------|
| Id | int | Identity PK |
| SourceTenantId | GUID | Tenant requesting cover |
| TargetTenantId | GUID | Partner being asked |
| BookingId | int | FK to Booking |
| Status | int | Enum: Requested, Accepted, Declined, Expired, Cancelled |
| OfferedPrice | decimal(18,2) | Price offered to partner |
| Notes | nvarchar(500) | |
| RequestedAt | datetime2 | |
| RespondedAt | datetime2? | |
| RespondedByUserId | int? | |

### SettlementRecord (NEW)

| Column | Type | Notes |
|--------|------|-------|
| Id | int | Identity PK |
| BookingId | int | FK to Booking |
| SourceTenantId | GUID | Tenant who owns the booking |
| FulfillingTenantId | GUID | Tenant who fulfilled it |
| DriverId | int | Driver who did the job |
| GrossAmount | decimal(18,2) | Total customer/account charge |
| PartnerAmount | decimal(18,2) | Amount paid to partner |
| CommissionAmount | decimal(18,2) | Platform or source commission |
| SettlementModel | int | Enum: ReferralFee, PercentageSplit, NetFulfilment, PassThrough |
| SettlementStatus | int | Enum: Pending, Invoiced, Paid, Disputed |
| CreatedAt | datetime2 | |

### SavedAddress (NEW)

| Column | Type | Notes |
|--------|------|-------|
| Id | int | Identity PK |
| TenantId | GUID | |
| CustomerId | int? | FK to Customer (null = tenant-wide) |
| Label | nvarchar(100) | e.g. "Home", "Office", "Mum's house" |
| Address | nvarchar(250) | |
| PostCode | nvarchar(9) | |
| Lat | decimal(10,6)? | |
| Lng | decimal(10,6)? | |
| Type | int | Enum: Customer, POI, Account |

---

## 4. Supporting Entities (carried forward with TenantId)

All existing entities get a `TenantId GUID` column with a global query filter.

| Entity | Legacy Lines | Purpose |
|--------|-------------|---------|
| AccountInvoice | 49 | Generated invoices for accounts |
| AccountPassenger | — | Named passengers on an account |
| AccountTariff | 31 | Per-account tariff overrides — has DUAL columns: Account Initial/FirstMile/AdditionalMile AND Driver Initial/FirstMile/AdditionalMile (confirmed from screenshots) |
| BookingChangeAudit | 39 | Audit trail for booking edits |
| CompanyConfig | 35 | Tenant-level settings (becomes tenant config) |
| CreditNote | 41 | Invoice credit notes |
| DocumentExpiry | — | Driver document tracking (DBS, MOT, insurance, etc.) |
| DriverAllocation | 26 | Allocation history |
| DriverAvailability | 33 | Declared availability blocks |
| DriverAvailabilityAudit | — | Availability change log |
| DriverExpense | 30 | Driver-submitted expenses |
| DriverInvoiceStatement | 73 | Driver settlement statements |
| DriverLocationHistory | 32 | GPS trail |
| DriverMessage | — | Direct messages to drivers |
| DriverOnShift | — | Current shift state |
| DriverShiftLog | — | Shift start/end history |
| FixedPriceJourney | — | Pre-set route prices |
| GeoFence | — | Geographic zones |
| JobOffer | — | Push job offers to drivers |
| LocalPOI | 41 | Points of interest (airports, stations, schools, etc.) |
| MessagingNotifyConfig | 46 | Per-event messaging rules |
| QRCodeClick | — | Marketing QR tracking |
| ReviewRequest | — | Customer review requests |
| Tariff | 30 | Pricing tariffs |
| TurnDown | — | Driver turn-down records |
| UINotification | — | In-app notifications |
| UrlMapping | — | Short URL mappings |
| UserActionLog | — | Operator action audit |
| WebAmendmentRequest | — | Customer amendment requests |
| WebBooking | 75 | Customer portal bookings (pending approval) |
| ZoneToZonePrice | — | Fixed zone-to-zone pricing |

---

## 4. Enums (carried forward)

| Enum | Values |
|------|--------|
| BookingScope | Cash=0, Account=1, Rank=2, All=3, Card=4 |
| BookingStatus | None, AcceptedJob, RejectedJob, Complete, RejectedJobTimeout |
| ConfirmationStatus | Select=0, Confirmed=1, Pending=2 |
| PaymentStatus | Select=0, Paid=2, Pending=3 |
| VehicleType | Unknown, Saloon, Estate, MPV, MPVPlus, SUV |
| DriverAvailabilityType | NotSet=0, Available=1, Unavailable=2 |
| AppJobStatus | OnRoute=3003, AtPickup=3004, PassengerOnBoard=3005, SoonToClear=3006, Clear=3007, NoJob=3008 |
| AppJobOffer | Accept=2000, Reject=2001, TimedOut=2002 |
| WebBookingStatus | Default, Accepted, Rejected |
| DocumentType | Insurance, MOT, DBS, VehicleBadge, DriverLicence, SafeGuarding, FirstAidCert, DriverPhoto |
| SentMessageType | DriverOnAllocate, DriverOnUnAllocate, DriverOnAmend, DriverOnCancel, CustomerOnAllocate, etc. |
| SendMessageOfType | None=0, WhatsApp=1, Sms=2, Push=3 |
| LocalPOIType | Not_Set, Train_Station, Supermarket, Airport, School, Hospital, etc. |

---

## 5. Data Migration — Tenant Import Wizard

The import wizard enables existing Ace Taxis data to migrate into Red Taxi as the first tenant.

### Import Strategy
1. Create Tenant record for Ace Taxis
2. Import reference data: Tariffs, Accounts, AccountTariffs, LocalPOIs, CompanyConfig, MessagingNotifyConfig, ZoneToZonePrices, GeoFences
3. Import drivers: AppUsers (driver role) + UserProfiles + DocumentExpirys
4. Import customers: AccountPassengers, UrlMappings
5. Import transactional data: Bookings + BookingVias + BookingChangeAudits
6. Import financial data: AccountInvoices, DriverInvoiceStatements, CreditNotes, DriverExpenses
7. All imported records receive the Ace Taxis `TenantId`
8. ID mapping table tracks old ID → new ID for FK resolution

### Import Wizard UI
- Step 1: Connect to source SQL Server (connection string)
- Step 2: Preview data counts (X bookings, Y drivers, Z accounts)
- Step 3: Select what to import (reference data, drivers, bookings, financial)
- Step 4: Run import with progress bar
- Step 5: Validation report (missing FKs, data issues)

---

## 6. Global Query Filter Pattern

```csharp
// In RedTaxiDbContext.OnModelCreating
modelBuilder.Entity<Booking>().HasQueryFilter(b => b.TenantId == _currentTenantId);
modelBuilder.Entity<Account>().HasQueryFilter(a => a.TenantId == _currentTenantId);
// ... all tenant-scoped entities
```

This ensures every query is automatically scoped to the current tenant. No manual `WHERE TenantId = X` needed.
