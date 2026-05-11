# Content — API Surface (V1)

Content exposes primarily **Admin APIs**. Public read APIs live in the Reading module.

Base path (Admin): `/api/v1/admin/content`

> All endpoints in this module require Bearer auth + explicit authorization policies.
> Content write APIs commit Content truth and required `OutboxMessage` rows atomically, then downstream workers publish/process asynchronously.
> Success of Content write APIs is defined by **Content truth commit**. It does **not** mean downstream audit, SEO, notifications, caches, or projections have already completed.

---

## 1) API posture in V1

Content V1 focuses on the synchronous content-truth lane.

### Identifier posture

- Admin endpoints MAY use internal numeric `articleId` in V1.
- API responses and cross-module contracts MUST include `articlePublicId`.
- Outbox events use `AggregateType = Article`, `AggregateId = ArticlePublicId`, and `Version = Article.Version`.

### Included in V1

- article draft creation and update
- article lifecycle actions
  - publish
  - unpublish
  - archive
  - soft-delete
- edit history reads
- category management
- tag management

### Optional in V1

- detailed revision diff endpoint
- category/tag soft-delete flows if policy requires them

### Not primary in V1

- public content read APIs
- synchronous SEO/notification/cache update inside content write request
- reporting/projection APIs as truth-owning surfaces
- archived article restore (`Archived -> Draft`) unless a later lifecycle policy explicitly enables it
- physical purge; V1 delete semantics are soft-delete only

**Rule:** Content owns lifecycle and visibility truth for content objects. Downstream consumers follow committed Content truth; they do not define it.

**Rule:** Reading may serve public read APIs, but public visibility must be validated against Content truth or a truth-safe projection that cannot expose non-public articles.

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
  "categoryId": 1,
  "tagIds": [1, 2],
  "coverMediaId": 10
}
```

#### Response (201)

```json
{
  "articleId": 123,
  "articlePublicId": "01HX0000000000000000000000",
  "status": "Draft",
  "version": 1,
  "createdAt": "2026-03-02T10:30:00Z"
}
```

#### Rules

- create success means Content truth committed
- truth change + `OutboxMessage` must commit atomically in the same local Content transaction
- must emit `content.article_created`
- outbox envelope: `AggregateType = Article`, `AggregateId = ArticlePublicId`, `Version = Article.Version`
- duplicate retries should converge safely by idempotency policy
- downstream audit, SEO, or notification effects must not block create success

### `GET /articles`

List articles for admin, including non-public states.

#### Query

- `page`, `pageSize`
- `status` *(optional; must align with current lifecycle truth)*
- `categoryId` *(optional bigint)*
- `tagId` *(optional bigint)*
- `sort` *(optional, allowlist)*: `-updatedAt`, `-publishedAt`, `title`

#### Response (200)

Standard list envelope.

#### Rules

- returned lifecycle states must remain aligned with current Content lifecycle policy
- each article item must include both `articleId` and `articlePublicId`
- undocumented sort fields must be rejected deterministically
- admin reads for immediate post-write confirmation should prefer authoritative Content truth

### `GET /articles/{articleId}`

Admin detail view.

#### Rules

- `{articleId}` is the internal numeric Article ID in V1 admin routes
- response bodies must include `articlePublicId`
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
  "categoryId": 1,
  "tagIds": [1, 2],
  "coverMediaId": 10
}
```

#### Response (200)

```json
{
  "updated": true,
  "articleId": 123,
  "articlePublicId": "01HX0000000000000000000000",
  "status": "Draft",
  "version": 2
}
```

#### Rules

- update policy must define whether changes are:
  - draft-only
  - or partially allowed in published state
