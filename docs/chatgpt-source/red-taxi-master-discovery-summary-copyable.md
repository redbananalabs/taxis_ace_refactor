# RED TAXI – MASTER DISCOVERY SUMMARY
*(Input for generating final PRD and supporting engineering documentation)*

---

# 1. Project Overview

Red Taxi is a **multi-tenant SaaS taxi dispatch and booking platform**.

The first tenant will be **Ace Taxis**, whose current operational system is being used as the reference implementation.

The goal is:

1. Replicate the operational behaviour of Ace first
2. Generalise it into a configurable SaaS platform
3. Add new capabilities afterwards

The system must support:

- Taxi dispatch operations
- Driver job allocation
- Customer bookings
- Account billing
- Driver communications
- SaaS onboarding for operators

Speed of delivery is important.  
Architecture should be **clean but pragmatic**.

---

# 2. Current Ace System (Discovery)

## Backend

Current Ace platform includes:

- C# API
- React admin panel
- Role-based permissions
- RabbitMQ messaging
- Redis (used for GPS caching/in-memory state)
- Revolut integration for payment links
- Twilio WhatsApp messaging
- Local SMS gateway via Android device

Current API structure has many endpoints in single controllers and should be reorganised.

Preferred new structure:

```
Features/
  Bookings/
  Dispatch/
  Drivers/
  Accounts/
  Billing/
  Notifications/
  Messaging/
```

---

# 3. Booking Sources

Bookings may originate from:

1. Operator entering booking in admin
2. Website booking form
3. Passenger mobile app
4. Phone call handled by operator
5. CMAC integration
6. External API integrations
7. Recurring bookings (school runs etc.)
8. Driver-entered bookings
9. Rank pickups
10. WhatsApp AI booking agent

### WhatsApp AI Booking

Customers can message WhatsApp such as:

"Taxi from Gillingham to Heathrow tomorrow at 5am."

AI agent collects:

- pickup
- destination
- time
- passenger details
- confirmation

Then automatically creates the booking.

---

# 4. Booking System Behaviour

Booking includes:

- Pickup address
- Destination
- Via stops
- Pickup time
- Arrive By time
- Return journey
- Recurring booking
- Passenger name
- Passenger phone
- Passenger email
- Passenger count
- Notes
- Pricing
- Payment/work type
- Account number (optional)

Addresses include:

- address text
- postcode lookup
- latitude
- longitude

Bookings support **multiple via stops**.

---

# 5. Recurring Bookings

Ace supports recurrence.

Frequency:

- Daily
- Weekly
- Fortnightly

Options:

- days of week
- repeat end date
- repeat end type

Used for:

- school runs
- contract journeys
- recurring passenger bookings

---

# 6. Booking Flags

Bookings may include flags such as:

- ASAP
- Return journey
- Arrive By
- All Day
- Manually priced

Arrive By mode calculates pickup time using routing.

---

# 7. Pricing Engine

Ace recalculates pricing **live** when booking fields change.

Triggers include:

- pickup address change
- destination change
- via change
- passenger count change
- date/time change
- account number change

The system calculates:

- customer price
- driver price

Manual override is allowed.

---

# 8. Tariffs

Accounts may have **different tariffs**.

Tariff may influence:

- customer price
- driver price
- billing behaviour

System must support:

- tenant default tariff
- account-specific tariff override

---

# 9. Work Types (currently called “Scope”)

Ace uses a field named **Scope**, but this should be renamed.

Current types:

- Cash
- Card
- Rank
- Account

Recommended name in Red Taxi:

- Work Type
- Booking Type

---

# 10. Rank Jobs

Rank work is when:

- driver waits at taxi rank
- driver gets passenger directly
- driver records job in system
- driver enters final price

Operator may take commission.

---

# 11. Dispatch Engine

Jobs can be allocated:

1. From booking form (driver selected on save)
2. From dispatch board (manual selection)

Drivers are shown with:

- driver number
- driver name
- vehicle type
- registration
- driver colour

Driver colour is used visually on dispatch board.

---

# 12. Dispatch Board Visual Behaviour

When a job is allocated:

- job turns **driver colour**

When accepted:

- job becomes **crosshatched pattern**

Rejected jobs display prefix:

```
[R]
```

Timeout prefix:

