# ADR-0021 — Clock, Time, and Ordering Policy (V1)

**Status:** Accepted  
**Date:** 2026-03-09  
**Decision owners:** Architecture / Platform  
**Scope:** System-wide (timestamps, elapsed-time measurement, clock synchronization assumptions, ordering/conflict rules)  
**Related:**
- `../architecture/arc42/05-quality-requirements.md`
- `../architecture/arc42/06-measurement-guide.md`
- `../architecture/arc42/11-replication-v1.md`
- `../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../architecture/arc42/14-distributed-systems-assumptions-v1.md`
- ADR-0013 (Outbox & delivery semantics)
- ADR-0015 (Cache policy & invalidation)
- ADR-0018 (Transaction boundaries & consistency model)
- ADR-0019 (System model and fault assumptions)
- ADR-0020 (Timeout, retry, and failure detection policy)

---

## Context

CommercialNews V1 depends on time in many different places, including:

- business timestamps (`CreatedAt`, `UpdatedAt`, `PublishedAt`, `OccurredAt`)
- security expiry (`exp`, reset token expiry, verification expiry)
- scheduling and retry timing (`NextRetryAt`, TTL, retry windows)
- latency measurement and timeout behavior
- log chronology and incident analysis
- event ordering and stale-write protection

Without a system-wide clock and ordering policy, several risky patterns tend to appear:

- using wall-clock timestamps to measure elapsed time
- using cross-node timestamps as proof of causal ordering
- relying on `UpdatedAt` or “largest timestamp wins” to resolve distributed write conflicts
- assuming NTP makes clocks exact enough for correctness-sensitive ordering
- mixing business timestamps, timeout timers, and concurrency sequencing into one ambiguous concept of “time”

CommercialNews needs one explicit decision that states:

- which kind of clock is used for which purpose
- what level of trust we place in synchronized clocks
- what timestamps can and cannot prove
- how ordering-sensitive paths should be designed safely
- how logs, metrics, expiry, and retries should interpret time

---

## Decision

### 1) CommercialNews distinguishes between two kinds of time

CommercialNews V1 explicitly separates:

#### A) Time-of-day / wall-clock time
Used for:
- business timestamps
- audit/log timestamps
- expiry moments
- scheduled timestamps
- operator-readable chronology

#### B) Monotonic elapsed time
Used for:
- timeout measurement
- latency measurement
- elapsed duration
- retry waiting intervals
- local timing inside one process

These two concepts must not be treated as interchangeable.

---

### 2) UTC wall-clock is required for business and system timestamps

CommercialNews V1 uses UTC wall-clock time for persisted timestamps that need human, business, or system meaning.

Examples:
- `CreatedAt`
- `UpdatedAt`
- `PublishedAt`
- `UnpublishedAt`
- `OccurredAt`
- `ViewedAt`
- `SentAt`
- `ExpiryDate`
- `NextRetryAt`

All persisted business/system timestamps must:
- be in UTC at rest
- follow one consistent server-side policy
- avoid depending on client-provided time for authority

This keeps:
- business history coherent
- audit/reporting understandable
- scheduling/retention rules consistent

---

### 3) Monotonic timers are required for elapsed-time measurement

CommercialNews V1 requires monotonic or equivalent elapsed-time measurement for:
- request duration
- handler duration
- dependency call duration
- timeout countdowns
- retry waits measured locally in a process
- local performance instrumentation

Wall-clock time must **not** be treated as the primary mechanism for elapsed-time measurement, because:
- it may jump forward or backward
- it may be adjusted by NTP
- it may behave unexpectedly under clock correction
- it can produce misleading or even negative durations

This decision applies to:
- API latency measurement
- Worker processing timing
- dependency timeout handling
- benchmark-style metrics
- internal scheduling loops that measure “how long has passed?”

---

### 4) Clock synchronization is useful, but not a correctness authority

CommercialNews V1 assumes:
- clock synchronization (for example via NTP) is operationally necessary
- synchronized clocks are useful for logs, reporting, expiry, and general operations
- synchronized clocks are **not** exact enough to serve as sole authority for cross-node causality or stale-write resolution

Therefore:
- cross-node wall-clock timestamps are treated as helpful evidence
- they are not treated as complete proof of ordering or freshness
- correctness-sensitive decisions must not depend solely on “which timestamp is larger”

