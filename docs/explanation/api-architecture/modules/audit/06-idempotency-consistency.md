# Audit — Idempotency & Consistency (V1)

This document defines Audit-specific idempotency, append-only consistency, lag posture, ordering posture, replay/reconciliation safety, and investigation readiness.

System-wide rules live in:
- `../../../../architecture/arc42/11-replication-v1.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/14-distributed-systems-assumptions-v1.md`
- `../../../../architecture/arc42/15-consistency-ordering-and-consensus-v1.md`
- `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- ADR-0013 (Outbox & delivery semantics)
- ADR-0018 (Transaction boundaries & consistency model)
- ADR-0019 (System model and fault assumptions)
- ADR-0020 (Timeout, retry, and failure detection policy)
- ADR-0021 (Clock, time, and ordering policy)
- ADR-0022 (Versioning and fencing strategy)
- ADR-0023 (Consistency, ordering, and consensus boundaries)
- ADR-0024 (Distributed coordination and singleton work policy)
- ADR-0025 (Batch processing and derived state policy)
- ADR-0026 (Batch job orchestration and materialization policy)
- ADR-0027 (Stream processing and derived state policy)
- ADR-0028 (Consumer idempotency, replay, and rebuild policy)

---

## 0) Role of Audit in the system

Audit is a **governance evidence module**.

It does not own business truth such as:
- publication state
- identity security state
- authorization truth
- notification truth
- routing truth

Instead, Audit owns:
- durable evidence that an auditable event was observed and persisted according to policy

Therefore:
- Audit is not the source of truth for whether a business action was valid
- Audit is the source of truth for whether the system recorded an investigation-ready audit fact

This distinction is important:
- business truth may exist before audit catches up
- audit lag must not block business success
- audit evidence is authoritative for investigations only after it has been persisted

---

## 1) Truth vs derived

### 1.1 Truth (Audit store)
Audit owns an append-only, investigation-ready log:
- immutable audit records with actor, action, target, timestamps, `correlationId`
- local ingestion metadata required for operability
- durable duplicate-prevention outcome where policy requires it

Retention is policy-driven.  
Accidental deletes are not allowed.

### 1.2 Audit truth is evidence truth, not business truth
Audit answers questions like:
- was this auditable event recorded?
- what event identity produced this audit record?
- what actor/action/resource metadata was captured?
- when was it observed and persisted?

Audit does **not** answer, by itself:
- whether the originating truth state still currently exists
- whether the business action was the latest state transition
- whether another module’s current truth has since changed

For those questions, investigations must correlate audit with module-owned truth.

### 1.3 Derived (allowed)
Any of the following are derived and may lag:
- audit dashboards
- summaries
- reporting views
- analytics or timeline materializations
- archive indexes
- compliance/reporting projections
- completeness reconciliation reports
- replay candidate sets

**Rule:** investigation-grade audit persistence is truth for Audit; summaries, dashboards, reports, and repair outputs are derived.

### 1.4 Consistency class for Audit
Audit intentionally uses multiple consistency classes:

#### Strong truth-backed consistency
Required for:
- append-only persistence of canonical audit facts
- dedupe outcome for canonical event identity
- immutability of recorded evidence
- local audit-store integrity for investigation-ready facts

#### Ordered / causality-sensitive consistency
Required only where:
- a consumer must avoid recording stale or duplicate representations of the same canonical fact
- replay/reprocess must distinguish missing evidence from already-persisted evidence
- corrective records or related evidence chains must preserve explicit linkage
- a bounded repair/reconciliation workflow must not publish contradictory derived completeness state

#### Eventual consistency
Accepted for:
- arrival of audit evidence relative to the originating business action
- dashboards and derived views
- replay/remediation convergence
- cross-module reporting materializations
- archival and summarization outputs

---

## 2) Idempotency (mandatory)

### 2.1 Primary idempotency key
Use `EventId / MessageId` as the primary idempotency key.

If the same triggering event is delivered multiple times, Audit must not create duplicates.

Recommended enforcement:
- unique constraint/index on `AuditEventId` or `MessageId`
- deterministic consumer dedupe keyed by `EventId / MessageId`

**Rule:** one canonical triggering event produces at most one canonical audit record.

### 2.2 Duplicate delivery vs duplicate candidate
Audit must distinguish:

#### A) Duplicate delivery
The same upstream message is delivered again.  
Protection:
- dedupe by `EventId / MessageId`

#### B) Multiple upstream candidates describing the same business action
This can happen if:
- upstream emits redundant events
- retry/replay surfaces multiple similar records
- several events loosely describe one governance action

Preferred posture:
- upstream should emit one canonical auditable event
- if that is not possible, Audit must define and document a canonical business dedupe key

### 2.3 Canonical mapping rule
Audit should prefer:
- one upstream event -> one audit record

If multiple upstream events are intentionally distinct, they may produce multiple audit rows.  
If they are merely duplicate representations of the same auditable fact, they must converge deterministically.

### 2.4 Idempotency is preferred over singleton assumptions
Audit correctness must not depend on:
- only one consumer instance running
- one process being “the current audit leader”
- local ownership belief
- startup order
- one replay worker being assumed exclusive without authoritative protection

Audit should instead rely on:
- canonical event identity
- append-only inserts
- durable dedupe
- replay-safe consumer behavior

### 2.5 Idempotency must survive replay
If the same canonical event is seen again due to:
- worker restart
- broker redelivery
- DLQ replay
- completeness remediation
- bounded backfill

the result must still converge to:
- one persisted audit fact
- zero silent history mutation
- one clear processing outcome (`inserted` or `deduped`)

---

## 3) Consistency expectations

### 3.1 Eventual consistency is expected
Audit is eventually consistent relative to the originating action.

Expected behavior:
- publish/unpublish/governance/identity action succeeds immediately at the originating truth boundary
- audit entry appears later, bounded by backlog/lag

### 3.2 Audit must never block core flows
Audit lag is acceptable.  
Audit unavailability is not acceptable as a reason to fail:
- Content truth commit
- Identity truth commit
- Authorization truth commit
- other originating module truth commits

Audit must be:
- observable
- recoverable
- replay-safe

### 3.3 No read-your-writes guarantee for audit evidence
Immediately after a business action succeeds, the originating caller is not guaranteed that audit evidence is already queryable.

The architecture guarantees:
- originating truth committed
- auditable event/outbox intent committed where required
- audit consumer will attempt ingestion asynchronously

### 3.4 Timeout ambiguity posture
A timeout or missing acknowledgment in the audit pipeline does **not** prove:
- the upstream action did not happen
- the audit record was not persisted
- the event was not published
- the event will never be retried

Investigations must distinguish:
- business truth ambiguity
- audit ingestion lag
- confirmed persisted audit evidence

### 3.5 No global ordering assumption
Audit does **not** require:
- one strict total global order across all events in the system
- one perfect chronology that can be reconstructed from timestamps alone

**Rule:** stable event identity and durable evidence matter more than a global ordering illusion.

### 3.6 Cause-before-evidence rule
Audit evidence must follow a committed domain cause.

That means:
- business truth happens first
- outbox/event publication forms the causal bridge
- audit evidence arrives later

Audit may lag behind the cause.  
It must not invent evidence ahead of a committed canonical event.

---

## 4) Transaction boundary (V1)

### 4.1 Truth boundary
The Audit transaction boundary stops at the Audit-owned append-only record and its local processing metadata.

Typical truth changes include:
- append a new audit record
- persist local dedupe markers where needed
- update local ingestion/processing metadata
- record local outcome such as inserted/deduped/failed if policy requires it

### 4.2 Audit is never part of the originating business transaction
Audit MUST NOT be included as a required success condition in:
- Content publish/unpublish transactions
- Identity register/verify/reset transactions
- Authorization governance transactions
- any other originating module’s truth commit

The originating module commits its own truth change and outbox intent first.  
Audit reacts after that commit.

### 4.3 Atomic commit set inside Audit
Within the Audit module itself, processing should commit atomically:
- the append-only audit row
- the idempotency/dedupe decision required to prevent duplicates
- any local ingestion metadata required for observability or repair

This local transaction is separate from the originating business transaction.

### 4.4 Outside the transaction
The following MUST NOT be treated as part of a single atomic unit with audit persistence:
- the originating module’s truth commit
- broker delivery across the whole system
- other modules’ downstream side effects
- reporting/dashboard refresh
- external HTTP/API calls
- archival/summarization workflow completion

Distributed atomic commit across business truth + broker + audit store is out of scope in V1.

### 4.5 Transaction duration rule
Audit transactions must be short:
- no waiting on downstream systems
- no long-running enrichment workflow inside one open DB transaction
- no cross-request interactive transaction
- no retry loops that stretch one local transaction indefinitely

Audit should persist the audit fact quickly and defer all non-essential follow-up work.

### 4.6 Concurrency expectations
Audit must assume:
- duplicate event delivery
- retry storms during incidents
- replay/reprocess operations
- concurrent consumers where deployment policy allows it
- delayed deliveries after newer business truth already exists
- overlapping reconciliation/replay runs if remediation is retried

At minimum, the design must prevent:
- duplicate audit rows for the same canonical event
- accidental mutation of previously written audit facts
- contradictory persistence outcomes for the same canonical audit fact
- replay/remediation workflows publishing contradictory derived completeness views

Typical implementation options include:
- durable unique constraints on `AuditEventId / MessageId`
- append-only insert semantics
- deterministic dedupe behavior for duplicate deliveries
- replay-safe consumer logic

### 4.7 No heterogeneous distributed transaction
Audit does **not** attempt one atomic workflow across:
- originating truth store
- RabbitMQ/broker
- Audit store
- dashboards/reporting stores
- external systems

Atomicity stops at:
- the originating module’s truth + Outbox boundary
- and separately at Audit’s own local append-only transaction

---

## 5) Audit immutability and correction model

### 5.1 Append-only rule
Audit history is append-only by default.

Previously written audit facts must not be silently overwritten.

### 5.2 Correction rule
If correction is required by policy:
- append a new corrective record, or
- use an explicit correction model documented separately

Do not silently mutate historical audit rows in place.

### 5.3 Why immutability matters
Append-only audit truth protects:
- investigations
- governance review
- post-incident timeline reconstruction
- replay and evidence integrity

A mutable audit log weakens all of the above.

### 5.4 Timestamps are not mutation authority
Audit timestamps are useful evidence, but they do not justify rewriting older audit facts or deciding that a newer-looking timestamp should replace earlier persisted evidence.

### 5.5 Correction does not erase prior evidence
If an operator or workflow discovers a mapping or classification problem:
- the fix must remain auditable itself
- prior evidence must remain visible or explicitly superseded by policy
- replay/remediation must not silently erase the original record trail

---

## 6) Replication mechanics (consumer posture)

Audit is a pure async consumer:
- it consumes Outbox-published events from other modules (`Content`, `Identity`, `Authorization`, etc.)
- delivery is at-least-once, so duplicates are expected

Consumer requirements:
- idempotent inserts
- retry-safe processing
- append-only persistence
- safe logging (no secrets/tokens/PII)
- durable uniqueness protection for canonical event identity

Audit should not rely on:
- perfect upstream ordering
- perfect clock alignment
- absence of replay
- immediate consumer success

### 6.1 Outbox is the causal boundary
For Audit, the causal rule is:
- business truth happens first at the originating module
- audit evidence is recorded later from committed outbox-backed events

This means:
- Audit evidence is downstream of truth
- delayed audit does not mean the business action was unreal
- replay/remediation must begin from originating truth + Outbox evidence path, not from time assumptions alone

### 6.2 Duplicate delivery vs stale delivery
Audit must distinguish:
- **duplicate delivery**: same canonical event is delivered again
- **stale delivery**: an older business event arrives after newer business truth exists elsewhere

For Audit:
- duplicate delivery must dedupe
- stale delivery is often still recordable if it is a distinct canonical fact
- Audit should not silently reinterpret business chronology beyond the evidence it actually receives

### 6.3 Replay is a normal recovery mechanism
Replay/backfill/reprocess are valid recovery paths for Audit when:
- ingestion lagged
- consumer crashed
- DLQ accumulated
- completeness reconciliation found gaps

Replay must stay:
- bounded
- observable
- append-only safe
- dedupe-safe

---

## 7) Retry, DLQ, replay, and reconciliation posture

### 7.1 Retry strategy
At-least-once delivery is assumed.

Retries must be:
- bounded
- backoff-based
- safe under duplicate delivery
- safe under replay

Typical posture:
- transient errors -> retry with backoff
- permanent errors -> DLQ or Dead state with alerting

### 7.2 DLQ handling
DLQ or Dead-state records must be:
- inspectable
- correlated with `correlationId`
- replayable after fix where policy allows
- visible to operators

### 7.3 Replay safety
Replay/reprocess operations must not create duplicate audit facts for already-persisted canonical events.

Replay should:
- re-attempt missing evidence
- not duplicate already-recorded evidence
- preserve append-only semantics

### 7.4 Reconciliation posture
Audit completeness reconciliation workflows may:
- compare bounded expected-event sets against persisted audit evidence
- produce mismatch reports
- generate replay candidate sets

They must not:
- rewrite canonical audit truth
- treat dashboards or summaries as evidence truth
- silently auto-correct historical evidence without explicit replay/correction policy

### 7.5 Retry-safe design beats exclusive execution assumptions
Audit reliability must not depend on:
- a single audit worker being “the current owner”
- one process being guaranteed current after timeout ambiguity
- naive singleton/leader assumptions

If future ownership-sensitive audit workflows are introduced, they must use authoritative generation/fencing checks rather than local belief.

### 7.6 Rerun safety for derived audit workflows
Archival, summarization, and completeness-report workflows must be safe to rerun on the same bounded input.

This means:
- summaries must be replaceable or explicitly versioned where needed
- partial candidate output must not masquerade as completed report state
- rerun must not corrupt canonical append-only evidence

### 7.7 Full bounded reprocess is acceptable
If a bounded scope of audit evidence is uncertain or known incomplete, a full bounded reprocess is acceptable when safer than fragile selective repair, provided that:
- canonical event identity is preserved
- dedupe remains enforced
- existing evidence is not mutated
- results remain observable

---

## 8) Ordering and completeness posture

### 8.1 Strict global ordering is not required for correctness
Audit is append-only and does not require strict total ordering for correctness.

However:
- event identity must be stable
- timestamps must be recorded consistently in UTC
- correlation identifiers must allow cross-module reconstruction

### 8.2 Timestamps are evidence, not sole authority
For investigations, operators may use:
- `OccurredAt`
- ingestion timestamps
- persisted audit order
- `correlationId / messageId`
- business identifiers

But audit correctness must not assume:
- timestamps from different nodes are perfect causal truth
- sorting by time alone fully reconstructs what happened

**Rule:** timestamps support investigations; they do not replace correlation and authoritative module truth.

### 8.3 Completeness is a governance signal
Audit completeness matters.

Potential gaps must be detectable through:
- backlog growth
- consumer failure rates
- DLQ entries
- replay counts
- missing expected canonical events during investigation
- reconciliation mismatch reports

Audit completeness is therefore both:
- an observability concern
- a governance concern

### 8.4 No global consensus claim for audit chronology
Audit does not claim:
- one infallible global order across all module events
- one perfect cluster-wide timeline

It claims instead:
- durable evidence persistence
- canonical event identity
- append-only integrity
- correlation-assisted reconstruction

### 8.5 Derived completeness views must not outrun evidence truth
If a reconciliation or completeness workflow publishes:
- mismatch reports
- completeness summaries
- repaired coverage views

those outputs remain derived and must not claim stronger truth than persisted evidence records.

---

## 9) Coordination and ownership posture (Audit)

### 9.1 Audit does not require global singleton coordination by default
Ordinary Audit correctness must not depend on:
- one global audit leader
- one process being “the only ingestor”
- startup order deciding current authority
- timeout-only assumptions about which consumer is current

Audit correctness should instead be achieved through:
- canonical event identity
- append-only inserts
- durable dedupe
- replay-safe consumer behavior

### 9.2 If future ownership-sensitive audit workflows are introduced
If a future Audit workflow truly requires one current owner
(for example exclusive backfill of a partition or one-current audit repair owner),
that workflow must define:
- ownership source of truth
- monotonic generation/fencing token
- resource-side rejection of stale owner actions

Naive leader/lock patterns are not acceptable.

### 9.3 Safe non-progress beats unsafe duplicate repair
If ownership is ambiguous for a correctness-sensitive replay/reconciliation/publication workflow, Audit must prefer:
- delayed remediation
- operator retry
- stale-owner rejection
- continued evidence-truth preservation

over unsafe dual replay, unsafe double-publish of derived reports, or contradictory repair mutation.

---

## 10) Observability signals (Audit-specific)

### 10.1 Minimum signals
Minimum signals include:
- consumer success/failure rate
- retry volume and error classification
- DLQ size and oldest DLQ age
- ingestion lag/backlog (queue depth, outbox age)
- dedupe hits / unique-key conflict rate
- replay/reprocess volume
- reconciliation mismatch count
- archival/summarization workflow freshness where applicable
- candidate publication/cutover failures for important derived reports
- ownership-generation mismatch count if future ownership-sensitive workflows are introduced

### 10.2 Logging requirements
Logs should include:
- `correlationId`
- `eventId / messageId`
- `action`
- `resourceType + resourceId`
- processing outcome (`inserted / deduped / failed / replayed`)

Logs must remain:
- privacy-aware
- secret-safe
- usable for investigations

### 10.3 Health layering
Audit observability should distinguish:
- consumer liveness
- queue/backlog health
- dedupe pressure
- replay/remediation activity
- audit completeness risk
- derived-reporting / archival workflow lag

A green process is not enough if:
- backlog is growing
- DLQ is aging
- expected audit facts are not appearing

---

## 11) Canonical mapping fields

Recommended canonical mapping fields include:
- `AuditEventId` (`EventId / MessageId`)
- `ActorUserId`
- `Action`
- `ResourceType + ResourceId`
- `OccurredAt`
- `CorrelationId`

These fields support:
- dedupe
- investigations
- trace reconstruction
- governance review

They do not replace originating module truth where current business state must be confirmed.

### 11.1 Mapping stability rule
Canonical mapping should remain stable across:
- retry
- replay
- backfill
- remediation

A replay of the same canonical event should map to the same canonical audit identity.

---

## 12) Summary

Audit correctness in V1 rests on fifteen rules:

1. Audit owns evidence truth, not originating business truth.  
2. Audit is append-only and must never block the originating truth flow.  
3. Duplicate delivery must not create duplicate audit facts.  
4. Audit immutability is the default; corrections append new facts instead of rewriting old ones.  
5. Replay must fill gaps without duplicating already-recorded evidence.  
6. Timestamps help investigations, but correlation and module truth remain necessary.  
7. Audit lag is acceptable only if it is visible, recoverable, and governable.  
8. No global ordering or distributed transaction is assumed for Audit workflows.  
9. Business truth and audit evidence are linked causally through outbox-backed events, not by timestamp assumptions alone.  
10. Dashboards, summaries, and completeness reports are derived, not evidence truth.  
11. Reconciliation and replay support audit completeness, but do not rewrite canonical evidence.  
12. Important derived audit workflows must be rerun-safe.  
13. Replay, duplicate delivery, and bounded backfill are normal and must remain safe.  
14. Safe non-progress is preferable to unsafe synthetic evidence mutation.  
15. Singleton/ownership semantics are not relied on unless explicitly protected by authoritative generation/fencing rules.