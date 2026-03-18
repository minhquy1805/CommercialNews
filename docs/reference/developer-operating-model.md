# Developer Operating Model (CommercialNews)

This document defines the **developer role** in a component- and module-oriented architecture for CommercialNews.
It clarifies how developers should implement boundaries, evolve internal design, and provide feedback to refine architecture.

> Related:
> - Modules & dependency rules: `docs/explanation/architecture/arc42/03-building-blocks-modularity.md`
> - Runtime scenarios: `docs/explanation/architecture/arc42/04-runtime-view-v1.md`
> - Quality requirements: `docs/explanation/architecture/arc42/05-quality-requirements.md`
> - Measurement guide: `docs/explanation/architecture/arc42/06-measurement-guide.md`
> - Governance / fitness functions: `docs/explanation/architecture/arc42/07-architecture-governance.md`
> - Components: `docs/explanation/architecture/arc42/08-components.md`

---

## 1) Core idea

- Architecture defines **modules** (logical boundaries) and **components** (deployable artifacts).
- Developers take those boundaries and break work down further into:
  - classes, functions, and subcomponents
  - internal patterns, error handling, performance considerations

**Key principle:** the initial component design is a **first draft**.  
Implementation reveals friction, hidden coupling, and real constraints—developers must provide feedback so boundaries can be refined.

---

## 2) Developer responsibilities in component-based thinking

### A) Make boundaries real in code
Architects define boundaries; developers enforce them through implementation:
- solution/project structure and folder conventions
- dependency direction (who is allowed to reference whom)
- interfaces/contracts and access control
- avoiding “shortcuts” that bypass ownership rules

### B) Optimize internal design within the boundary
Inside a component/module, developers own the “lion’s share” of design:
- cohesive class structure and clear responsibilities
- predictable error handling
- performance hotspots and resource usage
- testing strategy that supports safe refactoring

### C) Feed reality back into architecture
During implementation, developers often discover:
- boundaries that cause repeated cross-module changes
- workflows smeared across multiple modules
- dependency cycles or accidental coupling
- data ownership friction
- operational complexity that is disproportionate to business value

Developers must surface these issues early and propose refinements (split, merge, contract changes, or revised sync/async boundaries).

---

## 3) Architecture is a hypothesis; code is validation

Treat architectural decisions as hypotheses that must survive contact with reality:
- real workload shapes (bursts, spikes, retries)
- edge cases and messy data
- framework constraints
- latency and failure modes
- security attack patterns

A good architecture evolves based on the feedback loop.

---

## 4) DevOps / SRE / Security responsibilities (baseline expectations)

### DevOps mindset
Developers turn design assumptions into objective checks:
- tests and contract tests run in CI
- builds produce clear component artifacts
- rules are codified as gates (where appropriate)

### SRE mindset
Developers identify failure modes early:
- timeouts, retries, and fallback behavior
- safe idempotency under at-least-once delivery
- logging/tracing that supports incident diagnosis
- metrics that reflect the characteristics we care about (latency percentiles, error rates, backlog/lag)

### Security mindset
Developers are the first line of defense:
- enforce authn/authz at the correct boundary
- validate input and message schemas
- never hard-code secrets; follow secrets management practices
- maintain dependency hygiene (keep CVEs under control)

**Guardrails help, but developers drive the system.**

---

## 5) Common mistakes (and how to avoid them)

### Mistake 1: “Architect designed it; dev just codes.”
**Result:** architecture-on-paper diverges from architecture-in-code, leading to technical debt.  
**Fix:** treat architecture as a hypothesis; implementation validates and refines it.

### Mistake 2: Breaking boundaries for speed without feedback
**Result:** coupling grows, ownership blurs, and the system becomes hard to evolve.  
**Fix:** if a boundary causes pain, raise it, document it, and refine the architecture—don’t bypass silently.

### Mistake 3: Over-abstraction too early
**Result:** unnecessary complexity, slower iteration, harder maintenance.  
**Fix:** keep it simple first; refactor when pain is real and measurable.

---

## 6) Developer checklist for CommercialNews (practical)

When implementing a module/component:

1) **Extract core workflows** (use cases) and map them to application handlers/services.
2) **Define contracts clearly**
   - API endpoints and request/response DTOs
   - event/message schemas and compatibility expectations
   - DTOs vs domain models (do not leak domain objects across boundaries)
3) **Enforce boundaries**
   - project/namespace structure
   - dependency direction rules
   - avoid direct DB/schema access across modules
4) **Observability baseline**
   - structured logs with correlation IDs
   - metrics for critical paths (latency/error rate/backlog)
5) **Security baseline**
   - policy enforcement on sensitive endpoints
   - input validation and safe error handling
   - secrets management compliance
6) **Feedback loop**
   - record friction: “module A and B change together too often”, “contract is hard to use”
   - propose a refinement: split/merge, contract versioning, sync→async boundary changes
   - capture major decisions in ADRs when needed

---