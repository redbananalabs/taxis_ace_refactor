# Dispatch and Job Sharing Logic

## Dispatch design goal
Produce sensible, explainable assignment decisions that operators can trust and override.

## Driver candidate scoring inputs
Possible inputs:
- driver availability state,
- estimated distance to pickup,
- estimated time to pickup,
- vehicle suitability,
- job priority,
- driver shift status,
- driver recent workload,
- customer/driver affinity if relevant,
- partner coverage eligibility.

## Suggested scoring model
Weighted score example:
- availability gate: must be eligible
- vehicle compatibility gate
- distance / ETA score
- priority handling bonus
- fairness / workload balancing modifier
- preferred driver / account modifier
- manual penalty for recently declined jobs
- partner-company option only after internal options fall below threshold or timeout

## Dispatch flow
1. Booking created or becomes dispatchable.
2. System validates booking completeness.
3. Internal driver candidate pool generated.
4. Candidate scores calculated.
5. Dispatcher sees recommendation or system auto-assigns if policy permits.
6. If no viable internal driver:
   - escalate to substitute driver pool if configured,
   - or escalate to partner-company coverage.
7. Once assigned, driver notified.
8. Booking state updated as job progresses.

## Manual override
Dispatcher must be able to:
- choose a different driver,
- force partner handoff,
- hold a booking unassigned,
- lock an assignment.

## Substitute driver logic
Substitute drivers:
- may behave like temporary internal capacity,
- should appear separately in reporting,
- may use different commercial terms,
- may require extra approval before assignment.

## Partner-company job sharing logic
### Use cases
- own fleet at capacity,
- geographic convenience,
- out-of-area drop/pickup,
- night/weekend coverage,
- reciprocal company agreements.

### Flow
1. Source tenant creates cover request.
2. Target tenant accepts or declines.
3. If accepted, target tenant dispatches within their fleet or designated driver pool.
4. Source tenant retains visibility appropriate to relationship.
5. Settlement generated per agreed rules.

## Settlement examples
- fixed referral fee,
- percentage split,
- net fulfilment price,
- account customer pass-through.

## Audit requirements
Need full trace of:
- who offered cover,
- who accepted,
- when assignment changed,
- what commercial rule applied.
