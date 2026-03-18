# Contracts & Standards — CommercialNews (V1)

This document defines the **API contract standards** for CommercialNews V1.
Goal: consistent contracts, low coupling, safe evolution, and production operability.

Derives from:
- Constraints: `../architecture/arc42/02-constraints.md`
- Quality requirements: `../architecture/arc42/05-quality-requirements.md`
- Measurement guide: `../architecture/arc42/06-measurement-guide.md`
- Governance: `../architecture/arc42/07-architecture-governance.md`

---

## 1) REST maturity target (pragmatic baseline)

CommercialNews targets **Richardson Level 2** as the practical default:
- resource-oriented URLs
- standard HTTP methods
- meaningful status codes
- consistent error model and response shapes

**Action endpoints** are allowed for explicit domain transitions (publish/unpublish/restore) where PATCH semantics are ambiguous and governance/audit is required.

---

## 2) API taxonomy and base paths

### 2.1 Public vs Admin separation
- Public: `/api/v1/...`
- Admin: `/api/v1/admin/...`

**Rule:** any endpoint that mutates governance-sensitive state MUST be under `/admin/` and MUST have explicit authorization policy enforcement.

### 2.2 Resource naming
- Use **plural nouns** for collections: `/articles`, `/categories`, `/tags`, `/media`
- Use stable IDs: `/articles/{articleId}`
- Use consistent case style for paths (**kebab-case recommended**)  
  Examples: `/forgot-password`, `/resend-verification`, `/role-permissions`

> If the project already uses a different style, keep it consistent everywhere. Do not mix styles.

### 2.3 Action endpoints (state transitions)
Use `:{action}` for explicit transitions:
- `POST /api/v1/admin/articles/{id}:publish`
- `POST /api/v1/admin/articles/{id}:unpublish`
- `POST /api/v1/admin/media/{id}:restore`
- `POST /api/v1/admin/articles/{id}:archive` (optional)

Rules:
- actions are verbs
- actions must document **idempotency** behavior
- actions must emit audit events (async)

---

## 3) HTTP method semantics (rules)

- `GET` — read-only, safe, cacheable (by policy)
- `POST` — create resources or execute actions (`:{action}`)
- `PUT` — replace/update resource where full update is appropriate
- `PATCH` — partial update when semantics are clear and stable
- `DELETE` — delete (prefer soft delete where policy requires)

**Rule:** avoid “RPC in disguise” endpoints like `/doThing` unless it is explicitly an action endpoint with clear domain semantics.

---

## 4) Standard headers and metadata

### 4.1 Correlation and tracing
- Client MAY send: `X-Correlation-Id`
- Server SHOULD echo `X-Correlation-Id` in response (if present)
- Errors MUST include `traceId` in the response body.

### 4.2 Content types
- Request/response: `application/json`
- File/media uploads: explicitly documented (multipart or pre-signed upload in future)

### 4.3 Time
- All timestamps are ISO-8601 UTC: `2026-03-02T10:30:00Z`

### 4.4 Idempotency key (recommended for risky endpoints)
- Header: `Idempotency-Key: <uuid>`
- Required (or strongly recommended) for:
  - `POST /auth/register`
  - `POST /auth/forgot-password`
  - `POST /auth/resend-verification`
  - governance actions where clients may retry (publish/unpublish)

---

## 5) IDs and representation rules

### 5.1 Stable ID contract
- `ArticleId`, `UserId`, `MediaId`, etc. are stable across modules.
- IDs are opaque to clients (no schema/meaning leakage).

### 5.2 Representation boundaries (reduce coupling)
- Public APIs must not expose internal DB schema concepts.
- Avoid exposing join tables or internal audit storage structures as public resources.
- Keep DTOs stable and versioned by policy (OpenAPI).

---

## 6) Request/response conventions

