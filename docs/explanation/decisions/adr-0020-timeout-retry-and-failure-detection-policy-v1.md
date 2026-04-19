# ADR-0020 — Timeout, Retry, and Failure Detection Policy (V1)

**Status:** Accepted  
**Date:** 2026-03-09  
**Decision owners:** Architecture / Platform  
**Scope:** System-wide (timeouts, retries, failure interpretation, degraded-node handling, health semantics)  
**Related:**
- `../architecture/arc42/04-runtime-view-v1.md`
- `../architecture/arc42/05-quality-requirements.md`
- `../architecture/arc42/06-measurement-guide.md`
- `../architecture/arc42/11-replication-v1.md`
- `../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../architecture/arc42/14-distributed-systems-assumptions-v1.md`
- ADR-0013 (Outbox & delivery semantics)
- ADR-0015 (Cache policy & invalidation)
- ADR-0018 (Transaction boundaries & consistency model)
- ADR-0019 (System model and fault assumptions)

---

## Context

CommercialNews V1 runs in a partially synchronous environment with:

- multiple deployable components
- asynchronous side effects
- cache-first read paths with truth fallback
- at-least-once delivery through Outbox → Broker → Consumers
- runtime conditions where latency may spike, processes may pause, and dependencies may be temporarily slow or unreachable

Without a system-wide policy, several dangerous behaviors may appear:

- timeout being treated as proof that the remote side did nothing
- retries being added ad hoc and causing duplicate side effects
- health checks being mistaken for proof of business-flow health
- long waits causing thread starvation, queue buildup, and cascading failures
- short waits causing false suspicions and premature failover behavior
- background consumers retrying blindly and amplifying downstream outages
- producer-side publish failures being conflated with consumer-side processing failures
- ambiguous provider outcomes leading to replay of the same user-visible intent without proper idempotency protection

CommercialNews needs one explicit decision that defines:

- how timeout should be interpreted
- where retries are allowed vs dangerous
- what retry-safe design means in V1
- how failure detection should work at the architecture level
- how degraded dependencies should affect truth paths and side-effect paths differently
- how producer-side and consumer-side ambiguity should be reasoned about separately

---

## Decision

### 1) Timeout is an operational heuristic, not proof of remote failure

CommercialNews V1 adopts the following rule:

- a timeout means the caller stopped waiting
- a timeout does **not** prove that the remote side did nothing
- a timeout does **not** prove that no state change occurred
- a timeout does **not** prove that a broker publish, email send, or downstream write failed before any effect

Therefore:

- every timeout outcome is treated as **ambiguous**
- retry logic must assume duplicate effects are possible
- reconciliation must be based on authoritative truth state, not timeout interpretation alone

This rule applies to:

- API → DB / cache / broker / external service calls
- Worker → broker / store / provider calls
- internal HTTP or service-to-service calls if introduced later

---

### 2) Timeout values are policy-driven, measured, and dependency-class aware

CommercialNews V1 does not allow arbitrary timeout values chosen by intuition alone.

Timeouts must be chosen according to:

- operation type
- dependency class
- user-facing latency expectations
- observed latency distribution
- downstream saturation behavior
- consequence of false timeout vs long wait

At a minimum, timeout policy must distinguish between:

#### A) Synchronous hot-path operations

Examples:

- public list/detail
- slug routing
- login/refresh
- admin validation steps requiring immediate response

These should have tighter latency budgets because:

- the caller is waiting directly
- excessive wait harms UX and throughput
- failure should degrade or fail explicitly rather than hang indefinitely

#### B) Background / async processing

Examples:

- outbox publish
- notification send
- audit ingestion
- interaction aggregation
- projection refresh

These may tolerate longer waits than hot paths, but must still remain bounded to prevent:

- worker starvation
- backlog explosion
- retry storms
- invisible consumer stalls

#### C) Dependency classes must not share one universal timeout mindset

At minimum, teams must reason separately about:

- database calls
- cache / Redis calls
- broker publish / broker consume interactions
- external provider calls such as email
- internal HTTP calls if introduced later

**Rule:** timeout budgets must be selected per dependency class and per flow type, not globally guessed once and reused everywhere.

---

### 3) Retry is allowed only where semantics are explicitly safe

CommercialNews V1 rejects the assumption that “retry is harmless.”

