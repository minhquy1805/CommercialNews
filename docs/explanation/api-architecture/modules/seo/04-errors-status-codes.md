# SEO — Errors & Status Codes (V1)

## 1) Standard error envelope

See: `../../02-contracts-and-standards.md`

All SEO APIs return errors using the standard API error envelope.

SEO errors must avoid leaking unsafe visibility information on public routing paths.

---

## 2) Status code mapping

### 2.1 Public routing APIs

Applies to:

- `GET /api/v1/seo/resolve`
- `GET /api/v1/seo/metadata`

| Status | Meaning |
|---|---|
| `200 OK` | Successful route or metadata read. |
| `400 Bad Request` | Invalid query shape, invalid scope, invalid slug, invalid resource type, or malformed input. |
| `404 Not Found` | Safe not-found. Used when slug/resource metadata is missing or must not be exposed. Public responses must not leak whether the underlying content exists but is non-public. |
| `429 Too Many Requests` | Rate limited or abuse protection triggered. |
| `500 Internal Server Error` | Unexpected server-side failure. |
| `503 Service Unavailable` | SEO truth store or required dependency is temporarily unavailable and no safe fallback can serve the request. |

Public routing APIs must prefer safe `404` or safe deny over incorrect public exposure.

---

### 2.2 Admin SEO APIs

Applies to:

- `GET /api/v1/admin/seo/articles/{articlePublicId}`
- `PUT /api/v1/admin/seo/articles/{articlePublicId}`
- optional utility endpoints such as `/generate-slug` and `/slug-availability`

| Status | Meaning |
|---|---|
| `200 OK` | Successful admin read, update, slug generation, or availability check. |
| `400 Bad Request` | Invalid request shape, invalid slug format, invalid scope, invalid canonical URL format, invalid metadata length, invalid resource type. |
| `401 Unauthorized` | Missing, invalid, or expired authentication. |
| `403 Forbidden` | Authenticated caller lacks the required SEO permission. |
| `404 Not Found` | Target article/resource or SEO metadata was not found where the admin operation requires it. |
| `409 Conflict` | Slug already belongs to another resource in the same scope, or another SEO-owned uniqueness invariant is violated. |
| `412 Precondition Failed` | Stale version, `If-Match` mismatch, row version mismatch, or compare-and-set failure. |
| `422 Unprocessable Entity` | Request is syntactically valid but violates a semantic SEO rule, if the project chooses to distinguish semantic validation from `400`. |
| `429 Too Many Requests` | Admin anomaly protection, repeated utility calls, or rate-limit policy. |
| `500 Internal Server Error` | Unexpected server-side failure. |
| `503 Service Unavailable` | Required dependency unavailable and operation cannot complete safely. |

---

## 3) Important status code rules

### 3.1 `404` on public routing must be safe

Public slug resolution must not reveal sensitive publication state.

For public APIs, `404` may mean:

- slug does not exist
- slug exists but route is inactive
- target content is not publicly visible
- SEO cannot safely confirm route visibility

The public response must not distinguish these cases in a dangerous way.

---

### 3.2 `409 Conflict` is for ownership conflicts

Use `409 Conflict` when the requested operation conflicts with current SEO truth.

Examples:

- slug already exists in the same scope
- slug belongs to another `ResourceType + ResourcePublicId`
- a unique SEO route/metadata ownership invariant is violated

Example error code:

- `SEO.SLUG_CONFLICT`

---

### 3.3 `412 Precondition Failed` is for stale writes

Use `412 Precondition Failed` when the client attempts to update SEO truth based on an old version.

Examples:

- `If-Match` does not match current SEO version
- request version is older than current row version
- compare-and-set update fails due to concurrent modification

Example error codes:

- `SEO.STALE_VERSION`
- `SEO.PRECONDITION_FAILED`

---

### 3.4 Async downstream failure must not change committed sync success

If an admin SEO write commits successfully, downstream failures must not turn the API response into failure.

Examples of post-commit async effects:

- cache invalidation
- sitemap refresh
- search/index refresh
- audit ingestion
- downstream SEO projection update

If SEO truth commit succeeds but downstream async work later fails, the original API response remains successful. The failure must be handled through outbox retry, consumer retry, DLQ/dead state, observability, or reconciliation.

---

### 3.5 Utility endpoint results are advisory

For:

- `POST /generate-slug`
- `GET /slug-availability`

A `200 OK` response does not reserve slug ownership.

Final slug ownership is determined only by a successful SEO truth-bound write commit.

---

## 4) Error codes

### 4.1 Input validation

- `SEO.INVALID_SCOPE`
- `SEO.INVALID_SLUG`
- `SEO.INVALID_RESOURCE_TYPE`
- `SEO.INVALID_RESOURCE_PUBLIC_ID`
- `SEO.INVALID_CANONICAL_URL`
- `SEO.INVALID_METADATA_LENGTH`
- `SEO.INVALID_OG_IMAGE_URL`
- `SEO.INVALID_ROBOTS_DIRECTIVE`

### 4.2 Not found / safe deny

- `SEO.SLUG_NOT_FOUND`
- `SEO.ROUTE_NOT_FOUND`
- `SEO.METADATA_NOT_FOUND`
- `SEO.RESOURCE_NOT_FOUND`
- `SEO.SAFE_NOT_FOUND`

For public APIs, prefer a generic safe not-found code if detailed distinction could leak visibility information.

### 4.3 Conflict / ownership

- `SEO.SLUG_CONFLICT`
- `SEO.ROUTE_OWNERSHIP_CONFLICT`
- `SEO.METADATA_OWNERSHIP_CONFLICT`
- `SEO.CANONICAL_RULE_VIOLATION`

