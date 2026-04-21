# Content — API Surface (V1)

Content exposes primarily **Admin APIs**. Public read APIs live in the Reading module.

Base path (Admin): `/api/v1/admin/content`

> All endpoints in this module require Bearer auth + explicit authorization policies.  
> Content write APIs commit Content truth first and emit async intent through the standard post-commit path where required.  
> Success of Content write APIs is defined by **Content truth commit**. It does **not** mean downstream audit, SEO, notifications, caches, or projections have already completed.

---

## 1) API posture in V1

Content V1 focuses on the synchronous content-truth lane.

### Included in V1

- article draft creation and update
- article lifecycle actions
  - publish
  - unpublish
  - archive
  - restore
- edit history reads
- category management
- tag management

### Optional in V1

- archive / restore if lifecycle policy enables them
- detailed revision diff endpoint
- soft-delete flows if policy requires them

### Not primary in V1

- public content read APIs
- synchronous SEO/notification/cache update inside content write request
- reporting/projection APIs as truth-owning surfaces

**Rule:** Content owns lifecycle and visibility truth for content objects. Downstream consumers follow committed Content truth; they do not define it.

---

## 2) Articles (admin)

### `POST /articles`

Create a draft article.

#### Headers

- `Idempotency-Key` *(recommended for safe retry under ambiguous client/network outcomes)*
- `X-Correlation-Id` *(optional)*

#### Request

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

#### Response (201)

```json
{
  "articleId": "string",
  "status": "Draft",
  "version": 1,
  "createdAt": "2026-03-02T10:30:00Z"
}
```

#### Rules

- create success means Content truth committed
- duplicate retries should converge safely by idempotency policy
- downstream audit, SEO, or notification effects must not block create success

### `GET /articles`

List articles for admin, including non-public states.

#### Query

- `page`, `pageSize`
- `status` *(optional; must align with current lifecycle truth)*
- `categoryId` *(optional)*
- `tagId` *(optional)*
- `sort` *(optional, allowlist)*: `-updatedAt`, `-publishedAt`, `title`

#### Response (200)

Standard list envelope.

#### Rules

- returned lifecycle states must remain aligned with current Content lifecycle policy
- undocumented sort fields must be rejected deterministically
- admin reads for immediate post-write confirmation should prefer authoritative Content truth

### `GET /articles/{articleId}`

Admin detail view.

#### Rules

- may include edit metadata and current lifecycle/version state
- must reflect authoritative Content truth for current lifecycle visibility state

### `PUT /articles/{articleId}`

Update article fields.

#### Headers

- `If-Match` or version-based concurrency header *(recommended, if adopted by API policy)*
- `X-Correlation-Id` *(optional)*

#### Request

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

#### Response (200)

```json
{
  "updated": true,
  "version": 2
}
```

#### Rules

- update policy must define whether changes are:
  - draft-only
  - or partially allowed in published state
- must create edit-history entry by policy
- should enforce stale-write protection via version, rowversion, or compare-and-set semantics
- update success is defined by Content truth commit only

### `POST /articles/{articleId}:publish`

Publish an article.

#### Headers

- `Idempotency-Key` *(recommended)*
- `X-Correlation-Id` *(optional)*

#### Request

```json
{
  "publishNote": "optional string"
}
```

#### Response (200)

```json
{
  "articleId": "string",
  "status": "Published",
  "publishedAt": "2026-03-02T10:30:00Z",
  "version": 7
}
```

#### Rules

- lifecycle transition must be valid
- truth change + per-article version advancement + outbox record must commit atomically
- must emit `Content.ArticlePublished`
- emitted async message should carry stable `MessageId`
- must not block on downstream async processing
- repeated equivalent publish requests should converge safely as no-op or documented idempotent success
- public visibility truth is determined by Content, not by downstream projection freshness

### `POST /articles/{articleId}:unpublish`

Unpublish an article and record a reason.

#### Headers

- `Idempotency-Key` *(recommended)*
- `X-Correlation-Id` *(optional)*

#### Request

