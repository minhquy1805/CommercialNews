# Content — Errors & Status Codes (V1)

## 1) Standard error envelope

Content follows the standard error envelope defined in:

- `../../02-contracts-and-standards.md`

All failures should return:

```json
{
  "traceId": "string",
  "error": {
    "code": "CONTENT.SOME_ERROR",
    "message": "Human-friendly message",
    "details": []
  }
}
```

Rules:

- `traceId` must always be included for failures
- `code` must be stable and machine-readable
- `message` must be safe for clients and must not leak sensitive internals
- `details` is optional and is primarily used for validation failures

## 2) Status code mapping

### 2.1 Success codes

#### 200 OK

Used for:

- successful reads
- successful updates
- successful lifecycle actions
- successful idempotent no-op operations when documented as stable success

#### 201 Created

Used for:

- draft/article creation
- category creation
- tag creation

### 2.2 Client error codes

#### 400 Bad Request

Used for:

- malformed request
- validation failures
- missing mandatory fields
- unsupported sort field
- illegal request shape
- invalid lifecycle action input where treated as request error

#### 401 Unauthorized

Used for:

- missing bearer token
- invalid token
- expired token
- authentication failure before policy evaluation

#### 403 Forbidden

Used for:

- authenticated caller lacks required policy/permission
- content action denied by authorization policy
- object-level content action denied when combined with centralized authorization

#### 404 Not Found

Used for:

- article not found
- category not found
- tag not found
- revision not found
- safe resource-not-found semantics on admin surfaces

#### 409 Conflict

Used for:

- invalid lifecycle transition caused by current truth state
- article/category/tag uniqueness conflict where owned by Content
- optimistic concurrency conflict
- deterministic taxonomy/content invariant conflict

#### 429 Too Many Requests

Used for:

- rate-limited requests
- retry storm or abuse-protection trigger

### 2.3 Server / dependency failure codes

#### 500 Internal Server Error

Used for:

- unexpected server-side failure

#### 503 Service Unavailable

Used for:

- required authoritative truth path unavailable

**Rule:** downstream async failure in audit, SEO, notifications, caches, projections, or search must not turn a committed Content write into `5xx`.

## 3) Core rule: truth success vs downstream side effects

For content-changing operations, API success means:

- Content truth committed successfully

Where applicable, it also means:

- async intent/outbox committed successfully

API success does not guarantee that:

- audit is already queryable
- SEO is already updated
- notification delivery has completed
- public/read projections are already refreshed
- caches are already invalidated
- search/index artifacts are already current

Async side-effect delay or retry must not turn a committed Content write into `5xx`.

## 4) Error code catalog (V1 baseline)

### 4.1 Generic / validation errors

- `CONTENT.VALIDATION_FAILED`
- `CONTENT.INVALID_REQUEST`
- `CONTENT.INVALID_SORT_FIELD`
- `CONTENT.CONCURRENCY_CONFLICT`

### 4.2 Article errors

- `CONTENT.ARTICLE_NOT_FOUND`
- `CONTENT.INVALID_STATE_TRANSITION`
- `CONTENT.ARTICLE_NOT_PUBLISHABLE`
- `CONTENT.ARTICLE_ALREADY_PUBLISHED`
- `CONTENT.ARTICLE_ALREADY_DRAFT`
- `CONTENT.ARTICLE_ALREADY_ARCHIVED`
- `CONTENT.ARTICLE_ALREADY_SOFT_DELETED`
- `CONTENT.UNPUBLISH_REASON_REQUIRED`
- `CONTENT.REVISION_NOT_FOUND`

### 4.3 Taxonomy errors

- `CONTENT.CATEGORY_NOT_FOUND`
- `CONTENT.TAG_NOT_FOUND`
- `CONTENT.TAXONOMY_ORPHAN_REFERENCE`
- `CONTENT.CATEGORY_CONFLICT`
- `CONTENT.TAG_CONFLICT`

### 4.4 Policy / authorization-facing errors

- `CONTENT.POLICY_DENIED`
- `CONTENT.AUTHENTICATION_REQUIRED`

### 4.5 Write / commit errors

- `CONTENT.WRITE_COMMIT_FAILED`
- `CONTENT.OUTBOX_INTENT_COMMIT_FAILED`

