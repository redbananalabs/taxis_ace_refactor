# Taxi Dispatch Master Pack

This pack is the full handoff set for the Taxi Dispatch SaaS concept discussed so far.

## Purpose
Give Claude enough context to take over as lead architect and expand the system into a production-ready blueprint.

## Included
- Executive overview
- Full PRD
- System architecture
- Module map
- Data model
- API surfaces
- Dispatch logic
- Multi-tenant + partner fleet model
- Pricing / packaging
- AI roadmap
- Codex implementation plan
- Risks / decisions / open questions
- Claude handoff prompt

## Product intent
Build a modern taxi dispatch SaaS for small to medium taxi operators, starting with real-world operator needs:
- booking and dispatch
- operator dashboard
- driver app
- substitute / external drivers
- overflow job sharing between companies
- white-label SEO booking websites feeding jobs directly into the system
- future AI automation across booking, pricing, routing, and support

## Important grounding assumptions
- Keep v1 commercially realistic and simple enough to implement.
- Strong support for small rural operators as well as larger regional fleets.
- Pricing and entitlements must support very different fleet sizes and booking volumes.
- External / substitute drivers must be first-class entities, not a hack.
- Cross-company coverage and job sharing should be designed in from the start, even if launched in phases.

## Recommended next move
Ask Claude to:
1. critique the pack,
2. tighten the domain model,
3. turn this into a production architecture,
4. produce implementation tasks for Codex / engineering agents.
