# Evolutionary Architecture with APIs — CommercialNews Playbook (V1 → V2+)

This document applies Chapter 8 (*Evolving Systems with APIs*) from *Mastering API Architecture* to CommercialNews.
Core idea: **APIs are seams** — boundaries that enable safe, incremental change without breaking consumers.

CommercialNews uses APIs to evolve across:
- module refactoring (cleaner boundaries, lower coupling)
- runtime changes (async side effects, projections)
- topology changes (splitting components/services when justified)

Related:
- Constraints: `../architecture/arc42/02-constraints.md`
- Modularity & dependency rules: `../architecture/arc42/03-building-blocks-modularity.md`
- Runtime scenarios: `../architecture/arc42/04-runtime-view-v1.md`
- Quality requirements: `../architecture/arc42/05-quality-requirements.md`
- Governance & fitness functions: `../architecture/arc42/07-architecture-governance.md`
- Components: `../architecture/arc42/08-components.md`
- Architecture style: `../architecture/arc42/09-architecture-style.md`
- Edge routing: `05-edge-gateway-and-traffic-management.md`

---

## 1) APIs as seams (the primary evolution mechanism)

### 1.1 What is a seam?
A **seam** is a boundary where we can “cut, replace, and reconnect” behavior with minimal impact on the rest of the system.

### 1.2 Why seams matter for CommercialNews
CommercialNews must:
- ship V1 quickly (constraints)
- protect the read path under burst load (quality drivers)
- evolve toward projections/read models in V2+ without rewriting core modules

Seams enable:
- incremental migration (strangler-style routing)
- internal rewrites behind stable contracts
- gradual adoption of new patterns (read models, new workers)

---

## 2) Cohesion and coupling (the universal controls)

### 2.1 Cohesion rule (high cohesion)
Each module/API focuses on a single domain purpose.
Anti-example:
- “News API does upload + SEO + email + analytics” → low cohesion → slow evolution.

CommercialNews cohesion posture:
- Content owns lifecycle and taxonomy (source of truth).
- SEO owns slug/canonical/meta policy (reacts to publication state).
- Media owns attachments + primary/ordering rules.
- Reading owns read facade/caching policy (V1) and projections (V2+).
- Interaction owns views/likes/comments (non-blocking by policy).
- Notifications and Audit are consumers (side effects only).

### 2.2 Coupling rule (low coupling)
Consumers must not depend on producer internals.
CommercialNews coupling rules:
- shared DB **not shared schema**: module tables belong to a module.
- no cross-module DB queries except explicitly allowed read-only policy in V1.
- cross-module references are IDs only (`ArticleId`, `UserId`), not object graphs.
- events are used for side effects and projections.

**Rule:** evolution speed is limited by coupling, not by code volume.

---

## 3) Goals, constraints, and finish lines (avoid endless migrations)

### 3.1 Why migrations fail
Migrations commonly become:
- open-ended (no finish line)
- expensive and low-value
- demoralizing for teams

### 3.2 Mandatory “Evolution Packet” (for any major evolution)
Every evolution step must define:

1) **Goals**
- Functional: what capability improves?
- Cross-functional: which characteristics improve (latency, availability, security, operability)?

2) **Constraints**
- what must not regress (read-path SLOs, policy coverage, auditability, compatibility)

3) **Finish line**
- measurable acceptance criteria (fitness functions, metrics)

**Example (good):**
> “Make media uploads safer and more operable without changing public contracts; enable independent rollout.”

**Example (bad):**
> “Split Media into microservices.”

---

## 4) Pattern playbook (recommended vs avoided)

### 4.1 Strangler Fig + Facade routing (recommended)
Use a stable entrypoint and gradually route traffic to new implementations.
CommercialNews examples:
- route `/api/v1/media/*` to a new Media component/service in V2+
- route `/api/v1/search` to a dedicated Search component later
- keep `/api/v1/articles/*` stable while internal composition changes

Rules:
- keep the client contract stable
- migrate one slice at a time
- use observability gates and fast rollback
- document routing decisions and triggers in ADRs

### 4.2 Adapter pattern (use sparingly)
Use adapters only for representation/protocol differences.
Rules:
- adapters translate protocols; they do not own domain logic
- avoid hiding business rules inside the gateway

### 4.3 Anti-pattern: API layer cake (avoid)
Avoid stacking multiple API layers calling each other (A → B → C):
- increases latency and failure propagation
- hides ownership and creates unclear contracts
- makes evolution slower due to implicit coupling

CommercialNews guardrail:
- Reading module composes by policy, but must not become an uncontrolled chain.

---

## 5) Evolution path for CommercialNews (V1 → V2+)

### 5.1 V1 (baseline)
- Service-based modular monolith (API + Worker)
- strict module boundaries and ownership
- selective event-driven side effects:
  - audit ingestion
  - notifications (email)
  - interaction aggregation
- Reading is a **read facade** (bounded queries + caching policy)

### 5.2 V2 (next step)
Primary upgrades:
- convert Reading into a **Read Model**:
  - subscribe to domain events
  - build projections / denormalized read stores
- introduce full-text search or managed search
- Interaction:
  - unique view semantics (policy)
  - moderation model for comments
- SEO:
  - slug alias/redirect strategy
  - sitemap/robots automation

### 5.3 V3+ (topology changes if justified)
Split into additional components/services only when signals justify:
- Read Model / Projection component
- Interaction component
- Media component
- Search component

**Rule:** do not split by ideology. Split when metrics show pressure.

---

## 6) Triggers for topology change (when to split)

Introduce new components/services when one or more triggers persist:
- sustained burst traffic requires independent scaling (read model becomes dominant)
- Interaction becomes a top cost/latency driver
- coordinated releases become frequent due to coupling
- blast radius must be reduced (operational risk)
- teams need independent deployability (organizational trigger)

When triggered:
- write an ADR
- update:
  - `08-components.md`
  - `04-runtime-view-v1.md`
  - rollout strategies and observability gates

---

## 7) Fitness functions for evolution (must not regress)

Evolution must preserve these guardrails:
1) **No cyclic dependencies** between modules  
2) **No boundary violations** (ownership rules respected)  
3) **100% policy coverage** for admin endpoints  
4) **Non-blocking read path** (no sync dependency on audit/notifications/aggregation)  
5) **Safe logging** (no secrets/tokens/PII leaks)  

These map to:
- `../architecture/arc42/07-architecture-governance.md`

---

## 8) Operational discipline during evolution (how to stay safe)

When migrating/routing/splitting:
- keep contracts stable (OpenAPI diff checks)
- apply progressive delivery (canary/blue-green) with gates
- maintain correlation IDs across sync + async boundaries
- monitor:
  - read path P95/P99 + error rate
  - async backlog/lag + DLQ
  - security signals (401/403, rate-limit spikes)

---

## 9) Output artifacts (what to record)

For each major evolution step:
- an ADR with goals/constraints/finish line
- updated module docs (API surface + runtime flows + dependencies)
- updated routing plan (gateway doc) if traffic is moved
- validation plan:
  - tests (unit/contract/component/integration)
  - observability gates (metrics/alerts)

---

## 10) Practical guidance for a small team (V1 constraints)

CommercialNews is designed to be feasible for a solo/small team:
- strict boundaries reduce mental load over time
- avoid premature distributed complexity
- evolve using seams and routing before splitting services
- keep guardrails minimal but enforced