Retry is allowed only when one of the following is true:

#### A) The operation is naturally idempotent

Examples:

- cache invalidation
- overwrite of a deterministic derived value
- safe fetch/re-read
- set-to-known-state updates

#### B) The operation is protected by explicit idempotency

Examples:

- consumer dedupe by `MessageId`
- email delivery dedupe by `MessageId` and/or business intent key
- audit append with unique event/message key
- projection apply guarded by aggregate version

#### C) The operation can be safely reconciled from truth after ambiguity

Examples:

- retrying a read from truth after cache timeout
- resyncing projection state from truth when a version gap is detected
- investigating an ambiguous write outcome via truth-backed resource state

Retry is **not** automatically safe for:

- user-visible side effects
- external provider calls with ambiguous acknowledgment
- stale-owner workflows
- operations without dedupe or replay-safe design

---

### 4) Timeout does not authorize same-intent replay without protection

A timeout does not grant permission to immediately replay the same visible intent unless safety is already guaranteed.

Examples of dangerous same-intent replay:

- resending a verification email immediately after an ambiguous provider timeout
- triggering another password reset email after ambiguous completion of the first send
- replaying a governance notification without idempotency protection
- repeating external provider calls where acknowledgment may have been lost after the effect already happened

Therefore CommercialNews V1 requires:

- idempotency guardrails before same-intent replay
- or truth-state reconciliation before replay
- or explicit operator-controlled remediation when ambiguity cannot be resolved safely

**Rule:** ambiguous timeout outcomes must not trigger blind same-intent replay.

---

### 5) Core truth transactions must not contain retry loops over external dependencies

CommercialNews V1 keeps the truth transaction boundary short and local.

Therefore the following must not be embedded as “retry until success” inside the originating truth transaction:

- broker publish
- email send
- cache invalidation as a success condition
- external HTTP/API calls
- long-running dependency calls

Truth flow success is based on:

- truth commit
- and outbox commit when async work is required

Retry responsibility then moves to:

- outbox publisher
- async consumer
- reconciliation job
- explicit operator remediation

This protects core flows from:

- long lock duration
- transaction stretching
- retry ambiguity inside open transactions
- cascading failures from slow or broken dependencies

---

### 6) Producer-side retry and consumer-side retry are different failure domains

CommercialNews V1 explicitly separates:

#### A) Producer-side retry / failure

Examples:

- writing to broker from outbox publisher
- publication timeout before broker handoff certainty
- outbox message entering retry / failed / dead state

This failure domain exists **before or during broker handoff**.

#### B) Consumer-side retry / failure

Examples:

- notification consumer receives a message but email provider call times out
- audit consumer receives a message but DB write fails
- projection consumer detects a version gap and must resync

This failure domain exists **after broker handoff**.

These two layers must not be conflated.

They differ in:

- observability signals
- retry ownership
- dead-letter ownership
- reconciliation paths
- operational response

**Rule:** when discussing timeout/retry behavior, teams must state whether they mean producer-side publication ambiguity or consumer-side processing ambiguity.

---

### 7) Failure detection is heuristic and threshold-based

CommercialNews V1 does not assume it can know with certainty whether a remote node is truly dead.

Instead, failure detection is based on:

- timeout thresholds
- retries within policy
- health/readiness probes
- broker lag/failure signals
- flow-level observability

This means:

- “suspected failed” is often the practical state
- operational decisions may be based on non-response thresholds
- those decisions are useful, but not absolute truth

CommercialNews explicitly rejects reasoning such as:

- “one timeout proves that node is dead”
- “one green health endpoint proves the full business flow is healthy”
- “a live process is definitely making safe progress”

---

### 8) Health must be interpreted at multiple layers

CommercialNews V1 distinguishes at least four health concepts:

#### A) Process liveness
Is the process/container alive?

#### B) Readiness
Is the component ready to accept normal traffic right now?

#### C) Dependency health
Can required dependencies be used within acceptable thresholds?

#### D) Business-flow health
Are important end-to-end use cases actually working within expected error/latency limits?

This means:

- liveness alone is not enough
- readiness alone is not enough
- dependency reachability is not enough
- truth-path and async-path health need separate signals

Examples:

