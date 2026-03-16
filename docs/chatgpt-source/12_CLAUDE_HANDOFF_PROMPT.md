# Copyable Claude Handoff Prompt

You are the lead product architect and systems designer for this project.

I am handing you a Taxi Dispatch SaaS concept pack.
Your job is to take over from here and turn it into a production-grade architecture and implementation plan.

Context:
- This is for a modern taxi dispatch SaaS aimed at small to medium taxi operators.
- It must support booking intake, dispatch, operator workflows, driver workflows, pricing, reporting, and multi-tenant SaaS structure.
- It must explicitly support substitute drivers and inter-company job sharing / coverage.
- It should be commercially suitable for operators of very different sizes.
- It should integrate cleanly with white-label taxi booking websites and SEO landing pages.
- The preferred engineering style is simple modular monolith + vertical slices in .NET:
  - Taxi.Api
  - Taxi.App
  - Taxi.Data
  - optional Taxi.Domain
- In Taxi.App use one file per feature where sensible, endpoints call handlers directly, and handlers use DbContext directly.
- Avoid overengineering v1.

Your tasks:
1. Review and critique the pack.
2. Rewrite it as a stronger, production-ready PRD and architecture spec.
3. Tighten the domain model and identify missing entities/workflows.
4. Turn the module map into a phased engineering plan with priorities.
5. Define recommended database schema, event model, and service boundaries.
6. Define dispatch logic in more detail.
7. Propose a sensible v1, v2, and v3 rollout.
8. Highlight commercial packaging recommendations and risks.
9. Identify anything that should be simplified further before build starts.
10. Produce a build roadmap suitable for Codex / AI coding agents.

Output required:
- Executive critique
- Revised architecture
- Revised module map
- Domain model recommendations
- Build phases
- Risks and simplifications
- Final recommended implementation approach
