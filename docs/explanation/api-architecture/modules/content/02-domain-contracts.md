# Content — Domain Contracts (V1)

## 1) Ownership

Content is the source of truth for:

- article lifecycle truth
- article visibility truth
- article metadata and body truth
- taxonomy truth for categories and tags
- article-to-tag attachment truth
- edit history / revision truth for content changes

Content is the canonical source of truth for:

- whether an article is `Draft`, `Published`, or `Archived` according to current lifecycle policy
- whether an article is soft-deleted through `IsDeleted`
- what the stable `ArticlePublicId` is
- whether an article is publicly visible
- which category an article belongs to
- which tags are attached to an article
- what the current article version is
- what the historical revision trail is

Content does **not** own:

- user identity truth
- authorization/governance truth
- media-object truth
- notification delivery truth
- audit evidence truth
- search/index/projection truth
- public-read serving truth in Reading

### Boundary rule

Content owns lifecycle and visibility truth for content objects.

Other modules may react to committed Content truth, but they must not override it.
Downstream projections, SEO, notifications, caches, or public-read models do not define article lifecycle or visibility truth.

`ArticleId` is the internal SQL identity. `ArticlePublicId` is the stable public/cross-module identity and must be used where Content truth crosses module or public API boundaries.

---

## 2) Core entities (conceptual contract)

### 2.1 Article

An `Article` represents the canonical content object managed by Content.

**Fields**

- `ArticleId`
- `ArticlePublicId`
- `Title`
- `Summary`
- `Body`
- `Status`
- `IsDeleted`
- `AuthorUserId`
- `CategoryId`
- `CoverMediaId?`
- `Version`
- `CreatedAt`
- `UpdatedAt`
- `PublishedAt?`
- `UnpublishedAt?`
- `ArchivedAt?`
- `DeletedAt?`
- `DeletedBy?`

**Notes**

- `ArticleId` is internal storage identity.
- `ArticlePublicId` is stable public/cross-module identity and should be used in event aggregate identity.
- `Status` must remain aligned with the current lifecycle policy.
- `CoverMediaId` is only a reference; Media owns media-object truth.
- `Version` is the authoritative freshness/version marker for article truth and per-aggregate event ordering.
- public visibility requires `Status='Published' AND IsDeleted=0`.
- public visibility is derived from current Content lifecycle truth, not from downstream read-model freshness.

### 2.2 Category

A `Category` represents article taxonomy grouping owned by Content.

**Fields**

- `CategoryId`
- `Name`
- `NameNormalized?`
- `Slug?`
- `IsActive?` *(optional if lifecycle is modeled)*
- `ParentCategoryId?` *(optional V2 hook)*
- `CreatedAt?`
- `UpdatedAt?`

**Notes**

- category naming/uniqueness is policy-governed
- if parent-child taxonomy is introduced later, it must remain explicitly modeled
- category lifecycle/delete semantics must not produce invalid article truth

### 2.3 Tag

A `Tag` represents article tagging taxonomy owned by Content.

**Fields**

- `TagId`
- `Name`
- `NameNormalized?`
- `IsActive?` *(optional if lifecycle is modeled)*
- `CreatedAt?`
- `UpdatedAt?`

**Notes**

- tag naming/uniqueness is policy-governed
- tag lifecycle/delete semantics must not produce invalid attachment truth

### 2.4 ArticleTag

`ArticleTag` represents the attachment of a tag to an article.

**Fields**

- `ArticleId`
- `TagId`
- `AttachedAt?`
- `AttachedByUserId?`

**Notes**

- attachment truth belongs to Content
- duplicate active attachment must not create duplicate truth
- attach/detach behavior must be deterministic and idempotent where applicable

### 2.5 ArticleRevision

`ArticleRevision` represents append-only edit history for article changes.

**Fields**

- `RevisionId`
- `ArticleId`
- `EditedByUserId`
- `EditedAt`
- `ArticleVersion?`
- `OldTitle?`
- `OldSummary?`
- `OldBody?`
- `ChangeSummary?`

**Notes**

- V1 stores the previous snapshot before a meaningful edit (`OldTitle`, `OldSummary`, `OldBody`)
- revision storage may later become diff-based or hybrid by ADR/policy
- revision history is historical evidence/content history
- revision history does not replace current live article truth

