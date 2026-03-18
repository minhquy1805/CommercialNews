# Architecture Decisions (ADR) — CommercialNews

This folder contains **Architecture Decision Records (ADRs)** for CommercialNews.

ADRs capture:
- **what** we decided
- **why** we decided it (context + alternatives)
- **trade-offs** and consequences
- **follow-ups** and enforcement notes

They exist to:
- prevent architectural drift
- support onboarding
- make changes auditable over time
- preserve the reasoning behind system-wide rules, especially around consistency, reliability, security, ordering, coordination, stream processing, and derived-state behavior

---

## How to use ADRs

### When to create an ADR
Create an ADR when a decision:
- impacts multiple modules/components
- changes system-wide non-functional behavior (security, availability, operability, recoverability)
- affects data contracts or external API behavior
- changes correctness boundaries (transactions, caching, retries, ordering, truth ownership)
- changes how derived state is built, streamed, published, repaired, or served
- changes consumer replay/idempotency semantics
- introduces or narrows coordination/ownership semantics
- is costly to reverse (store placement, replication semantics, identity strategy, caching, authz model, coordination approach, derived-state orchestration, stream-processing behavior)

### How to write
Keep ADRs short and practical:
- Context (what problem we’re solving)
- Decision (the rule we will follow)
- Alternatives considered (brief)
- Consequences (pros/cons)
- Implementation notes + follow-ups

### Status lifecycle
- **Proposed** → **Accepted** → (**Superseded** | **Deprecated**)
- When superseding, link both directions:
  - old ADR: “Superseded by ADR-XXXX”
  - new ADR: “Supersedes ADR-XXXX”

---

## ADR index (V1)

### Replication, consistency, ordering, runtime correctness, stream/consumer behavior, and derived-state policy
- **ADR-0011 — Replication Topology (V1)**  
  Single-leader truth + Outbox → Broker → Consumers; explicit consistency guarantees; derived-store fallbacks.

- **ADR-0013 — Outbox & Delivery Semantics (V1)**  
  At-least-once delivery; idempotent consumers; per-aggregate ordering; retry/DLQ and operability metrics.

- **ADR-0018 — Transaction Boundaries & Consistency Model (V1)**  
  Local truth transactions as the core correctness boundary; outbox in the same transaction; eventual side effects; selective read-your-writes.

- **ADR-0019 — System Model and Fault Assumptions (V1)**  
  Partially synchronous timing, crash-recovery faults, faulty-but-honest internal nodes, malicious/malformed external inputs, truth-vs-derived posture, timeout ambiguity, and fallback-first correctness.

- **ADR-0020 — Timeout, Retry, and Failure Detection Policy (V1)**  
  Timeout as ambiguity, not proof of failure; bounded retries with backoff/jitter; health layering; truth reconciliation after ambiguous outcomes; graceful degradation under partial failure.

- **ADR-0021 — Clock, Time, and Ordering Policy (V1)**  
  UTC wall-clock for persisted/system timestamps; monotonic timing for duration/latency; timestamps are not causality; versioning/ordering must not rely on wall-clock freshness alone.

- **ADR-0022 — Versioning and Fencing Strategy (V1)**  
  Explicit versioning for ordered state; stale-write rejection; projection freshness protection; fencing/generation semantics where ownership can transfer.

- **ADR-0023 — Consistency, Ordering, and Consensus Boundaries (V1)**  
  Strong truth-backed consistency for correctness-critical state; per-aggregate / causal ordering where required; eventual consistency for lag-tolerant derived behavior; no premature consensus or global ordering assumptions.

- **ADR-0024 — Distributed Coordination and Singleton Work Policy (V1)**  
  Minimize coordination by design; prefer idempotency, partitioning, DB-enforced winners, and replay-safe workflows; require fencing/generation semantics for ownership transfer; no naive singleton/leader patterns.

- **ADR-0025 — Batch Processing and Derived State Policy (V1)**  
  Derived state is subordinate to truth; batch outputs are rebuildable and must not become hidden authority; candidate outputs must be validated before publication when correctness matters.

- **ADR-0026 — Batch Job Orchestration and Materialization Policy (V1)**  
  Batch and reconciliation workflows must be bounded, rerun-safe, observable, and explicit about ownership, publication/cutover, freshness, and stale-run protection.

- **ADR-0027 — Stream Processing and Derived State Policy (V1)**  
  Stream processing is a first-class derived-state lane; truth remains upstream and authoritative; stream outputs are replayable, rebuildable, and subordinate to truth.

- **ADR-0028 — Consumer Idempotency, Replay, and Rebuild Policy (V1)**  
  Consumers must be safe under at-least-once delivery, duplicates, replay, stale delivery, and rebuild/resync; message dedupe and truth-backed resync rules are mandatory where correctness matters.

### Data stores & identifiers
- **ADR-0012 — Data Store Placement (V1)**  
  SQL as OLTP truth, Redis as derived-only, append-only logs in SQL, async aggregates with V2 hooks.

- **ADR-0014 — Public Identifier Strategy (V1)**  
  Separate internal PK vs public opaque IDs (ULID/UUID) vs slugs; slug routing sidecar; no numeric ID exposure.

### Caching
- **ADR-0015 — Cache Policy & Invalidation (Redis) (V1)**  
  Cache-aside by default; event-driven invalidation + TTL fallback; correctness guardrails for SEO, visibility, and security-sensitive paths.

