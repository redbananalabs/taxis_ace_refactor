# API Surfaces

## Public / tenant-facing APIs
### Bookings
- POST /api/bookings
- GET /api/bookings/{id}
- GET /api/bookings
- POST /api/bookings/{id}/quote
- POST /api/bookings/{id}/assign
- POST /api/bookings/{id}/cancel
- POST /api/bookings/{id}/complete

### Customers
- GET /api/customers
- POST /api/customers
- GET /api/customers/{id}

### Drivers
- GET /api/drivers
- POST /api/drivers
- GET /api/drivers/{id}
- POST /api/drivers/{id}/status
- POST /api/drivers/{id}/location
- GET /api/drivers/availability

### Vehicles
- GET /api/vehicles
- POST /api/vehicles
- GET /api/vehicles/{id}

### Pricing
- POST /api/pricing/quote
- GET /api/pricing/routes
- POST /api/pricing/rules

### Dispatch
- POST /api/dispatch/suggest
- POST /api/dispatch/assign
- POST /api/dispatch/reassign
- POST /api/dispatch/escalate

### Partner Coverage
- GET /api/partners
- POST /api/partners/cover-requests
- POST /api/partners/cover-requests/{id}/accept
- POST /api/partners/cover-requests/{id}/decline
- GET /api/partners/jobs

### Reporting
- GET /api/reports/bookings
- GET /api/reports/drivers
- GET /api/reports/partners
- GET /api/reports/revenue

## Internal workflow endpoints / handlers
These may remain internal commands in a modular monolith rather than external HTTP APIs:
- CreateBooking
- QuoteBooking
- SuggestDriverCandidates
- AssignDriver
- EscalateToPartnerCoverage
- RecordDriverStatusChange
- CompleteBooking
- GenerateSettlement
- ReconcilePartnerCoverage

## Events worth modelling
- BookingCreated
- BookingQuoted
- BookingAssigned
- BookingReassigned
- BookingCancelled
- DriverStatusChanged
- DriverLocationUpdated
- CoverRequested
- CoverAccepted
- CoverDeclined
- BookingCompleted
- SettlementCreated
