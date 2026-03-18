# Architect Operating Model (CommercialNews)

This document defines how architecture is **defined, refined, managed, and governed** in CommercialNews.
It is intentionally lightweight and project-specific.

> Links:
> - Modules & dependency rules: `docs/explanation/architecture/arc42/03-building-blocks-modularity.md`
> - Quality requirements: `docs/explanation/architecture/arc42/05-quality-requirements.md`
> - Measurement guide: `docs/explanation/architecture/arc42/06-measurement-guide.md`
> - Governance / fitness functions: `docs/explanation/architecture/arc42/07-architecture-governance.md`
> - Components: `docs/explanation/architecture/arc42/08-components.md`

---

## 1) Define
- Define module boundaries as business capabilities (Content, SEO, Media, Interaction, Identity, Authorization, Audit, Notifications).
- Define ownership: each module owns its data and rules.
- Define component boundaries for V1: Public API + Background Worker.

## 2) Refine
We refine boundaries when signals appear:
- repeated cross-module changes for one feature (high change coupling)
- read path latency regressions caused by synchronous chains
- scaling needs differ strongly across modules (e.g., Interaction vs Content)
- recurring incidents tied to contract coupling or shared schema

## 3) Manage
- Prefer async for side effects (audit, notifications, view aggregation).
- Keep sync chains short on the read path.
- Do not share domain entities/DTOs across modules; use versioned contracts.
- Use IDs as cross-module references; avoid object graph coupling.

## 4) Govern
- Enforce dependency rules and prevent cyclic dependencies (CI fitness functions).
- Enforce policy coverage for admin endpoints.
- Ensure non-blocking read path and safe logging practices.
- Measure and observe key characteristics (latency percentiles, error rates, backlog/lag).

## 5) ADR triggers (when a decision must be recorded)
Write an ADR when changing:
- module/component boundaries
- sync vs async integration strategy
- contract versioning rules or event payloads
- data ownership or retention policies