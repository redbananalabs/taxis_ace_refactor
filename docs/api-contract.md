# Red Taxi Platform — API Contract

**Version:** 2.0  
**Last Updated:** 2026-03-16  
**Total Legacy Endpoints:** 232  
**Legacy Auth Gaps (Auth=None needing review):** 35

---

## 1. Overview

This document maps the legacy API surface (232 endpoints) to the new Red Taxi feature structure. The v2 API will follow RESTful conventions with consistent naming. Legacy v1 endpoints are maintained during migration.

---

## 2. Endpoint Inventory by Feature

### 2.1 Bookings (28 endpoints)

| Legacy Action | Verb | Legacy Route | v2 Route | Auth |
|--------------|------|-------------|----------|------|
| CreateBooking | POST | Create | POST /api/v2/bookings | Yes |
| UpdateBooking | POST | Update | PUT /api/v2/bookings/{id} | Yes |
| UpdateBookingDate | POST | UpdateDate | PATCH /api/v2/bookings/{id}/date | Yes |
| CancelBooking | POST | Cancel | POST /api/v2/bookings/{id}/cancel | Yes |
| RestoreCancelled | POST | RestoreCancelled | POST /api/v2/bookings/{id}/restore | Yes |
| Complete | POST | Complete | POST /api/v2/bookings/{id}/complete | Yes |
| GetBookingsToday | GET | Today | GET /api/v2/bookings/today | Yes |
| GetBookings | POST | DateRange | GET /api/v2/bookings?start=&end= | Yes |
| GetById | GET | FindById | GET /api/v2/bookings/{id} | Yes |
| FindBookings | POST | FindBookings | GET /api/v2/bookings/search?term= | Yes |
| FindByTerm | GET | FindByTerm | GET /api/v2/bookings/find?term= | Yes |
| GetPickupHistory | GET | PickupHistory | GET /api/v2/bookings/pickup-history | Yes |
| GetActionLogs | GET | GetActionLogs | GET /api/v2/bookings/{id}/audit | Yes |
| MergeBookings | GET | MergeBookings | POST /api/v2/bookings/merge | Yes |
| RankPickup | POST | RankCreate | POST /api/v2/bookings/rank | Yes |
| CancelJobsByDateRange | POST | CancelByDateRange | POST /api/v2/bookings/bulk-cancel | Yes |
| ManualPriceUpdate | POST | ManualPrice | PATCH /api/v2/bookings/{id}/price | Yes |
| SendCustomerConfirmation | GET | SendConfirmationText | POST /api/v2/bookings/{id}/send-confirmation | Yes |
| SendQuote | POST | SendQuote | POST /api/v2/bookings/{id}/send-quote | Yes |
| UpdateQuote | POST | UpdateQuote | PUT /api/v2/bookings/{id}/quote | Yes |
| UpdatePaymentStatus | GET | UpdatePaymentStatus | PATCH /api/v2/bookings/{id}/payment-status | Yes |

### 2.2 Dispatch (7 endpoints)

| Legacy Action | Verb | Legacy Route | v2 Route | Auth |
|--------------|------|-------------|----------|------|
| Allocate | POST | Allocate | POST /api/v2/dispatch/allocate | Yes |
| SoftAllocate | POST | SoftAllocate | POST /api/v2/dispatch/soft-allocate | Yes |
| ConfirmAllSoftAllocates | POST | ConfirmAllSoftAllocates | POST /api/v2/dispatch/confirm-soft | Yes |
| RecordTurnDown | GET | RecordTurnDown | POST /api/v2/dispatch/turn-down | Yes |
| GetCOAEntrys | GET | GetCOAEntrys | GET /api/v2/dispatch/coa | Yes |
| CreateCOAEntry | POST | CreateCOAEntry | POST /api/v2/dispatch/coa | Yes |
| RemoveCancellOnArrival | POST | RemoveCOA | DELETE /api/v2/dispatch/coa/{id} | Yes |

### 2.3 Pricing (4 endpoints)

