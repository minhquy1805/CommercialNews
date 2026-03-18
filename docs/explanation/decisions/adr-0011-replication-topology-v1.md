# ADR-0011 — Replication Topology (V1)

**Status:** Accepted  
**Date:** 2026-03-04  
**Decision owners:** Architecture / Platform  
**Scope:** System-wide (all modules)  
**Related:**
- `../architecture/arc42/04-runtime-view-v1.md`
- `../architecture/arc42/08-components.md`
- `../architecture/arc42/10-system-data.md`
- `../architecture/arc42/11-replication-v1.md`
- `../api-architecture/01-api-architecture-charter-v1.md`
- `../api-architecture/09-observability-and-slos.md`

---

## Context

CommercialNews V1 is **read-heavy and bursty** and must preserve:
- public visibility correctness (draft/unpublished must never leak)
- security-critical identity and authorization behavior
- non-blocking side effects (audit, notifications, interaction aggregation)
- operability under partial failures and replication lag

The system consists of:
- **Public API** (sync write + read path)
- **Background Worker** (async workflows)
- shared database(s), broker, and caches

Replication is required to scale reads and decouple failure domains, but must not compromise invariants.

---

## Decision

### 1) Truth stores are single-leader in V1
All business-critical writes commit to the **primary** truth store (single-leader).
Read replicas may exist, but V1 correctness must not depend on them.

### 2) Cross-module side effects replicate via Outbox → Broker → Consumers
V1 adopts **Outbox pattern** as the application-level replication log:
- domain state change and outbox record are committed atomically (same DB transaction)
- Background Worker publishes outbox messages to the broker (RabbitMQ)
- consumers execute side effects and update derived state

### 3) Delivery model is at-least-once; consumers are idempotent
Async processing is **at-least-once**. Therefore:
- all consumers MUST be idempotent (dedupe by `messageId`)
- where ordering matters, events carry `aggregateId + version`
- consumers enforce per-aggregate ordering or resync from truth when gaps occur

### 4) Consistency guarantees are explicit and scoped
V1 explicitly supports:
- **Read-your-writes** for admin and identity self-state flows (primary reads / write-return)
- **Consistent prefix reads** for cause→effect pipelines (materialize before notify/index)
- **Monotonic reads** for user-facing lists where “time going backward” is unacceptable (stable read path / sticky routing)

### 5) Derived stores are allowed to lag but must have fallbacks
Redis caches and derived/projection stores may be stale. The system must:
- prefer correctness-first fallbacks (truth reads or safe “updating” responses)
- never return misleading 404/empty due to lag on critical routes (SEO slug → content visibility)
- ensure unpublished content is never re-exposed, regardless of async delays

---

## Alternatives considered

1) **Synchronous replication for all writes**
- Pros: stronger durability/read freshness.
- Cons: reduces availability; a slow/unreachable replica can block core flows; conflicts with V1 “non-blocking” requirement.

2) **Multi-leader (active/active) writes**
- Pros: better write availability under multi-region partitions.
- Cons: requires conflict resolution; increases risk for invariants (slug uniqueness, publish workflow, permissions). Too complex for V1.

3) **Leaderless (Dynamo-style) as truth store**
- Pros: high availability via quorum reads/writes.
- Cons: quorum does not provide strong guarantees; conflict/versioning complexity; not suitable for V1 invariants and governance/security flows.

---

## Consequences

### Positive
- Clear separation of sync (truth) vs async (side effects)
- Robust under partial failures; core flows do not block
- Operable: backlog/lag is measurable (outbox age, queue depth, consumer latency)
- Evolvable: derived stores can be added/rebuilt without changing truth semantics

### Negative / Trade-offs
- Replication lag is expected; some views may be eventually consistent
- Requires careful idempotency and ordering design in workers
- Requires explicit fallbacks to avoid user-visible anomalies (prefix violations)

---

## Implementation notes (V1)

- Implement Outbox schema and publisher in Worker.
- Define event envelope fields: `messageId`, `eventType`, `aggregateId`, `version`, `occurredAt`, `correlationId`.
- Implement consumer dedupe and per-aggregate ordering where needed.
- Add replication SLIs:
  - outbox pending count + oldest pending age
  - queue depth per consumer
  - consumer latency p95/p99
  - dedupe hits / duplicate prevention
  - SEO fallback rate

---

## Follow-ups

- For each module: document replication overrides in `modules/{module}/06-idempotency-consistency.md`.
- If V2 introduces projections as primary read sources: add ingestion checkpoints and explicit projection freshness SLOs.
- If multi-region writes/offline operation becomes a requirement: open a new ADR for conflict resolution strategy.