- API process alive but DB timeouts rising
- Worker process alive but queue lag growing indefinitely
- Redis healthy but routing correctness falling back to truth too often
- health endpoint green while notification pipeline is effectively stalled

#### Async pipeline business-flow health examples

For async flows, business-flow health must include outcome-based signals such as:

- verification email requests committed but not delivered within target lag
- password reset requests committed but not delivered within target lag
- audit events published but not ingested within expected lag budget
- outbox published successfully but downstream consumer backlog continuing to age

**Rule:** “worker alive” or “queue reachable” is not sufficient proof that an async business flow is healthy.

---

### 9) CommercialNews uses different failure posture for truth paths vs side-effect paths

#### 9.1 Truth paths

Truth paths must prefer:

- short bounded waits
- fast failure over ambiguous hanging
- clear success based on local truth commit only
- authoritative reconciliation from truth if callers later need to verify ambiguous outcomes

Truth paths must not be held hostage by:

- notification delay
- audit delay
- interaction aggregation lag
- cache update lag

#### 9.2 Side-effect paths

Side-effect paths may:

- retry with backoff
- queue work
- accumulate backlog temporarily
- enter degraded mode
- route poison work to DLQ or dead state

But they must remain:

- idempotent
- observable
- bounded
- recoverable

This separation is mandatory for V1 reliability.

---

### 10) Retry policy is bounded, backoff-based, and jittered

CommercialNews V1 adopts the following retry posture:

- retries must be **bounded**
- retries must use **backoff**
- retries should include **jitter**
- repeated failure must become observable as backlog, dead state, or DLQ, not infinite hidden looping

This applies to:

- outbox publishing
- async consumers
- provider integrations
- reconciliation loops where applicable

Retries must not become:

- infinite silent loops
- synchronized storms across many workers
- hidden latency inflation on user-facing flows

---

### 11) Retry policy must preserve throughput under failure

Retry is not just about eventual success. It must also avoid collapsing the system under stress.

Therefore CommercialNews V1 requires retry design to consider:

- worker concurrency limits
- queue growth
- oldest pending age
- downstream saturation
- poison-message handling
- retry amplification risk

Bounded waiting and backpressure are preferred over “keep trying immediately.”

---

### 12) Ambiguous outcomes must be reconciled from truth

When a user-facing or operator-facing flow times out or becomes ambiguous, CommercialNews V1 prefers:

- query authoritative truth state
- expose current resource state
- use correlation/message identifiers for investigation
- reconcile async effects via replay / retry / rebuild where safe

The architecture must not assume that a timeout answer is sufficient to conclude:

- success
- failure
- no-op
- duplicate absence

Truth-backed reconciliation is the safe path.

---

### 13) Degraded dependencies must not automatically become total outages

CommercialNews V1 requires graceful degradation where correctness allows it.

Examples:

- if Redis is down, fallback to truth reads where safe
- if notification provider is down, keep truth flow successful and accumulate retryable backlog
- if audit consumer is behind, core admin/content flow remains valid while backlog becomes visible
- if interaction aggregation is delayed, reading still works and counters may lag

This rule does **not** allow degradation to violate:

- publication visibility correctness
- identity/security correctness
- governance truth correctness

---

### 14) False suspicion is acceptable; silent corruption is not

Because timeouts and failure detectors are heuristic, CommercialNews accepts that:

- occasionally a healthy-but-slow node may be treated as degraded or effectively unavailable

But CommercialNews does **not** accept:

- stale actor continuing unsafe writes
- duplicate visible side effects without dedupe controls
- stale cache overriding truth correctness
- silent inconsistency caused by naive retry assumptions

When forced to choose, the architecture prefers:

- temporary performance/availability degradation

over

- silent correctness loss

---

## Decision summary

CommercialNews V1 adopts the following timeout/retry/failure-detection posture:

- timeout is a waiting limit, not proof of failure
- timeouts must be treated as ambiguous
- timeout budgets are policy-driven and selected by flow type and dependency class
- retries are allowed only where operations are idempotent, deduped, or truth-reconcilable
- same-intent replay after timeout requires idempotency protection or truth reconciliation
- truth transactions remain short and do not include retrying external side effects
- producer-side publication ambiguity and consumer-side processing ambiguity are separate failure domains
- failure detection is heuristic and threshold-based
- health is multi-layered: liveness, readiness, dependency, and business-flow health
- truth paths fail fast or degrade safely
- side-effect paths retry with bounded backoff and observability
- degraded dependencies should produce safe degradation, not unnecessary total outage
- truth-backed reconciliation is the source of certainty after ambiguous outcomes