| Legacy Action | Verb | Legacy Route | v2 Route | Auth |
|--------------|------|-------------|----------|------|
| GetPrice | POST | GetPrice | POST /api/v2/pricing/calculate | Public |
| GetDuration | GET | GetDuration | GET /api/v2/pricing/duration | Public |
| GetTariffConfig | GET | GetTariffConfig | GET /api/v2/pricing/tariffs | Yes |
| SetTariffConfig | POST | SetTariffConfig | PUT /api/v2/pricing/tariffs | Yes |

### 2.4 Payments (7 endpoints)

| Legacy Action | Verb | Legacy Route | v2 Route | Auth |
|--------------|------|-------------|----------|------|
| SendPaymentLink | GET | PaymentLink | POST /api/v2/payments/{bookingId}/link | Yes |
| ResendPaymentLink | GET | ReminderPaymentLink | POST /api/v2/payments/{bookingId}/resend | Yes |
| RefundPayment | GET | RefundPayment | POST /api/v2/payments/{bookingId}/refund | Yes |
| GetPaymentReceipt | GET | DownloadReceipt | GET /api/v2/payments/{bookingId}/receipt | Public |
| SendPaymentReceipt | GET | SendPaymentReceipt | POST /api/v2/payments/{bookingId}/send-receipt | Yes |
| RevPaymentUpdate | POST | RevPaymentUpdate | POST /api/v2/payments/webhook | Public |
| CreateRevoluttWebHook | GET | CreateRevWebHook | POST /api/v2/payments/setup-webhook | Yes |

### 2.5 Fleet / Drivers (28 endpoints)

| Legacy Action | Verb | Legacy Route | v2 Route | Auth |
|--------------|------|-------------|----------|------|
| DriversList | GET | DriversList | GET /api/v2/fleet/drivers | Yes |
| DriverAdd | POST | DriverAdd | POST /api/v2/fleet/drivers | Yes |
| DriverUpdate | POST | DriverUpdate | PUT /api/v2/fleet/drivers/{id} | Yes |
| DriverDelete | POST | DriverDelete | DELETE /api/v2/fleet/drivers/{id} | Yes |
| DriverLockout | GET | DriverLockout | POST /api/v2/fleet/drivers/{id}/lockout | Yes |
| DriverResendLogin | GET | DriverResendLogin | POST /api/v2/fleet/drivers/{id}/resend-login | Yes |
| GetAvailability | GET | GetAvailability | GET /api/v2/fleet/availability?date= | Yes |
| SetAvailability | POST | SetAvailability | POST /api/v2/fleet/availability | Yes |
| DeleteAvailability | GET | DeleteAvailability | DELETE /api/v2/fleet/availability/{id} | Yes |
| DriversOnShift | GET | DriversOnShift | GET /api/v2/fleet/on-shift | Yes |
| DriverExpirys | GET | GetDriverExpirys | GET /api/v2/fleet/expirys | Yes |
| UpdateDriverExpiry | POST | UpdateDriverExpiry | PUT /api/v2/fleet/expirys | Yes |
| GetAllUsersGPS | GET | GetAllGPS | GET /api/v2/fleet/gps | Yes |
| GetUserGPS | GET | GetGPS | GET /api/v2/fleet/gps/{userId} | Yes |

### 2.6 Driver App (25 endpoints)

