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
- ADR-0020 (Timeout, retry, and failure detection policy)
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

Duplicate pressure may originate from more than one place:

- **producer-side publication ambiguity**
  - outbox publish retried
  - worker restart during publish ambiguity
- **broker/runtime duplication**
  - broker redelivery
  - consumer restart after partial work
- **business-intent duplication**
  - multiple different messages representing the same harmful outward effect unless guarded

Without a system-wide policy, teams may produce fragile handlers such as:

- blind append/insert logic
- duplicate emails on retry
- duplicate audit rows
- counters inflated by replay
- stale events overwriting newer projections
- derived stores that cannot be rebuilt after lag, corruption, or mismatch
- consumers that dedupe only by message identity but still allow duplicate harmful business outcomes

We need an explicit decision that defines:

- the default correctness assumption for consumer processing
- required idempotency mechanisms
- ordering/version rules
- replay expectations
- rebuild/reconciliation obligations for derived state
- how to reason about same-message duplicates vs same-intent duplicates

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
- refresh or invalidate cache in a way that affects behavior materially
- maintain derived security/governance state
- drive outward side effects that users or operators can observe

The system must assume a message may be processed more than once.

---

### 3) Stable event identity is required

Every event used for async consumer processing must carry stable identity sufficient for dedupe and traceability.

Minimum approved identifiers are:

- `MessageId`
- `AggregateId`
- `Version` when ordered aggregate transitions matter

Recommended supporting fields include:

- `EventType`
- `OccurredAt`
- `CorrelationId`

These identifiers must remain stable under:

- worker restart
- consumer retry
- replay/rebuild
- correlation across logs and investigations

**Naming rule:** CommercialNews uses **`MessageId`** as the system-wide contract name for async message identity.  
If a module currently uses names such as `EventId` or `MessageKey`, they should be treated as module-local representations of the same concept and converged toward `MessageId` where practical.

---

### 4) Two layers of idempotency are required: message-level and business-level

CommercialNews distinguishes two independent but complementary protections.

#### 4.1 Message-level idempotency

Protects against processing the same delivered message multiple times.

Typical mechanisms:

- durable processed-message table keyed by `MessageId`
- unique constraints on append targets
- dedupe-aware delivery logs
- monotonic apply state per aggregate

#### 4.2 Business-level idempotency

Protects against duplicate harmful business effects even if:

- the same message is retried
- different messages represent the same business intent
- timeout ambiguity leads to attempted same-intent replay
- operator replay/recovery overlaps with already-completed effect

Examples:

- emails must not be sent twice for the same intended delivery unless policy explicitly allows resend
- audit append must not create duplicate investigation records
- projection updates must not reapply stale versions
- counters must not inflate under replay if exact semantics matter
- governance notifications must not be emitted twice for the same effective change unless explicitly intended

**Rule:** message-level idempotency alone is not always sufficient.  
Where duplicates are harmful, business-level idempotency is also mandatory.

---

### 5) Duplicate source must not change correctness obligations

CommercialNews treats duplicates from all of the following as normal design inputs:

- outbox publish retry
- broker redelivery
- consumer restart
- replay/rebuild job
- same-intent re-emission from upstream under allowed business policy

Consumer correctness must not depend on knowing which duplicate source occurred.

**Rule:** duplicate origin may help investigation, but it must not be required for safe handler behavior.

---

### 6) Ordering-sensitive consumers must use aggregate versioning or truth resync

Where event order matters for correctness, consumers must not rely on arrival order alone.

Approved V1 ordering model:

- per-aggregate ordering only
- `AggregateId` + monotonic `Version` on events
- consumers apply only valid forward progress
- when gaps or stale versions are detected, consumer must:
  - reject stale event safely
  - or resync from truth
  - or defer until consistent state is re-established

No global total order is assumed.

---

### 7) Projection updates must be version-aware or set-based

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

### 8) External side effects require durable delivery tracking or equivalent safeguards

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

A side effect is not considered safe merely because the consumer usually runs once.

**Rule:** “send first, maybe record later” is not acceptable for important external effects.

---

### 9) Same-intent replay must be policy-controlled

CommercialNews distinguishes:

#### A) Duplicate message replay
Same `MessageId` appears again.

Protection:
- message-level idempotency

#### B) Same-intent replay
A different message may attempt the same outward harmful effect.

Protection:
- business-level idempotency
- or explicit resend/replay policy
- or truth-backed reconciliation before effect is retried

Examples:

- resend verification email
- password reset notification
- governance email
- publication-related subscriber notification

**Rule:** same-intent replay must not happen implicitly merely because the system timed out, restarted, or retried upstream work.

---

