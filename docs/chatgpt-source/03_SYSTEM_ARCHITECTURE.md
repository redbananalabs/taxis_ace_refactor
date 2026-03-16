# System Architecture

## Architecture principle
Use a modular monolith for v1 unless there is a strong operational reason to split services earlier.

This keeps delivery simpler while preserving clear boundaries for later extraction.

## Main application surfaces
1. Operator Web App
2. Driver App / Mobile Web App
3. Public Booking API
4. Admin / Back Office
5. Internal background processing layer

## Proposed layers
### Presentation
- operator dashboard
- admin dashboard
- driver UI
- public booking forms / embedded widgets

### Application
- booking workflows
- dispatch workflows
- driver workflows
- pricing workflows
- partner workflows
- notification workflows

### Domain
- bookings
- drivers
- companies
- vehicles
- assignments
- fares
- partner cover
- settlements

### Infrastructure
- database
- background jobs / queue
- mapping provider
- sms/email/push
- auth
- file storage
- observability

## Real-time requirements
Needs real-time or near-real-time updates for:
- booking queue changes,
- driver status changes,
- driver location updates,
- reassignment,
- new urgent jobs.

This can be handled with:
- websocket-style updates for dashboard,
- efficient polling fallback where required.

## Recommended v1 shape
### Frontend
- Next.js / modern web app for operator/admin
- React Native or mobile web for driver v1
- public booking surfaces integrated into white-label websites

### Backend
Given the user's stated preference for simple modular monolith / vertical slices in .NET:
- Taxi.Api
- Taxi.App
- Taxi.Data
- optional Taxi.Domain

Feature slices such as:
- Features/Bookings/Bookings.cs
- Features/Drivers/Drivers.cs
- Features/Dispatch/Dispatch.cs
- Features/Companies/Companies.cs
- Features/Pricing/Pricing.cs

Endpoints in Taxi.Api call handlers directly.
No EF logic in controllers.
Handlers use DbContext directly.

## Infrastructure considerations
- PostgreSQL recommended for relational core
- Redis optional for cache/realtime assistance
- background job processor for notifications and scheduled work
- map/geocoding provider abstraction
- SMS provider abstraction
- cloud deployment with environment isolation per stage

## Why modular monolith first
- faster to build,
- simpler to operate,
- easier for Codex to maintain,
- fewer moving parts,
- still compatible with later service extraction.
