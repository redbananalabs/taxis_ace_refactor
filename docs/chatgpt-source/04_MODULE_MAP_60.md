# Complete Module Map (Target 40–60 modules)

This module map is intended to let AI coding agents work in parallel with minimal overlap.

## A. Platform & Tenancy
1. Tenant Registry
2. Company Profile
3. Company Branding / White-Label Settings
4. User Accounts
5. Roles & Permissions
6. Audit Log
7. Feature Flags / Entitlements

## B. Customer & Booking Intake
8. Customer Directory
9. Customer Notes / Preferences
10. Saved Addresses / Places
11. Booking Capture
12. Scheduled Booking Manager
13. ASAP Booking Manager
14. Return Journey Handling
15. Multi-Stop Journey Handling
16. Accessibility / Special Requirements
17. Booking Validation Rules
18. Booking Status Lifecycle

## C. Pricing & Quotes
19. Fare Rules
20. Fixed Route Pricing
21. Area / Zone Pricing
22. Time-Based Surcharges
23. Extras / Fees
24. Quote Calculator
25. Manual Price Override
26. Promotional / Contract Pricing

## D. Dispatch Core
27. Driver Availability Engine
28. Candidate Selection Engine
29. Dispatch Scoring Engine
30. Manual Assignment
31. Assisted Assignment Suggestions
32. Reassignment / Recovery
33. No-Driver Escalation
34. Booking Priority Rules
35. Dispatch Timeline / History

## E. Driver & Fleet
36. Driver Directory
37. Driver Status Tracking
38. Driver Compliance Metadata
39. Substitute Driver Registry
40. Vehicle Directory
41. Vehicle Capabilities / Class
42. Driver-Vehicle Pairing
43. Driver Shift / Scheduling
44. Driver Earnings Summary

## F. Location & Journey Execution
45. Driver Location Ingestion
46. Live Fleet Map
47. ETA / Distance Estimation
48. Navigation Handoff
49. Pickup / Onboard / Complete Workflow
50. Proof / Notes / Incident Capture

## G. Partner Network / Cross-Tenant Coverage
51. Partner Company Registry
52. Cover Request Workflow
53. Partner Acceptance / Decline
54. Cross-Tenant Assignment
55. Settlement & Revenue Share Rules
56. Partner Job Audit Trail

## H. Notifications & Communication
57. SMS Notifications
58. Email Notifications
59. Push / In-App Notifications
60. Operator Alerts / Exceptions

## I. Finance & Reporting
61. Payment Status Tracking
62. Account / Business Customer Billing
63. Driver Payout Support
64. Reporting Dashboard
65. Analytics & KPIs
66. Export / CSV / Statements

## J. Integrations & Automation
67. Public API
68. Webhook/Event Outbox
69. Website Booking Integration
70. AI Voice Booking Intake (future)
71. AI Dispatch Assistance (future)
72. Forecasting & Demand Signals (future)

## Suggested agent parallelisation groups
- Group 1: tenancy, auth, permissions
- Group 2: booking intake and validation
- Group 3: pricing and quoting
- Group 4: dispatch core
- Group 5: driver/fleet
- Group 6: location/live map
- Group 7: partner coverage
- Group 8: notifications
- Group 9: reporting/finance
- Group 10: integrations/future AI
