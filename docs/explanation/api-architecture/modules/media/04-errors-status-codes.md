# Media — Errors & Status Codes (V1)

## 1) Standard error envelope

See: `../../02-contracts-and-standards.md`

All Media errors must use the standard error envelope.

Error responses should include:

- `code`
- `message`
- `traceId` / `correlationId`
- optional `details`
- optional `fieldErrors`

Rules:

- Do not expose storage secrets, internal paths, signed URLs, provider credentials, or raw SQL errors.
- Validation messages must be safe for admin clients.
- Timeout ambiguity must not be presented as proof that the operation failed or did nothing.
- Async side-effect failures after Media truth commit must not change the original Media API success response.

---

## 2) Success status codes

### 2.1 Media objects

| Endpoint | Success |
|---|---:|
| `POST /items` | `201 Created` |
| `GET /items` | `200 OK` |
| `GET /items/{mediaId}` | `200 OK` |
| `PATCH /items/{mediaId}` | `200 OK` |
| `DELETE /items/{mediaId}` | `200 OK` |
| `POST /items/{mediaId}:restore` | `200 OK` |

### 2.2 Article attachments

| Endpoint | Success |
|---|---:|
| `POST /articles/{articleId}/attachments` | `200 OK` |
| `DELETE /articles/{articleId}/attachments/{mediaId}` | `200 OK` |
| `POST /articles/{articleId}/attachments:reorder` | `200 OK` |
| `POST /articles/{articleId}/attachments:set-primary` | `200 OK` |

Success means:

- Media truth committed
- and, if the command emits async side effects, Outbox intent committed

Success does not mean:

- Outbox has already been published
- Audit has already consumed the event
- Reading/SEO/CDN/cache/variant workflows completed
- binary delivery or derivatives are available

---

## 3) Status code mapping

### 3.1 `400 Bad Request`

Use for syntactic or semantic validation failures where the request is invalid before applying domain state.

Examples:

- invalid JSON
- missing required field
- invalid `mediaId` format
- invalid `articleId` format
- unsupported `type`
- unsafe metadata field
- unsafe `altText` / caption value
- invalid reorder list shape
- reorder list contains duplicates
- reorder list does not match current active attachment set
- missing `expectedVersion` for reorder
- missing `expectedVersion` for set-primary
- attempt to change immutable storage identity fields through `PATCH`

Example codes:

- `MEDIA.VALIDATION_FAILED`
- `MEDIA.INVALID_MEDIA_ID`
- `MEDIA.INVALID_ARTICLE_ID`
- `MEDIA.TYPE_NOT_ALLOWED`
- `MEDIA.METADATA_UNSAFE`
- `MEDIA.ALT_TEXT_UNSAFE`
- `MEDIA.INVALID_REORDER_LIST`
- `MEDIA.EXPECTED_VERSION_REQUIRED`
- `MEDIA.IMMUTABLE_FIELD_UPDATE_NOT_ALLOWED`

---

### 3.2 `401 Unauthorized`

Use when the caller is not authenticated.

Example codes:

- `AUTH.UNAUTHENTICATED`

---

### 3.3 `403 Forbidden`

Use when the caller is authenticated but lacks the required Media permission/policy.

Examples:

- cannot register media
- cannot update media
- cannot delete/restore media
- cannot attach/detach media to articles
- cannot reorder attachments
- cannot set primary media

Example codes:

- `AUTHORIZATION.POLICY_DENIED`
- `MEDIA.PERMISSION_DENIED`

---

### 3.4 `404 Not Found`

Use when the requested resource does not exist or is not visible to the caller by policy.

Examples:

- media item not found
- article not found by allowed read-only validation
- attachment not found where the operation requires an existing attachment

Example codes:

- `MEDIA.MEDIA_NOT_FOUND`
- `MEDIA.ARTICLE_NOT_FOUND`
- `MEDIA.ATTACHMENT_NOT_FOUND`

Policy note:

- For admin endpoints, returning `404` for missing article/media is acceptable.
- Do not expose unrelated module internals in error details.

---

### 3.5 `409 Conflict`

Use when the request is syntactically valid but conflicts with current Media truth or concurrency rules.

Examples:

- media is deleted and cannot be attached
- selected media is not attached and cannot be set as primary
- duplicate active attachment
- primary invariant conflict
- `expectedVersion` mismatch
- restore window expired
- restore would violate current policy
- concurrent reorder or set-primary conflict
- media is already in a state that cannot transition by policy

Example codes:

- `MEDIA.MEDIA_DELETED`
- `MEDIA.ATTACHMENT_ALREADY_EXISTS`
- `MEDIA.MEDIA_NOT_ATTACHED`
- `MEDIA.PRIMARY_CONSTRAINT_VIOLATION`
- `MEDIA.VERSION_CONFLICT`
- `MEDIA.RESTORE_WINDOW_EXPIRED`
- `MEDIA.INVALID_STATE_TRANSITION`
- `MEDIA.CONCURRENT_MODIFICATION`

---

### 3.6 `422 Unprocessable Entity` optional

Use only if the project’s global API standard distinguishes domain rule violations from basic validation.

If the project does not use `422`, map these to `400` or `409`.

Possible examples:

- metadata is syntactically valid but semantically forbidden
- media type is structurally valid but violates policy
- restore request violates retention/legal policy

Recommended V1 posture:

- Prefer `400` for request validation failures.
- Prefer `409` for conflicts with current Media truth.
- Avoid introducing `422` unless used consistently across modules.

---

### 3.7 `429 Too Many Requests`

Use when upload/register/attachment operations exceed abuse or rate-limit policy.

Examples:

- too many register attempts
- too many attach/reorder operations
- media abuse protection triggered

Example codes:

- `MEDIA.RATE_LIMITED`
- `MEDIA.UPLOAD_ABUSE_LIMIT_EXCEEDED`

---

### 3.8 `500 Internal Server Error`

Use for unexpected server-side failures.

Examples:

- unhandled exception
- unexpected persistence failure
- unexpected serialization failure

Example codes:

- `MEDIA.UNEXPECTED_ERROR`
- `MEDIA.PERSISTENCE_ERROR`

Rules:

- Do not expose raw SQL/storage/provider errors.
- Log sanitized internal diagnostics with `correlationId`.

---

### 3.9 `503 Service Unavailable`

Use when a required dependency for the synchronous truth path is unavailable.

Examples:

- database unavailable
- required storage validation dependency unavailable, if registration depends on it
- required Content article validation dependency unavailable, if policy requires synchronous validation

Example codes:

- `MEDIA.DEPENDENCY_UNAVAILABLE`
- `MEDIA.STORAGE_UNAVAILABLE`
- `MEDIA.CONTENT_VALIDATION_UNAVAILABLE`

Rules:

- Do not use `503` for downstream async lag after truth commit.
- Audit lag, Outbox backlog, CDN lag, Reading cache lag, or variant processing lag must not turn a committed Media write into failure.

---

## 4) Endpoint-specific error mapping

### 4.1 `POST /items`

Possible errors:

| Status | Code | Meaning |
|---:|---|---|
| `400` | `MEDIA.VALIDATION_FAILED` | Invalid request body |
| `400` | `MEDIA.TYPE_NOT_ALLOWED` | Media type not allowed |
| `400` | `MEDIA.METADATA_UNSAFE` | Metadata contains unsafe/unsupported fields |
| `400` | `MEDIA.IDEMPOTENCY_KEY_INVALID` | Malformed idempotency key |
| `409` | `MEDIA.IDEMPOTENCY_KEY_REUSED_WITH_DIFFERENT_REQUEST` | Same key reused for different semantic request |
| `429` | `MEDIA.RATE_LIMITED` | Register abuse/rate limit |
| `503` | `MEDIA.STORAGE_UNAVAILABLE` | Required storage validation unavailable |
| `500` | `MEDIA.PERSISTENCE_ERROR` | Unexpected persistence failure |

Notes:

- If storage upload already happened but DB registration fails, cleanup/reconciliation handles potential orphan storage objects.
- A timeout during registration is ambiguous. Caller should reconcile with `GET /items` or `GET /items/{mediaId}` if an identifier exists.

---

### 4.2 `PATCH /items/{mediaId}`

Possible errors:

| Status | Code | Meaning |
|---:|---|---|
| `400` | `MEDIA.VALIDATION_FAILED` | Invalid request body |
| `400` | `MEDIA.METADATA_UNSAFE` | Unsafe metadata |
| `400` | `MEDIA.IMMUTABLE_FIELD_UPDATE_NOT_ALLOWED` | Attempt to update immutable storage identity |
| `404` | `MEDIA.MEDIA_NOT_FOUND` | Media item does not exist |
| `409` | `MEDIA.MEDIA_DELETED` | Cannot update deleted media by policy |
| `500` | `MEDIA.PERSISTENCE_ERROR` | Unexpected persistence failure |