### 6.1 Standard list response
All list endpoints return:
```json
{
  "items": [],
  "pageInfo": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 123,
    "totalPages": 7
  }
}

6.2 Pagination rules

page is 1-based

pageSize has:

a default (documented)

a hard max (enforced, documented)

Sorting/filtering must not change paging correctness (no duplicate/missing items beyond expected updates).

6.3 Sorting rules

Query param: sort

sort=publishedAt (ascending)

sort=-publishedAt (descending)

Rule:

only allow documented sort fields

reject unknown sort fields with 400 and a clear error code

6.4 Filtering rules

Filtering is endpoint-specific and documented explicitly:

/api/v1/articles?categoryId=...&tagId=...

/api/v1/articles?status=Published (admin endpoints only, where applicable)

Rules:

do not provide “free-form where” filters in public APIs

document every filter param (type, allowed values, defaults)

6.5 Partial responses / field selection (V1 stance)

Out of scope in V1.

If needed in V2+, add fields= with strict allowlists.

7) Status codes (standard mapping)

200 OK — successful read/action

201 Created — resource created

204 No Content — successful delete with no body

400 Bad Request — validation errors, invalid tokens, illegal inputs

401 Unauthorized — missing/invalid access token

403 Forbidden — authenticated but not allowed (policy failure)

404 Not Found — resource not found (safe not-found; do not leak drafts)

409 Conflict — uniqueness conflicts / concurrency conflicts (admin contexts)

412 Precondition Failed — optimistic concurrency via ETag/If-Match (optional V1, recommended V2)

429 Too Many Requests — rate limited

500 / 503 — server failure / dependency failure

Rule: async side-effect failures (email/audit/aggregation) must not force 5xx on core flows. They are handled asynchronously and surfaced via observability/backlog.

8) Error model and taxonomy
8.1 Standard error envelope

All errors return:

{
  "traceId": "string",
  "error": {
    "code": "MODULE.ERROR_CODE",
    "message": "Human-friendly message",
    "details": []
  }
}
8.2 Error codes (naming)

Format:

IDENTITY.*

AUTHORIZATION.*

CONTENT.*

SEO.*

MEDIA.*

READING.*

INTERACTION.*

AUDIT.*

NOTIFICATIONS.*

Examples:

IDENTITY.INVALID_CREDENTIALS

IDENTITY.TOKEN_EXPIRED

CONTENT.INVALID_STATE_TRANSITION

SEO.SLUG_CONFLICT

MEDIA.PRIMARY_CONSTRAINT_VIOLATION

INTERACTION.RATE_LIMITED

AUTHORIZATION.POLICY_DENIED

8.3 Validation error details (recommended shape)

For input validation:

{
  "traceId": "string",
  "error": {
    "code": "COMMON.VALIDATION_FAILED",
    "message": "Validation failed.",
    "details": [
      { "field": "email", "reason": "Invalid format." },
      { "field": "password", "reason": "Too short." }
    ]
  }
}
8.4 Anti-enumeration rules (Identity)

Endpoints that must not leak account existence:

/api/v1/auth/forgot-password

/api/v1/auth/resend-verification

Always return:

200 { "accepted": true }

9) OpenAPI as contract (production posture)
9.1 OpenAPI is the source of truth

OpenAPI defines schemas, examples, and error shapes.

OpenAPI must be updated in the same PR as code changes.

9.2 CI checks (recommended baseline)

Breaking change detection on PR:

fail on field removals/renames

fail on optional → required

fail on response shape changes

Allow additive changes (new optional fields, new endpoints).

9.3 Choosing OpenAPI features (be deliberate)

Use OpenAPI to protect compatibility and developer experience:

examples for critical endpoints

schema validation

contract diffs

Avoid “spec complexity traps” in V1:

generating everything blindly without lifecycle control

treating generated SDKs as the only client path (unless you own the lifecycle)

9.4 REST vs gRPC specs (no “golden spec”)

If gRPC is introduced later:

proto evolves independently

do not auto-generate proto ↔ OpenAPI as the primary workflow

10) Read path caching stance (policy-level)

CommercialNews prioritizes read path performance and availability.
Caching is allowed but must be:

explicit (documented)

safe with publication state (never cache drafts/unpublished as public)

V1 stance:

caching policies are defined per endpoint in the Reading module docs

API must support graceful degradation when non-critical subsystems fail

11) Security and privacy contract rules (minimum necessary data)

Do not expose sensitive PII unnecessarily.

Do not return tokens/secrets in logs or error details.

Keep event payloads minimal to reduce leakage risk.

Admin endpoints must avoid “excessive data exposure” by returning only what’s necessary.

12) Glossary (V1 contract terms)

Public API: endpoints used by anonymous/authenticated end users for reading.

Admin API: endpoints used by admins/authors for governance-sensitive operations.

Governance boundary: actions requiring policy enforcement + audit (publish/unpublish, role changes).

Non-critical side effects: audit ingestion, notifications, interaction aggregation (async).

Seam: a boundary that enables safe evolution without breaking consumers.