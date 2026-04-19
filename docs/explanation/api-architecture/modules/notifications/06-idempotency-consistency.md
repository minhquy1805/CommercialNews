# Notifications — Idempotency & Consistency (V1)

This document defines Notifications-specific idempotency, delivery semantics, replay safety, provider ambiguity handling, ordering posture, replication-lag behavior, and bounded workflow safety.

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
- ADR-0015 (Redis cache policy)
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

## 0) Role of Notifications in the system

Notifications is a **side-effect execution module**.

It does not decide whether a domain action is valid.
It reacts after another module has already committed truth.

Examples:
- Identity truth decides that verification/reset mail should be triggered
- Content truth decides that article-publish notification is allowed
- Notifications executes the send attempt and records delivery state

Therefore:
- Notifications is not part of the originating truth transaction
- delivery success is not part of the originating business success condition
- delivery state is operational truth for investigation, not business truth for the originating workflow

---

## 1) Truth vs derived

### 1.1 Notifications truth (delivery auditability)
Notifications owns delivery records and operational delivery truth, such as:
- email delivery log
- delivery status
- attempt count
- send timestamps
- last error classification
- dead/DLQ-equivalent terminal state
- optional templates registry if modeled in Notifications

This truth answers questions such as:
- did Notifications attempt this intent?
- how many times?
- what was the last known provider outcome?
- is the intent still pending, failed, sent, or dead?

### 1.2 Originating truth belongs elsewhere
The truth for “whether an email should be sent” belongs to the originating domain:
- Identity
- Content
- other owning modules as introduced

Notifications does not own:
- account verification truth
- password reset token truth
- article publish truth
- governance truth

It only executes the side effect after the originating truth has committed.

### 1.3 Derived / supplementary guards
Supplementary mechanisms may include:
- Redis dedupe keys with TTL
- metrics/aggregates
- dashboards
- derived delivery views
- batch-generated summaries
- replay candidate sets

Durable dedupe is preferred when duplicates are harmful.
For email, duplicates are harmful by default.

### 1.4 Consistency class for Notifications
Notifications intentionally uses multiple consistency classes:

#### Strong truth-backed consistency
Required for:
- durable delivery-state truth
- durable dedupe outcome for a canonical email intent
- append/update correctness of delivery records
- monotonic-enough protection against stale or contradictory terminal-state overwrites

#### Ordered / causality-sensitive consistency
Required where:
- a delivery attempt must not outrun prerequisite domain truth
- stale retries must not overwrite newer delivery state
- replay must distinguish duplicate intent from legitimate new intent
- delivery orchestration or materialization uses explicit freshness/version markers
- a batch resend/remediation workflow must not publish stale or duplicate resend candidates over fresher state

#### Eventual consistency
Accepted for:
- actual provider delivery relative to originating business success
- dashboards and delivery summaries
- queue/backlog convergence
- retry-driven eventual delivery or visible terminal failure
- replay/remediation convergence

---

## 2) Delivery semantics (V1)

Notifications is:
- event-driven
- at-least-once tolerant
- eventually consistent with respect to the originating flow

This means:
- the domain flow returns success/accepted first
- email delivery occurs later and may lag
- retries and duplicate deliveries are normal
- provider acknowledgments may be ambiguous

**Non-negotiable rule:** repeated processing of the same logical email intent MUST NOT send duplicate emails.

### 2.1 What success means
For the originating domain flow:
- success means truth committed (and outbox committed where required)

For Notifications:
- success means the delivery pipeline eventually converges to a visible state such as:
  - sent
  - failed and retryable
  - dead / terminal failure
  - explicitly suppressed by dedupe

### 2.2 No immediate delivery guarantee
CommercialNews V1 does not promise:
- immediate email delivery
- synchronous provider completion before API success
- read-your-writes for delivery state immediately after originating command success

The guarantee is:
- eventual execution or visible terminal failure state

### 2.3 No global ordering assumption
Notifications does **not** assume:
- one global total order across all email intents
- one per-user strict ordering for all notifications
- one cluster-wide authoritative sequence of provider calls

**Rule:** ordering is only required where a specific workflow depends on it; otherwise dedupe and safe state convergence matter more than strict order.

### 2.4 Stream-style reality
Notification processing must assume:
- duplicate event delivery
- replay after crash/restart
- worker redelivery after timeout
- late processing after backlog recovery
- overlapping remediation or replay attempts

These are normal operational cases, not edge-case accidents.

---

## 3) Idempotency & dedupe strategy (mandatory)