```
[RT]
```

Improvement needed:

Show indicator of **which driver rejected**.

---

# 13. Driver Job Offer Channels

Drivers receive jobs through:

## Local SMS Gateway

Backend sends message to RabbitMQ.

Android device polls queue every 30 seconds and sends SMS.

Used because it is cheaper.

---

## WhatsApp via Twilio

Driver receives formatted message.

Driver replies:

```
Accept
Reject
```

System updates booking state.

---

## Driver App Push Notification

Driver receives push notification and can accept or reject.

---

# 14. Offer Retry Behaviour

Jobs may be retried.

Example:

- 3 attempts
- 30 second delay

System should support configurable:

- retry count
- retry delay
- retry enable or disable

If driver repeatedly rejects or ignores jobs:

Driver may be **temporarily banned**.

---

# 15. Driver Workflow

Drivers can:

- accept job
- navigate to pickup
- navigate through via stops
- navigate to destination
- update status
- complete job

Navigation should support **entire journey including vias**.

---

# 16. Driver Status Flow

Statuses are configurable.

Minimal example:

```
Accept
Complete
```

Extended example:

```
Accept
On Route
Arrived
Passenger Onboard
Complete
```

Some accounts may require full status flow.

---

# 17. Driver Availability

Drivers may:

- toggle online in app
- call office to log on
- start work without login if pre-allocated

Drivers also submit **planned availability ahead of time**.

System must support:

- planned availability
- live availability
- manual operator login
- pre-booked driver jobs

---

# 18. GPS Tracking

System must record:

- driver live location
- historical GPS
- journey routes

Addresses should store:

- latitude
- longitude

GPS will be used for:

- dispatch
- analytics
- route evidence

---

# 19. Account Jobs

Account booking flow:

```
Customer books
Driver allocated
Job completed OR cancelled on arrival
```

If driver allocated and job cancelled:

**still billable**.

---

# 20. Account Billing

Currently invoicing is manually checked.

Batch pricing attempts group journeys by:

- passenger
- postcode
- journey grouping

Exact logic requires Ace code analysis.

System must support:

- invoice grouping
- draft invoice review
- automatic batch generation

---

# 21. Credits and Rebilling

Account journeys may require:

- credit
- rebill
- invoice correction

System must support:

- credit notes
- rebilling
- audit history

---

# 22. Payment Processing

Ace sends **payment links via Revolut**.

Card processing fees are deducted from the **driver balance**.

System should track:

- tenant card fees
- driver deductions
- financial balances

---

# 23. Messaging and Communications

Operators should be able to send:

- scheduled SMS
- scheduled WhatsApp
- reminders
- marketing messages

Channels:

- local SMS
- provider SMS
- WhatsApp
- email (future)

Operators should choose preferred channel.

---

# 24. Marketing Messages

Operators should be able to send:

- SMS marketing
- WhatsApp marketing

Compliance considerations required.

---

# 25. SaaS Platform

Platform must support:

- tenant onboarding
- free trial
- email templates
- subscription tiers

Suggested entry plan:

```
5 drivers
low booking volume
```

---

# 26. Future Features

Planned future enhancements include:

- AI voice agent to chase drivers who ignore jobs
- AI WhatsApp booking assistant
- HMRC Making Tax Digital integration
- advanced dispatch automation

---

# 27. Architecture Guidelines

System should prioritise:

- speed of development
- modular vertical slices
- clear domain language
- configurable tenant rules

Interfaces may be used where helpful, for example:

```
INotificationChannel
IPaymentProvider
IGpsProvider
IDriverOfferChannel
```

---

# 28. Project Readiness

Current estimated readiness:

**~77 / 100**

Major areas still needing discovery:

- CMAC integration
- exact Ace pricing engine
- full database schema
- driver earnings settlement
- dispatch board logic
- account invoice grouping rules

---

# Instruction for the next AI

Using the information above:

1. Produce a **complete Product Requirements Document**
2. Produce supporting documents including:
   - system architecture
   - domain model
   - module breakdown
   - API design guidance
   - SaaS tenant architecture
3. Identify missing areas that require discovery
4. Improve naming where Ace terminology is unclear
5. Maintain compatibility with Ace workflows

Output the result as a **structured engineering specification suitable for development**.