### 2.6 ArticleLifecycleEvent

`ArticleLifecycleEvent` represents append-only governance history for publish, unpublish, archive, and soft-delete actions.

**Fields**

- `EventId`
- `ArticleId`
- `ArticleVersion`
- `ActionType`
- `FromStatus?`
- `ToStatus?`
- `Reason?`
- `ActorUserId`
- `OccurredAt`
- `CorrelationId?`
- `MetadataJson?`

**Notes**

- `ArticleLifecycleEvent` is append-only.
- `ArticleVersion` must align with `Article.Version` and `OutboxMessage.Version`.
- Unpublish requires `Reason`.
- Soft-delete is recorded as an action; it is not a separate Article status.

---

## 3) Lifecycle rules and invariants

### 3.1 Baseline lifecycle model

Content V1 uses a small lifecycle model. Unpublish is an action, not a separate Article status.

V1 states:

- `Draft`
- `Published`
- `Archived`

Soft-delete is modeled separately with `IsDeleted=1`; it is not a status.

### 3.2 Allowed transitions (V1)

Allowed V1 transitions:

- `Draft -> Published`
- `Published -> Draft` *(Unpublish action)*
- `Published -> Archived`
- `Draft -> Archived` *(optional policy)*
- `Archived -> no transitions in V1`

Archived restore is out of scope for V1 unless a later lifecycle policy explicitly enables `Archived -> Draft`.

### 3.3 Visibility invariant

Public reads only expose:

- `Status='Published' AND IsDeleted=0`

Public read must never expose:

- `Draft` articles
- `Archived` articles
- soft-deleted articles
- any article whose Content visibility truth is uncertain

Public visibility truth is determined by current Content truth, not by downstream public-read projection freshness.

### 3.4 Publish / unpublish invariants

- publish must be a valid lifecycle transition
- unpublish must be a valid lifecycle transition
- unpublish must record a mandatory reason
- repeated equivalent lifecycle actions must converge safely as no-op or documented idempotent success
- publish/unpublish correctness must not depend on downstream SEO, notifications, caches, or projections

### 3.5 Archive / restore invariants

- archive must be a truth-bound lifecycle action
- archive must remove public visibility immediately according to Content truth
- restore is out of scope for V1 unless a later lifecycle policy explicitly enables `Archived -> Draft`
- downstream lag must not re-expose archived content incorrectly

### 3.6 Soft-delete invariants

- soft-delete sets `IsDeleted=1`; it is not physical deletion
- soft-delete does not have to change `Status`
- public visibility must stop immediately for soft-deleted articles
- physical purge is out of scope for V1 and requires an explicit retention/admin policy

### 3.7 Version / freshness invariant

- article truth must advance version deterministically on meaningful content/lifecycle changes
- stale writes must be rejected or resolved by documented optimistic concurrency policy
- lifecycle events must store `ArticleVersion = Article.Version`
- Outbox messages for Article events must carry `Version = Article.Version`
- `UpdatedAt` alone must not be the sole freshness authority where version-based protection exists

---

## 4) Edit history invariants

- updates must create a traceable revision record according to policy
- revision history is append-only by intent
- historical records must remain attributable:
  - who changed it
  - when it changed
  - what changed (according to snapshot/diff policy)
- revision history must not silently replace current article truth
- revision history should be tamper-evident at policy level

---

## 5) Taxonomy invariants

### 5.1 Category invariants

- an article must not reference a non-existent category
- category naming/uniqueness must be deterministic under the chosen normalization policy
- deleting or deactivating categories must not leave invalid article truth
- any reassignment/orphan-prevention policy must be explicit

### 5.2 Tag invariants

- tags attached to articles must exist
- duplicate tag attachment must not create duplicate truth
- tag naming/uniqueness must be deterministic under the chosen normalization policy
- deleting or deactivating tags must not leave invalid attachment truth

### 5.3 Attachment invariants

- `ArticleTag` must not reference missing article or missing tag
- attach/detach rules must be deterministic
- stale attach/detach attempts must not corrupt current attachment truth

---

## 6) Authorization and policy invariants

- publish/unpublish/archive/soft-delete actions must be protected by authorization policies
- category and tag mutation must be protected by authorization policies
- edit/update rules must follow documented content policy:
  - draft-only updates
  - or limited published-state edits, if supported