| Legacy Action | Verb | Legacy Route | v2 Route | Auth |
|--------------|------|-------------|----------|------|
| GetProfile | GET | GetProfile | GET /api/v2/driver-app/profile | Yes |
| TodayJobs | GET | TodaysJobs | GET /api/v2/driver-app/jobs/today | Yes |
| FutureJobs | GET | FutureJobs | GET /api/v2/driver-app/jobs/future | Yes |
| CompletedJobs | GET | CompletedJobs | GET /api/v2/driver-app/jobs/completed | Yes |
| GetActiveJob | GET | GetActiveJob | GET /api/v2/driver-app/jobs/active | Yes |
| SetActiveJob | POST | SetActiveJob | POST /api/v2/driver-app/jobs/active | Yes |
| GetJobOffers | GET | GetJobOffers | GET /api/v2/driver-app/offers | Yes |
| JobOfferReply | GET | JobOfferReply | POST /api/v2/driver-app/offers/{id}/reply | Yes |
| JobStatusReply | GET | JobStatusReply | POST /api/v2/driver-app/jobs/{id}/status | Yes |
| Arrived | GET | Arrived | POST /api/v2/driver-app/jobs/{id}/arrived | Yes |
| Complete | POST | CompleteJob | POST /api/v2/driver-app/jobs/{id}/complete | Yes |
| DriverShift | GET | DriverShift | POST /api/v2/driver-app/shift | Yes |
| DashTotals | GET | DashTotals | GET /api/v2/driver-app/dashboard | Yes |
| Earnings | GET | Earnings | GET /api/v2/driver-app/earnings | Yes |
| UpdateGPS | POST | UpdateGPS | POST /api/v2/driver-app/gps | Yes |
| UpdateFCM | POST | UpdateFCM | POST /api/v2/driver-app/fcm | Yes |
| UploadDocument | POST | UploadDocument | POST /api/v2/driver-app/documents | Yes |
| AddExpense | POST | AddExpense | POST /api/v2/driver-app/expenses | Yes |
| GetExpenses | GET | GetExpenses | GET /api/v2/driver-app/expenses | Yes |

### 2.7 Accounts & Invoicing (36 endpoints)

| Legacy Action | Verb | Legacy Route | v2 Route | Auth |
|--------------|------|-------------|----------|------|
| GetAccountsList | GET | GetList | GET /api/v2/accounts | Yes |
| AddAccount | POST | AddAccount | POST /api/v2/accounts | Yes |
| UpdateAccount | POST | UpdateAccount | PUT /api/v2/accounts/{id} | Yes |
| DeleteAccount | GET | DeleteAccount | DELETE /api/v2/accounts/{id} | Yes |
| GetAccountTariffs | GET | GetAccountTariffs | GET /api/v2/accounts/tariffs | Yes |
| CreateOrUpdateAccountTariff | POST | ... | PUT /api/v2/accounts/tariffs | Yes |
| AccountGetChargableJobs | POST | ... | GET /api/v2/accounts/{id}/chargeable | Yes |
| AccountPostOrUnPostJobs | POST | ... | POST /api/v2/accounts/post-jobs | Yes |
| AccountCreateInvoice | POST | ... | POST /api/v2/accounts/invoices | Yes |
| GetInvoices | GET | GetInvoices | GET /api/v2/accounts/invoices | Yes |
| GetInvoice | GET | DownloadInvoice | GET /api/v2/accounts/invoices/{id}/pdf | Yes |
| GetInvoiceCSV | GET | DownloadInvoiceCSV | GET /api/v2/accounts/invoices/{id}/csv | Yes |
| MarkInvoiceAsPaid | GET | MarkInvoiceAsPaid | PATCH /api/v2/accounts/invoices/{id}/paid | Yes |
| ReSendInvoice | GET | ResendInvoice | POST /api/v2/accounts/invoices/{id}/resend | Yes |
| CreditInvoice | GET | DeleteInvoice | POST /api/v2/accounts/invoices/{id}/credit | Yes |
| CreditJourneys | POST | CreditJourneys | POST /api/v2/accounts/credit-journeys | Yes |
| GetCreditNotes | GET | GetCreditNotes | GET /api/v2/accounts/credit-notes | Yes |
| GetCreditNote | GET | DownloadCreditNote | GET /api/v2/accounts/credit-notes/{id}/pdf | Yes |
| DriverGetChargableJobs | POST | ... | GET /api/v2/fleet/drivers/{id}/chargeable | Yes |
| DriverCreateStatements | POST | ... | POST /api/v2/fleet/statements | Yes |
| DriverGetStatments | POST | ... | GET /api/v2/fleet/statements | Yes |
| GetStatement | GET | DownloadStatement | GET /api/v2/fleet/statements/{id}/pdf | Yes |
| MarkStatementAsPaid | GET | MarkStatementAsPaid | PATCH /api/v2/fleet/statements/{id}/paid | Yes |
| ResendDriverStatement | GET | ResendDriverStatement | POST /api/v2/fleet/statements/{id}/resend | Yes |
| VATOutputs | POST | VATOutputs | GET /api/v2/accounts/vat-outputs | Yes |
| GetZonePrices | GET | GetZonePrices | GET /api/v2/pricing/zone-prices | Yes |
| AddOrUpdateZonePrice | POST | ... | PUT /api/v2/pricing/zone-prices | Yes |

