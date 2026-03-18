# Content — API Surface (V1)

Content exposes primarily **Admin APIs**. Public read APIs live in the Reading module.

Base path (Admin): `/api/v1/admin/content`

> All endpoints in this module require Bearer auth + explicit authorization policies.
> Governance actions must emit audit events (async ingestion).
> Success of Content write APIs is defined by **Content truth commit**, not by downstream completion of audit, SEO, notifications, caches, or projections.

---

## 1) Articles (admin)

### POST `/articles`

Create a draft article.

**Headers**

* `Idempotency-Key` (recommended for safe retry under ambiguous client/network outcomes)
* `X-Correlation-Id` (optional)

**Request**

```json
{
  "title": "string",
  "summary": "string",
  "body": "string",
  "categoryId": "string",
  "tagIds": ["string"],
  "coverMediaId": "string"
}
```

**Response (201)**

```json
{
  "articleId": "string",
  "status": "Draft",
  "version": 1,
  "createdAt": "2026-03-02T10:30:00Z"
}
```

---

### GET `/articles`

List articles for admin (includes non-public states).

**Query**

* `page`, `pageSize`
* `status` (optional): `Draft | Published | Unpublished | Archived`
* `categoryId` (optional)
* `tagId` (optional)
* `sort` (optional, allowlist): `-updatedAt`, `-publishedAt`, `title`

**Response (200)**

Standard list envelope.

**Notes**

* If unpublish semantics later change by ADR, keep this enum aligned with lifecycle truth.

---

### GET `/articles/{articleId}`

Admin detail view (may include edit metadata and current lifecycle/version state).

---

### PUT `/articles/{articleId}`

Update draft/article fields (policy-driven: allow updates in Draft only, or limited updates in Published).

**Headers**

* `If-Match` or version-based concurrency header (recommended, if adopted by API policy)
* `X-Correlation-Id` (optional)

**Request**

```json
{
  "title": "string",
  "summary": "string",
  "body": "string",
  "categoryId": "string",
  "tagIds": ["string"],
  "coverMediaId": "string"
}
```

**Response (200)**

```json
{
  "updated": true,
  "version": 2
}
```

**Notes**

* Must create an edit-history entry (who/when/what changed) by policy.
* Should enforce stale-write protection via version/rowversion/compare-and-set semantics according to module policy.
* Update success is defined by Content truth commit only.

---

### POST `/articles/{articleId}:publish`

Publish an article.

**Headers**

* `Idempotency-Key` (recommended)
* `X-Correlation-Id` (optional)

**Request**

```json
{
  "publishNote": "optional string"
}
```

**Response (200)**

```json
{
  "articleId": "string",
  "status": "Published",
  "publishedAt": "2026-03-02T10:30:00Z",
  "version": 7
}
```

**Rules**

* Lifecycle transition must be valid.
* Truth change + per-article version advancement + outbox record must commit atomically.
* Must emit `ArticlePublished` event for downstream async consumers (Audit, SEO, Notifications, future projections).
* Must not block on async processing.
* Repeated equivalent publish requests should converge safely as no-op or documented idempotent success.
* Public visibility truth is determined by Content, not by downstream projection freshness.

---

### POST `/articles/{articleId}:unpublish`

Unpublish an article and record a reason (mandatory).

**Headers**

* `Idempotency-Key` (recommended)
* `X-Correlation-Id` (optional)

**Request**

```json
{
  "reason": "PolicyViolation|ContentIssue|Other",
  "note": "optional string"
}
```

**Response (200)**

```json
{
  "articleId": "string",
  "status": "Unpublished",
  "version": 8
}
```

**Rules**

* Reason is mandatory.
* Unpublish semantics (Draft vs separate state) is an ADR decision; keep consistent.
* Truth change + per-article version advancement + outbox record must commit atomically.
* Must emit `ArticleUnpublished` event.
* Public read must stop showing it immediately based on Content truth, even if downstream derived state lags.
* Repeated equivalent unpublish requests should converge safely as no-op or documented idempotent success.

---

### POST `/articles/{articleId}:archive`

Archive an article (optional V1; if enabled, defines non-public state).

**Headers**

* `Idempotency-Key` (recommended)
* `X-Correlation-Id` (optional)

**Response (200)**

```json
{
  "articleId": "string",
  "status": "Archived",
  "version": 9
}
```

**Rules**

* Archive is a truth-bound lifecycle action.
* If downstream derived state lags, archive truth still wins for visibility correctness.
* Repeated equivalent archive requests should converge safely.

---

### POST `/articles/{articleId}:restore`

Restore from archived (optional V1).

**Headers**

* `Idempotency-Key` (recommended)
* `X-Correlation-Id` (optional)

**Response (200)**

```json
{
  "articleId": "string",
  "status": "Draft",
  "version": 10
}
```

**Rules**

* Restore semantics must remain aligned with lifecycle policy.
* Repeated equivalent restore requests should converge safely as no-op or documented idempotent success.

---

### DELETE `/articles/{articleId}`

Delete is policy-driven (prefer soft delete if required).
If supported, must be audited and must not leak drafts publicly.

**Rules**

* Delete/soft-delete semantics must stay consistent with lifecycle truth.
* Downstream lag must not re-expose deleted/non-public content.
* If this operation emits async side effects, they remain post-commit and non-blocking.

---

## 2) Edit history (admin)

### GET `/articles/{articleId}/revisions`

List edit history for an article.

**Response (200)**

```json
{
  "items": [
    {
      "revisionId": "string",
      "editedAt": "2026-03-02T10:30:00Z",
      "editedBy": "userId",
      "summary": "string"
    }
  ],
  "pageInfo": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

**Notes**

* Revision history is append-only by intent.
* Historical revisions do not replace current Content truth.

---

### GET `/articles/{articleId}/revisions/{revisionId}`

Get a specific revision snapshot/diff (policy-defined).

---

## 3) Categories (admin)

Base: `/categories`

### GET `/categories`

List categories.

### POST `/categories`

Create category.

### PUT `/categories/{categoryId}`

Update category.

### DELETE `/categories/{categoryId}`

Delete category (policy-defined; avoid orphan references).

---

## 4) Tags (admin)

Base: `/tags`

### GET `/tags`

List tags.

### POST `/tags`

Create tag.

### PUT `/tags/{tagId}`

Update tag.

### DELETE `/tags/{tagId}`

Delete tag (policy-defined; avoid orphan references).

---

## 5) Versioning and conventions

* All endpoints use `/api/v1`.
* List responses use `{ items[], pageInfo{} }`.
* Errors use the standard error envelope.
* High-impact lifecycle actions should expose or align with Content version semantics where practical.
* Async downstream completion is not part of API success semantics for Content writes.

---

## 6) Rate limiting (policy-level)

Admin endpoints are protected by authz; still apply protection against:

* accidental loops
* compromised admin tokens
* repeated ambiguous retries on high-impact lifecycle actions

Rate limits are implementation-defined; monitor anomalies.
