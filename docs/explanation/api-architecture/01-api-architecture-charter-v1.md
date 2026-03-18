# API Architecture Charter — CommercialNews (V1)

This charter defines the **non-negotiable API rules** for CommercialNews V1.
Its goal is to keep APIs **consistent**, **secure**, **operable**, and **evolvable** for years without breaking consumers.

It derives from:

* Constraints: `../architecture/arc42/02-constraints.md`
* Modularity & ownership: `../architecture/arc42/03-building-blocks-modularity.md`
* Runtime scenarios: `../architecture/arc42/04-runtime-view-v1.md`
* Quality requirements: `../architecture/arc42/05-quality-requirements.md`
* Measurement guide: `../architecture/arc42/06-measurement-guide.md`
* Governance: `../architecture/arc42/07-architecture-governance.md`
* Components: `../architecture/arc42/08-components.md`
* Architecture style: `../architecture/arc42/09-architecture-style.md`
* Replication: `../architecture/arc42/11-replication-v1.md`

---

## 1) Core principles (V1)

### 1.1 Contract-first, standards-first

* We treat API contracts as long-lived products.
* We adopt a single standard for naming, errors, paging/filter/sort, and semantics.

**Reason:** building APIs is easy; keeping them stable for 2–3 years is hard.

### 1.2 Read path first

Public reading (list/detail/by slug) is the primary workload driver.

* Read endpoints must remain **fast and available** under burst traffic.
* Non-critical workflows must never degrade reading.

### 1.3 Governance is mandatory

Admin actions must be:

* protected by explicit authorization policies
* traceable via audit
* non-blocking on async side effects

### 1.4 Asynchronous side effects by design

Audit ingestion, notifications, and interaction aggregation are async to:

* decouple failure domains
* isolate burst traffic
* support retries safely

---

## 2) API style decisions (REST-first in V1)

### 2.1 North–south APIs (client-facing)

CommercialNews uses **REST over HTTP** for public and admin APIs.

### 2.2 East–west calls (future)

V1 is a service-based modular monolith (API + Worker). Internal calls are in-process.
If V2+ introduces separate services:

* REST remains the default for north–south
* gRPC may be considered for high-volume east–west where coupling is controlled

**Rule:** do not force a single “golden spec” across REST and gRPC. They evolve independently.

---

## 3) Endpoint taxonomy (paths, ownership, and boundaries)

### 3.1 Public vs Admin separation

* Public: `/api/v1/...`
* Admin: `/api/v1/admin/...`

**Reason:** clearer governance boundaries, simpler policy coverage, safer operations.

### 3.2 Resources vs actions

* Prefer resource-oriented endpoints for CRUD.
* Use action endpoints (`:{action}`) for explicit domain transitions where PATCH semantics are ambiguous:

  * `POST /api/v1/admin/articles/{id}:publish`
  * `POST /api/v1/admin/articles/{id}:unpublish`
  * `POST /api/v1/admin/media/{id}:restore`

**Rule:** action endpoints must document idempotency and emit audit events.

### 3.3 Module ownership and coupling

* Each module logically owns its data and rules.
* Modules exchange only stable IDs (e.g., `ArticleId`, `UserId`) and events.
* Cross-module DB access is forbidden except for explicitly allowed V1 read-only policy.

---

## 4) Contract rules (consistency across all APIs)

### 4.1 Standard request/response shapes

* List responses use a standard `{ items[], pageInfo{} }` envelope.
* Sorting/filtering conventions are consistent across modules.
* Date/time is ISO-8601 in UTC.

### 4.2 Standard error envelope

All APIs return errors using:

```json id="3j7mvp"
{
  "traceId": "string",
  "error": {
    "code": "MODULE.ERROR_CODE",
    "message": "Human-friendly message",
    "details": []
  }
}
```

### 4.3 Anti-enumeration for sensitive flows

Identity endpoints must not leak account existence:

* forgot password
* resend verification

Return `200 { "accepted": true }` even when the email does not exist.

### 4.4 OpenAPI as the contract

OpenAPI is not “just docs”; it is the contract used for:

* validation and examples
* breaking change detection (diff)
* consumer alignment

---

## 5) Versioning & compatibility (versioning is a feature)

### 5.1 Version conveyance

API versions are conveyed in the URL: `/api/v1`

### 5.2 Compatibility posture

Default stance: backward compatible changes only in V1.
Breaking changes require a new major version (`/api/v2`) and a migration plan.

