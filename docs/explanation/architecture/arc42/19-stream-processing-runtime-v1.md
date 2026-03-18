# 19) Stream Processing Runtime (V1)

This section describes the **runtime shape** of stream-style processing in CommercialNews V1.

It explains how committed truth changes move through:

* outbox
* Background Worker
* broker delivery
* idempotent consumers
* derived-state updates
* repair / rebuild / reconciliation paths

This section focuses on **runtime behavior**, not on API contracts and not on per-module endpoint details.

### Related

* Runtime view: `04-runtime-view-v1.md`
* Architecture style: `09-architecture-style.md`
* Replication: `11-replication-v1.md`
* Transactions & consistency: `13-transactions-and-consistency-v1.md`
* Distributed systems assumptions: `14-distributed-systems-assumptions-v1.md`
* Consistency / ordering / consensus: `15-consistency-ordering-and-consensus-v1.md`
* Batch / derived data: `16-batch-processing-and-derived-data-v1.md`
* Dataflow / batch workflows: `17-dataflow-and-batch-workflows-v1.md`
* Stream processing & derived state: `18-stream-processing-and-derived-state-v1.md`
* ADR-0013 (Outbox & delivery semantics)
* ADR-0015 (Cache policy & invalidation)
* ADR-0021 (Clock, time, and ordering policy)
* ADR-0022 (Versioning and fencing strategy)
* ADR-0024 (Distributed coordination and singleton work policy)

---

## 19.1 Purpose

CommercialNews V1 has three runtime lanes:

* **Lane A** — synchronous request/response for truth-sensitive flows
* **Lane B** — asynchronous event-driven side effects and derived-state maintenance
* **Lane C** — bounded batch / rebuild / reconciliation workflows

This document focuses on **Lane B** and its connection to **Lane C**.

The goals are:

* keep truth commits independent from downstream completion
* make async propagation observable and recoverable
* preserve correctness under retry, duplicate delivery, lag, and worker restart
* provide bounded recovery paths when derived systems drift or fall behind

---

## 19.2 Canonical V1 stream runtime pattern

The canonical V1 runtime path is:

1. API handler validates command and authorization
2. owner module writes truth change
3. owner module writes outbox record in the same transaction
4. transaction commits
5. API returns success based on truth commit only
6. Background Worker polls pending outbox records
7. Worker publishes messages to RabbitMQ
8. broker delivers at least once
9. idempotent consumers apply side effects or update derived systems
10. observability tracks lag, retries, dedupe, failures, and backlog
11. bounded rebuild/reconciliation workflows repair drift if needed

### 19.2.1 Runtime contract

This pattern guarantees:

* truth change and outbox intent are atomic
* side effects are post-commit
* duplicate delivery is tolerated
* lag is visible
* recovery does not require distributed commit

It does **not** guarantee:

* all consumers are caught up before API success
* cache invalidation is complete before response returns
* notification/audit/search/projection work finishes synchronously
* global ordering across all event families

---

## 19.3 Runtime components involved

### 19.3.1 Public API component

Responsible for:

* validating commands
* enforcing authz/authn
* applying truth changes
* writing outbox records atomically
* serving truth-sensitive reads
* falling back safely when derived systems are stale

### 19.3.2 Background Worker component

Responsible for:

* polling outbox
* publishing events to the broker
* consuming events
* running retry-safe side effects
* maintaining selected derived outputs
* executing bounded rebuild / replay / reconciliation workflows

### 19.3.3 RabbitMQ broker

Responsible for:

* buffering messages
* decoupling producer and consumer timing
* enabling retry / redelivery behavior
* isolating bursty downstream work from truth commit flows

RabbitMQ in V1 is delivery-oriented, not the system-wide permanent history source.

### 19.3.4 Truth stores

Responsible for authoritative state.

Examples:

* Content truth
* Identity truth
* Authorization truth
* SEO truth-owned policy data

