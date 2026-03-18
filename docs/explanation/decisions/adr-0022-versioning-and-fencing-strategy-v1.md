# ADR-0022 — Versioning and Fencing Strategy (V1)

**Status:** Accepted  
**Date:** 2026-03-09  
**Decision owners:** Architecture / Platform  
**Scope:** System-wide (stale-write prevention, ordered state transitions, projection freshness, stale-actor protection)  
**Related:**
- `../architecture/arc42/03-building-blocks-modularity.md`
- `../architecture/arc42/04-runtime-view-v1.md`
- `../architecture/arc42/11-replication-v1.md`
- `../architecture/arc42/12-partitioning-v1.md`
- `../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../architecture/arc42/14-distributed-systems-assumptions-v1.md`
- ADR-0013 (Outbox & delivery semantics)
- ADR-0018 (Transaction boundaries & consistency model)
- ADR-0019 (System model and fault assumptions)
- ADR-0020 (Timeout, retry, and failure detection policy)
- ADR-0021 (Clock, time, and ordering policy)

---

## Context

CommercialNews V1 already assumes:

- partially synchronous runtime conditions
- crash-recovery faults
- process pauses and stale local beliefs are normal
- outbox-based async propagation with at-least-once delivery
- cross-node wall-clock timestamps are not sufficient authority for causality or ordering

These assumptions create several correctness risks:

- admin edit forms may overwrite newer state with stale data
- projection consumers may apply out-of-order events
- a paused or stale worker may wake up and continue acting as if it still owns work
- retries may re-apply state transitions that should be rejected as stale
- consumers may receive older messages after newer state is already materialized

Without a system-wide strategy, teams may fall back to unsafe shortcuts such as:
- `last timestamp wins`
- naive read-modify-write
- “check once then assume authority”
- “caller says it is still current”
- overwrite-without-version-check

CommercialNews needs one explicit decision that defines:

- when versioning is required
- when fencing/generation semantics are required
- what stale-write protection looks like in V1
- how ordered transitions should be enforced
- how projections and owned resources should reject stale actors

---

## Decision

### 1) CommercialNews uses explicit versioning for ordering-sensitive state

When state transitions are ordered or stale overwrite is possible, CommercialNews V1 requires explicit versioning.

Versioning may take one of the following approved forms:

- aggregate version number
- revision number
- rowversion / optimistic concurrency token
- expected-version compare-and-set contract
- monotonic sequence number
- generation number for ownership transfer

The exact mechanism may vary by module and storage technology, but the rule is consistent:

- **ordered or conflict-sensitive state must not be protected by wall-clock timestamps alone**

---

### 2) Versioning is required at truth boundaries where stale writes are plausible

Versioning or equivalent optimistic concurrency control is required for truth writes in cases such as:

- document-style admin edits
- content updates where a stale edit form may overwrite newer data
- SEO metadata updates where a stale operator or delayed command may revert newer values
- Media ordering/primary-selection flows where final state must not be overwritten by stale admin actions
- any write path where the command depends on previously read state and is later committed

The default rule is:

- if a user or process can read state, wait, and later write based on that stale snapshot,
  the write path should have explicit stale-write detection

---

### 3) Per-aggregate versioning is required for ordered async effects

Whenever ordered transitions exist for an aggregate and async consumers act on them,
CommercialNews V1 requires events to carry:

- `AggregateId`
- `Version` (monotonic per aggregate)

Consumers must follow this rule:

- apply only when the incoming version is valid relative to the current materialized state
- if the event is older than current materialized state, reject or ignore it as stale
- if a gap or out-of-order situation is detected and correctness depends on exact order, resync from truth

No global event ordering is assumed.
Ordering is **per aggregate**, not across the whole system.

---

### 4) Projections must reject stale updates

Projection consumers and derived-state writers must not overwrite newer materialized state with older events.

Approved V1 strategies include:

- store `LastAppliedVersion` per aggregate in the projection record
- apply event only if `IncomingVersion > LastAppliedVersion`
- or apply only if `IncomingVersion == ExpectedNextVersion` when strict sequencing is required
- if gaps are detected and strict order matters, rebuild or resync from truth

This applies to:
- future read-model projections
- SEO or derived routing metadata when version-sensitive
- interaction aggregates if they materialize ordered domain state
- any replayable consumer where delayed redelivery could otherwise revert newer state

---