CommercialNews accepts that:
- clock skew exists
- drift exists
- synchronization accuracy is finite
- time uncertainty exists even when clocks appear healthy

---

### 5) Wall-clock timestamps do not define causality

CommercialNews V1 explicitly rejects the rule:
- “the update with the later wall-clock timestamp is necessarily the correct or newest one”

Cross-node physical timestamps may be misleading because of:
- clock skew
- message delay
- buffering
- async processing lag
- process pause
- retry/replay timing differences

Therefore wall-clock timestamps are **not sufficient authority** for:
- distributed stale-write resolution
- conflict resolution between competing cross-node writes
- event causality across nodes
- deciding which effect logically happened later when ordering matters

---

### 6) Ordering-sensitive paths must use explicit sequencing or versioning

When ordering matters, CommercialNews V1 requires one or more of the following explicit mechanisms:

- aggregate version numbers
- revision numbers
- optimistic concurrency tokens
- rowversion / compare-and-set style checks
- monotonic sequence numbers
- generation/fencing-style tokens where ownership can transfer

This applies to:
- Content lifecycle and edits where stale overwrite is possible
- projection updates
- worker ownership or singleton-style execution where stale actors may wake up
- any derived-state pipeline where out-of-order application can corrupt state

Wall-clock timestamps may be stored for investigation, but they are not the primary correctness mechanism.

---

### 7) Last-write-wins by wall-clock timestamp is disallowed for correctness-critical data

CommercialNews V1 disallows using naive “last write wins by physical timestamp” as the sole conflict-resolution rule for correctness-critical state.

This is especially disallowed for:
- publication truth
- identity/security state
- authorization truth
- SEO routing truth
- ordering-sensitive projections
- governance records
- any workflow where a stale actor could overwrite a newer decision

If a module wants last-write-wins behavior for a non-critical derived view, it must:
- document why data loss or stale overwrite is acceptable
- show why the value is not correctness-critical
- document how reconciliation/rebuild would recover from disorder

The default posture is:
- **versioning over wall-clock**
- **truth over derived**
- **reject stale writes rather than silently overwrite**

---

### 8) Logs and traces require both time and identity

CommercialNews V1 requires that logs and traces use:
- UTC timestamps
- correlation identifiers
- message/request/trace identifiers where relevant

Reason:
- timestamps alone do not reconstruct causality reliably in distributed flows
- incident investigation must be able to connect:
  - request → truth write → outbox → publish → consume → side effect
- cross-node chronology is approximate without explicit correlation

Therefore operators must never rely on timestamps alone to reconstruct truth in a complex incident.

---

### 9) Expiry and scheduling are wall-clock concepts, but skew-aware

CommercialNews V1 treats expiry and schedule deadlines as wall-clock / point-in-time semantics.

Examples:
- email verification expiry
- password reset expiry
- refresh token expiry
- outbox retry scheduling (`NextRetryAt`)
- retention cutoffs
- future scheduled operations if introduced

These features should use UTC wall-clock timestamps, but must remember:
- node clocks may not match perfectly
- validation at boundaries may need limited tolerance where appropriate
- client-side clocks must not become authority

The system should prefer:
- server-side generation and evaluation of critical expiry values
- explicit truth-backed validation over trusting external time assertions

---

### 10) Client-supplied time is not authority

CommercialNews V1 treats client-provided time as untrusted by default.

Client timestamps may be logged or carried as informational metadata where useful, but they must not become authority for:
- token validity
- security-sensitive expiry
- governance correctness
- publication ordering
- state transition precedence

Authority for such decisions remains server-side.

---

### 11) Metrics and SLO measurement must not confuse timestamp with duration

CommercialNews V1 requires that observability distinguish between:

#### A) Event time / timestamp
Useful for:
- when something happened
- audit chronology
- retention windows
- human investigation

#### B) Processing duration / latency
Useful for:
- response-time SLOs
- handler time
- consumer processing time
- dependency latency
- timeout tuning

Average-looking “timestamps with milliseconds” are not a substitute for correct duration measurement.
Precision of formatting is not the same as correctness of elapsed-time measurement.

---

### 12) Time uncertainty is real and acknowledged

CommercialNews V1 adopts the practical assumption that every wall-clock reading has uncertainty.

This means:
- timestamps can support investigation
- timestamps can support reporting
- timestamps can support expiry and scheduling
- timestamps should not be overinterpreted as perfect truth at sub-millisecond or cross-node causality level

