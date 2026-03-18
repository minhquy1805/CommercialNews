# ADR-0028 — Consumer Idempotency, Replay, and Rebuild Policy (V1)

**Status:** Accepted  
**Date:** 2026-03-11  
**Decision owners:** Architecture / Platform  
**Scope:** System-wide (all outbox publishers, RabbitMQ consumers, derived-state updaters, and recovery workflows)  
**Related:**
- `../architecture/arc42/11-replication-v1.md`
- `../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../architecture/arc42/19-stream-processing-runtime-v1.md`
- ADR-0013 (Outbox & delivery semantics)
- ADR-0015 (Cache policy & invalidation)
- ADR-0021 (Clock, time, and ordering policy)
- ADR-0022 (Versioning and fencing strategy)
- ADR-0024 (Distributed coordination and singleton work policy)

---

## Context

CommercialNews V1 uses:

- local truth transactions
- atomic outbox writes
- Background Worker publish
- RabbitMQ delivery
- asynchronous consumers for side effects and derived-state maintenance

By decision, delivery is **at-least-once end-to-end**.
That means the runtime must assume:

- outbox publish may retry
- broker delivery may duplicate
- consumers may crash after partial work
- messages may be replayed after restart
- older messages may arrive after newer ones for the same aggregate
- bounded rebuild/reconciliation workflows may intentionally reapply effects

Without a system-wide policy, teams may produce fragile handlers such as:

- blind append/insert logic
- duplicate emails on retry
- duplicate audit rows
- counters inflated by replay
- stale events overwriting newer projections
- derived stores that cannot be rebuilt after lag, corruption, or mismatch

We need an explicit decision that defines:

- the default correctness assumption for consumer processing
- required idempotency mechanisms
- ordering/version rules
- replay expectations
- rebuild/reconciliation obligations for derived state

---

## Decision

### 1) CommercialNews V1 assumes at-least-once delivery and targets effectively-once outcomes
CommercialNews V1 does **not** assume exactly-once delivery across heterogeneous systems.

The default assumption is:

- publish can retry
- broker can redeliver
- consumer processing can restart
- duplicate handling is a normal responsibility

Therefore, V1 correctness is achieved by:

- idempotent consumer behavior
- version-aware apply rules where ordering matters
- explicit replay and rebuild posture
- safe fallback over stale or duplicated effects

Exactly-once claims are out of scope unless explicitly proven for a narrow mechanism.

---

### 2) Consumer idempotency is mandatory for all important handlers
All important consumers MUST be safe under duplicate processing.

This applies to consumers that:

- send emails or notifications
- append audit entries
- update projections/read models
- update counters or summaries
- emit downstream derived effects
- refresh or invalidate cache in a way that affects behavior
- maintain derived security/governance state

The system must assume a message may be processed more than once.

---

### 3) Stable event identity is required
Every event used for async consumer processing must carry stable identity sufficient for dedupe and traceability.

Minimum approved identifiers are:

- `MessageId` / `EventId`
- `AggregateId`
- `Version` when ordered aggregate transitions matter

These identifiers must remain stable under:

- worker restart
- consumer retry
- replay/rebuild
- correlation across logs and investigations

---

### 4) Two layers of idempotency are required: message-level and business-level
CommercialNews distinguishes:

#### 4.1 Message-level idempotency
Protects against processing the same delivered message multiple times.

Typical mechanisms:

- durable processed-message table keyed by `MessageId`
- unique constraints on append targets
- dedupe-aware delivery logs
- monotonic apply state per aggregate

#### 4.2 Business-level idempotency
Protects against duplicate harmful business effects even if message handling is retried.

Examples:

- emails must not be sent twice for the same intended delivery
- audit append must not create duplicate investigation records
- projection updates must not reapply stale versions
- counters must not inflate under replay if exact semantics matter

Both levels are mandatory where duplicates are harmful.

---

### 5) Ordering-sensitive consumers must use aggregate versioning or resync from truth
Where event order matters for correctness, consumers must not rely on arrival order alone.

Approved V1 ordering model:

- per-aggregate ordering only
- `AggregateId` + monotonic `Version` on events
- consumers apply only valid forward progress
- when gaps or stale versions are detected, consumer must:
  - reject stale event safely, or
  - resync from truth, or
  - defer until consistent state is re-established

No global total order is assumed.

---

### 6) Projection updates must be version-aware or set-based
Consumers that maintain projections/read models must avoid blind mutation where replay or out-of-order delivery can corrupt state.

Approved patterns include:

- idempotent upsert
- replace-by-version
- apply-only-if-incoming-version-is-newer
- set-based recomputation for bounded repair
- durable last-applied version per aggregate

Disallowed for important projections:

- blind append with no dedupe
- blind overwrite with no version check when stale overwrite is possible
- relying on arrival order as if it were authoritative

---

### 7) External side effects require delivery logs or equivalent durable dedupe
For external effects such as:

- sending email
- push delivery
- webhook-like integrations
- other non-transactional outbound effects

framework-level retry alone is insufficient.

These effects must use durable idempotency mechanisms such as:

- delivery log keyed by `MessageId`
- business idempotency key
- unique send record
- explicit sent/finalized state machine

A side effect is not considered safe merely because the consumer “usually only runs once.”

---

### 8) Every important derived system must have a replay or rebuild path
Any derived output important enough to affect UX, admin operations, or investigations must have at least one recovery strategy.