- must create edit-history entry by policy
- truth change + `ArticleRevision` + `OutboxMessage` must commit atomically in the same local Content transaction
- must emit `content.article_updated`
- outbox envelope: `AggregateType = Article`, `AggregateId = ArticlePublicId`, `Version = Article.Version`
- should enforce stale-write protection via version, rowversion, or compare-and-set semantics
- update success is defined by Content truth commit only
- for Published articles, `content.article_updated` may require public cache/projection invalidation; consumers must use `articlePublicId` + `version` to prevent stale overwrite

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
  "articleId": 123,
  "articlePublicId": "01HX0000000000000000000000",
  "status": "Published",
  "publishedAt": "2026-03-02T10:30:00Z",
  "version": 7
}
```

#### Rules

- lifecycle transition must be valid
- truth change + `ArticleLifecycleEvent` + `OutboxMessage` must commit atomically in the same local Content transaction
- must emit `content.article_published`
- outbox envelope: `AggregateType = Article`, `AggregateId = ArticlePublicId`, `Version = Article.Version`
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
  "articleId": 123,
  "articlePublicId": "01HX0000000000000000000000",
  "status": "Draft",
  "lifecycleAction": "Unpublish",
  "unpublishedAt": "2026-03-02T10:30:00Z",
  "version": 8
}
```

#### Rules

- `reason` is mandatory
- V1 unpublish semantics are `Published -> Draft`; `Unpublished` is not a valid Article status
- lifecycle semantics must remain aligned with current Content ADR/policy
- truth change + `ArticleLifecycleEvent` + `OutboxMessage` must commit atomically in the same local Content transaction
- must emit `content.article_unpublished`
- outbox envelope: `AggregateType = Article`, `AggregateId = ArticlePublicId`, `Version = Article.Version`
- emitted async message should carry stable `MessageId`
- public read must stop showing it immediately based on Content truth, even if downstream derived state lags
- repeated equivalent unpublish requests should converge safely as no-op or documented idempotent success

### `POST /articles/{articleId}:archive`

Archive an article.

#### Headers

- `Idempotency-Key` *(recommended)*
- `X-Correlation-Id` *(optional)*

#### Response (200)

```json
{
  "articleId": 123,
  "articlePublicId": "01HX0000000000000000000000",
  "status": "Archived",
  "archivedAt": "2026-03-02T10:30:00Z",
  "version": 9
}
```

#### Rules

- archive is a truth-bound lifecycle action
- if enabled, archive semantics must remain aligned with lifecycle policy
- truth change + `ArticleLifecycleEvent` + `OutboxMessage` must commit atomically in the same local Content transaction
- must emit `content.article_archived`
- outbox envelope: `AggregateType = Article`, `AggregateId = ArticlePublicId`, `Version = Article.Version`
- if downstream derived state lags, archive truth still wins for visibility correctness
- repeated equivalent archive requests should converge safely
- archive event publication must remain non-blocking after commit

### `POST /articles/{articleId}:soft-delete`

Soft-delete an article.

#### Headers

- `Idempotency-Key` *(recommended)*
- `X-Correlation-Id` *(optional)*

#### Response (200)

```json
{
  "articleId": 123,
  "articlePublicId": "01HX0000000000000000000000",
  "status": "Published",
  "isDeleted": true,
  "deletedAt": "2026-03-02T10:30:00Z",
  "version": 10
}
```

#### Rules

- V1 soft-delete sets `IsDeleted=1`; it is not physical deletion
- soft-delete does not have to change `Status`; the response returns the current status after soft-delete
- public visibility remains blocked by `Status='Published' AND IsDeleted=0`
- physical purge is out of scope for V1
- truth change + `ArticleLifecycleEvent` + `OutboxMessage` must commit atomically in the same local Content transaction
- must emit `content.article_soft_deleted`
- outbox envelope: `AggregateType = Article`, `AggregateId = ArticlePublicId`, `Version = Article.Version`
- downstream lag must not re-expose soft-deleted or non-public content
- repeated equivalent soft-delete requests should converge safely as no-op or documented idempotent success

---

## 3) Edit history (admin)

### `GET /articles/{articleId}/revisions`

List edit history for an article.

#### Response (200)

```json
{
  "items": [
    {
      "revisionId": 1,
      "editedAt": "2026-03-02T10:30:00Z",
      "editedBy": 123,
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
- V1 should prefer deactivate/soft-delete semantics when a category is referenced by existing articles
- physical deletion is allowed only when no references exist or by explicit retention/admin policy
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
- V1 should prefer deactivate/soft-delete semantics when a tag is referenced by existing articles
- physical deletion is allowed only when no references exist or by explicit retention/admin policy
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