### 19.3.5 Derived stores

Responsible for acceleration, enrichment, summary, or serving support.

Examples:

* Redis
* projections/read models
* search-serving artifacts
* counters and summaries
* notification delivery views
* audit reporting summaries

---

## 19.4 Canonical event envelope at runtime

Where stream semantics matter, emitted events should carry at least:

* `MessageId`
* `EventType`
* `AggregateType`
* `AggregateId`
* `Version` (when ordering matters)
* `OccurredAt`
* `CorrelationId`
* `Payload`

Runtime logs and metrics should also associate:

* publish attempt count
* consumer attempt count
* handler name
* queue/backlog state
* outcome status
* dedupe result where applicable

---

## 19.5 Content-derived runtime pipelines

Content is one of the main truth producers in CommercialNews V1.

### 19.5.1 Publish pipeline

#### Sync path
1. Admin requests publish
2. Authorization checks permission
3. Content validates lifecycle transition
4. Content writes:
   * article truth change
   * lifecycle/history row if applicable
   * outbox event `ArticlePublished`
5. transaction commits
6. API returns success

#### Async path
7. Worker publishes `ArticlePublished`
8. downstream consumers process:
   * Audit appends publish event
   * Notifications may send subscriber notifications
   * SEO updates derived serving artifacts or routing-supporting state
   * future search/index projections update if enabled

#### Runtime guarantees
* publication truth exists before any downstream effect matters
* lagging SEO/audit/notification consumers do not roll back publication truth
* duplicate deliveries must not create duplicate harmful effects

#### Runtime risks
* outbox backlog growth
* duplicate publish delivery
* stale SEO-serving artifacts
* delayed notifications
* delayed audit ingestion

#### Runtime-safe posture
* truth-backed visibility remains authoritative
* public correctness must not depend on SEO consumer freshness alone
* consumers enforce idempotency using `MessageId` and/or `(AggregateId, Version)`

---

### 19.5.2 Unpublish pipeline

#### Sync path
1. Admin requests unpublish with reason
2. Authorization checks permission
3. Content writes:
   * visibility/lifecycle truth change
   * reason/history data
   * outbox event `ArticleUnpublished`
4. transaction commits
5. API returns success

#### Async path
6. Worker publishes `ArticleUnpublished`
7. downstream consumers process:
   * Audit appends unpublish event + reason
   * SEO removes/deprioritizes derived serving state as policy defines
   * future read/search projections adjust visibility

#### Runtime guarantees
* unpublished content must not reappear because a consumer lags
* public read path enforces visibility using truth if derived state is uncertain

#### Runtime-safe posture
* safe fallback to truth over stale routing/projection confidence
* reconciliation jobs may later repair delayed serving artifacts

---

## 19.6 Identity/Auth-derived runtime pipelines

Identity and auth flows are truth-sensitive and security-critical.

### 19.6.1 Registration and verification email pipeline

#### Sync path
1. User submits registration
2. Identity validates input
3. Identity writes:
   * new account in unverified state
   * verification token/request state
   * outbox event `UserRegistered` or `VerificationRequested`
4. transaction commits
5. API returns success

#### Async path
6. Worker publishes the event
7. Notifications sends verification email idempotently

#### Verification path
8. User clicks verification link
9. Identity validates token and writes:
   * verified truth state
   * token consumption/update state
   * outbox event `UserEmailVerified`
10. transaction commits
11. API returns verified success

#### Downstream effects
12. audit or notification consumers may record/notify as needed

#### Runtime guarantees
* verification state is authoritative in truth store
* email delivery lag does not invalidate registration truth
* after successful verification, self-state reads must be truth-strong

---

### 19.6.2 Forgot password / reset pipeline

#### Sync request path
1. User requests reset
2. Identity writes:
   * reset request/token state
   * outbox event `PasswordResetRequested`
3. transaction commits
4. API returns non-leaking response