### 3.1 Two-layer idempotency
Notifications must implement both:

1. **Message-level dedupe**  
   Use `MessageId / EventId` from the event envelope

2. **Business-level dedupe**  
   Use a stable identity for the email intent

Reason:
- the same event may be redelivered
- some workflows may legitimately create a new email intent later
- message-level dedupe alone is not enough
- business-level dedupe alone is not enough

### 3.2 Canonical business dedupe key (V1)

Each email type must define exactly one canonical business dedupe key.

This key is the authoritative identity of one logical email intent for duplicate-prevention purposes.

Requirements:

- the canonical key must be stable for the same logical intent
- the canonical key must change for a legitimately new intent
- the canonical key must be documented per notification type
- the canonical key must be enforceable at the durable truth boundary
- duplicate-send prevention must rely on this canonical key, not on worker-local memory or broker behavior

**Rule:** for each email type, the system must define one official canonical business dedupe key and use it consistently across producer, consumer, replay, and remediation flows.

### 3.2.1 Canonical dedupe key registry (V1 baseline)

The following canonical business dedupe keys are recommended as the V1 contract baseline.

#### Verification email

Canonical key:

`(TemplateKey = "verify-email", RecipientUserId, VerificationTokenId)`

Rules:

- the same verification token must map to the same logical email intent
- replay/redelivery for the same token must not send again
- a resend that generates a new verification token is a new logical intent and may send again

#### Reset password email

Canonical key:

`(TemplateKey = "reset-password", RecipientUserId, ResetTokenId)`

Rules:

- the same reset token must map to the same logical email intent
- duplicate processing for the same reset token must not send again
- a newly issued reset token creates a new logical intent and may send again

#### New article notification (if enabled)

Canonical key:

`(TemplateKey = "new-article", RecipientUserId, ArticleId, PublishedVersion)`

If `PublishedVersion` is not modeled explicitly, policy may use:

`(TemplateKey = "new-article", RecipientUserId, ArticleId, PublishedAt)`

Rules:

- one published article version should map to one logical notification intent per recipient
- replay for the same article publication intent must not send again
- a later republish or a new published version may create a new logical intent if product policy allows it

#### Contract rule

If a notification type is added in the future, it must define:

- its canonical business dedupe key
- what counts as duplicate replay
- what counts as legitimate retrigger/new intent

No notification type may ship without this being documented.

### 3.3 Storage for dedupe

Preferred (durable):

- SQL uniqueness on `EmailDelivery.MessageKey` or equivalent business dedupe key

Allowed (supplementary only):

- Redis dedupe set with TTL to reduce DB contention or hot retries

**Rule:** Redis-only dedupe is not sufficient protection for harmful duplicate emails.

### 3.3 Storage for dedupe
Preferred (durable):
- SQL uniqueness on `EmailDelivery.MessageKey` or equivalent business dedupe key

Allowed (supplementary only):
- Redis dedupe set with TTL to reduce DB contention or hot retries

**Rule:** Redis-only dedupe is not sufficient protection for harmful duplicate emails.

### 3.4 Duplicate delivery vs valid retrigger
Notifications must distinguish:

#### A) Duplicate processing of the same intent
Must not send again.

#### B) A legitimately new intent
May send again.

### 3.4.1 Dedupe-hit behavior (mandatory)

When duplicate processing of the same canonical intent is detected, the system must behave deterministically.

#### Required behavior

A dedupe hit must:

- prevent a duplicate visible send
- preserve canonical delivery truth
- remain safe under replay, retry, and redelivery
- produce operator/audit visibility where policy requires it

#### Default V1 behavior

For V1, the recommended behavior is:

- do **not** call the provider again
- do **not** create a second active delivery intent for the same canonical business key
- do **not** move the workflow backward from a newer terminal state
- return or record a deterministic duplicate-handled outcome

#### State behavior

If the canonical intent is already in a terminal or authoritative state such as:

- `Sent`
- `Dead`
- `Suppressed`
- `Deduped`
- another policy-defined terminal state

then duplicate processing must not overwrite that state with an older or weaker interpretation.

If the implementation uses an explicit duplicate state, it may record:

- `Suppressed`
- `Deduped`

Otherwise, it may keep the existing canonical state unchanged and emit duplicate-handling metadata separately.

#### Attempt-record behavior

The implementation must define one consistent rule for duplicate hits:

Option A — no new attempt row  
- keep the canonical delivery record unchanged
- increment or record duplicate-hit metrics/logs only

