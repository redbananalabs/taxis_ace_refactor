# Data Model

## Core entities
### Tenant / Company
Represents a taxi operator or dispatch business using the platform.

Suggested fields:
- Id
- Name
- Slug
- Status
- Plan
- ActiveDriverLimit
- MonthlyBookingLimit
- BrandingConfig
- Timezone
- BillingContact
- CreatedAt

### User
- Id
- TenantId
- Name
- Email
- PasswordHash / AuthProviderId
- Role
- IsActive

### Driver
- Id
- TenantId
- Type (Internal, Substitute, PartnerExternal)
- Name
- Phone
- Status
- Rating
- DefaultVehicleId
- HomeBase
- IsBookable
- Notes

### Vehicle
- Id
- TenantId
- Registration
- MakeModel
- Capacity
- VehicleClass
- WheelchairCapable
- LuggageCapacity
- Active

### Customer
- Id
- TenantId
- Name
- Phone
- Email
- AccountType
- Notes
- MarketingSource
- DefaultPickupNotes

### Address / Place
- Id
- TenantId
- Label
- AddressLine
- Postcode
- Lat
- Lng
- Type

### Booking
- Id
- TenantId
- CustomerId
- Source
- ServiceType
- PickupAt
- PickupAddressId
- DropoffAddressId
- PassengerCount
- LuggageCount
- AccessibilityFlags
- Status
- QuoteAmount
- FinalAmount
- PricingMethod
- Priority
- Notes
- AssignedDriverId
- AssignedVehicleId
- AssignedPartnerTenantId
- CreatedByUserId
- CreatedAt

### BookingStop
- Id
- BookingId
- Sequence
- AddressId
- StopType

### DispatchAssignment
- Id
- BookingId
- AssignedDriverId
- AssignedByUserId
- AssignmentType (Manual, Assisted, Auto, Partner)
- Score
- Status
- AssignedAt
- RespondedAt

### DriverLocationPing
- Id
- DriverId
- CapturedAt
- Lat
- Lng
- Speed
- Heading

### PartnerRelationship
- Id
- TenantId
- PartnerTenantId
- Status
- CoverageRules
- CommercialTerms

### CoverRequest
- Id
- SourceTenantId
- TargetTenantId
- BookingId
- RequestedAt
- Status
- OfferedPrice
- Notes

### SettlementRecord
- Id
- BookingId
- SourceTenantId
- FulfillingTenantId
- DriverId
- GrossAmount
- PartnerAmount
- CommissionAmount
- SettlementStatus

## Important modelling note
Do not treat substitute drivers and partner coverage as the same thing.

### Substitute driver
A person temporarily used by the tenant, but effectively operating under that tenant's dispatch control for that job or period.

### Partner coverage
A booking fulfilled by another tenant/company under an inter-company arrangement.

These need different records, permissions, and settlement logic.