The system therefore prefers:
- explicit versioning for correctness
- correlation IDs for tracing
- truth-backed reconciliation after ambiguity
over
- overconfidence in finely formatted timestamps

---

## Decision summary

CommercialNews V1 adopts the following clock, time, and ordering policy:

- UTC wall-clock timestamps are used for business/system timestamps and scheduling moments
- monotonic timers are used for elapsed duration, timeout measurement, and latency instrumentation
- synchronized clocks are useful operationally but are not sole authority for correctness-sensitive ordering
- cross-node wall-clock timestamps do not define causality
- ordering-sensitive logic must rely on explicit versioning, sequencing, or fencing-style mechanisms
- naive last-write-wins by physical timestamp is disallowed for correctness-critical state
- logs and traces require both timestamps and correlation identifiers
- client-provided time is untrusted for correctness/security decisions

---

## Consequences

### Positive

- Prevents misuse of wall-clock time for duration measurement
- Prevents unsafe timestamp-based stale-write resolution
- Improves correctness under clock skew, delay, and pause conditions
- Strengthens module design toward explicit version/revision semantics
- Makes observability and incident analysis more trustworthy
- Clarifies expiry and schedule handling without pretending clocks are perfect

### Negative / Trade-offs

- Some flows require additional version fields or concurrency markers
- Developers cannot rely on simple “newest timestamp wins” shortcuts
- Incident analysis still requires disciplined correlation, not just log sorting
- Clock-aware design is more explicit and slightly more complex than naive timestamp usage

---

## Alternatives considered

### 1) Use wall-clock time for everything
- Pros: simple mental model, fewer concepts.
- Cons: unsafe for durations, unsafe for causality, unsafe for distributed stale-write resolution.

Rejected.

### 2) Treat NTP-synchronized clocks as accurate enough for correctness-sensitive ordering
- Pros: convenient.
- Cons: overconfident; ignores skew, uncertainty, pause, and network-induced ambiguity.

Rejected.

### 3) Resolve conflicts by “largest UpdatedAt wins”
- Pros: simple implementation.
- Cons: can silently discard causally newer or logically correct state; unsafe for critical truth and ordered projections.

Rejected.

### 4) Ignore persisted timestamps altogether and use only logical sequencing
- Pros: stronger focus on causality.
- Cons: insufficient for business meaning, auditability, scheduling, and retention.

Rejected.

### 5) Introduce highly specialized time infrastructure assumptions in V1
- Pros: could support stronger time semantics in theory.
- Cons: excessive cost/complexity for CommercialNews V1; not justified by system scope.

Rejected.

---

## Implementation notes (V1)

- Persist business/system timestamps in UTC consistently.
- Use monotonic-style elapsed-time measurement for:
  - API request duration
  - Worker handler duration
  - dependency latency
  - timeout countdowns
- Module docs must identify any ordering-sensitive write path and state which mechanism is used:
  - version
  - revision
  - rowversion
  - expected-version check
  - generation/fencing token
- Logs should include:
  - UTC timestamp
  - correlation/request/message identifiers
  - version/revision identifiers where ordering matters
- Projection consumers should prefer version-aware application over timestamp-based conflict handling.
- Identity and security expiry checks must be evaluated from server-side truth and UTC policy, not client time.

---

## Operational guidance (V1)

### Recommended operational controls
- monitor time synchronization health and large clock drift conditions
- use more than one trusted time source where infrastructure policy allows
- treat sudden clock anomalies as operational risk, not cosmetic noise
- ensure latency dashboards measure elapsed duration correctly, not by wall-clock subtraction alone

### Recommended investigation posture
During incidents:
- do not assume sorted timestamps alone reveal truth
- correlate using request/message/correlation IDs
- verify truth state directly when ordering is ambiguous
- treat cross-node chronology as approximate unless reinforced by explicit sequencing

---

## Follow-ups

- Create ADR-0022: Versioning and Fencing Strategy (V1)
- Update module docs, especially:
  - `06-idempotency-consistency.md`
  - `07-observability-slos.md`
  - `08-dependencies-and-ownership.md`
- Ensure any module using:
  - `UpdatedAt`
  - `OccurredAt`
  - retry scheduling
  - projection freshness
  explicitly documents whether the timestamp is:
  - informational
  - scheduling-related
  - or correctness-authoritative (which should be rare and justified)