Option B — duplicate-observation attempt row  
- record an observational attempt or audit event
- mark it explicitly as duplicate-suppressed / provider-not-called
- do not treat it as a real send attempt

**Rule:** duplicate observation must never be counted as a successful new send attempt.

#### Metrics and audit behavior

A dedupe hit should produce visibility through some combination of:

- duplicate-hit counter
- structured log event
- audit/operational event
- remediation/reporting signal

This is important for:

- replay diagnosis
- retry-storm detection
- queue redelivery analysis
- operational confidence

#### Safety rule

A duplicate hit is not an error by default.  
It is a normal distributed-systems outcome that must converge safely.

#### Contract rule

For the same notification type and the same canonical business dedupe key, the implementation must not mix inconsistent behaviors such as:

- sometimes resend
- sometimes suppress
- sometimes create a second live intent

unless the policy explicitly distinguishes those cases.

Example:
- resend verification should create a **new verification token / new intent**
- old intent remains dedupe-protected
- new intent must not be blocked by old dedupe state

### 3.5 Idempotency is preferred over singleton assumptions
Notification correctness must not depend on:
- only one worker instance running
- one process being “the current sender”
- startup order
- local ownership belief
- one replay/remediation worker being assumed exclusive without explicit protection

Notifications should instead rely on:
- canonical message identity
- canonical business intent identity
- durable dedupe
- safe delivery-state transitions
- replay-safe processing

### 3.6 Dedupe must survive replay
If the same message is replayed after:
- worker restart
- queue redelivery
- remediation workflow rerun
- backlog recovery

dedupe protection must still prevent accidental re-send of the same canonical intent.

---

## 4) Consistency expectations

### 4.1 Eventual consistency is expected
Email sending is eventually consistent:
- domain flow returns success/accepted first
- email arrives later
- delivery logs may lag under backlog or provider degradation

### 4.2 No read-your-writes guarantee for delivery
Identity/Content/admin flows do not promise immediate “email already sent” visibility.

They promise:
- originating truth committed
- notification intent enqueued or outboxed
- delivery will be attempted asynchronously

### 4.3 Timeout ambiguity is real
A provider timeout does **not** prove:
- the email was not sent
- the provider did not accept it
- the request had no external effect

This is a core design rule.

Therefore:
- retries must assume duplicate-send risk
- dedupe/business intent identity is mandatory
- final state transitions must be careful not to treat timeout as proof of “no-op”

### 4.4 Operational delivery truth is separate from provider certainty
Notifications can record:
- sent
- failed
- pending retry
- dead
- ambiguous provider outcome if modeled

But it should not overclaim certainty where the provider acknowledgment was ambiguous.

If ambiguity is tracked explicitly, it should remain visible for investigation and reconciliation.

### 4.5 Cause-before-effect rule
A notification must not be sent ahead of its required domain cause.

Examples:
- verification email requires a valid verification intent
- reset email requires a valid reset intent
- publish email requires publish truth or publish intent according to policy

Notifications may lag after the cause.
They must not outrun it.

### 4.6 Latest visible state must win over stale retry belief
If a stale worker or remediation job believes an email is still retryable, but canonical delivery state now says:
- `Sent`
- `Dead`
- `Suppressed`
- or any newer terminal state

then stale processing must be rejected or ignored.

---

## 5) Transaction boundary (V1)

### 5.1 Truth boundary
The Notifications transaction boundary stops at Notifications-owned delivery state.

Typical truth changes include:
- create/update delivery records
- increment attempt counts
- record provider response classification
- mark delivery as sent / failed / dead / suppressed
- persist last error and timestamps
- persist durable dedupe markers when policy requires them

### 5.2 Notifications is never part of the originating domain truth transaction
Notifications MUST NOT be included as a required success condition in:
- Identity register/verify/reset transactions
- Content publish/unpublish transactions
- Authorization governance transactions
- any other module’s truth commit

The originating module decides “this intent exists.”  
Notifications reacts after that decision is already committed.

### 5.3 Atomic commit set inside Notifications
Within Notifications itself, a delivery-processing step should commit atomically:
- delivery state mutation
- attempt/error metadata
- durable dedupe state if stored locally
- any local delivery log mutation required for auditability

This transaction is local to Notifications and separate from the originating business transaction.

### 5.4 Outside the transaction
The following MUST NOT be treated as one atomic unit with Notifications truth:
- originating module truth commit
- whole-system broker delivery
- Redis state
- provider-side final certainty beyond the current observed attempt
- downstream analytics/reporting refresh
- batch summary or remediation-report publication

