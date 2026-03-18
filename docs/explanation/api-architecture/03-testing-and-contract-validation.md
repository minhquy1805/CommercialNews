# Testing & Contract Validation — CommercialNews (V1)

This document applies Chapter 2 (*Testing Strategies*) from *Mastering API Architecture* to CommercialNews.
Testing is treated as an **architecture decision**: we choose test types to protect contracts, governance boundaries, and runtime behavior under real workloads.

Related:
- Runtime scenarios: `../architecture/arc42/04-runtime-view-v1.md`
- Quality requirements: `../architecture/arc42/05-quality-requirements.md`
- Measurement guide: `../architecture/arc42/06-measurement-guide.md`
- Governance/fitness functions: `../architecture/arc42/07-architecture-governance.md`

---

## 1) Goals (what testing must prove in V1)

Testing must protect these outcomes:

1) **Contract stability**  
   - prevent accidental breaking changes for public/admin APIs
   - prevent breaking event consumers (Worker)

2) **Governance correctness**  
   - admin endpoints enforce authorization policies
   - publish/unpublish and role/permission changes are auditable

3) **Read-path protection**  
   - interaction/audit/notifications never block reading
   - degrade gracefully when non-critical subsystems fail

4) **Async workflow integrity**  
   - consumers are idempotent (at-least-once tolerant)
   - retries do not cause duplicates (email, audit entries, counters)

---

## 2) Test pyramid for CommercialNews (V1)

V1 uses a pragmatic mix:

- **Unit tests** (many): validate invariants and business rules cheaply
- **Contract tests** (mandatory): prevent breaking consumers
- **Component tests** (many): API behavior as a black box with isolated deps
- **Integration tests** (few but critical): boundary correctness with real infra
- **E2E tests** (very few): core journeys only

**Rule:** do not “E2E everything”. E2E is expensive and fragile.

---

## 3) Test types and what they validate

### 3.1 Unit tests (foundation)
**Purpose:** validate logic and invariants inside modules.

Must cover:
- Content lifecycle transitions (Draft → Published → Archived; Unpublish policy)
- SEO slug uniqueness/stability rules (policy behavior)
- Media primary/ordering rules (0..1 primary; deterministic reorder)
- Interaction like/unlike idempotency logic (per user/article)
- Identity token rules (verification gating, rotation/revocation triggers)

Guidelines:
- test validation and mapping edge cases
- avoid over-mocking (do not write tests that only assert mock interactions)

---

### 3.2 Contract tests (contracts are the “source of truth”)
CommercialNews uses two contract surfaces:

#### A) HTTP API contracts (Public + Admin)
**Contract artifact:** OpenAPI (source of truth).

Contract testing MUST:
- validate request/response schemas and examples
- enforce standardized errors (envelope + error codes)
- detect breaking changes via OpenAPI diff

**When a PR changes an endpoint:**
- OpenAPI must change in the same PR
- the diff check must pass or the PR must include a versioning plan

#### B) Event contracts (API → Worker)
**Contract artifact:** event envelope + payload schemas.

Contract testing MUST:
- enforce the envelope fields (EventId/OccurredAt/CorrelationId/EventType/Version/Payload)
- validate payload schema compatibility (additive changes only in V1)
- ensure consumer handlers tolerate retries (idempotency)

---

### 3.3 Component tests (API as a black box, isolated dependencies)
**Purpose:** validate endpoint behavior end-to-end inside the API component, without real infra.

Scope:
- routing, controllers, serialization
- authn/authz middleware and policy enforcement
- validation errors and error envelope
- idempotency behavior (where supported)
- response shapes (paging, sorting, filtering)

Dependencies are mocked:
- DB repositories
- message broker/outbox publisher
- email provider clients

**Why it matters:** component tests catch contract regressions and “boundary mistakes” cheaply and reliably in CI.

---

### 3.4 Integration tests (critical boundaries with real infra)
**Purpose:** validate correctness at real boundaries that tend to fail in production.

V1 recommended integration targets:
- **Database constraints/index behavior** for hot paths:
  - SEO slug uniqueness enforcement
  - Content state transition persistence correctness