---

### 4.3 `DELETE /items/{mediaId}`

Possible errors:

| Status | Code | Meaning |
|---:|---|---|
| `404` | `MEDIA.MEDIA_NOT_FOUND` | Media item does not exist |
| `409` | `MEDIA.INVALID_STATE_TRANSITION` | Delete not allowed by current policy |
| `500` | `MEDIA.PERSISTENCE_ERROR` | Unexpected persistence failure |

Notes:

- Repeated delete should converge safely by policy.
- If primary selections are cleared, this is part of Media truth mutation and should be reflected in event payload.

---

### 4.4 `POST /items/{mediaId}:restore`

Possible errors:

| Status | Code | Meaning |
|---:|---|---|
| `404` | `MEDIA.MEDIA_NOT_FOUND` | Media item does not exist |
| `409` | `MEDIA.RESTORE_WINDOW_EXPIRED` | Restore outside retention window |
| `409` | `MEDIA.INVALID_STATE_TRANSITION` | Restore not allowed by current state/policy |
| `500` | `MEDIA.PERSISTENCE_ERROR` | Unexpected persistence failure |

---

### 4.5 `POST /articles/{articleId}/attachments`

Possible errors:

| Status | Code | Meaning |
|---:|---|---|
| `400` | `MEDIA.VALIDATION_FAILED` | Invalid request body |
| `400` | `MEDIA.IDEMPOTENCY_KEY_INVALID` | Malformed idempotency key |
| `404` | `MEDIA.ARTICLE_NOT_FOUND` | Article does not exist by validation policy |
| `404` | `MEDIA.MEDIA_NOT_FOUND` | Media item does not exist |
| `409` | `MEDIA.MEDIA_DELETED` | Deleted media cannot be attached |
| `409` | `MEDIA.ATTACHMENT_ALREADY_EXISTS` | Active attachment already exists |
| `409` | `MEDIA.PRIMARY_CONSTRAINT_VIOLATION` | Primary invariant could not be satisfied |
| `409` | `MEDIA.IDEMPOTENCY_KEY_REUSED_WITH_DIFFERENT_REQUEST` | Same key reused for different attach intent |
| `500` | `MEDIA.PERSISTENCE_ERROR` | Unexpected persistence failure |

---

### 4.6 `DELETE /articles/{articleId}/attachments/{mediaId}`

Possible errors:

| Status | Code | Meaning |
|---:|---|---|
| `404` | `MEDIA.ARTICLE_NOT_FOUND` | Article does not exist by validation policy |
| `404` | `MEDIA.MEDIA_NOT_FOUND` | Media item does not exist |
| `404` | `MEDIA.ATTACHMENT_NOT_FOUND` | Attachment does not exist, if policy requires strict delete |
| `500` | `MEDIA.PERSISTENCE_ERROR` | Unexpected persistence failure |

Notes:

- If policy is convergent delete, repeated detach may return `200` with `detached = true` or `detached = false`.
- If the detached media was primary, primary selection is cleared and event payload includes `primaryCleared = true`.

---

### 4.7 `POST /articles/{articleId}/attachments:reorder`

Possible errors:

| Status | Code | Meaning |
|---:|---|---|
| `400` | `MEDIA.EXPECTED_VERSION_REQUIRED` | `expectedVersion` missing |
| `400` | `MEDIA.INVALID_REORDER_LIST` | Invalid list shape, duplicates, or wrong set |
| `404` | `MEDIA.ARTICLE_NOT_FOUND` | Article does not exist by validation policy |
| `409` | `MEDIA.VERSION_CONFLICT` | `expectedVersion` mismatch |
| `409` | `MEDIA.INVALID_STATE_TRANSITION` | Reorder not allowed by current state |
| `500` | `MEDIA.PERSISTENCE_ERROR` | Unexpected persistence failure |

---

### 4.8 `POST /articles/{articleId}/attachments:set-primary`

Possible errors:

| Status | Code | Meaning |
|---:|---|---|
| `400` | `MEDIA.EXPECTED_VERSION_REQUIRED` | `expectedVersion` missing |
| `404` | `MEDIA.ARTICLE_NOT_FOUND` | Article does not exist by validation policy |
| `404` | `MEDIA.MEDIA_NOT_FOUND` | Media item does not exist |
| `409` | `MEDIA.VERSION_CONFLICT` | `expectedVersion` mismatch |
| `409` | `MEDIA.MEDIA_DELETED` | Deleted media cannot be set primary |
| `409` | `MEDIA.MEDIA_NOT_ATTACHED` | Media is not attached to the article |
| `409` | `MEDIA.PRIMARY_CONSTRAINT_VIOLATION` | Primary invariant could not be satisfied |
| `500` | `MEDIA.PERSISTENCE_ERROR` | Unexpected persistence failure |