Distributed atomic commit across domain truth + broker + Notifications + provider is out of scope in V1.

### 5.5 Transaction duration rule
Notifications transactions must be short:
- do not hold DB state open while waiting on unrelated work
- keep provider interaction and state persistence tightly bounded
- avoid long-running batches inside a single open DB transaction
- avoid infinite retry loops inside one transaction

### 5.6 Concurrency expectations
Notifications must assume:
- duplicate event delivery
- worker retries
- provider timeout ambiguity
- replay of already-processed messages
- racing retry attempts if worker coordination is imperfect
- stale worker/process resumes after pause/restart
- overlapping remediation/replay runs if triggered more than once

At minimum, the design must prevent:
- duplicate sends for the same email intent
- duplicate durable delivery rows for one intent
- contradictory final states caused by racing retries
- stale processing attempts overwriting a newer terminal state incorrectly
- replay/remediation workflows publishing stale resend candidates over fresher delivery truth

### 5.7 No heterogeneous distributed transaction
Notifications does **not** attempt one atomic workflow across:
- originating domain truth
- RabbitMQ/broker
- Notifications store
- email provider
- dashboards/analytics stores

Atomicity stops at:
- originating module truth + Outbox boundary
- and separately at Notifications’ own local delivery-state transaction

---

## 6) Delivery-state transition rules

### 6.1 Delivery states must be monotonic enough for safe convergence
Typical states may include:
- `Pending`
- `Sending` (optional)
- `Sent`
- `Failed` (retryable)
- `Dead`
- `Suppressed` / `Deduped` (optional explicit state)
- `Ambiguous` (optional, if provider certainty is modeled explicitly)

The state model should avoid allowing stale retries to overwrite a newer terminal state incorrectly.

### 6.2 Terminal-state protection
Once an intent has reached a terminal state such as:
- `Sent`
- `Dead`
- `Suppressed` (where policy treats it as terminal)

older or racing attempts must not silently move it backward to a less authoritative state.

Resource-side/state-store validation should reject or ignore stale transitions.

### 6.3 Attempt metadata is operational truth
Attempt counts, last error, and timestamps are important for:
- investigations
- retry policy
- dead-letter decisions
- incident diagnostics

These fields must remain consistent with the current delivery state.

### 6.4 Resource-side protection beats worker belief
A worker saying:
- “this email is still unsent”
- “this retry still owns the right to update state”
- “timeout means we can safely resend”

is not sufficient.

The authoritative delivery state must decide whether an update is still valid.

### 6.5 Ambiguous outcomes should remain explicit where needed
If provider certainty cannot be established safely, the model should allow:
- explicit ambiguity state
- retry decision under policy
- operator review/remediation path

rather than forcing false confidence into `Sent` or `Failed`.

---

## 7) Retry, DLQ, replay, and remediation policy (V1)

### 7.1 Retry policy
Retry posture:
- exponential backoff with jitter
- max attempts defined by policy
- retry only on classified transient errors
- bounded retries only

Examples of transient conditions:
- SMTP/network timeout
- temporary provider outage
- throttling / temporary upstream failure

### 7.2 Permanent failure / dead-letter posture
Send to DLQ or mark as `Dead` when:
- max attempts are exceeded
- recipient is invalid
- required template is missing
- prerequisite truth is invalid and non-recoverable
- provider returns a permanent classified error

### 7.3 Retry safety rule
Retries must not create duplicate sends for the same intent.

Therefore retries must always be paired with:
- message dedupe
- business-intent dedupe
- safe delivery-state transitions
- provider-call wrapping that respects ambiguity

### 7.4 Alerts and backlog posture
Alerts are required for:
- DLQ/dead growth
- outbox age growth
- sustained queue backlog growth
- unusual dedupe-hit spikes
- unusual provider timeout ambiguity spikes if tracked

### 7.5 Replay/remediation posture
Replay/remediation workflows may:
- select bounded failed or ambiguous delivery sets
- regenerate resend candidates
- apply retry where still valid
- generate operator-visible remediation outputs

They must not:
- ignore current delivery truth
- resend blindly without dedupe/state validation
- treat stale remediation candidate sets as authoritative

### 7.6 Retry-safe design beats exclusive execution assumptions
Notifications reliability must not depend on:
- one current sender process
- one queue consumer being “surely current”
- timeout-only ownership assumptions