- public-read visibility rules do not replace admin authorization rules; they compose with them

---

## 7) Truth vs derived rules

### 7.1 Truth owned by Content

Content owns:

- article lifecycle truth
- article visibility truth
- article public identity truth (`ArticlePublicId`)
- article soft-delete truth (`IsDeleted`)
- article body/metadata truth
- article taxonomy references
- article-tag attachment truth
- revision history truth
- lifecycle event truth

### 7.2 Derived outputs not owned as truth

The following may be downstream or derived:

- SEO/materialized slug or metadata projections
- notifications about publish/unpublish
- public-read models in Reading
- search/index documents
- cache entries
- reports and summaries

These may lag, be rebuilt, or be replayed, but they do not become Content truth.

### 7.3 Downstream lag rule

Downstream lag must not:

- make draft, archived, or soft-deleted content appear public
- keep archived content visible
- keep soft-deleted content visible
- hide already-published content on a truth-critical admin confirmation path
- redefine current lifecycle truth

---

## 8) Domain events (for async side effects)

Content records minimal, privacy-aware events through the required Outbox path for async side effects.

### 8.1 Event identity and posture

Async Content events must:

- be represented by an `OutboxMessage` committed atomically with Content truth
- carry stable `MessageId`
- include `CorrelationId` where available
- use `AggregateType = Article`, `AggregateId = ArticlePublicId`, and `Version = Article.Version` for Article events
- be safe under duplicate delivery, replay, and delay
- remain minimal and privacy-aware

### 8.2 Typical V1 events

Article events emitted by V1 Phase 1:

- `content.article_created`
- `content.article_updated`
- `content.article_published`
- `content.article_unpublished`
- `content.article_archived`
- `content.article_soft_deleted`

Reserved event names for later phases:

- `content.article_purged`
- `content.category_created`
- `content.category_updated`
- `content.category_soft_deleted`
- `content.tag_created`
- `content.tag_updated`
- `content.tag_soft_deleted`

### 8.3 Event payload rules

Payloads must be minimal and privacy-aware.

Article event envelope:

- `AggregateType = Article`
- `AggregateId = ArticlePublicId`
- `Version = Article.Version`

Typical payload examples:

- `content.article_published`
  - `{ ArticleId, ArticlePublicId, PublishedAt, Version }`
- `content.article_unpublished`
  - `{ ArticleId, ArticlePublicId, Reason, UnpublishedAt, Version }`
- `content.article_archived`
  - `{ ArticleId, ArticlePublicId, ArchivedAt, Version }`
- `content.article_soft_deleted`
  - `{ ArticleId, ArticlePublicId, IsDeleted, DeletedAt, Version }`

Payloads should include only the minimum necessary data needed by downstream consumers.

### 8.4 Downstream consumer posture

Typical downstream consumers may include:

- Audit
- SEO
- Notifications
- Reading/public projections
- search/indexing
- analytics/reporting

Downstream consumers must tolerate:

- at-least-once delivery
- duplicate delivery
- delayed delivery
- replay after crash or rebuild

Content truth remains the authoritative rebuild source.

---

## 9) Read/write truth contract

### 9.1 Write success means

For content-changing operations, success means:

- Content truth committed successfully
- required async intent/outbox committed for operations that emit Article integration events

Write success does **not** guarantee that:

- audit is already queryable
- SEO is already updated
- notification delivery has completed
- public projections are already refreshed
- caches are already invalidated

### 9.2 Read-after-write requirement

For admin confirmation flows:

- immediate post-write reads must reflect authoritative current Content truth
- reconciliation after timeout must use Content truth, not downstream projection visibility

---

## 10) V1 boundaries and exclusions

V1 includes:

- article lifecycle management
- taxonomy management
- revision history reads
- truth-first async event emission after commit

V1 may optionally include:

- detailed revision diff views
- category/tag soft-delete flows
- richer taxonomy lifecycle

V1 does not require:

- synchronous SEO update inside content write request
- synchronous notification delivery
- synchronous search indexing
- public read APIs inside Content
- archived article restore
- physical purge

Public read remains owned by Reading, but must follow committed Content truth or a truth-safe projection that cannot expose non-public articles.