```json
{
  "reason": "PolicyViolation|ContentIssue|Other",
  "note": "optional string"
}
```

#### Response (200)

```json
{
  "articleId": "string",
  "status": "Unpublished",
  "version": 8
}
```

#### Rules

- `reason` is mandatory
- lifecycle semantics must remain aligned with current Content ADR/policy
- truth change + per-article version advancement + outbox record must commit atomically
- must emit `Content.ArticleUnpublished`
- emitted async message should carry stable `MessageId`
- public read must stop showing it immediately based on Content truth, even if downstream derived state lags
- repeated equivalent unpublish requests should converge safely as no-op or documented idempotent success

### `POST /articles/{articleId}:archive`

Archive an article *(optional V1)*.

#### Headers

- `Idempotency-Key` *(recommended)*
- `X-Correlation-Id` *(optional)*

#### Response (200)

```json
{
  "articleId": "string",
  "status": "Archived",
  "version": 9
}
```

#### Rules

- archive is a truth-bound lifecycle action
- if enabled, archive semantics must remain aligned with lifecycle policy
- if downstream derived state lags, archive truth still wins for visibility correctness
- repeated equivalent archive requests should converge safely
- if archive emits an async event, it must remain post-commit and non-blocking

### `POST /articles/{articleId}:restore`

Restore an archived article *(optional V1)*.

#### Headers

- `Idempotency-Key` *(recommended)*
- `X-Correlation-Id` *(optional)*

#### Response (200)

```json
{
  "articleId": "string",
  "status": "Draft",
  "version": 10
}
```

#### Rules

- restore semantics must remain aligned with lifecycle policy
- repeated equivalent restore requests should converge safely as no-op or documented idempotent success
- if restore emits async events, they remain post-commit and non-blocking

### `DELETE /articles/{articleId}`

Delete article according to policy.

#### Rules

- delete/soft-delete semantics must remain consistent with lifecycle truth
- V1 should prefer a clearly documented posture:
  - soft delete
  - or archive as the normal non-public terminal path
- downstream lag must not re-expose deleted or non-public content
- if delete emits async side effects, they remain post-commit and non-blocking

---

## 3) Edit history (admin)

### `GET /articles/{articleId}/revisions`

List edit history for an article.

#### Response (200)

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

#### Rules

- revision history is append-only by intent
- revisions are historical evidence/content history
- historical revisions do not replace current Content truth

### `GET /articles/{articleId}/revisions/{revisionId}`

Get a specific revision snapshot or diff.

#### Rules

- exact snapshot/diff behavior is policy-defined
- revision reads must not be confused with current live lifecycle truth

---

## 4) Categories (admin)

Base: `/categories`

### `GET /categories`

List categories.

### `POST /categories`

Create category.

### `PUT /categories/{categoryId}`

Update category.

### `DELETE /categories/{categoryId}`

Delete category according to policy.

#### Rules

- category naming/uniqueness rules must be documented and enforced consistently
- delete semantics must avoid orphan references or invalid content truth
- if category changes trigger downstream effects, those remain post-commit and non-blocking

---

## 5) Tags (admin)

Base: `/tags`

### `GET /tags`

List tags.

### `POST /tags`

Create tag.

### `PUT /tags/{tagId}`

Update tag.

### `DELETE /tags/{tagId}`

Delete tag according to policy.

#### Rules

- tag naming/uniqueness rules must be documented and enforced consistently
- delete semantics must avoid orphan references or invalid content truth
- if tag changes trigger downstream effects, those remain post-commit and non-blocking

---

## 6) Versioning and conventions

- All endpoints use `/api/v1`.
- List responses use `{ items[], pageInfo{} }`.
- Errors use the standard error envelope.
- High-impact lifecycle actions should expose or align with Content version semantics where practical.
- Async downstream completion is not part of API success semantics for Content writes.

---

## 7) Rate limiting (policy-level)

Admin endpoints are protected by authz, but still require protection against:

- accidental loops
- compromised admin tokens
- repeated ambiguous retries on high-impact lifecycle actions

Rate limits are implementation-defined; anomalies should be monitored.