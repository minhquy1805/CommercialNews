# Component Identification Cycle (CommercialNews)

This document captures how CommercialNews identifies and refines **components** using the iterative “component identification cycle” (Chapter 8).

It is **not a waterfall process**. It is a feedback loop designed to prevent architectural drift and to keep component granularity aligned with real-world requirements, workload shape, and operational constraints.

> Key idea: **initial component design is a hypothesis**. Implementation and real traffic validate (or refute) it.

---

## Reading flow (recommended)

Start here when you are deciding what should be a component and how to evolve it:

1) **Module boundaries (logical design)**  
- `docs/explanation/architecture/arc42/03-building-blocks-modularity.md`

2) **Runtime scenarios (actual wiring: sync vs async)**  
- `docs/explanation/architecture/arc42/04-runtime-view-v1.md`

3) **Quality requirements (drivers & priorities)**  
- `docs/explanation/architecture/arc42/05-quality-requirements.md`

4) **Measurement guide (how we measure characteristics)**  
- `docs/explanation/architecture/arc42/06-measurement-guide.md`

5) **Governance (fitness functions / guardrails)**  
- `docs/explanation/architecture/arc42/07-architecture-governance.md`

6) **Components (deployable artifacts and mapping)**  
- `docs/explanation/architecture/arc42/08-components.md`

7) **Reference: wiring/coupling concepts**  
- `docs/reference/connascence-and-contract-coupling.md`  
- `docs/reference/architecture-quantum.md`

8) **Decisions (when changes are made)**  
- `docs/explanation/decisions/` (ADRs)

---

## 1) What is a “component” in this cycle?

- **Module**: a logical boundary in the codebase (cohesion, ownership, low coupling).
- **Component**: a physical deployment unit (build/version/deploy/scan/rollback/observe).

A system can have many modules but only a few components (modular monolith), or map modules to separate components (microservices).

**CommercialNews V1 intent**  
- Keep module boundaries clear (domain partitioning).
- Use a small number of deployable components (API + Worker), then evolve based on signals.

---

## 2) The component identification cycle (5 steps)

### Step 1 — Identify initial components
**Goal**: propose a first set of top-level components based on the chosen partitioning strategy (domain vs technical).

**Inputs**
- Business capabilities: `docs/explanation/domain/business-capabilities.md`
- Module map: `docs/explanation/architecture/arc42/03-building-blocks-modularity.md`

**Output artifacts**
- Initial component inventory and mapping: `docs/explanation/architecture/arc42/08-components.md`

**CommercialNews V1 default**
- Component A: **Public API**
- Component B: **Background Worker**
- External dependencies: database, message broker, object storage (optional)

---

### Step 2 — Assign requirements to components
**Goal**: map explicit + inferred requirements to components to test “fit”.

**Inputs**
- Capability requirements (explicit/inferred/additional context):  
  `docs/explanation/domain/capability-requirements.md`
- Domain concerns: `docs/explanation/domain/domain-concerns.md`

**What to look for (fit signals)**
- A story touches too many components → boundary may be wrong or workflow smeared.
- Many components need deep knowledge of Identity → identity is leaking across boundaries.
- A component owns too many unrelated requirements → “god component” risk.

**Outputs**
- Updated mapping in `08-components.md`
- Candidate ADRs when trade-offs appear: `docs/explanation/decisions/`

---

### Step 3 — Analyze roles and responsibilities
**Goal**: ensure responsibilities are cohesive and boundaries are at the right granularity.

**Inputs**
- Module profiles and responsibilities:  
  `docs/explanation/architecture/arc42/03-building-blocks-modularity.md`  
  `docs/explanation/architecture/arc42/quality/` (module characteristic profiles)
- Runtime workflows: `docs/explanation/architecture/arc42/04-runtime-view-v1.md`

**What to look for**
- Component/module responsibilities are unclear or overlapping.
- The same workflow logic appears scattered in multiple places.
- Dependency cycles and forced shared ownership.

**Outputs**
- Refined responsibilities in 03 and 04
- Updated module profiles when responsibilities shift

---

### Step 4 — Analyze architecture characteristics
**Goal**: validate whether a component’s responsibilities align with the required characteristics.

**Inputs**
- System-level quality requirements: `docs/explanation/architecture/arc42/05-quality-requirements.md`
- Measurement plan: `docs/explanation/architecture/arc42/06-measurement-guide.md`
- Module characteristic profiles: `docs/explanation/architecture/arc42/quality/`

**What to look for**
- Conflicting characteristics inside a single component:
  - read path performance vs heavy background work
  - public UX vs governance security/audit
- Synchronous wiring that forces operational coupling (tail latency, cascading failures).

**Outputs**
- Proposed refactor plans (split/merge responsibilities)
- ADRs for major trade-offs (sync vs async, read model strategy, contract versioning)

---

### Step 5 — Restructure components
**Goal**: evolve components based on real feedback from implementation and operations.

**Inputs**
- Implementation feedback (developer friction)
- Operational signals (latency percentiles, error rates, backlog/lag)
- Governance violations (boundary breaks, cycles)

**Actions**
- Split/merge components or responsibilities.
- Replace synchronous wiring with asynchronous boundaries for side effects.
- Introduce read models/projections when read path demands it.
- Refine contracts (versioning) and enforce idempotency where needed.

**Outputs**
- Updated `08-components.md`
- Updated `04-runtime-view-v1.md` (wiring changes)
- Updated `05-quality-requirements.md` / profiles (priority shifts)
- New ADRs in `docs/explanation/decisions/`
- Updated guardrails in `07-architecture-governance.md`

---

## 3) Restructure triggers (practical signals)

Use these triggers to decide when to restructure:

1) **Change coupling**: one feature routinely touches many components/modules.
2) **Read path regression**: P95/P99 latency grows due to synchronous dependency chains.
3) **Hot path pressure**: Interaction volume causes contention or slows reading.
4) **Security boundary leaks**: identity/authorization logic scattered across domain modules.
5) **Frequent coordinated deploys**: multiple components must deploy together (high connascence).
6) **Recurring incidents**: same operational failure mode repeats.

---

## 4) CommercialNews usage guidance (V1 → V2+)

### V1 posture
- Prefer a small number of deployable components (API + Worker).
- Keep domain boundaries strong at the module level.
- Use async for side effects (audit, notification, view aggregation).

### V2+ evolution
- Introduce additional components only when justified by:
  - distinct operational needs
  - clear data ownership boundaries
  - manageable contract coupling (versioning discipline)
- Consider read models/projections if read bursts require them.

---

## 5) Key references
- Connascence and contract coupling: `docs/reference/connascence-and-contract-coupling.md`
- Architecture quantum: `docs/reference/architecture-quantum.md`
- Governance / fitness functions: `docs/explanation/architecture/arc42/07-architecture-governance.md`