### 2.8 Messaging (8 endpoints)

| Legacy Action | Verb | Legacy Route | v2 Route | Auth |
|--------------|------|-------------|----------|------|
| SendMessageToDriver | POST | ... | POST /api/v2/messaging/driver/{id} | Yes |
| SendMessageToAllDrivers | POST | ... | POST /api/v2/messaging/drivers/broadcast | Yes |
| SendTextMessage | POST | SendText | POST /api/v2/messaging/sms | Yes |
| GetMessages | GET | Get | GET /api/v2/messaging/sms | Public* |
| GetMessageConfig | GET | GetMessageConfig | GET /api/v2/messaging/config | Yes |
| UpdateMessageConfig | POST | UpdateMessageConfig | PUT /api/v2/messaging/config | Yes |
| WhatsApp RecieveReply | POST | RecieveReply | POST /api/v2/messaging/whatsapp/webhook | Public |
| WhatsApp Send | GET | Send | POST /api/v2/messaging/whatsapp/send | Public* |

*Public endpoints need auth review — likely should be secured or rate-limited.

### 2.9 Reporting (13 endpoints)

| Legacy Action | Verb | Legacy Route | v2 Route | Auth |
|--------------|------|-------------|----------|------|
| GetDashData | GET | Dashboard | GET /api/v2/reports/dashboard | Yes |
| RevenueByMonth | POST | RevenueByMonth | GET /api/v2/reports/revenue | Yes |
| PayoutsByMonth | POST | PayoutsByMonth | GET /api/v2/reports/payouts | Yes |
| ProfitsByDateRange | POST | ProfitsByDateRange | GET /api/v2/reports/profits | Yes |
| ProfitabilityOnInvoices | POST | ProfitabilityOnInvoices | GET /api/v2/reports/profitability | Yes |
| TotalProfitabilityByPeriod | POST | ... | GET /api/v2/reports/profitability-total | Yes |
| DriverEarningsReport | POST | DriverEarningsReport | GET /api/v2/reports/driver-earnings | Yes |
| GetBookingScopeBreakdown | POST | ... | GET /api/v2/reports/scope-breakdown | Yes |
| GetGrowthByPeriod | POST | GetGrowthByPeriod | GET /api/v2/reports/growth | Yes |
| GetTopCustomers | POST | GetTopCustomers | GET /api/v2/reports/top-customers | Yes |
| GetAverageDuration | POST | GetAverageDuration | GET /api/v2/reports/avg-duration | Yes |
| GetVehicleTypeCounts | POST | ... | GET /api/v2/reports/vehicle-types | Yes |
| GetPickupPostcodes | POST | GetPickupPostcodes | GET /api/v2/reports/pickup-postcodes | Yes |
| DuplicateBookingsReport | POST | ... | GET /api/v2/reports/duplicates | Yes |

### 2.10 Web Booking Portal (14 endpoints)