### 4.4 Concurrency

- `SEO.STALE_VERSION`
- `SEO.PRECONDITION_FAILED`
- `SEO.VERSION_MISMATCH`
- `SEO.CONCURRENT_MODIFICATION`

### 4.5 Manual override / policy

- `SEO.MANUAL_OVERRIDE_PROTECTED`
- `SEO.AUTO_SYNC_NOT_ALLOWED`
- `SEO.SLUG_CHANGE_NOT_ALLOWED`
- `SEO.ROUTE_DEACTIVATION_NOT_ALLOWED`

### 4.6 Async / derived-state processing

These are usually not returned directly by public/admin write APIs after truth commit. They are used in logs, worker errors, DLQ/dead state, reconciliation reports, or admin diagnostics.

- `SEO.EVENT_DUPLICATE_IGNORED`
- `SEO.EVENT_STALE_IGNORED`
- `SEO.EVENT_VERSION_GAP_DETECTED`
- `SEO.EVENT_RESYNC_REQUIRED`
- `SEO.PROJECTION_APPLY_FAILED`
- `SEO.REBUILD_FAILED`
- `SEO.RECONCILIATION_MISMATCH`

### 4.7 Infrastructure / dependency

- `SEO.STORE_UNAVAILABLE`
- `SEO.CACHE_UNAVAILABLE`
- `SEO.OUTBOX_WRITE_FAILED`
- `SEO.UNEXPECTED_ERROR`

---

## 5) Recommended endpoint-specific behavior

### `GET /api/v1/seo/resolve`

Possible responses:

- `200 OK`
- `400 Bad Request`
- `404 Not Found`
- `429 Too Many Requests`
- `500 Internal Server Error`
- `503 Service Unavailable`

Recommended safe public errors:

- `SEO.INVALID_SCOPE`
- `SEO.INVALID_SLUG`
- `SEO.SAFE_NOT_FOUND`
- `SEO.STORE_UNAVAILABLE`

---

### `GET /api/v1/seo/metadata`

Possible responses:

- `200 OK`
- `400 Bad Request`
- `404 Not Found`
- `429 Too Many Requests`
- `500 Internal Server Error`
- `503 Service Unavailable`

Recommended errors:

- `SEO.INVALID_RESOURCE_TYPE`
- `SEO.INVALID_RESOURCE_PUBLIC_ID`
- `SEO.METADATA_NOT_FOUND`
- `SEO.SAFE_NOT_FOUND`

---

### `GET /api/v1/admin/seo/articles/{articlePublicId}`

Possible responses:

- `200 OK`
- `400 Bad Request`
- `401 Unauthorized`
- `403 Forbidden`
- `404 Not Found`
- `500 Internal Server Error`
- `503 Service Unavailable`

Recommended errors:

- `SEO.INVALID_RESOURCE_PUBLIC_ID`
- `SEO.RESOURCE_NOT_FOUND`
- `SEO.METADATA_NOT_FOUND`
- `SEO.STORE_UNAVAILABLE`

---

### `PUT /api/v1/admin/seo/articles/{articlePublicId}`

Possible responses:

- `200 OK`
- `400 Bad Request`
- `401 Unauthorized`
- `403 Forbidden`
- `404 Not Found`
- `409 Conflict`
- `412 Precondition Failed`
- `422 Unprocessable Entity`
- `429 Too Many Requests`
- `500 Internal Server Error`
- `503 Service Unavailable`

Recommended errors:

- `SEO.INVALID_SCOPE`
- `SEO.INVALID_SLUG`
- `SEO.INVALID_CANONICAL_URL`
- `SEO.INVALID_METADATA_LENGTH`
- `SEO.RESOURCE_NOT_FOUND`
- `SEO.SLUG_CONFLICT`
- `SEO.STALE_VERSION`
- `SEO.MANUAL_OVERRIDE_PROTECTED`
- `SEO.OUTBOX_WRITE_FAILED`
- `SEO.STORE_UNAVAILABLE`

---

### `POST /api/v1/admin/seo/generate-slug`

Possible responses:

- `200 OK`
- `400 Bad Request`
- `401 Unauthorized`
- `403 Forbidden`
- `429 Too Many Requests`
- `500 Internal Server Error`

Recommended errors:

- `SEO.INVALID_SCOPE`
- `SEO.INVALID_SLUG`
- `SEO.INVALID_METADATA_LENGTH`

This endpoint is advisory and does not reserve ownership.

---

### `GET /api/v1/admin/seo/slug-availability`

Possible responses:

- `200 OK`
- `400 Bad Request`
- `401 Unauthorized`
- `403 Forbidden`
- `429 Too Many Requests`
- `500 Internal Server Error`
- `503 Service Unavailable`

Recommended errors:

- `SEO.INVALID_SCOPE`
- `SEO.INVALID_SLUG`
- `SEO.INVALID_RESOURCE_TYPE`
- `SEO.INVALID_RESOURCE_PUBLIC_ID`

This endpoint is advisory and does not reserve ownership.

---

## 6) Retry and timeout posture

Timeouts must be treated as ambiguous.

For admin writes:

- if truth commit fails or cannot be confirmed, return a bounded failure according to the standard error policy
- if truth commit succeeds, downstream async failures must not change the response
- clients may retry only with idempotency protection or after truth-state reconciliation

For async consumers:

- duplicate messages must be deduped by `MessageId`
- stale events must be ignored or trigger resync
- version gaps must trigger retry, defer, reconciliation, or bounded rebuild