### 5) Fencing/generation semantics are required where ownership can transfer

When only one actor should be allowed to act on a resource at a time, and ownership may transfer,
CommercialNews V1 requires fencing-style protection.

Typical cases:
- singleton or lane-owned background jobs
- scheduled execution ownership
- consumer ownership transfer
- maintenance/rebuild tasks where a former owner may wake up after pause/restart

The required rule is:

- every granted ownership must carry a monotonic generation/fencing token
- every correctness-sensitive write or state transition under that ownership must carry the token/generation
- the resource or authoritative store must reject operations using an older generation than one already observed

This prevents:
- stale actor resumes
- former owner continues acting after transfer
- duplicate or conflicting execution after pause/restart

---

### 6) Resource-side validation is preferred over caller self-belief

CommercialNews V1 explicitly prefers resource-side stale-write rejection over trusting the caller’s local belief.

This means:
- a caller saying “I still own this” is not sufficient
- a caller saying “this is still the latest value” is not sufficient
- the resource or authoritative store must verify:
  - expected version
  - current revision
  - current generation/fencing token
  - or an equivalent authoritative freshness marker

This applies equally to:
- sync truth writes
- async projection updates
- owned job execution states
- workflow transitions that must reject stale actors

---

### 7) Wall-clock timestamps are never the primary freshness authority

CommercialNews V1 disallows using only:
- `UpdatedAt`
- `OccurredAt`
- `ProcessedAt`
- “largest timestamp wins”

as the primary stale-write/freshness check for correctness-critical state.

Timestamps may be stored for:
- audit
- investigation
- human-readable chronology
- retention and reporting

But correctness must be driven by:
- version
- revision
- rowversion
- compare-and-set
- generation/fencing
- explicit state machine rules

This decision is mandatory because:
- clocks skew
- messages delay
- workers pause
- retries replay
- timestamp order is not causal order

---

### 8) State machines remain authoritative for lifecycle legality

Versioning and fencing do not replace domain lifecycle rules.

For truth aggregates with lifecycle transitions, CommercialNews V1 requires:
- lifecycle legality to be enforced by the owning module
- versioning to prevent stale overwrite
- state-machine validation to prevent illegal transitions

Examples:
- Content publish/unpublish/archive rules
- Identity verification/reset state changes
- Authorization mutation workflows where explicit invariants apply

Therefore:
- version protects against stale writers
- state machine protects against illegal transitions
- both may be needed on the same write path

---

### 9) Duplicate delivery and stale delivery are different problems

CommercialNews V1 distinguishes:

#### A) Duplicate delivery
The same event/message arrives again.
Protection:
- message-level dedupe
- idempotency key
- replay-safe handler design

#### B) Stale delivery
An older event arrives after newer state is already applied.
Protection:
- aggregate version/revision checks
- `LastAppliedVersion`
- expected-version logic
- reject/ignore stale updates

Consumers must account for both.
Message dedupe alone is not enough if older but different events may arrive after newer state.

---

### 10) Module-level storage mechanisms may differ, policy remains the same

CommercialNews V1 allows modules to implement the policy using storage-appropriate mechanisms.

Examples of acceptable implementations:

#### SQL truth stores
- `rowversion`
- explicit `Version` column
- `WHERE Version = @ExpectedVersion` update semantics
- unique + compare-and-set constraints where appropriate

#### Mongo/document stores
- version field in document
- conditional update on expected version/revision
- last-applied version in derived document

#### Redis / derived caches
- version in payload for stale-write rejection where needed
- Redis must not be sole authority for correctness-critical ownership decisions

The architecture does not force one universal technical primitive.
It forces one universal **correctness rule**:
- stale writes and stale actors must be rejectable by authoritative state.

---

## Decision summary

CommercialNews V1 adopts the following versioning and fencing strategy:

- explicit versioning is required for ordered or stale-write-sensitive state
- async ordered transitions use `AggregateId + Version`
- projections must reject stale events/updates using stored version state
- ownership-transfer scenarios require fencing/generation semantics
- resource-side validation is preferred over caller self-confidence
- wall-clock timestamps are not valid freshness authority for correctness-critical state
- state machines and versioning complement each other, not replace each other
- duplicate-delivery protection and stale-delivery protection must both be considered

---

## Consequences

### Positive