### 10) Every important derived system must have an approved recovery path

Any derived output important enough to affect:

- UX
- admin operations
- investigation
- governance visibility
- operational confidence

must have at least one recovery strategy.

Approved strategies:

- replay from retained operational history
- rebuild from truth
- reconciliation against authoritative state
- bounded recomputation/candidate regeneration

Not every system requires the same recovery shape.

**Rule:** important derived systems need a documented recovery posture, but that posture may be replay, rebuild, reconciliation, or bounded recompute depending on the data role and cost profile.

---

### 11) Rebuild/reconciliation workflows are first-class recovery tools

CommercialNews V1 formally allows batch-assisted recovery for stream-derived systems.

Approved uses include:

- rebuilding projections
- rematerializing serving artifacts
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

### 12) Safe non-progress beats unsafe stale apply

When the consumer cannot establish safe forward progress due to:

- stale version
- missing prior event
- ownership ambiguity
- uncertain authority
- dedupe uncertainty in a harmful side effect
- ambiguity about whether the same intent already succeeded

the runtime must prefer:

- reject
- retry
- defer
- resync
- rebuild
- operator-controlled remediation

over silently applying a possibly wrong effect.

This is especially important for:

- governance-related projections
- publication visibility artifacts
- security-sensitive derived state
- externally visible notifications
- append-only investigative records

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

### D) Documented recovery posture for important derived systems

Document at least one approved recovery strategy for:

- SEO-derived serving artifacts
- read projections that influence experience materially
- authorization-effective views/caches where applicable
- interaction summaries/counters where correctness/reputation matters
- audit/reporting-derived outputs

### E) Same-intent protection for harmful outward effects

For harmful outward effects, document:

- what counts as the business intent
- what key or state machine protects that intent
- when resend/replay is allowed by policy
- when operator intervention is required

---

## Approved examples

### 1) Audit append

Approved:

- unique `MessageId`
- append only if not already present
- replay-safe append behavior

Disallowed:

- append blindly on every retry

---

### 2) Email delivery

Approved:

- unique delivery record by `MessageId`
- and/or business idempotency key where resend ambiguity exists
- retry updates send state safely
- resend only if policy explicitly allows it

Disallowed:

- send first, maybe record later
- relying only on “single consumer instance” as protection

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

### 5) Governance notification

Approved:

- emit outward notification only if delivery/business guard confirms this change has not already produced the same effect
- replay is policy-controlled and visible

Disallowed:

- resending outward governance effect on every consumer retry

---

## Disallowed assumptions in V1

CommercialNews V1 explicitly disallows the following assumptions:

### 1) “RabbitMQ won’t redeliver this in practice”
Not acceptable. Duplicate delivery is a normal design assumption.

### 2) “The handler is fast, so idempotency is optional”
Not acceptable. Crash and restart can still duplicate effects.

### 3) “Projection order is whatever arrived first”
Not acceptable for ordered lifecycle aggregates.

### 4) “If a derived store is corrupted we can just delete it manually later”
Not acceptable without a documented replay/rebuild/reconciliation path.

### 5) “Exactly once is guaranteed because we only have one consumer instance today”
Not acceptable. Topology can change, crashes can happen, and redelivery still exists.

### 6) “Message-level dedupe is enough for all harmful effects”
Not acceptable. Same-intent duplicates may still be possible.

### 7) “A timeout means the first attempt probably did nothing”
Not acceptable. Timeout ambiguity must be handled as real ambiguity.

---

## Consequences

### Positive

- makes async behavior predictable under retry/replay
- protects important side effects from duplicate harm
- prevents stale overwrite in ordered aggregate projections
- clarifies distinction between same-message duplicate and same-intent duplicate
- gives clear recovery posture for lagging or corrupted derived systems
- aligns runtime expectations with outbox, timeout ambiguity, and at-least-once delivery

### Negative / Trade-offs

- requires extra schema/state for dedupe and delivery tracking
- increases implementation discipline for consumers
- some handlers must manage version state or truth resync logic
- some outward effects need business-intent guards in addition to message dedupe
- rebuild/reconciliation workflows become operational responsibilities

---

## Implementation notes (V1)

- Module docs must specify idempotency keys and replay behavior in `06-idempotency-consistency.md`.
- Observability docs must include:
  - dedupe hits
  - same-intent rejects where measurable
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
- Module docs should explicitly say whether duplicate risk is handled by:
  - message-level dedupe
  - business-level guard
  - version-aware projection apply
  - reconciliation from truth
  - or a combination

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
  - same-intent replay policy
  - ordering/version requirements
  - resync/rebuild/reconciliation behavior
- Update `decisions/README.md` and arc42 index to include ADR-0028