- **Message broker + consumer idempotency**
  - publish event → consumer processes → duplicate delivery does not duplicate side effects
- **Outbox/Inbox patterns** (if implemented)
  - event persistence + dispatch reliability

Guidelines:
- keep the integration suite small and stable
- run via reproducible infra (containers) when possible
- never depend on shared external environments in CI

---

### 3.5 End-to-End tests (few, high-value journeys only)
**Purpose:** validate that the system works as users experience it, with realistic auth and configs.

Core journeys (V1 minimal set):
1) **Admin publish journey**  
   Create draft → publish → public read sees it; audit event ingested (eventual consistency allowed).
2) **Unpublish governance journey**  
   Unpublish with reason → public read must not expose; audit records reason.
3) **Public read journey**  
   List → open by slug → view tracking does not block; degrade gracefully if Interaction is down.
4) **Identity onboarding journey**  
   Register → verification email requested (async) → verify email → login/refresh.
5) **Account recovery journey**  
   Forgot password (anti-enumeration) → reset password with token (time-bound, single-use by policy).
6) **Authorization governance journey**  
   Assign role/permission → policy enforced → audit entry is recorded.

Guidelines:
- E2E should run with TLS/auth settings close to production
- keep E2E minimal; do not turn E2E into a replacement for unit/component tests

---

## 4) CI/CD guardrails (fitness functions as tests)

CommercialNews uses automated guardrails to prevent drift (arc42 governance).

### 4.1 Build-time (CI) gates (V1 starter set)
1) **No cyclic dependencies** between modules  
2) **No boundary violations** (respect ownership/dependency rules)  
3) **Authorization policy coverage** for admin endpoints remains 100%  
4) **OpenAPI breaking change detection** (fail on breaking diffs)  
5) **Safe logging checks** (no token/PII patterns)  

Start with report-only where needed, then promote to hard gates (progressive enforcement).

### 4.2 Deploy-time and runtime
Testing expands in production through:
- canary rollout gates (Chapter 5)
- synthetic checks for read endpoints
- monitoring-based validation for async backlog/lag and idempotency anomalies

---

## 5) Mapping tests to runtime scenarios (arc42/04)

This section ensures tests protect the exact scenarios we care about.

### Scenario 1 — Publish
- Unit: lifecycle invariants
- Component: `/admin/articles/{id}:publish` policy + response + error envelope
- Integration: event emission + consumer idempotency
- E2E: publish → public read sees → audit ingested (eventual)

### Scenario 3 — Public read by slug
- Component: list/detail response shapes + filtering
- Integration: SEO slug uniqueness and lookup performance characteristics
- E2E: open by slug; Interaction failure does not block reading

### Scenario 4–5 — Identity flows
- Component: anti-enumeration responses + rate-limit behavior (where testable)
- Integration: refresh token rotation/reuse detection logic (as implemented)
- E2E: register→verify→login→refresh; forgot→reset

### Scenario 6 — Role/permission changes
- Component: policy enforcement and audit emission
- Integration/E2E: audit event ingestion is reliable and observable

---

## 6) Contract evolution rules (tests enforce them)

### 6.1 HTTP APIs
Allowed (compatible) in V1:
- add new endpoints
- add optional fields
Not allowed without versioning plan:
- remove/rename fields
- optional → required
- response shape changes

### 6.2 Events
Allowed (compatible) in V1:
- add optional fields to payloads
Not allowed without versioning plan:
- change meaning of existing fields
- remove fields consumers rely on
- change event type semantics without bumping `Version`

---

## 7) Output artifacts (what the repo should contain)

Minimum artifacts for V1:
- OpenAPI spec(s) for Public/Admin APIs
- event schema documentation (envelope + payloads per event type)
- contract diff in CI (breaking change detection)
- test suites organized by type (unit/component/integration/E2E)

---

## 8) Practical guidelines (avoid common traps)

- Do not over-mock: tests should validate behavior, not mock call order.
- Keep integration suite small; prefer deterministic infra.
- E2E is for confidence, not for coverage.
- Production still wins: complement tests with observability, safe rollouts, and runbooks.