# Connascence and Contract Coupling (Reference)

This document explains **connascence** as a practical way to reason about coupling in modular and distributed systems.
It is used in CommercialNews to:
- preserve module independence (avoid “distributed monolith” traps)
- design stable **API/event contracts**
- decide **sync vs async** boundaries
- reason about **architecture quantum** (what must change/deploy together)

---

## Reading flow (recommended)

Start here when you are designing or reviewing boundaries and contracts:

1) **Architecture drivers & priorities**  
   - `docs/explanation/architecture/arc42/05-quality-requirements.md`
2) **Module boundaries and dependency rules**  
   - `docs/explanation/architecture/arc42/03-building-blocks-modularity.md`
3) **Runtime scenarios (sync vs async, failure modes)**  
   - `docs/explanation/architecture/arc42/04-runtime-view-v1.md`
4) **Measurement plan (what we observe)**  
   - `docs/explanation/architecture/arc42/06-measurement-guide.md`
5) **Governance (fitness functions / guardrails)**  
   - `docs/explanation/architecture/arc42/07-architecture-governance.md`
6) **This document (Connascence & contract coupling)**  
   - `docs/reference/connascence-and-contract-coupling.md`
7) **Decision records (when a boundary/contract decision is made)**  
   - `docs/explanation/decisions/` (ADRs)

---

## 1) Why connascence (why we “change the measuring stick”)

Traditional code-level coupling metrics (e.g., afferent/efferent coupling) can become too fine-grained when you are reasoning
about system/module boundaries—especially when modules might evolve into separate services.

Connascence is useful because it frames coupling in terms of **change impact**:

> Two components are connascent if a change in one **forces a change in the other** for the system to remain correct.

This is the coupling you feel most strongly in architecture:
- “Do we need to deploy both together?”
- “Did a contract change break another module?”
- “Does a dependency chain amplify latency and failures?”

---

## 2) Core definition (architecture-level coupling)

### 2.1 Definition
Components **A** and **B** are connascent if:
- A change in A requires B to be changed (or reconfigured) to keep the overall system correct.

### 2.2 Why this matters
Connascence directly determines:
- **independent evolution** (can modules change separately?)
- **independent deployment** (can modules deploy separately?)
- **independent operations** (can modules differ in operational characteristics without breaking the system?)

When connascence is high, your “microservices” (or modules) behave like a single unit.
When connascence is low, modules can evolve independently.

---

## 3) Types of connascence

Connascence is commonly grouped into:
- **Static connascence** (build-time / design-time)
- **Dynamic connascence** (runtime)

### 3.1 Static connascence (build-time)

Static connascence exists when components share compile-time artifacts, such as:
- shared domain classes/entities
- shared DTOs that both sides compile against
- shared database schema access (direct table reads/writes across boundaries)

**Key symptom**
- A change in a shared type/schema causes compilation failures or forced synchronized releases.

#### Example (classic “shared Address class”)
If Service A and Service B both compile against a shared `Address` class:
- changing `Address` requires **both** services to update and redeploy
- they are **statically connascent**

#### Production reality
Shared domain models across services/modules are a common path toward a **distributed monolith**.
The system looks split, but deployments remain tightly coupled.

#### CommercialNews example
If Content and SEO share a NuGet package with `ArticleDto`:
- renaming `Slug` to `UrlSlug` breaks builds or runtime behavior
- Content and SEO must deploy together
- the *real* architecture quantum becomes **Content + SEO**

**Guideline**
- Avoid shared domain DTO/entities across modules/services.
- If a shared library is unavoidable, restrict it to stable cross-cutting utilities
  (telemetry wrappers, shared logging primitives), not business models.

---

### 3.2 Dynamic connascence (runtime)

Dynamic connascence appears in runtime wiring:
- synchronous request chains
- asynchronous event contracts
- assumptions about ordering, delivery semantics, and processing timeliness

Dynamic connascence is especially important in distributed and event-driven designs.

#### 3.2.1 Synchronous dynamic connascence (call-and-wait)

A synchronous call means the caller waits for the callee to respond.
This creates **runtime wiring** that couples:
- availability
- latency
- failure behavior

**Operational consequences**
- **End-to-end availability drops** as synchronous dependencies increase.
- **Latency compounds** across chains (P95/P99 tail latency grows quickly).
- **Cascading failures** become more likely (one slow service exhausts upstream pools).

**SRE/DevOps patterns typically required**
- tight timeouts
- bounded retries (retry budgets)
- circuit breakers
- bulkheads
- graceful degradation
- distributed tracing to see dependency chains

**Security consequences**
- each synchronous hop is another security boundary: auth propagation, token forwarding, SSRF risk, etc.

**CommercialNews example**
If Public Query calls Content, then SEO, then Media synchronously for every request:
- the read path becomes fragile under spikes
- failure of any dependency can degrade the entire read experience

**Guideline**
- Keep synchronous chains short, especially on the read path.
- Use sync only when an immediate answer is required (e.g., slug routing).

---

#### 3.2.2 Asynchronous dynamic connascence (event-driven)