If future ownership-sensitive workflows are introduced, they must use authoritative generation/fencing checks rather than naive singleton assumptions.

### 7.7 Rerun safety for derived workflows
Cleanup, summary, and remediation-report workflows must be safe to rerun on the same bounded input.

This means:
- rerun must not resend canonical intents accidentally
- partial candidate summary/report output must not masquerade as completed state
- rerun must respect latest delivery-state truth

### 7.8 Full replay is acceptable if safer than selective repair
If provider ambiguity or backlog recovery leaves delivery-state too uncertain, a bounded full replay/remediation decision process is acceptable when:
- canonical intent dedupe remains in place
- current state is checked before resend
- stale candidate resend sets do not override fresher truth

---

## 8) Ordering and replay safety

### 8.1 Strict ordering is usually not required
Notifications usually does not require global or per-user strict ordering for correctness.

However, it must be replay-safe:
- same event replay must not resend
- duplicate worker processing must not resend
- stale retries must not overwrite newer delivery truth incorrectly

### 8.2 Valid retrigger must produce a new intent
Workflows such as resend verification must create:
- a new token
- a new business dedupe key
- a new logical delivery intent

This avoids:
- blocking a valid new send
- conflating duplicate replay with legitimate resend

### 8.3 Prefix-consistency prerequisites
If an email depends on a prior cause being materialized, the consumer should fetch required truth state before sending.

Examples:
- verification token must exist and still be valid by policy
- reset token must exist and be usable
- article publish notification should confirm publish truth if policy requires it

If prerequisites are missing:
- fail safely
- classify as retryable or terminal according to policy
- do not send speculative email

### 8.4 No global total order across notifications
Notifications does **not** require:
- one total order for all sends across all templates
- one strict per-user ordering for every workflow
- one cluster-wide delivery sequence

Correctness depends on dedupe and intent identity first, and only on ordering where a specific workflow requires it.

### 8.5 Replay must respect latest known delivery truth
When replaying:
- dead-letter items
- failed sends
- ambiguous outcomes
- recovered backlog

the replay path must re-check current state before acting.
Replay is a recovery mechanism, not authority to resend blindly.

---

## 9) Versioning and stale-attempt protection

### 9.1 Notifications usually relies more on dedupe than aggregate ordering
For many email flows, business-intent dedupe is the primary protection.

However, if delivery-state materialization or orchestration becomes more complex,
Notifications may also need:
- version-aware state transitions
- expected-state checks
- monotonic attempt sequencing
- generation/fencing semantics for worker ownership

### 9.2 Resource-side protection
The delivery state store must not trust a worker’s local belief that:
- “this intent is still unsent”
- “this retry still owns the right to update state”

The authoritative delivery state must verify whether an update is still current and acceptable.

### 9.3 Timestamps are informational, not primary freshness authority
`SentAt`, `LastAttemptAt`, `FailedAt`, and similar fields are operationally useful.  
They are not sufficient authority by themselves for resolving racing or stale write attempts.

### 9.4 Stale resend candidates must not cut over as current truth
If remediation or batch resend candidate sets are materialized:
- they remain derived operational artifacts
- they must not outrun fresher delivery-state truth
- stale candidates should be rejected, rerun, or revalidated before action

---

## 10) Batch / summary / cleanup posture

### 10.1 Delivery truth vs derived outputs
Canonical delivery state is Notifications truth.

Derived outputs may include:
- delivery summaries
- provider error-class reports
- backlog summaries
- remediation candidate sets
- cleanup candidate sets

These outputs are useful but remain subordinate to canonical delivery truth.

### 10.2 Candidate-before-publication
If Notifications publishes an important derived summary/report:
- build candidate first
- validate candidate
- publish/cut over explicitly
- do not treat partial candidate state as complete active output

### 10.3 Recompute is acceptable
If a derived summary is cheap enough to rebuild from canonical delivery records, recomputation may be preferred over fragile incremental patching.

### 10.4 Cleanup is bounded and policy-driven
Cleanup or retention workflows must be bounded and must not destroy artifacts still required for:
- dedupe policy
- investigation
- remediation
- compliance/retention obligations

### 10.5 Derived remediation/reporting outputs remain non-authoritative
Remediation reports, backlog summaries, resend candidate sets, and cleanup candidate sets:
- help operations
- may lag
- may be rebuilt
- do not become canonical delivery truth

---

## 11) Coordination and ownership posture (Notifications)