These represent server-side failures where Content truth and required async intent cannot be committed atomically. Client-facing messages must remain safe, for example: "Content write could not be completed."

## 5) Conflict guidance

Use `409 Conflict` when:

- the request is structurally valid, but current Content truth blocks the lifecycle action
- uniqueness invariants are violated where owned by Content
- optimistic concurrency detects stale write
- deterministic taxonomy/content invariants reject the mutation

Examples:

- publish requested for an already published article
- unpublish requested for a Draft article
- archive requested for an Archived article
- soft-delete requested for an already soft-deleted article when not treated as idempotent success
- update submitted with stale version/etag
- category/tag uniqueness conflict

Use `400 Bad Request` when:

- the request itself is malformed or invalid
- required input fields are missing
- the request payload shape is illegal

Examples:

- missing unpublish reason
- invalid page/pageSize
- unsupported sort field
- malformed lifecycle request body

**Lifecycle distinction:** `400` means the request is missing or carries invalid data; `409` means the request is structurally valid but current Content truth does not allow the action.

Recommended examples:

- missing unpublish reason → `400 CONTENT.UNPUBLISH_REASON_REQUIRED`
- unpublish Draft article → `409 CONTENT.INVALID_STATE_TRANSITION` or `409 CONTENT.ARTICLE_ALREADY_DRAFT`
- publish already Published article → `409 CONTENT.ARTICLE_ALREADY_PUBLISHED`, unless treated as documented idempotent success
- soft-delete already `IsDeleted=1` → `200` idempotent success for equivalent repeated commands, or `409 CONTENT.ARTICLE_ALREADY_SOFT_DELETED` when policy requires a conflict

## 6) Validation error shape

For validation failures, use the standard validation pattern:

```json
{
  "traceId": "string",
  "error": {
    "code": "COMMON.VALIDATION_FAILED",
    "message": "Validation failed.",
    "details": [
      { "field": "title", "reason": "Title is required." },
      { "field": "reason", "reason": "Unpublish reason is required." }
    ]
  }
}
```

Typical validation cases:

- missing title / summary / body where required
- missing categoryId where required by policy
- missing unpublish reason
- invalid page or pageSize
- unsupported sort field
- illegal lifecycle action payload

## 7) Safe not-found and public exposure rule

Admin endpoints may return `404` normally under safe resource semantics.

Public query / public-read surfaces must not leak the existence of:

- drafts
- articles unpublished back to Draft
- archived content
- soft-deleted content

**Rule:** public visibility must follow Content truth; missing/non-public outcomes must be safe and non-leaking.

## 8) Suggested status mapping by operation

### Articles

- `POST /articles` → `201`
- `GET /articles` → `200`
- `GET /articles/{articleId}` → `200`
- `PUT /articles/{articleId}` → `200`
- `POST /articles/{articleId}:publish` → `200`
- `POST /articles/{articleId}:unpublish` → `200`
- `POST /articles/{articleId}:archive` → `200`
- `POST /articles/{articleId}:soft-delete` → `200`

Archived restore and physical purge are out of scope for V1.

### Revisions

- `GET /articles/{articleId}/revisions` → `200`
- `GET /articles/{articleId}/revisions/{revisionId}` → `200`

### Categories

- `GET /categories` → `200`
- `POST /categories` → `201`
- `PUT /categories/{categoryId}` → `200`
- `DELETE /categories/{categoryId}` → `200` or `204`, depending on documented delete policy

### Tags

- `GET /tags` → `200`
- `POST /tags` → `201`
- `PUT /tags/{tagId}` → `200`
- `DELETE /tags/{tagId}` → `200` or `204`, depending on documented delete policy

## 9) Operational rule for lifecycle-sensitive uncertainty

If Content state cannot be evaluated safely on a lifecycle-sensitive path because truth is unavailable:

- fail safely
- do not infer lifecycle success/failure from downstream projection visibility
- do not infer publish/unpublish/archive state from SEO, caches, or public read models

Recommended error codes:

- `CONTENT.CONCURRENCY_CONFLICT`
- `CONTENT.INVALID_STATE_TRANSITION`

or a documented module/system dependency failure code where authoritative truth is unavailable.

Status:

- usually `409` for lifecycle or freshness conflict
- `503` only when the authoritative truth path is unavailable and the request cannot be safely evaluated
