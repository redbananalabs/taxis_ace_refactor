# Red Taxi Platform — Module Map

**Version:** 2.0  
**Last Updated:** 2026-03-16  
**Source:** Merged from ChatGPT pack (72 modules) + legacy codebase analysis

---

## Overview

This module map is designed to let AI coding agents and developers work in parallel with minimal overlap. Each module is a vertical slice with its own commands, queries, handlers, and DTOs.

---

## A. Platform & Tenancy (7 modules)

| # | Module | Phase | Description |
|---|--------|-------|-------------|
| 1 | Tenant Registry | 1 | Tenant CRUD, provisioning, lifecycle |
| 2 | Company Profile | 1 | Company details, branding, base postcode |
| 3 | Company Branding / White-Label | 4 | Logo, colours, custom domain config |
| 4 | User Accounts | 1 | Identity, registration, password management |
| 5 | Roles & Permissions | 1 | SuperAdmin, TenantAdmin, Dispatcher, Driver, AccountUser, WebBooker |
| 6 | Audit Log | 1 | User actions, booking changes, dispatch decisions |
| 7 | Feature Flags / Entitlements | 4 | SaaS tier gating, usage counters, soft/hard limits |

## B. Customer & Booking Intake (11 modules)

| # | Module | Phase | Description |
|---|--------|-------|-------------|
| 8 | Customer Directory | 1 | Customer CRUD, phone lookup, marketing source |
| 9 | Customer Notes / Preferences | 2 | Default pickup notes, saved preferences |
| 10 | Saved Addresses | 2 | Customer and tenant-wide saved locations |
| 11 | Booking Capture | 1 | Create/edit/duplicate bookings |
| 12 | Scheduled Booking Manager | 1 | Future-dated bookings, repeat bookings |
| 13 | ASAP Booking Manager | 1 | Immediate dispatch bookings |
| 14 | Return Journey Handling | 1 | Auto-create return booking with swapped addresses |
| 15 | Multi-Stop (Via) Handling | 1 | Add/remove/reorder via stops |
| 16 | Accessibility / Special Requirements | 2 | Wheelchair, child seat, luggage flags |
| 17 | Booking Validation Rules | 1 | Address required, postcode format, date in future |
| 18 | Booking Status Lifecycle | 1 | State machine: Created → Allocated → ... → Completed |

## C. Pricing & Quotes (8 modules)

| # | Module | Phase | Description |
|---|--------|-------|-------------|
| 19 | Fare Rules / Tariff Engine | 1 | Base fare, per-mile, per-minute, minimums |
| 20 | Fixed Route Pricing | 2 | Pre-set prices for common routes |
| 21 | Area / Zone Pricing | 2 | Zone-to-zone price grid |
| 22 | Time-Based Surcharges | 2 | Night rates, bank holiday rates |
| 23 | Extras / Fees | 2 | Airport surcharge, waiting time, parking |
| 24 | Quote Calculator | 1 | Google Distance Matrix integration, price computation |
| 25 | Manual Price Override | 1 | Operator overrides auto-price, ManuallyPriced flag |
| 26 | Account / Contract Pricing | 2 | Per-account tariff overrides |

## D. Dispatch Core (9 modules)

| # | Module | Phase | Description |
|---|--------|-------|-------------|
| 27 | Driver Availability Engine | 1 | Availability grid, shift declarations, manual override |
| 28 | Candidate Selection Engine | 3 | Generate pool of eligible drivers per booking |
| 29 | Dispatch Scoring Engine | 3 | Weighted scoring: distance, ETA, workload, affinity |
| 30 | Manual Assignment | 1 | Operator picks driver from list |
| 31 | Soft Allocate / Confirm | 1 | Tentative assignment, bulk confirm |
| 32 | Reassignment / Recovery | 1 | Change driver, handle no-shows |
| 33 | No-Driver Escalation | 5 | Substitute → partner → unallocated exception |
| 34 | School Run Merge | 1 | Drag-and-drop merge when same dest + account |
| 35 | Dispatch Timeline / History | 2 | Audit trail of all dispatch decisions |

## E. Driver & Fleet (9 modules)