### Security & authorization
- **ADR-0016 — Authorization Model (RBAC/ABAC + Policies) (V1)**  
  RBAC baseline + ABAC for context; policy-based enforcement; stable permission taxonomy; safe caching posture.

### Data partitioning
- **ADR-0017 — Partitioning Strategy (V1)**  
  Partition-ready posture for future scale; avoid premature sharding while shaping schemas, keys, and read/write paths so hotspot mitigation and per-partition evolution remain possible.

---

## Reading order (recommended)

For the most important V1 correctness story, read in this order:

1. **ADR-0011** — replication topology  
2. **ADR-0013** — outbox and delivery semantics  
3. **ADR-0015** — cache policy and invalidation  
4. **ADR-0018** — transaction boundaries and consistency model  
5. **ADR-0019** — system model and fault assumptions  
6. **ADR-0020** — timeout, retry, and failure detection  
7. **ADR-0021** — clock, time, and ordering  
8. **ADR-0022** — versioning and fencing strategy  
9. **ADR-0023** — consistency, ordering, and consensus boundaries  
10. **ADR-0024** — distributed coordination and singleton work policy  
11. **ADR-0025** — batch processing and derived state policy  
12. **ADR-0026** — batch job orchestration and materialization policy  
13. **ADR-0027** — stream processing and derived state policy  
14. **ADR-0028** — consumer idempotency, replay, and rebuild policy  

That path explains the core V1 posture:
- where truth lives
- what may lag
- what success means
- how retries and duplicates are handled
- why timeouts and timestamps are not simple truth
- how stale writers and stale workers are rejected safely
- where strong truth, causal ordering, and eventual consistency each apply
- why consensus and coordination are minimized by design in V1
- why derived state stays subordinate to truth
- how batch/reconciliation/materialization workflows are bounded and published safely
- how stream consumers behave under replay, duplicate delivery, lag, stale events, and truth-backed rebuild/resync

---

## Naming convention

File name format (recommended):
- `adr-XXXX-short-title-v1.md`

Examples:
- `adr-0013-outbox-and-delivery-semantics-v1.md`
- `adr-0019-system-model-and-fault-assumptions-v1.md`
- `adr-0021-clock-time-and-ordering-policy-v1.md`
- `adr-0023-consistency-ordering-and-consensus-boundaries-v1.md`
- `adr-0024-distributed-coordination-and-singleton-work-policy-v1.md`
- `adr-0025-batch-processing-and-derived-state-policy-v1.md`
- `adr-0026-batch-job-orchestration-and-materialization-policy-v1.md`
- `adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## Relationship to other docs

- arc42 is the **architecture narrative** and system views: `../architecture/arc42/`
- API Architecture Charter is the **non-negotiable API rules**: `../api-architecture/`
- module docs under `../api-architecture/modules/` describe module-specific API/runtime/idempotency rules
- ADRs are the **decision history** that explains why arc42, module docs, and API rules look the way they do

### Practical split of responsibilities
- **arc42** explains the architecture as a coherent system
- **ADRs** record the binding decisions and trade-offs
- **module docs** apply those decisions at module level
- **reference/** explains reusable ideas, vocabulary, and heuristics

### Rule of consistency across documentation
If you change:
- truth-vs-derived boundaries
- retry/idempotency rules
- stream-processing posture
- replay/rebuild/resync behavior
- publication/cutover policy for important derived outputs
- coordination/ownership semantics

then ADRs, arc42, and affected module docs should be updated together or in the same PR.

---

## Current architecture themes reflected in ADRs

Across the accepted ADR set, CommercialNews V1 follows these recurring themes:

- truth is explicit and owned by module
- derived state may lag
- core flows succeed on truth commit, not side-effect completion
- outbox is the standard async replication boundary
- at-least-once delivery is normal
- idempotency is mandatory
- versioning beats timestamp-based freshness
- cache is acceleration only, never hidden truth
- ordering is scoped; global total order is not assumed by default
- cause must be durable before effect becomes meaningful
- stale writers and stale workers must be rejectable
- consensus and coordination are minimized by design
- derived outputs are rebuildable and subordinate to truth
- important materialized outputs should use candidate → validate → publish/cutover when correctness matters
- rerun-safe batch/reconciliation workflows are preferred over brittle one-shot jobs
- stream consumers must tolerate duplicate delivery, replay, lag, and stale events safely
- rebuild/resync from truth is preferred over blind trust in lagging derived state
- if uncertain, prefer safe fallback or fail-closed behavior over stale confidence
- observability must distinguish truth-path health from derived-path lag

---

## Next ADR candidates (V2+)

- Multi-region strategy (read replicas, write locality, conflict policy)
- Search/index pipeline (CDC vs outbox-derived projections)
- Projection checkpoints and freshness SLOs
- Data retention and archival policy (logs, outbox, media)
- Read-model promotion rules (when a projection becomes a primary serving source)
- Dedicated coordination-service ADR if future singleton scheduling / ownership transfer / cluster membership truly require it
- Stronger invariant-specific ADRs:
  - “last admin” protection
  - quota/balance-style aggregate guards
  - advanced moderation / abuse pipeline semantics
- Materialized search/read-serving promotion policy
- Batch backfill safety rules for large-scale rebuilds and cutovers
- Stream checkpointing / watermark policy if future stream processors require stronger progress tracking
- Cross-region replay/resync policy if future deployments widen failure domains