Approved strategies:

- replay from retained operational history
- rebuild from truth
- reconciliation against authoritative state
- bounded recomputation/candidate regeneration

A derived system with no replay or rebuild posture is not acceptable if it materially affects system behavior.

---

### 9) Rebuild/reconciliation workflows are first-class recovery tools, not exceptional hacks
CommercialNews V1 formally allows batch-assisted recovery for stream-derived systems.

Approved uses include:

- rebuilding projections
- rematerializing search/serving artifacts
- reconciling audit/reporting views
- recalculating counters or summaries
- repairing delayed or missing derived outputs

These workflows must be:

- bounded
- rerun-safe
- observable
- candidate-before-cutover where correctness matters

They do not redefine truth ownership.

---

### 10) Safe non-progress beats unsafe stale apply
When the consumer cannot establish safe forward progress due to:

- stale version
- missing prior event
- ownership ambiguity
- uncertain authority
- dedupe uncertainty in a harmful side effect

the runtime must prefer:

- reject
- retry
- defer
- resync
- rebuild

over silently applying a possibly wrong effect.

This is especially important for:

- governance-related projections
- publication visibility artifacts
- security-sensitive derived state
- externally visible notifications or append-only records

---

## Required implementation patterns (V1)

### A) Required event identity fields
For important async events, include:

- `MessageId`
- `EventType`
- `AggregateId`
- `Version` where ordered transitions matter
- `OccurredAt`
- `CorrelationId`

### B) Durable dedupe for critical effects
Use durable storage or equivalent safeguards for:

- audit append
- notification delivery
- critical derived-state apply tracking
- any effect where duplicates are materially harmful

### C) Version-aware apply for lifecycle/projection consumers
Use:

- monotonic aggregate version
- last-applied-version tracking
- resync on gaps/out-of-order where needed

### D) Rebuildability for important derived systems
Document at least one approved recovery strategy for:

- SEO-derived serving artifacts
- read projections that influence experience materially
- authorization-effective views/caches where applicable
- interaction summaries/counters where correctness/reputation matters
- audit/reporting-derived outputs

---

## Approved examples

### 1) Audit append
Approved:
- unique `MessageId` or `AuditEventId`
- append only if not already present
- replay-safe append behavior

Disallowed:
- append blindly on every retry

---

### 2) Email delivery
Approved:
- unique delivery record by `MessageId` or business idempotency key
- retry updates send state safely
- resend only if policy explicitly allows it

Disallowed:
- “send first, maybe record later” with no durable dedupe

---

### 3) Projection update from article lifecycle event
Approved:
- `ArticlePublished(articleId, version=7)`
- projection applies only if version 7 is newer than current
- duplicate version 7 is harmless
- stale version 6 is ignored or causes resync if needed

Disallowed:
- blindly overwrite projection with whatever arrives last

---

### 4) Counter/summary update
Approved when exactness matters:
- dedupe raw events
- bounded recompute/reconciliation
- commutative logic plus replay-safe semantics where acceptable

Disallowed:
- naive increment on every retry when duplicates materially distort output

---

## Disallowed assumptions in V1

CommercialNews V1 explicitly disallows the following assumptions:

### 1) “RabbitMQ won’t redeliver this in practice”
Not acceptable.
Duplicate delivery is a normal design assumption.

### 2) “The handler is fast, so idempotency is optional”
Not acceptable.
Crash and restart can still duplicate effects.

### 3) “Projection order is whatever arrived first”
Not acceptable for ordered lifecycle aggregates.

### 4) “If a derived store is corrupted we can just delete it manually later”
Not acceptable without a documented replay/rebuild path.

### 5) “Exactly once is guaranteed because we only have one consumer instance today”
Not acceptable.
Topology can change, crashes can happen, and redelivery still exists.

---

## Consequences

### Positive
- Makes async behavior predictable under retry/replay
- Protects important side effects from duplicate harm
- Prevents stale overwrite in ordered aggregate projections
- Gives clear recovery posture for lagging or corrupted derived systems
- Aligns runtime expectations with outbox and at-least-once delivery

### Negative / Trade-offs
- Requires extra schema/state for dedupe and delivery tracking
- Increases implementation discipline for consumers
- Some handlers must manage version state or truth resync logic
- Rebuild/reconciliation workflows become operational responsibilities

---

## Implementation notes (V1)

- Module docs must specify idempotency keys and replay behavior in `06-idempotency-consistency.md`.
- Observability docs must include:
  - dedupe hits
  - stale-version rejects
  - replay/rebuild lag
  - recovery workflow results
- Important consumers should log:
  - `MessageId`
  - `AggregateId`
  - `Version`
  - dedupe/apply decision
  - retry attempt
  - correlation ID
- Where version gaps are possible, consumer behavior must be explicit:
  - reject
  - resync
  - defer
  - rebuild
- External side-effect handlers must define durable send/apply state transitions.

---

## Follow-ups

- Update module docs, especially:
  - Content
  - SEO
  - Reading
  - Interaction
  - Notifications
  - Audit
  - Authorization
  - Identity
- Ensure module-level docs explicitly state:
  - emitted/consumed events
  - message-level dedupe keys
  - business-level idempotency keys
  - ordering/version requirements
  - resync/rebuild behavior
- Update `decisions/README.md` and arc42 index to include ADR-0028