---

## 5) Error code catalog

### 5.1 General

- `MEDIA.VALIDATION_FAILED`
- `MEDIA.UNEXPECTED_ERROR`
- `MEDIA.PERSISTENCE_ERROR`
- `MEDIA.DEPENDENCY_UNAVAILABLE`
- `MEDIA.PERMISSION_DENIED`
- `MEDIA.RATE_LIMITED`

### 5.2 Identity and lookup

- `MEDIA.INVALID_MEDIA_ID`
- `MEDIA.INVALID_ARTICLE_ID`
- `MEDIA.MEDIA_NOT_FOUND`
- `MEDIA.ARTICLE_NOT_FOUND`
- `MEDIA.ATTACHMENT_NOT_FOUND`

### 5.3 Media item policy

- `MEDIA.TYPE_NOT_ALLOWED`
- `MEDIA.METADATA_UNSAFE`
- `MEDIA.ALT_TEXT_UNSAFE`
- `MEDIA.IMMUTABLE_FIELD_UPDATE_NOT_ALLOWED`
- `MEDIA.MEDIA_DELETED`
- `MEDIA.RESTORE_WINDOW_EXPIRED`
- `MEDIA.INVALID_STATE_TRANSITION`

### 5.4 Attachment and primary policy

- `MEDIA.ATTACHMENT_ALREADY_EXISTS`
- `MEDIA.MEDIA_NOT_ATTACHED`
- `MEDIA.PRIMARY_CONSTRAINT_VIOLATION`
- `MEDIA.INVALID_REORDER_LIST`
- `MEDIA.EXPECTED_VERSION_REQUIRED`
- `MEDIA.VERSION_CONFLICT`
- `MEDIA.CONCURRENT_MODIFICATION`

### 5.5 Idempotency

- `MEDIA.IDEMPOTENCY_KEY_INVALID`
- `MEDIA.IDEMPOTENCY_KEY_REUSED_WITH_DIFFERENT_REQUEST`

### 5.6 Abuse and dependencies

- `MEDIA.UPLOAD_ABUSE_LIMIT_EXCEEDED`
- `MEDIA.STORAGE_UNAVAILABLE`
- `MEDIA.CONTENT_VALIDATION_UNAVAILABLE`

---

## 6) Async failure semantics

Media API success is truth-first.

After a successful Media write response:

- Outbox publication may still be pending.
- Audit ingestion may still be pending.
- Reading/SEO/cache/CDN/variant workflows may lag or not exist in V1.
- Consumer failures do not retroactively change Media API success.

Outbox publication failures:

- are tracked in Outbox state as `Failed` or `Dead`
- must be observable through operations/monitoring
- are not returned to the original caller after Media truth commit

Consumer-side failures:

- are handled by consumer retry/DLQ/idempotency policy
- must not be represented as Media API failure after truth commit

---

## 7) Timeout ambiguity and reconciliation

Timeouts must be treated as ambiguous.

Rules:

- Timeout does not prove register, update, attach, detach, reorder, set-primary, delete, or restore failed.
- Timeout does not prove no database mutation happened.
- Timeout does not prove no Outbox record was committed.
- Timeout does not prove no downstream side effect happened.
- Clients should reconcile by reading Media truth.
- Operators should reconcile using `correlationId`, `MessageId`, and Media/Audit records where available.

Recommended reconciliation paths:

| Ambiguous operation | Reconcile with |
|---|---|
| Register media | `GET /items`, `GET /items/{mediaId}` if known, or idempotency record |
| Update media metadata | `GET /items/{mediaId}` |
| Delete/restore media | `GET /items/{mediaId}` |
| Attach/detach media | attachment list / article media truth |
| Reorder media | attachment list ordered by `SortOrder` |
| Set primary | current primary media for article |

---

## 8) Notes on hiding vs exposing errors

Rules:

- Do not expose raw SQL constraint names.
- Do not expose storage provider internals.
- Do not expose full internal file paths if they reveal infrastructure layout.
- Do not expose signed URL secrets.
- Do not expose stack traces.
- Do not expose downstream consumer failures as synchronous Media API failures after truth commit.