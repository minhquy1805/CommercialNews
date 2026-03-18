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

CommercialNews needs one explicit decision that defines:

- how timeout should be interpreted
- where retries are allowed vs dangerous
- what retry-safe design means in V1
- how failure detection should work at the architecture level
- how degraded dependencies should affect truth paths and side-effect paths differently

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
- API → DB/cache/broker/external service calls
- Worker → broker/store/provider calls
- internal HTTP or service-to-service calls if introduced later

---

### 2) Timeout values are policy-driven and measured, not guessed

CommercialNews V1 does not allow arbitrary “nice-looking” timeout values chosen by intuition alone.

Timeouts must be chosen according to:
- operation type
- user-facing latency expectations
- observed latency distribution
- downstream saturation behavior
- consequence of false timeout vs long wait

At a minimum, timeout choices must distinguish between:

#### A) Synchronous hot-path operations
Examples:
- public list/detail
- slug routing
- login/refresh
- admin write validation steps that need immediate response

These should have tighter latency budgets because:
- the caller is waiting directly
- excessive wait harms UX and system throughput
- failure should degrade or fail explicitly rather than hang indefinitely

#### B) Background / async processing
Examples:
- outbox publish
- notification send
- audit ingestion
- interaction aggregation
- projection refresh

These may tolerate longer waits than hot paths, but must still remain bounded to prevent:
- worker thread starvation
- backlog explosion
- retry storms
- invisible consumer stalls

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
- email delivery dedupe by `MessageId` or business key
- audit append with unique event/message key
- projection apply guarded by aggregate version

#### C) The operation can be safely reconciled from truth after ambiguity
Examples:
- retrying a read from truth after cache timeout
- resyncing projection state from truth when version gap is detected

Retry is **not** automatically safe for:
- side effects visible to users
- external provider calls with ambiguous acknowledgment
- stale-owner workflows
- operations without dedupe or replay-safe design

---

### 4) Core truth transactions must not contain retry loops over external dependencies

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

### 5) Failure detection is heuristic and threshold-based

CommercialNews V1 does not assume it can know with certainty whether a remote node is “truly dead.”

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

### 6) Health must be interpreted at multiple layers

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
- readiness is not enough
- dependency reachability is not enough
- truth-path and async-path health need separate signals

Examples:
- API process alive but DB timeouts rising
- Worker process alive but queue lag growing indefinitely
- Redis healthy but public routing correctness falling back to truth too often
- health endpoint green while notification pipeline is effectively stalled

---

### 7) CommercialNews uses different failure posture for truth paths vs side-effect paths

#### 7.1 Truth paths
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

#### 7.2 Side-effect paths
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

### 8) Retry policy: bounded, backoff-based, jittered

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

### 9) Retry policy must preserve throughput under failure

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

### 10) Ambiguous outcomes must be reconciled from truth

When a user-facing or operator-facing flow times out or becomes ambiguous, CommercialNews V1 prefers:

- query authoritative truth state
- expose current resource state
- use correlation/message identifiers for investigation
- reconcile async effects via replay/retry/rebuild

The architecture must not assume that a timeout answer is sufficient to conclude:
- success
- failure
- no-op
- duplicate absence

Truth-backed reconciliation is the safe path.

---

### 11) Degraded dependencies must not automatically become total outages

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

### 12) False suspicion is acceptable; silent corruption is not

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
- retries are allowed only where operations are idempotent, deduped, or truth-reconcilable
- truth transactions remain short and do not include retrying external side effects
- failure detection is heuristic and threshold-based
- health is multi-layered: liveness, readiness, dependency, and business-flow health
- truth paths fail fast or degrade safely
- side-effect paths retry with bounded backoff and observability
- degraded dependencies should produce safe degradation, not unnecessary total outage
- truth-backed reconciliation is the source of certainty after ambiguous outcomes

---

## Consequences

### Positive

- Prevents dangerous assumptions around timeout and retry
- Protects truth flows from being stretched by slow dependencies
- Aligns retry behavior with idempotency and outbox semantics
- Reduces risk of duplicate emails/audit entries/derived-state corruption
- Encourages flow-level observability instead of simplistic health assumptions
- Supports graceful degradation under partial failure

### Negative / Trade-offs

- Some operations will return ambiguity that must be reconciled later
- Retry-safe design requires more discipline in consumers and providers
- Operators must monitor backlog, retry behavior, and dead states actively
- Conservative timeout/retry posture may reduce apparent aggressiveness in recovery
- False suspicion of degraded nodes is possible and expected in a partially synchronous system

---

## Alternatives considered

### 1) Treat timeout as failure by default
- Pros: simple mental model.
- Cons: unsafe under ambiguous network/provider outcomes; encourages duplicate side effects and bad incident reasoning.

Rejected.

### 2) Retry all failures aggressively
- Pros: may recover some transient issues quickly.
- Cons: causes duplicate effects, retry storms, queue collapse, downstream overload, and hidden operational debt.

Rejected.

### 3) No retries except manual intervention
- Pros: simpler correctness reasoning.
- Cons: poor resilience; too fragile for expected transient failures in async infrastructure.

Rejected.

### 4) One timeout policy for all operations
- Pros: easy to configure.
- Cons: ignores hot-path vs background differences; either too short for background or too long for synchronous user flows.

Rejected.

### 5) Health endpoint only as operational truth
- Pros: very simple.
- Cons: does not capture dependency degradation, consumer stalls, or business-flow failure under partial outage.

Rejected.

---

## Implementation notes (V1)

- Module docs must specify which operations are retry-safe and why.
- Consumer handlers must document:
  - idempotency keys
  - retry conditions
  - terminal failure behavior
  - reconciliation behavior after ambiguity
- Public/API write flows should provide a way to verify truth state after ambiguous client-side timeout, either by:
  - returning committed resource state when possible, or
  - allowing a follow-up truth read by resource identity
- Health endpoints should be complemented by:
  - queue lag metrics
  - outbox oldest pending age
  - consumer failure/retry signals
  - cache fallback rates
  - business-flow SLI/SLO dashboards

---

## Operational guidance (V1)

### Recommended signals
Minimum system-wide signals include:
- request latency percentiles on critical endpoints
- timeout rate by dependency and flow
- outbox pending count
- outbox oldest pending age
- broker queue depth (ready/unacked)
- consumer retry count and failure rate
- DLQ/dead-state size and age
- Redis fallback rate / truth fallback rate
- worker stall indicators (for example, long gaps without successful processing)

### Recommended posture during incidents
- prefer slowing non-critical derived behavior over stretching truth transactions
- prefer safe fallback over stale confidence
- prefer dead-letter/remediation over infinite blind retries
- prefer explicit degraded mode over hidden saturation

---

## Follow-ups

- Create ADR-0021: Clock, Time, and Ordering Policy (V1)
- Create ADR-0022: Versioning and Fencing Strategy (V1)
- Update module docs, especially:
  - `06-idempotency-consistency.md`
  - `07-observability-slos.md`
  - `08-dependencies-and-ownership.md`
- Ensure `06-measurement-guide.md` and module SLI docs expose timeout, backlog, retry, and fallback signals consistently