| Legacy Action | Verb | Legacy Route | v2 Route | Auth |
|--------------|------|-------------|----------|------|
| CreateWebBooking | POST | CreateWebBooking | POST /api/v2/web-booking/submit | Public |
| CreateCashBooking | POST | CreateCashBooking | POST /api/v2/web-booking/cash | Public |
| GetWebBookings | POST | GetWebBookings | GET /api/v2/web-booking/pending | Yes |
| AcceptWebBooking | POST | Accept | POST /api/v2/web-booking/{id}/accept | Yes |
| RejectWebBooking | POST | Reject | POST /api/v2/web-booking/{id}/reject | Yes |
| AmendAcceptWebBooking | POST | AmendAccept | POST /api/v2/web-booking/{id}/amend-accept | Yes |
| RequestAmendment | GET | RequestAmendment | POST /api/v2/web-booking/{id}/request-amend | Yes |
| RequestCancellation | GET | RequestCancellation | POST /api/v2/web-booking/{id}/request-cancel | Yes |
| GetAccountActiveBookings | GET | ... | GET /api/v2/web-booking/active | Yes |
| GetPassengerList | GET | GetPassengers | GET /api/v2/web-booking/passengers | Yes |
| AddNewPassenger | POST | AddNewPassenger | POST /api/v2/web-booking/passengers | Yes |
| DeletePassenger | DELETE | DeletePassenger | DELETE /api/v2/web-booking/passengers/{id} | Yes |
| GetAdressSuggestions | POST | ... | GET /api/v2/web-booking/address-suggest | Public |
| GetDuration | GET | GetDuration | GET /api/v2/web-booking/duration | Yes |

### 2.11 Address Lookup (6 endpoints)

| Legacy Action | Verb | Legacy Route | v2 Route | Auth |
|--------------|------|-------------|----------|------|
| DispatchSearch | GET | DispatchSearch | GET /api/v2/address/search | Public |
| Ideal | GET | IdealSearch | GET /api/v2/address/ideal | Public |
| IdealPostcode | GET | IdealPostcode | GET /api/v2/address/postcode | Public |
| PostcodeLookup | GET | PostcodeLookup | GET /api/v2/address/lookup | Public |
| Resolve | GET | Resolve | GET /api/v2/address/resolve | Public |
| WebBookerSearch | GET | WebBookerSearch | GET /api/v2/address/web-search | Public |

### 2.12 Identity & Auth (8 endpoints)

| Legacy Action | Verb | Legacy Route | v2 Route | Auth |
|--------------|------|-------------|----------|------|
| Login | POST | Login | POST /api/v2/auth/login | Public |
| Register | POST | Register | POST /api/v2/auth/register | Public |
| RefreshToken | POST | RefreshToken | POST /api/v2/auth/refresh | Public |
| ResetPassword | GET | ResetPassword | POST /api/v2/auth/reset-password | Yes |
| GetUser | GET | GetUser | GET /api/v2/users/{id} | Yes |
| ListUsers | GET | ListUsers | GET /api/v2/users | Yes |
| Update | POST | Update | PUT /api/v2/users/{id} | Yes |

### 2.13 Config & Admin (20+ endpoints)

Company config, POIs, geo-fences, notifications, QR codes, and misc admin operations. These map to `/api/v2/config/*`, `/api/v2/pois/*`, `/api/v2/geofences/*`.

---

## 3. Auth Review Required

The following legacy endpoints have `Auth=None` and need security review:

| Endpoint | Current | Recommendation |
|----------|---------|----------------|
| Address/* (6 endpoints) | None | Rate-limit, keep public |
| GetPrice | None | Rate-limit, keep public |
| GetDuration | None | Rate-limit, keep public |
| CallEvents/* (6 endpoints) | None | Secure with API key (telephony integration) |
| WhatsApp RecieveReply | None | Validate Twilio signature |
| RevPaymentUpdate | None | Validate Revolut webhook signature |
| GetMessages (SMS queue) | None | Secure with auth |
| WhatsApp Send | None | Secure with auth |
| CreateWebBooking | None | Rate-limit + CAPTCHA |
| CreateCashBooking | None | Rate-limit + CAPTCHA |
| RefreshJobOffers | None | Secure with auth |
| RetrieveJobOffer | None | Secure with auth |