#### Async path
5. Notifications sends reset email idempotently

#### Reset execution path
6. User submits reset token + new password
7. Identity validates token and writes:
   * credential/security truth change
   * reset token consumption/update state
   * outbox event `UserPasswordChanged`
8. transaction commits
9. API returns success

#### Runtime guarantees
* password truth change is authoritative immediately on commit
* email delivery lag does not block or roll back password reset truth
* self-state and auth flows after reset must observe current truth

#### Runtime risks
* duplicate email sends
* token replay attempts
* worker lag on delivery notifications
* cleanup/replay backlog for reset artifacts

---

## 19.7 Authorization-derived runtime pipelines

Authorization changes are governance-sensitive and require immediate truth correctness.

### 19.7.1 Role/permission change pipeline

#### Sync path
1. Admin requests role/permission change
2. Authorization validates permission to modify governance state
3. Authorization writes:
   * truth change in role/user-role/role-permission tables
   * outbox event such as `RoleAssigned` or `PermissionGranted`
4. transaction commits
5. API returns success

#### Async path
6. Worker publishes governance event
7. downstream consumers process:
   * Audit appends governance event
   * future effective-permission projections/caches update
   * future admin reporting or summaries update

#### Runtime guarantees
* governance truth is authoritative immediately after commit
* audit lag is acceptable only if visible and recoverable
* post-change governance reads must be truth-strong

#### Runtime-safe posture
* cache/projection lag must not misrepresent current governance truth for critical admin reads
* reconciliation jobs may rebuild effective-permission projections if needed

---

## 19.8 Reading and SEO runtime behavior

Reading Experience and SEO frequently operate on derived acceleration, but correctness still depends on truth.

### 19.8.1 Slug resolution and public article reads

#### Typical read path
1. Public request resolves slug
2. SEO cache/serving artifact may answer quickly
3. Reading fetches content detail and enrichments
4. response returns public article view

#### Runtime correctness rule
If derived routing or serving state is:

* stale
* missing
* suspected inconsistent

the runtime must fall back to truth-safe checks before returning an unsafe result.

This means:

* drafts/unpublished content must not leak
* stale derived enrichments may be tolerated if policy allows
* visibility correctness belongs to truth

### 19.8.2 Reactive SEO/update runtime

Content truth changes may trigger:

* SEO artifact refresh
* cache invalidation
* future search projection updates

These updates are async.
Their lag is tolerated only as long as truth fallback preserves correctness.

### 19.8.3 Runtime-safe posture
* read path remains non-blocking
* correctness beats cache confidence
* fallback-to-truth is valid degraded behavior
* stale derived serving state is observable, not invisible

---

## 19.9 Interaction runtime pipelines

Interaction is the most stream-shaped module in V1.

### 19.9.1 View signal ingestion

#### Read path
1. Public article is rendered
2. interaction signal is emitted asynchronously
3. response is not blocked by interaction processing

#### Async path
4. Worker or consumer accepts interaction signal
5. raw signal is stored or queued according to module policy
6. downstream aggregation updates counters or summaries eventually

#### Runtime guarantees
* reading latency is protected
* duplicate view signals are tolerated safely according to interaction policy
* counters may lag

### 19.9.2 Counter/summarization pipeline

Input may include:

* views
* likes
* comments
* future engagement events

Output may include:

* article counters
* hourly/daily summaries
* trending inputs
* future engagement summaries

Runtime posture:

* counters are derived, not truth
* replay/recompute is acceptable where safer than fragile incremental repair
* event-time semantics matter if windows/trending are meaningful

### 19.9.3 Future analytics/security runtime hooks

Potential future stream-style logic includes:

* trending windows
* bot/spam detection
* suspicious login/interaction patterns
* saved-query/content matching

These remain V2+ hooks unless explicitly adopted.

---

## 19.10 Notification runtime behavior

Notifications are pure side-effect pipelines from the standpoint of truth correctness.