Asynchronous messaging reduces call-time coupling:
- producer does not wait for consumers
- prevents synchronous cascading failures

But async does not remove coupling—it **moves coupling** to:
- **schema contracts**
- **delivery semantics**
- **ordering**
- **consumer lag (staleness)**

**Coupling forms in async systems**
- **Schema connascence**: changing event fields breaks consumers.
- **Semantic connascence**: consumers depend on business meaning, not just fields.
- **Delivery connascence**: at-least-once delivery implies idempotency.
- **Ordering connascence**: requiring strict order increases complexity.
- **Timeliness connascence**: lag/backlog affects product correctness and UX.

**DevOps/SRE patterns typically required**
- idempotency keys / deduplication
- retry strategy and DLQ
- poison message handling
- monitoring queue depth/lag and consumer error rates
- schema versioning and compatibility rules

**Security consequences**
- event bus ACLs (who can publish/consume what)
- avoid leaking PII/secrets in events
- auditability/correlation for important events

**CommercialNews example**
Content emits `ArticlePublished` and SEO consumes it:
- you reduce synchronous wiring
- but you now must maintain event compatibility and handle lag safely

**Guideline**
- Async is preferred for side effects (audit, notifications, view aggregation, indexing),
  but requires contract discipline and observability.

---

## 4) Connascence and Architecture Quantum (practical view)

**Architecture quantum** can be understood as:
> the smallest unit of the system that can evolve and be deployed independently.

In practice, your quantum is the set of components that are strongly connascent.

### 4.1 Identifying quantum by change impact
Ask:
- “When we change A, do we routinely have to change B?”
- “Can we deploy A without redeploying B?”
- “Do A and B require tight runtime coordination?”

Examples:
- Shared DTOs between Content and SEO → strong static connascence → same quantum.
- Sync chain Public Query → Content → SEO → Media → fragile runtime coupling → effectively one quantum.
- Event-driven Content → SEO with versioned contracts → looser coupling → separate quanta become possible.

### 4.2 Implication for CommercialNews evolution
CommercialNews can start as a modular monolith, but it should preserve the option to split modules later.
Connascence analysis helps ensure that “future split” remains realistic.

---

## 5) Practical rules for CommercialNews (V1)

### 5.1 Avoid static connascence across modules
- Do **not** share domain DTO/entities as a package between modules.
- Prefer contract boundaries: API schemas or event schemas, versioned explicitly.

### 5.2 Minimize synchronous wiring on the read path
- Keep read path dependency chains short.
- Prefer:
  - slug lookup (SEO) → articleId
  - then read data with bounded logic
- Avoid “call multiple modules synchronously per request” unless justified and bounded.

### 5.3 Prefer async for side effects
Use async events for:
- audit ingestion
- notifications
- view tracking/aggregation
- indexing/projections (V2)

### 5.4 Treat async as at-least-once by default
- consumers must be idempotent
- duplicates must be tolerated
- lag must be monitored

---

## 6) Contract discipline (how we prevent async coupling from becoming chaos)

### 6.1 Event contract rules
- Version events when changes are not backward-compatible.
- Keep payload minimal (“minimum necessary data”).
- Do not put secrets/tokens/PII into event payloads.

### 6.2 Compatibility mindset
- Prefer adding optional fields over breaking field renames/removals.
- Deprecate fields gradually with clear timelines (ADR-driven).
- Consider “consumer-driven” thinking: avoid changes that silently break consumers.

### 6.3 Schema vs semantics
Even when schema is compatible, semantic changes can break consumers.
Document semantic assumptions in ADRs when needed.

---

## 7) Sync vs async decision checklist

Use **sync** when:
- the caller requires an immediate answer to continue
- the dependency is stable, fast, and bounded
- failure handling is clear (timeouts, fallbacks)

Use **async** when:
- the work is a side effect (audit/notification/indexing)
- workload is bursty (views)
- availability and latency of the core flow must not depend on consumers

---

## 8) Common pitfalls (what to watch for)

- **Async ≠ no coupling**: coupling shifts to contract and delivery semantics.
- **Shared domain packages** across modules create distributed monolith behavior.
- **Overusing sync** amplifies tail latency and cascading failures.
- **Retry without idempotency** creates duplicates and corruption.
- **No lag monitoring** makes failures silent until users complain.

---

## 9) Where this shows up in our docs

- Boundary rules and allowed/forbidden dependencies:
  - `docs/explanation/architecture/arc42/03-building-blocks-modularity.md`
- Runtime wiring decisions and failure modes:
  - `docs/explanation/architecture/arc42/04-runtime-view-v1.md`
- Characteristics impacted by wiring (performance, availability, reliability):
  - `docs/explanation/architecture/arc42/05-quality-requirements.md`
- Measurement plan (what we observe, percentiles, lag, backlog):
  - `docs/explanation/architecture/arc42/06-measurement-guide.md`
- Guardrails/fitness functions (prevent shared DTOs, enforce boundaries, detect cycles):
  - `docs/explanation/architecture/arc42/07-architecture-governance.md`
- Specific decisions (slug policy, read model strategy, event contract versioning):
  - `docs/explanation/decisions/` (ADRs)

---