### 5.3 Deprecation lifecycle (policy)

* Introduce new version in parallel
* Publish migration guide
* Communicate a sunset date
* Monitor usage
* Retire old version (optionally `410 Gone` for retired endpoints)

---

## 6) Reliability & resilience rules (protect core flows)

### 6.1 Non-blocking requirement (core)

Core flows must succeed independently of non-critical subsystems:

* publish/unpublish must not depend on audit ingestion completion
* register/reset must not depend on email delivery success
* reading must not depend on interaction aggregation availability

### 6.2 Bounded waits and timeouts

* APIs must set timeouts and avoid unbounded waits.
* Avoid cascading failures via strict dependency rules.

### 6.3 Idempotency and retry safety

* Async consumers **MUST** be idempotent (at-least-once tolerant).
* Key endpoints **SHOULD** support idempotency keys where duplicates are likely:

  * register
  * resend verification
  * forgot password
  * publish/unpublish (if clients may retry)

### 6.4 Replication & consistency charter (V1)

CommercialNews replication is single-leader truth + async side effects.

**Rules**

* Truth writes commit to the primary store; derived stores may lag.
* APIs **MUST** declare where staleness is allowed (public reads) and where it is not (admin/self state).
* APIs **MUST** be safe under replication lag:

  * **Read-your-writes:** admin and identity self-state reads must reflect successful writes immediately (primary read or write-return).
  * **Monotonic reads:** avoid “appears then disappears” for user-facing lists where applicable (sticky routing or stable read path).
  * **Consistent prefix:** do not expose effects before causes (e.g., notification/indexability before content exists and is visible).

**Fallback**

* If a derived store is stale/unavailable, prefer correctness-first fallbacks (truth read or safe `"updating"` response) over misleading `404`/empty responses.

See: `../architecture/arc42/11-replication-v1.md`

---

## 7) Security baseline (must-haves in V1)

### 7.1 Admin policy coverage = 100%

Every admin endpoint must enforce an explicit authorization policy.

### 7.2 Least privilege

Roles/permissions are centralized in Authorization. No scattered ad-hoc checks.

### 7.3 Abuse controls

Sensitive endpoints must be rate-limited:

* register/login/refresh/resend/forgot/reset
* abuse-prone interaction endpoints (comments at minimum)

### 7.4 Safe logging & redaction

* Never log secrets, tokens, or sensitive PII.
* Audit payloads follow redaction rules and minimum-necessary data.

---

## 8) Async event contracts (Worker integration)

### 8.1 Event envelope (V1)

All domain events use a minimal envelope:

* `MessageId` (or `EventId`)
* `OccurredAt`
* `CorrelationId`
* `ActorUserId?` (optional)
* `EventType`
* `AggregateId`
* `Version` (required where ordering matters)
* `Payload`

**Rule:** event publication must be reliable (Outbox pattern recommended by V1 system rules).

### 8.2 Consumer requirements

Consumers must be:

* idempotent
* retry-safe with DLQ strategy
* observable (success/failure/backlog metrics)

Where ordered transitions exist:

* consumers **SHOULD** enforce per-aggregate ordering using `AggregateId + Version`, and resync from truth store when gaps are detected.

---

## 9) Operability rules (observability and SLO alignment)

### 9.1 Telemetry is required for critical flows

Critical flows must emit structured logs and metrics:

* auth flows (register/login/refresh/verify/reset)
* publish/unpublish
* email workflows
* async handlers (success/failure/backlog)

### 9.2 Measure percentiles, not averages

* Read path latency is tracked as P95/P99.
* Async health includes backlog/lag trends.

---

## 10) Governance and fitness functions (how we prevent drift)

Minimum V1 guardrails:

* No cyclic dependencies between modules
* No boundary violations (respect ownership rules)
* 100% authorization policy coverage for admin endpoints
* Non-blocking read path (no sync dependency on audit/notifications/aggregation)
* Safe logging (no secrets/tokens/PII leaks)

If a guardrail must be violated for a justified reason, capture it in an ADR with:

* rationale
* mitigation
* rollback plan
* success metrics

---

## 11) How to apply this charter per module

Every module under `modules/` must document:

* API surface + request/response shapes
* domain contracts (states, invariants, events)
* runtime flows (sync/async boundaries + failure modes)
* errors/status codes (taxonomy + anti-enumeration where needed)
* security & abuse controls
* idempotency/consistency expectations
* observability signals (SLIs)
* dependencies & ownership rules
* open questions tracked as ADRs