| # | Module | Phase | Description |
|---|--------|-------|-------------|
| 36 | Driver Directory | 1 | Driver CRUD, colour codes, vehicle type, reg |
| 37 | Driver Status Tracking | 1 | Online, Available, On Job, Break, Offline |
| 38 | Driver Compliance / Documents | 2 | DBS, MOT, insurance, licence expiry tracking |
| 39 | Substitute Driver Registry | 2 | Flag drivers as substitute, separate reporting |
| 40 | Vehicle Directory | 2 | Vehicle CRUD, type, capacity, capabilities |
| 41 | Vehicle Capabilities / Class | 2 | Saloon, Estate, MPV, SUV, wheelchair-accessible |
| 42 | Driver-Vehicle Pairing | 2 | Assign default vehicle to driver |
| 43 | Driver Shift / Scheduling | 3 | Shift start/end, break tracking, hours compliance |
| 44 | Driver Earnings Summary | 3 | Per-driver earnings, commission, expenses |

## F. Location & Journey Execution (6 modules)

| # | Module | Phase | Description |
|---|--------|-------|-------------|
| 45 | Driver Location Ingestion | 3 | GPS pings from driver app → Redis |
| 46 | Live Fleet Map | 3 | Map view of all driver positions |
| 47 | ETA / Distance Estimation | 1 | Google Distance Matrix wrapper |
| 48 | Navigation Handoff | 3 | Launch Google Maps / Waze from driver app |
| 49 | Pickup / Onboard / Complete Workflow | 3 | Driver app status progression |
| 50 | Proof / Notes / Incident Capture | 3 | Driver notes, photos, issue reporting |

## G. Partner Network / Cross-Tenant Coverage (6 modules)

| # | Module | Phase | Description |
|---|--------|-------|-------------|
| 51 | Partner Company Registry | 5 | Register/manage partner relationships |
| 52 | Cover Request Workflow | 5 | Create, send, accept, decline cover requests |
| 53 | Partner Acceptance / Decline | 5 | Partner tenant UI for reviewing cover requests |
| 54 | Cross-Tenant Assignment | 5 | Partner dispatches their own driver for the job |
| 55 | Settlement & Revenue Share | 5 | Referral fee, % split, net fulfilment, invoicing |
| 56 | Partner Job Audit Trail | 5 | Full trace: who offered, accepted, settled |

## H. Notifications & Communication (5 modules)

| # | Module | Phase | Description |
|---|--------|-------|-------------|
| 57 | SMS Notifications | 2 | Twilio SMS for customer/driver messages |
| 58 | WhatsApp Notifications | 2 | WhatsApp Business API for customer messages |
| 59 | Push / In-App Notifications | 2 | FCM for driver app, browser push for dispatch console |
| 60 | Operator Alerts / Exceptions | 2 | Unallocated bookings, driver no-response, COA alerts |
| 61 | WhatsApp Chatbot | 6 | AI-powered conversational booking via WhatsApp |

## I. Finance & Reporting (6 modules)

| # | Module | Phase | Description |
|---|--------|-------|-------------|
| 62 | Payment Link / Status Tracking | 2 | Revolut payment links, webhook status updates |
| 63 | Account / Business Invoicing | 2 | Invoice generation, credit notes, mark paid |
| 64 | Driver Payout / Statements | 3 | Driver statements, commission calculation |
| 65 | Reporting Dashboard | 3 | Revenue, bookings, driver utilisation, profitability |
| 66 | Analytics & KPIs | 4 | Advanced metrics, trends, comparisons |
| 67 | Export / CSV / Statements | 3 | Downloadable reports, PDF invoices/statements |

## J. Integrations & Booking Channels (6 modules)

| # | Module | Phase | Description |
|---|--------|-------|-------------|
| 68 | Public Booking API | 2 | REST API for website booking ingestion |
| 69 | Webhook / Event Outbox | 4 | Publish events for external consumers |
| 70 | Website Booking Integration | 6 | White-label website builder, embedded booking forms |
| 71 | Tenant Import Wizard | 4 | Migrate from legacy systems (Ace SQL Server) |
| 72 | Address Lookup Service | 1 | Ideal Postcodes + Google Places abstraction |

---

## Parallelisation Groups

These groups can be developed simultaneously with minimal cross-dependencies:

| Group | Modules | Team / Agent |
|-------|---------|-------------|
| G1: Platform | 1-7 | Tenancy, auth, permissions |
| G2: Booking | 8-18 | Booking intake, validation, lifecycle |
| G3: Pricing | 19-26 | Tariff engine, quotes, overrides |
| G4: Dispatch | 27-35 | Availability, assignment, scoring |
| G5: Fleet | 36-44 | Drivers, vehicles, compliance |
| G6: Location | 45-50 | GPS, maps, journey execution |
| G7: Partners | 51-56 | Partner network, cover, settlement |
| G8: Comms | 57-61 | SMS, WhatsApp, push, chatbot |
| G9: Finance | 62-67 | Payments, invoicing, reporting |
| G10: Integration | 68-72 | APIs, webhooks, websites, import |