### 11.1 Notifications does not require global singleton coordination by default
Ordinary Notifications correctness must not depend on:
- one global notifications leader
- one process being “the only sender”
- startup order deciding who is current
- timeout-only assumptions about ownership

Notifications correctness should instead be achieved through:
- canonical dedupe
- safe delivery-state transitions
- replay-safe consumers
- provider ambiguity handling
- resource-side state validation

### 11.2 If future ownership-sensitive workflows are introduced
If a future Notifications workflow truly requires one current owner
(for example exclusive batch-orchestration owner or one-current repair worker),
that workflow must define:
- ownership source of truth
- monotonic generation/fencing token
- resource-side rejection of stale owner actions

Naive leader/lock patterns are not acceptable.

### 11.3 Safe non-progress beats unsafe duplicate send
If ownership is ambiguous for a correctness-sensitive resend/remediation workflow, Notifications must prefer:
- delayed remediation
- stale-owner rejection
- operator retry
- preservation of current delivery truth

over unsafe dual send or stale retry overwrite.

---

## 12) Observability signals (Notifications-specific)

### 12.1 Minimum metrics
Minimum metrics include:
- send success/failure rate by template
- retry volume and retry reasons
- DLQ size and oldest DLQ age
- consumer processing latency (P95/P99)
- outbox backlog age and queue depth
- duplicate-prevention indicators:
  - message-level dedupe hits
  - business-key conflicts
- provider timeout ambiguity / reconciliation cases if tracked
- state-transition conflict or stale-attempt reject count if implemented
- remediation/replay mismatch count
- derived summary freshness age where summaries exist
- candidate publication/cutover failure count for important derived outputs
- ownership-generation mismatch count if future ownership-sensitive workflows are introduced

### 12.2 Logging requirements
Logs should include:
- `correlationId`
- `messageId / eventId`
- email intent key (hashed or redacted; never raw tokens)
- `recipientUserId` where appropriate
- provider result classification
- delivery-state transition
- retry/dead classification decision

Prefer:
- redacted/hardened identifiers
- no raw tokens
- no unnecessary raw email addresses where avoidable

### 12.3 Health layering
Notifications observability should distinguish:
- worker liveness
- queue health/backlog
- provider health and latency
- delivery-state convergence
- dedupe pressure / retry storm symptoms
- summary/remediation workflow lag

---

## 13) ADR hook

This module follows:
- ADR-0013 (Outbox & delivery semantics)
- ADR-0015 (Redis cache policy)
- ADR-0018 (Transaction boundaries & consistency model)
- ADR-0020 (Timeout, retry, and failure detection policy)
- ADR-0022 (Versioning and fencing strategy)
- ADR-0023 (Consistency, ordering, and consensus boundaries)
- ADR-0024 (Distributed coordination and singleton work policy)
- ADR-0025 (Batch processing and derived state policy)
- ADR-0026 (Batch job orchestration and materialization policy)
- ADR-0027 (Stream processing and derived state policy)
- ADR-0028 (Consumer idempotency, replay, and rebuild policy)

If delivery guarantees, provider model, or storage choice changes significantly
(for example provider switch, multi-region delivery, batch send orchestration, or stronger delivery state guarantees),
a new ADR is required.

---

## 14) Summary

Notifications correctness in V1 rests on seventeen rules:

1. Notifications owns delivery truth, not the originating business truth.  
2. Email delivery is eventual and must not block core domain success.  
3. Provider timeout does not prove “email not sent.”  
4. Two-layer idempotency is mandatory: message dedupe + business-intent dedupe.  
5. Each notification type must define exactly one canonical business dedupe key.  
6. Legitimate resend must create a new intent; duplicate replay must not.  
7. Dedupe must be durable enough to survive replay, redelivery, and restart.  
8. Dedupe-hit behavior must be deterministic and must never cause a duplicate visible send.  
9. Delivery-state transitions must resist stale/racing retry attempts.  
10. Operational visibility of backlog, dead state, ambiguity, and duplicate suppression is part of correctness, not an optional extra.  
11. No global ordering or distributed transaction is assumed for Notifications workflows.  
12. Notification delivery must follow committed domain causes; it must not outrun them.  
13. Replay/remediation workflows support recovery, but must remain bounded and rerun-safe.  
14. Derived summaries/reports are not canonical delivery truth.  
15. Important derived outputs must follow candidate-before-publication discipline.  
16. Safe non-progress is preferable to unsafe duplicate-send behavior.  
17. Singleton/ownership semantics are not relied on unless explicitly protected by authoritative generation/fencing rules.