---

## Consequences

### Positive

- prevents dangerous assumptions around timeout and retry
- protects truth flows from being stretched by slow dependencies
- aligns retry behavior with idempotency and outbox semantics
- reduces risk of duplicate emails, audit entries, and derived-state corruption
- improves distinction between producer-side and consumer-side failure handling
- encourages flow-level observability instead of simplistic health assumptions
- supports graceful degradation under partial failure

### Negative / Trade-offs

- some operations will return ambiguity that must be reconciled later
- retry-safe design requires more discipline in consumers and providers
- operators must monitor backlog, retry behavior, and dead states actively
- conservative timeout/retry posture may reduce apparent aggressiveness in recovery
- false suspicion of degraded nodes is possible and expected in a partially synchronous system

---

## Alternatives considered

### 1) Treat timeout as failure by default

**Pros**
- simple mental model

**Cons**
- unsafe under ambiguous network/provider outcomes
- encourages duplicate side effects and poor incident reasoning

Rejected.

### 2) Retry all failures aggressively

**Pros**
- may recover some transient issues quickly

**Cons**
- causes duplicate effects
- causes retry storms
- risks queue collapse
- overloads downstream systems
- creates hidden operational debt

Rejected.

### 3) No retries except manual intervention

**Pros**
- simpler correctness reasoning

**Cons**
- poor resilience
- too fragile for expected transient failures in async infrastructure

Rejected.

### 4) One timeout policy for all operations

**Pros**
- easy to configure

**Cons**
- ignores hot-path vs background differences
- ignores dependency-class differences
- becomes either too short for background work or too long for synchronous user flows

Rejected.

### 5) Health endpoint only as operational truth

**Pros**
- very simple

**Cons**
- does not capture dependency degradation
- does not capture consumer stalls
- does not capture business-flow failure under partial outage

Rejected.

---

## Implementation notes (V1)

### Module documentation requirements

Module docs must specify:

- which operations are retry-safe and why
- whether timeout ambiguity is producer-side or consumer-side
- what idempotency guard protects retries
- what truth-state reconciliation path exists after ambiguity

### Consumer handler requirements

Consumer handlers must document:

- idempotency keys
- retry conditions
- terminal failure behavior
- dead-letter behavior if applicable
- reconciliation behavior after ambiguity
- behavior when the same user-visible intent could be replayed

### Public/API write flow requirements

Public/API write flows should provide a way to verify truth state after ambiguous client-side timeout, either by:

- returning committed resource state when possible
- or allowing a follow-up truth read by resource identity

### Health and observability requirements

Health endpoints should be complemented by:

- queue lag metrics
- outbox oldest pending age
- consumer failure/retry signals
- cache fallback rates
- provider timeout/failure rates
- async business-flow lag dashboards

---

## Operational guidance (V1)

### Recommended signals

Minimum system-wide signals include:

- request latency percentiles on critical endpoints
- timeout rate by dependency and by flow
- outbox pending count
- outbox oldest pending age
- broker queue depth (ready/unacked)
- consumer retry count and failure rate
- DLQ/dead-state size and age
- Redis fallback rate / truth fallback rate
- provider timeout/failure rate
- worker stall indicators
- async flow lag indicators for critical flows such as verification/reset/audit ingestion

### Recommended posture during incidents

- prefer slowing non-critical derived behavior over stretching truth transactions
- prefer safe fallback over stale confidence
- prefer dead-letter/remediation over infinite blind retries
- prefer explicit degraded mode over hidden saturation
- prefer truth reconciliation over guessing from timeout symptoms
- do not trigger same-intent replay unless protection is already in place

---

## Follow-ups

- Create ADR-0021: Clock, Time, and Ordering Policy (V1)
- Create ADR-0022: Versioning and Fencing Strategy (V1)
- Update module docs, especially:
  - `06-idempotency-consistency.md`
  - `07-observability-slos.md`
  - `08-dependencies-and-ownership.md`
- Ensure `06-measurement-guide.md` and module SLI docs expose timeout, backlog, retry, fallback, and async flow lag signals consistently