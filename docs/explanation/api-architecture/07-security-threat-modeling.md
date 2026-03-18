# Security Threat Modeling — CommercialNews (V1)

This document turns Chapter 6 (*Threat Modeling*) from *Mastering API Architecture* into a **repeatable security process** for CommercialNews.

Threat modeling is not paperwork. It is how we:
- design security controls intentionally
- prioritize security work by risk
- validate that controls work (tests + telemetry + audit)

Related:
- Constraints: `../architecture/arc42/02-constraints.md`
- Runtime scenarios: `../architecture/arc42/04-runtime-view-v1.md`
- Quality requirements: `../architecture/arc42/05-quality-requirements.md`
- Measurement guide: `../architecture/arc42/06-measurement-guide.md`
- Governance: `../architecture/arc42/07-architecture-governance.md`
- AuthN/AuthZ: `08-authentication-and-authorization.md`
- Edge/gateway: `05-edge-gateway-and-traffic-management.md`

---

## 1) Scope and objectives (V1)

### 1.1 What we are protecting
- Accounts and sessions (Identity)
- Admin workflows (publish/unpublish, role/permission changes)
- Public read path integrity (no draft/unpublished exposure)
- Sensitive data (tokens, PII)
- Async pipelines (audit/notifications/interaction aggregation)

### 1.2 V1 security objectives (must-have)
1) Prevent **Broken Auth** and session abuse (token handling, rotation policy)
2) Prevent **BOLA/BFLA** on admin and user-scoped resources
3) Prevent **excessive data exposure** (public read must be safe)
4) Prevent **abuse** on sensitive endpoints (register/login/resend/forgot/reset, comments)
5) Ensure **auditability** for governance actions
6) Ensure **safe logging** (no tokens/PII in logs/audit/events)

---

## 2) Threat modeling process (repeatable 6-step loop)

CommercialNews uses a 6-step process:

1) **Objectives** (what matters most)
2) **Gather information** (architecture, endpoints, data, trust boundaries)
3) **Decompose with DFD** (data flows + boundaries)
4) **Identify threats** (STRIDE + OWASP API Top 10)
5) **Evaluate risks** (DREAD scoring)
6) **Validate** (tests + monitors + operational checks)

**Rule:** Threat modeling is revisited when:
- adding a new module/capability
- changing auth/token/session strategy
- exposing a new public endpoint
- adding a new async workflow or consumer
- changing gateway/routing/security posture

---

## 3) CommercialNews DFDs (V1)

### 3.1 DFD A — Public/API path (north–south)
**Flow:**
Client → Edge Gateway/Ingress → Public API Component → DB (shared, owned schemas) → Response

**Trust boundaries:**
- Internet boundary (client ↔ edge)
- Edge ↔ API component boundary
- API ↔ DB boundary

**High-risk hotspots:**
- authentication endpoints
- admin endpoints
- SEO slug resolve (public entry point)
- public read list/detail (exposure risk)

### 3.2 DFD B — Async path (side effects)
**Flow:**
API Component → Event Publish (outbox/broker) → Worker → (DB / Email provider)

**Trust boundaries:**
- API ↔ broker
- broker ↔ worker
- worker ↔ external email provider

**High-risk hotspots:**
- duplicate delivery (at-least-once)
- idempotency failures (duplicate emails/audit entries)
- secret leakage in payloads/logs/templates
- backlog growth (DoS by queue pressure)

---

## 4) STRIDE checklist (applied to CommercialNews)

Use STRIDE per element (edge, API, DB, broker, worker, email provider):

### S — Spoofing identity
Risks:
- stolen tokens, session fixation, fake service identity
Controls:
- strict token validation (`iss`, `aud`, `exp`)
- refresh token rotation + revocation triggers
- do not trust headers from the Internet (only from trusted proxy)

### T — Tampering
Risks:
- request tampering, payload manipulation, event tampering
Controls:
- input validation and DTO allowlists (prevent mass assignment)
- signing/secure transport (TLS), broker auth
- least-privilege DB permissions

### R — Repudiation
Risks:
- admin denies publish/unpublish or role changes
Controls:
- audit events for governance actions
- correlationId propagation across publish → event → consumer
- immutable append-only audit store (policy)

### I — Information disclosure
Risks:
- draft/unpublished content exposure
- tokens/PII in logs/events/templates
Controls:
- read path must filter by publication state at source-of-truth
- safe logging/redaction rules
- minimize event payloads

### D — Denial of service
Risks:
- login/register spam, comment spam
- hot article traffic spikes
- queue backlog explosion
Controls:
- rate limiting (edge + API)
- bounded timeouts, circuit breakers where appropriate
- async isolation for non-critical workflows

### E — Elevation of privilege
Risks:
- missing policy checks on admin endpoints
- BFLA on action endpoints (`:publish`, role assignment)
Controls:
- explicit authorization policies for 100% admin endpoints
- centralized policy enforcement (Authorization module)
- deny-by-default posture for admin

---

## 5) OWASP API Security Top 10 mapping (CommercialNews focus)