- Prevents stale overwrite in admin and system workflows
- Prevents delayed consumers from reverting newer derived state
- Protects against paused/restarted former owners acting on stale authority
- Makes projection behavior safer under at-least-once delivery and out-of-order arrival
- Aligns system behavior with chapter-8 assumptions about pauses, clocks, and incomplete knowledge
- Encourages explicit, reviewable correctness mechanisms instead of accidental timestamp-based behavior

### Negative / Trade-offs

- Adds version/generation fields and checks to several flows
- Requires more careful module documentation and handler logic
- Some flows need explicit conflict responses or operator-visible retry/reload behavior
- Strict sequencing can require resync/rebuild logic when gaps are observed
- Developers lose the convenience of naive overwrite semantics

---

## Alternatives considered

### 1) Use `UpdatedAt` / latest timestamp as freshness authority
- Pros: easy to implement.
- Cons: unsafe under skew, pause, message delay, and non-causal timestamp order.

Rejected.

### 2) Rely on message dedupe only
- Pros: simpler consumer logic.
- Cons: protects against duplicates of the same message, but not stale older messages arriving after newer state.

Rejected.

### 3) Trust caller-side lease or ownership check only
- Pros: simpler code paths.
- Cons: unsafe when former owners wake up after pause or network ambiguity.

Rejected.

### 4) No optimistic concurrency for admin/document-style edits
- Pros: fewer conflict responses to handle.
- Cons: stale overwrite becomes easy and often silent.

Rejected.

### 5) Global total ordering for all events
- Pros: simple mental model if it existed.
- Cons: unnecessary, expensive, and contrary to CommercialNews V1 architecture; per-aggregate ordering is enough and more realistic.

Rejected.

---

## Implementation notes (V1)

- Module docs must identify which entities/flows are version-sensitive.
- Any module-level `06-idempotency-consistency.md` should specify:
  - freshness mechanism used
  - whether version is per aggregate, per document, or per ownership grant
  - stale-write rejection behavior
  - gap handling / resync behavior where relevant
- Consumers should store enough metadata to reject stale materialization updates, typically:
  - `LastAppliedVersion`
  - or an equivalent monotonic freshness marker
- APIs that support user/admin edits should define conflict handling behavior clearly:
  - reload required
  - explicit conflict response
  - retry only after refresh of truth state
- Ownership-sensitive worker flows should store and verify generation/fencing values at the authoritative resource boundary

---

## Recommended application by module (V1)

### Content
Use explicit version/revision or equivalent optimistic concurrency for:
- draft edit/update
- publish/unpublish transitions where stale admin actions are possible
- any workflow where old edit state could overwrite newer content truth

### SEO
Use version-aware writes where async SEO reactions or admin edits could race.
Do not resolve truth-sensitive SEO conflicts by timestamp alone.

### Media
Protect reorder/set-primary/final-state mutation paths against stale overwrites.

### Identity
Use state-machine validation plus authoritative truth checks for security-state transitions.
Do not use timestamp-based overwrite rules for security truth.

### Authorization
Use explicit invariants and, where stale admin state is plausible, version/concurrency protection for governance changes.

### Audit
Message dedupe is primary; append-only truth usually reduces stale-overwrite risk.
Where materialized audit views exist, stale update protection still applies.

### Notifications
Delivery side effects require message/business idempotency first.
If delivery-state materializations become ordered and replayable, version-aware update rules must be applied.

### Reading / Projections
Projection records must track last-applied freshness markers and reject stale application.

---

## Operational guidance (V1)

### Recommended signals
- version-conflict / optimistic-concurrency rejection count
- stale-event rejection count
- projection resync/rebuild count
- ownership-generation mismatch count
- dedupe hit count vs stale-event reject count
- gap-detected / resync-triggered count for ordered consumers

### Investigation posture
When ordering incidents occur:
- check version/revision history first
- do not rely on timestamps alone
- verify whether a stale actor or delayed consumer attempted a lower-generation/lower-version write
- verify whether state machine legality and versioning both executed as intended

---

## Follow-ups

- Update module docs, especially:
  - `06-idempotency-consistency.md`
  - `08-dependencies-and-ownership.md`
  - `03-runtime-flows.md`
- Add module-specific notes for:
  - aggregate version fields
  - optimistic concurrency semantics
  - projection freshness fields
  - ownership generation/fencing checks
- If V2 introduces dedicated projection components, add explicit checkpoint + freshness ADRs where needed