### 19.10.1 Notification send pipeline

1. truth event commits with outbox
2. Worker publishes notification-triggering event
3. Notifications consumer resolves recipient/template/payload
4. email send is attempted
5. delivery state/log is recorded
6. retries occur on transient failure
7. dedupe prevents duplicate harmful delivery

### 19.10.2 Runtime guarantees
* email send is not part of truth transaction
* duplicate sends must be prevented by delivery idempotency
* delivery status may lag
* notification backlog is an operational concern, not a truth rollback condition

### 19.10.3 Runtime-safe posture
* delivery log or business-key dedupe is required
* DLQ/remediation must exist for poison cases
* visibility of failure/backlog is mandatory

---

## 19.11 Audit runtime behavior

Audit ingestion is append-only and asynchronous in normal operation.

### 19.11.1 Audit append pipeline

1. originating module commits truth + outbox
2. Worker publishes event
3. Audit consumer appends durable audit record idempotently

### 19.11.2 Runtime guarantees
* origin truth does not wait for audit completion
* audit lag is tolerated only if observable and repairable
* stable audit event identity must support dedupe and replay-safe append

### 19.11.3 Runtime-safe posture
* audit backlog and age are first-class metrics
* repair/replay workflows may restore missing audit-derived views
* sensitive governance actions must remain traceable after recovery

---

## 19.12 Worker failure and duplicate-delivery runtime paths

Fault tolerance is a normal runtime concern, not an exception.

### 19.12.1 Outbox publish failure

1. truth commit succeeds
2. Worker attempts publish
3. publish fails
4. outbox row remains pending or moves to retry state
5. retries continue according to policy

Effect:

* API success remains valid
* derived systems lag
* backlog metrics increase

### 19.12.2 Consumer crash before completion

1. broker delivers message
2. consumer starts processing
3. consumer crashes before completion/ack
4. broker redelivers later
5. consumer processes again

Effect:

* duplicate application is possible
* idempotency is mandatory

### 19.12.3 Duplicate publish after Worker restart

1. Worker publishes message
2. Worker crashes before marking publish progress durably enough
3. on restart, message is published again

Effect:

* downstream consumers may see same event more than once
* stable `MessageId` or `(AggregateId, Version)` is required

### 19.12.4 Out-of-order delivery

If two messages for the same aggregate are delayed differently, a consumer may see:

* newer version first
* older version later

Effect:

* version-aware handlers must reject or resync stale/out-of-order state
* global ordering is not assumed

---

## 19.13 Idempotent runtime patterns required in V1

### 19.13.1 Message-level dedupe
Consumers should be able to identify repeated events using:

* `MessageId`
* `EventId`
* durable processed-message tracking where required

### 19.13.2 Aggregate version checks
For lifecycle-sensitive entities, consumers should prefer:

* `(AggregateId, Version)`
* monotonic apply rules
* reject/reload behavior when stale or out-of-order events arrive

### 19.13.3 Upsert-over-blind-append where possible
Projection maintenance should prefer:

* idempotent upsert
* replace-by-version
* set-based updates

over blind re-append or non-deduped mutation.

### 19.13.4 External side-effect delivery logs
For external effects such as email/webhook-like delivery, the runtime should use:

* delivery keys
* unique send records
* retry-safe status transitions

---

## 19.14 Time and lag in runtime behavior

### 19.14.1 Event time vs processing time
Runtime pipelines may observe a large difference between:

* when the event occurred
* when the consumer handled it

This matters especially for:

* interaction windows
* analytics/trending
* security pattern detection
* lag/freshness dashboards

### 19.14.2 Runtime rule
Business semantics should prefer event time when correctness depends on when the action actually happened.

Operational pipeline health should use processing time where appropriate.

### 19.14.3 Late-arrival posture in V1
For V1 stream-style analytics or aggregation pipelines:

* late-event policy must be explicit
* hidden implicit semantics are not allowed
* simple bounded policies are preferred over overly complex correction machinery

---

## 19.15 Runtime joins in V1

CommercialNews V1 may use join-like runtime behavior in selected workflows.

### 19.15.1 Stream-table enrichment
Likely V1/near-V1 use:

* interaction signal + article metadata
* auth event + user/role status
* notification event + preference/routing data

Runtime rule:

* prefer local or maintained lookup state for hot paths over repeated remote truth lookups where safe
* if correctness depends on time-specific state, the “as-of” semantics must be explicit

### 19.15.2 Table-table maintained views
Likely V1/near-V1 use:

* article + SEO + media → public-serving view
* user + role + permission → effective-permission projection
* content + mappings → listing/facet summaries

Runtime rule:

* these are maintained derived outputs
* rebuild path must exist
* stale overwrite must be prevented by version-aware logic where relevant

### 19.15.3 Stream-stream correlation
Mostly future-facing in V1, but relevant for:

* notification sent → open
* search → click
* suspicious auth/interaction pattern detection

Runtime rule:

* window must be explicit
* unmatched cases must be defined
* replay semantics must not be hand-waved

---

## 19.16 Runtime observability points

Stream-runtime health must be observable at each stage.

### 19.16.1 Outbox stage
Track:

* pending count
* oldest pending age
* publish attempts
* publish failures
* retry count
* dead/outbox terminal failures if used

### 19.16.2 Broker stage
Track:

* queue depth
* ready vs unacked counts
* message age if available
* DLQ depth/age where enabled

### 19.16.3 Consumer stage
Track:

* handler success/failure rate
* retry count
* handler latency
* dedupe hits
* stale-version rejections
* poison-message/DLQ routing

### 19.16.4 Derived-store stage
Track:

* projection freshness lag
* fallback-to-truth rate
* mismatch/reconciliation counts
* rebuild backlog and age
* candidate publication/cutover success/failure for bounded workflows

### 19.16.5 Truth-vs-derived distinction
Dashboards and alerts must separate:

* truth-path success
* derived-path lag/backlog
* degraded but safe fallback behavior

A system may be truth-correct but operationally degraded due to derived lag.

---

## 19.17 Batch-assisted repair and recovery runtime

When continuous propagation is delayed or corrupted, bounded workflows recover health.

### 19.17.1 Typical repair triggers

Examples:

* outbox backlog after outage
* stale SEO-serving artifacts
* projection mismatch with truth
* lagging audit/reporting views
* interaction counter drift
* notification delivery gaps

### 19.17.2 Repair workflow model

1. define bounded candidate input
2. derive or compare against truth
3. generate repair candidate
4. validate
5. apply repair or cutover safely
6. record observability outcome

### 19.17.3 Runtime rule
Repair workflows support derived-state health.
They do not redefine truth ownership or silently rewrite authoritative business history.

---

## 19.18 Runtime non-goals in V1

CommercialNews V1 does not require the runtime to provide:

* synchronous end-to-end completion across all side effects
* global total ordering
* distributed commit across DB + broker + cache + external systems
* universal event replay from broker history alone
* hidden correction of every late event without policy
* heavyweight workflow-engine orchestration as a baseline dependency

---

## 19.19 Runtime summary (V1)

CommercialNews V1 runtime for stream-style processing is:

* **truth commit first**
* **outbox written atomically**
* **Worker publishes asynchronously**
* **broker delivers at least once**
* **consumers are idempotent**
* **derived systems may lag**
* **fallback to truth preserves correctness**
* **batch workflows repair what continuous propagation cannot finish cleanly**

This runtime posture allows CommercialNews to:

* keep user/admin truth flows short and authoritative
* isolate bursty and failure-prone side effects
* tolerate duplicates, retries, and lag safely
* recover derived-state health without distributed transactions
* evolve toward richer projections and analytics later without losing truth ownership discipline