This is a V1 practical mapping (not exhaustive). The goal is to avoid common API failures.

### A) Broken Object Level Authorization (BOLA)
Where:
- admin publish/unpublish
- edit/delete comments
- media attach/reorder/set primary
Controls:
- policy checks + object-level checks (resource ownership/status)
- do not rely on client-provided IDs without authorization

### B) Broken Authentication
Where:
- login/refresh/reset/verify
Controls:
- strong password hashing
- refresh rotation + reuse detection policy
- rate limiting on sensitive flows
- never log tokens/PII

### C) Excessive Data Exposure
Where:
- public article detail/list
- admin endpoints returning too much user info
Controls:
- DTOs designed for “minimum necessary data”
- explicit field exposure rules per endpoint
- strict publication-state filtering

### D) Lack of Resources & Rate Limiting
Where:
- register/login/resend/forgot/reset
- comments/views
Controls:
- rate limit and backoff
- payload size limits at edge
- pagination limits and bounded queries

### E) Mass Assignment
Where:
- create/update endpoints (articles, profile, media metadata)
Controls:
- DTO allowlists; ignore unknown fields
- server-side ownership of sensitive fields (status, authorId, timestamps)

### F) Injection
Where:
- search/filtering
- any dynamic queries
Controls:
- parameterized queries
- allowlist filter/sort fields (no free-form “where” in public API)

### G) Insufficient Logging & Monitoring
Where:
- governance actions
- auth failures and anomalies
- async failures/backlog
Controls:
- structured logs with correlation IDs
- metrics: success/failure, latency, backlog/lag, rate-limit triggers
- audit trail completeness checks

---

## 6) Risk scoring (DREAD) — how we prioritize

CommercialNews uses DREAD to rank threats:
- **Damage**
- **Reproducibility**
- **Exploitability**
- **Affected users**
- **Discoverability**

### V1 “top threats” to score first
Start by scoring these (typical high-impact set):
1) Missing authorization on admin action endpoints
2) Draft/unpublished exposure in public read path
3) Refresh token misuse (no rotation/reuse detection)
4) Token/PII leakage in logs/audit/events/templates
5) Rate-limit gaps on auth flows and comments
6) Duplicate email/audit entries due to non-idempotent consumers
7) Search/filter injection via dynamic query exposure

**Output:** a prioritized backlog with “mitigation + validation” per threat.

---

## 7) Control placement (gateway vs service vs worker)

### 7.1 What the gateway helps with (baseline)
- TLS termination
- coarse rate limiting and payload limits
- edge logs/metrics
- routing and canary/mirroring

### 7.2 What must be done in the API service (non-negotiable)
- fine-grained authorization (BOLA/BFLA)
- validation and DTO allowlists (mass assignment defense)
- safe responses (no excessive data exposure)
- safe logging/redaction enforcement

### 7.3 What must be done in the worker
- idempotent handlers (at-least-once tolerant)
- safe email templates (no token leakage)
- retries + DLQ + backlog monitoring

**Rule:** the gateway is not “security done”. It is layer 1.

---

## 8) Validation pack (turn threats into proof)

Every high-risk threat must have a validation plan across:
- **tests**
- **telemetry**
- **operational checks**

### 8.1 Test checklist (V1)
- unit tests for invariants (lifecycle, slug uniqueness rules, primary media)
- component tests for:
  - 401/403 behavior
  - missing policy coverage
  - anti-enumeration responses
- integration tests for:
  - idempotent consumers (duplicate delivery does not duplicate side effects)
  - DB uniqueness constraints (slug)
- E2E tests for core journeys (publish/read, register/verify, forgot/reset)

See: `03-testing-and-contract-validation.md`

### 8.2 Telemetry checklist (V1)
Must measure:
- auth success/failure + rate-limit triggers
- read path P95/P99 + error rates
- admin actions success/failure + policy denials
- worker handler success/failure + backlog/lag + DLQ growth
- audit ingestion completeness signals

See: `09-observability-and-slos.md` and arc42 measurement guide.

### 8.3 Operational checklist (V1)
- secrets not present in logs (periodic scans)
- restore/backup drills (RTO/RPO tracking)
- incident playbooks for auth spike, backlog spike, gateway config regression

---

## 9) Threat modeling outputs (what to produce)

For each module or major change, produce:
1) DFD (public + async path if applicable)
2) STRIDE threats list
3) OWASP mapping
4) DREAD scoring table (top threats)
5) Mitigation plan (controls + owners)
6) Validation pack (tests + metrics + runbook notes)

Store outputs under:
- `docs/explanation/decisions/` (ADRs)
- module docs under `api-architecture/modules/{module}/09-open-questions.md` (if not fully decided)

---

## 10) V1 “starter packet” (recommended minimal packet)

When starting a new module, create a short packet containing:
- Objectives (5–8 bullet points)
- Two DFDs (public + async)
- Top 10 threats list (STRIDE/OWASP)
- DREAD scoring for top 3–5
- Validation plan (tests + telemetry)

This keeps threat modeling lightweight and repeatable for a small team.