# System Data Model — Content (V1)

> **Recommended file:** `explanation/architecture/arc42/system-data/system-data-content-v1.md`
> **Module:** Content Management
> **Purpose:** Core content lifecycle (create/edit/publish/unpublish/archive/soft-delete), taxonomy, and audit-grade history — optimized for **read path performance**, **safe async side effects**, and **safe evolution**.

---

## 0) Data System fit (V1)

Content is the **read-heavy OLTP core**. It must remain fast under bursts and correct under governance rules.

- **Truth store:** SQL Server (row-store; index-driven reads)
- **Async side effects (non-blocking):** Audit, SEO, Notifications, cache invalidation, and downstream projections via **required Outbox + broker**
- **Redis:** public feed + detail cache, plus slug routing cache through SEO module
- **Append-only:** revisions + lifecycle events (retention-driven)

**Non-negotiables (from Quality Requirements and ADRs)**
- Read path priority (P95/P99) — interaction/audit/email must not degrade reading
- Governance actions must be traceable (publish/unpublish reasons)
- Evolvability: rolling upgrades require backward/forward compatible schema changes
- ADR-0013 / ADR-0018: when Content emits async side effects, the matching `OutboxMessage` is part of the same local Content transaction
- ADR-0014: public APIs and cross-module contracts use stable opaque IDs (`ArticlePublicId`), not numeric SQL identity as public identity

---

## 1) Scope & boundaries (V1)

### In scope (V1)
- **Article** current state (Draft/Published/Archived) with internal and public identifiers
- **Category** (taxonomy; optional hierarchy)
- **Tag** + attach/detach via **ArticleTag**
- **ArticleRevision** (append-only edit history)
- **ArticleLifecycleEvent** (append-only governance history; reasons required for sensitive actions)
- **Article integration events** through required outbox records

### Cross-module references
- **Identity** owns users; Content stores IDs:
  - `Article.AuthorUserId`
  - `ArticleRevision.EditedBy`
  - `ArticleLifecycleEvent.ActorUserId`
- **Media/SEO** are external:
  - Content keeps nullable hooks such as `Article.CoverMediaId` (media)
  - Slug/canonical lives in SEO module, accessed via contract
  - Slug is never the primary cross-module identity; `ArticlePublicId` is

---

## 2) Capability → Data mapping

### 2.1 Create Articles (draft first)
- `Article` stores current state
- `Article.ArticleId` is the internal SQL identity
- `Article.ArticlePublicId` is the stable public/cross-module identity
- `Category` classifies
- `Tag` + `ArticleTag` attach safely
- Required outbox event: `content.article_created`

### 2.2 Edit Articles (draft save) + edit history
- `Article` stores current state
- `ArticleRevision` stores edit trail (append-only)
  - actor + timestamp + correlationId
  - V1 stores the previous snapshot before a meaningful edit (`OldTitle`, `OldSummary`, `OldBody`)
  - V2: patch/diff (`PatchJson`) if needed
- Required outbox event for meaningful changes: `content.article_updated`

### 2.3 Publish / Unpublish / Archive / Soft Delete + record reasons
- `Article` stores `Status`, `PublishedAt`, `UnpublishedAt`, `ArchivedAt`
- `ArticleLifecycleEvent` stores governance actions separately:
  - from→to status, article version, actor, correlationId
  - **Reason required for Unpublish**
- Required outbox events:
  - `content.article_published`
  - `content.article_unpublished`
  - `content.article_archived`
  - `content.article_soft_deleted`

### 2.4 Category hierarchy (optional)
- `Category.ParentCategoryId` (self-FK)

### 2.5 Tag management
- `Tag` vocabulary
- `ArticleTag` bridge with idempotent attach

---

## 3) Workload & query patterns (V1) (DDIA Ch3)

> These patterns justify indexes and caching policy.

### 3.1 Public read (read-heavy, bursty)
- Published feed: `Status='Published' AND IsDeleted=0` ordered by `PublishedAt DESC`
- Category feed: `CategoryId + PublishedAt DESC`
- Tag feed: `TagId → ArticleId → Article` (bridge lookup)
- Detail:
  - public/cross-module identity: `ArticlePublicId`
  - internal admin/investigation identity: `ArticleId`
  - slug routing via SEO module resolves to `ArticlePublicId` and/or internal `ArticleId`, then Content enforces visibility
  - Slug resolution success is not visibility success; public detail reads must verify Content truth: `Status='Published' AND IsDeleted=0`

### 3.2 Admin workflows (write-light, correctness-heavy)
- Draft list: by author/status/createdAt
- Publish/unpublish/archive/soft-delete actions (governance + reason where required)
- View revision timeline and lifecycle timeline for investigations

### 3.3 Investigation queries (append-only logs)
- Revisions by `ArticleId` ordered by `EditedAt DESC`
- Lifecycle events by `ArticleId` ordered by `OccurredAt DESC`
- Lifecycle events by `ActorUserId` ordered by time
- Cross-system debugging by `ArticlePublicId` from outbox/event payloads

---

## 4) Dataflows (V1) — REST / DB / Broker (DDIA Ch4)

### 4.1 Required Outbox rule

For all Article domain changes that have async side effects, Content MUST insert an `OutboxMessage` in the same local Content transaction as the truth change.

Required Article event types:
- `content.article_created`
- `content.article_updated`
- `content.article_published`
- `content.article_unpublished`
- `content.article_archived`
- `content.article_soft_deleted`

Outbox envelope rules:
- `AggregateType = "Article"`
- `AggregateId = Article.ArticlePublicId`
- `Version = Article.Version`
- Payload includes both:
  - `ArticleId` — internal SQL identity
  - `ArticlePublicId` — stable public/cross-module identity

### 4.2 Create Draft (sync OLTP + async side effects)
**Sync (API → DB)**
1. Validate authorization (Authorization module)
2. Insert `Article` with:
   - `ArticleId` generated by SQL Server
   - `ArticlePublicId CHAR(26) NOT NULL` generated as ULID
   - `Status='Draft'`
   - `Version=1`
3. Attach category/tags where requested
4. MUST insert `OutboxMessage` with event `content.article_created`

**Async (Worker/Consumers)**
- Audit consumes event → writes audit log
- SEO may consume event → reserve/prepare slug candidate if policy allows
- Search/projections may consume event for admin-only indexing

### 4.3 Edit Draft / Update Article (sync OLTP + async side effects)
**Sync**
1. Validate authorization and optimistic concurrency
2. Update changed `Article` columns and increment `Version`
3. Insert `ArticleRevision` for meaningful edits (policy)
4. MUST insert `OutboxMessage` with event `content.article_updated`

**Async**
- Audit consumes event → writes edit audit log
- SEO may recompute metadata defaults or slug candidate
- Search/projections/cache consumers update derived state idempotently
- For Published articles, `content.article_updated` may require public cache/projection invalidation. Consumers must use `ArticlePublicId` + `Version` to prevent stale overwrite.

### 4.4 Publish (sync OLTP + async side effects)
**Sync**
1. Validate transition (Draft → Published)
2. Update `Article` (`Status='Published'`, `PublishedAt=now`, `Version++`)
3. Insert `ArticleLifecycleEvent` (`ActionType='Publish'`)
4. MUST insert `OutboxMessage` with event `content.article_published`

**Async**
- Audit consumes event → writes audit log
- SEO consumes event → ensures slug/canonical/meta are correct
- Notifications consumes event → optional "new article" emails
- Cache/projection consumers invalidate or refresh derived public reads

**Failure behavior**
- Publish succeeds even if audit/seo/notifications/cache refresh are delayed
- Backlogs are observable; consumers must be idempotent

### 4.5 Unpublish (sync OLTP + async side effects)
**Sync**
1. Validate transition (Published → Draft)
2. Update `Article` (`Status='Draft'`, `UnpublishedAt=now`, `Version++`)
3. Insert `ArticleLifecycleEvent` (`ActionType='Unpublish'`, `Reason` required)
4. MUST insert `OutboxMessage` with event `content.article_unpublished`

**Safety rule**
- Unpublish must not re-expose content even if async SEO/cache invalidation is delayed.
- Public reads must enforce visibility from Content truth, not from SEO or Redis alone.

### 4.6 Archive (sync OLTP + async side effects)
**Sync**
1. Validate transition (Published → Archived or Draft → Archived, per policy)
2. Update `Article` (`Status='Archived'`, `ArchivedAt=now`, `Version++`)
3. Insert `ArticleLifecycleEvent` (`ActionType='Archive'`)
4. MUST insert `OutboxMessage` with event `content.article_archived`

### 4.7 Soft Delete (sync OLTP + async side effects)
**Sync**
1. Validate authorization and retention/governance policy
2. Update `Article` (`IsDeleted=1`, `DeletedAt=now`, `DeletedBy=actor`, `Version++`)
3. Insert `ArticleLifecycleEvent` (`ActionType='SoftDelete'`) when policy requires lifecycle visibility
4. MUST insert `OutboxMessage` with event `content.article_soft_deleted`

**Semantics**
- `content.article_soft_deleted` represents a V1 soft-delete event (`IsDeleted=1`), not physical deletion.
- If retention later physically removes article data, that is a separate purge concern and should use a separate event such as `content.article_purged`.

### 4.8 Read list/detail
- Read path first checks Redis caches (policy), then falls back to SQL truth
- View tracking is non-blocking (Interaction module; async)
- Public reads must only return `Status='Published' AND IsDeleted=0`
- Slug resolution success is not visibility success; public detail reads must verify Content truth before returning an article.

---

## 5) Redis plan (Content V1) — read path priority

> Redis is derived. Content remains the truth store.

### 5.1 Feed caches (short TTL)
- `cn:feed:published:{page}:{size}:{filtersHash}` TTL 30–120s
- `cn:feed:cat:{categoryId}:{page}:{size}` TTL 30–120s
- `cn:feed:tag:{tagId}:{page}:{size}` TTL 30–120s

### 5.2 Article detail cache (longer TTL + invalidate-on-write)
- Public detail: `cn:article:{articlePublicId}:detail` TTL 5–30m
- Internal/admin detail may use `ArticleId` scoped keys only when not shared across public/cross-module boundaries

Invalidate on:
- edit (draft save) if the draft is visible in admin UI caches
- publish/unpublish/archive/soft-delete
- media primary/cover change
- seo canonical/slug change (policy-defined)

### 5.3 Notes (staleness policy)
- Feed caches are short-lived to avoid complex invalidation.
- Detail caches should be invalidated explicitly on publish/unpublish/archive/soft-delete.
- Stale Redis entries must never expose Draft, Archived, Unpublished, or soft-deleted articles.
- When visibility freshness is uncertain, public reads must fall back to Content truth or return safe 404.
- Redis values may speed reads, but Redis must not become hidden truth for Article visibility.

---

## 6) Entities (data dictionary)

> Keep current state in `Article`, keep content edits in `ArticleRevision`, and keep governance actions in `ArticleLifecycleEvent`. This separation prevents audit confusion and simplifies future workflow changes.

### 6.1 Article (Aggregate Root)
**Role:** current article state and aggregate version authority.

| Field | Type | Null | Default | Notes |
|---|---|---:|---|---|
| ArticleId | BIGINT IDENTITY | NO |  | Internal SQL PK |
| ArticlePublicId | CHAR(26) | NO | generated ULID | Stable public/cross-module identity; immutable |
| CategoryId | BIGINT | NO |  | FK → Category |
| AuthorUserId | BIGINT | NO |  | cross-module ref → Identity.UserAccount |
| Title | NVARCHAR(300) | NO |  | |
| Summary | NVARCHAR(1000) | YES |  | |
| Body | NVARCHAR(MAX) | NO |  | |
| Status | VARCHAR(20) | NO | `'Draft'` | Draft/Published/Archived |
| PublishedAt | DATETIME2(3) | YES |  | required when Published |
| UnpublishedAt | DATETIME2(3) | YES |  | |
| ArchivedAt | DATETIME2(3) | YES |  | |
| CoverMediaId | BIGINT | YES |  | cross-module hook → Media |
| Version | INT | NO | `1` | optimistic concurrency + event ordering |
| CreatedAt | DATETIME2(3) | NO | SYSUTCDATETIME() | |
| CreatedBy | BIGINT | YES |  | |
| UpdatedAt | DATETIME2(3) | NO | SYSUTCDATETIME() | |
| UpdatedBy | BIGINT | YES |  | |
| IsDeleted | BIT | NO | `0` | soft delete |
| DeletedAt | DATETIME2(3) | YES |  | |
| DeletedBy | BIGINT | YES |  | |

**Identifier rules**
- `ArticleId` is internal storage identity.
- `ArticlePublicId` is required for public API responses, cross-module references, event aggregate IDs, cache keys shared across boundaries, and logs shared across modules.
- `ArticlePublicId` is immutable and never reused.

### 6.2 Category
*(unchanged from ERD; taxonomy with optional self-parent hierarchy)*

### 6.3 Tag
*(unchanged from ERD; stable `Slug` for filtering and routes where policy allows)*

### 6.4 ArticleTag
*(unchanged from ERD; composite PK `(ArticleId, TagId)` ensures idempotent attach)*

### 6.5 ArticleRevision (append-only)
**Role:** edit history for investigation and audit support.

V1 stores the previous snapshot before a meaningful edit. In the current ERD this means `OldTitle`, `OldSummary`, and `OldBody` contain the values before the edit is applied; it is not a full post-edit version snapshot.

**V2 option:** if revision browsing needs complete version reconstruction, add explicit fields such as `RevisionNumber`, `TitleSnapshot`, `SummarySnapshot`, and `BodySnapshot`.

### 6.6 ArticleLifecycleEvent (append-only)
**Role:** governance history for publish/unpublish/archive/soft-delete actions.

| Field | Type | Null | Default | Notes |
|---|---|---:|---|---|
| EventId | BIGINT IDENTITY | NO |  | PK |
| ArticleId | BIGINT | NO |  | FK → Article |
| ArticleVersion | INT | NO |  | Article version after the lifecycle transition |
| ActionType | VARCHAR(30) | NO |  | Publish/Unpublish/Archive/SoftDelete/... |
| FromStatus | VARCHAR(20) | YES |  | |
| ToStatus | VARCHAR(20) | YES |  | |
| Reason | NVARCHAR(500) | YES |  | required for Unpublish |
| ActorUserId | BIGINT | NO |  | cross-module ref → Identity.UserAccount |
| OccurredAt | DATETIME2(3) | NO | SYSUTCDATETIME() | |
| CorrelationId | NVARCHAR(100) | YES |  | |
| MetadataJson | NVARCHAR(MAX) | YES |  | optional structured context |

`ArticleLifecycleEvent.ArticleVersion`, `Article.Version`, and `OutboxMessage.Version` must align for the same lifecycle transition. This makes lifecycle logs useful for debugging replay, stale events, and projection state.

---

## 7) Invariants (must always hold)

### 7.1 Lifecycle correctness
- Status ∈ `Draft | Published | Archived`
- Allowed transitions (V1)
  - Draft → Published
  - Published → Draft (Unpublish)
  - Published → Archived
  - Draft → Archived (optional)
  - Archived → (no transitions in V1)
- PublishedAt coherence:
  - If `Status='Published'` ⇒ `PublishedAt IS NOT NULL`
- Unpublish requires reason:
  - Any Unpublish action must store non-empty `Reason` in lifecycle log
- Public visibility:
  - Public reads only expose `Status='Published' AND IsDeleted=0`

### 7.2 Identifier correctness
- `Article.ArticlePublicId` is required, unique, immutable, and never reused.
- Public APIs and cross-module contracts use `ArticlePublicId` as the stable Article identity.
- Slug may be included as a routing/display concern, but slug is not Article identity.

### 7.3 Version-aware lifecycle
- `Article.Version` is the per-aggregate ordering authority for Article lifecycle events.
- Consumers must not rely on timestamp order or broker arrival order.
- Every Article outbox event that represents an aggregate transition carries:
  - `AggregateType = "Article"`
  - `AggregateId = ArticlePublicId`
  - `Version = Article.Version`
- Every `ArticleLifecycleEvent` for a lifecycle transition stores `ArticleVersion = Article.Version`.
- Consumers receiving stale or out-of-order versions must ignore, delay, or resync from Content truth according to ADR-0013.

### 7.4 Consistency
- No orphan category: `Article.CategoryId` references existing `Category`
- No orphan tag link: `ArticleTag` references existing `Article` & `Tag`
- Unique attachment: `(ArticleId, TagId)` unique
- Category no self-parent; no-cycles enforced in app layer

### 7.5 Auditability
- Meaningful edits create `ArticleRevision` (policy)
- Lifecycle actions create `ArticleLifecycleEvent`
- Both are append-only
- Traceability present (who/when/correlation)

---

## 8) Constraints (DB-level enforcement)

### 8.1 PKs
- Article(ArticleId)
- Category(CategoryId)
- Tag(TagId)
- ArticleTag(ArticleId, TagId)
- ArticleRevision(RevisionId)
- ArticleLifecycleEvent(EventId)

### 8.2 FKs (recommended delete behavior)
- Article.CategoryId → Category.CategoryId (**NO ACTION**)
- ArticleTag.ArticleId → Article.ArticleId (**NO ACTION** in audit-grade systems)
- ArticleTag.TagId → Tag.TagId (**NO ACTION**)
- ArticleRevision.ArticleId → Article.ArticleId (**NO ACTION**, preserve history)
- ArticleLifecycleEvent.ArticleId → Article.ArticleId (**NO ACTION**, preserve governance)
- Category.ParentCategoryId → Category.CategoryId (**NO ACTION**)

> Note: Prefer **soft delete** for Article; avoid cascades that erase history accidentally.

### 8.3 UNIQUE
- Article.ArticlePublicId unique (`UQ_Article_ArticlePublicId`)
- Tag.Slug unique (`UQ_Tag_Slug`)
- Optional: Category.Slug unique if used in routes

### 8.4 CHECK
- Article.Status enum: `IN ('Draft','Published','Archived')`
- ArticlePublicId format:
  - `LEN(ArticlePublicId) = 26` (ULID format validation may be app-level or DB-level by policy)
- PublishedAt required when Published:
  - `(Status <> 'Published') OR (PublishedAt IS NOT NULL)`
- Category no self-parent:
  - `(ParentCategoryId IS NULL) OR (ParentCategoryId <> CategoryId)`
- Lifecycle reason for Unpublish:
  - `(ActionType <> 'Unpublish') OR (Reason IS NOT NULL AND LEN(LTRIM(RTRIM(Reason))) > 0)`
- Lifecycle article version:
  - `ArticleVersion > 0`

---

## 9) Indexes (query-driven)

> Chosen to match public feed, filtering, admin lists, and investigations.

### 9.1 Public identity lookup
- **UQ_Article_ArticlePublicId**
  - Key: `(ArticlePublicId)`

### 9.2 Public feed (Published)
- **IX_Article_Status_PublishedAt**
  - Key: `(Status, PublishedAt DESC)`
  - Include (optional): `(ArticleId, ArticlePublicId, Title, Summary, CategoryId, CoverMediaId, AuthorUserId)`

If using soft delete, prefer SQL Server filtered index:
- **IX_Article_Published_NotDeleted** (filtered)
  - Key: `(PublishedAt DESC)`
  - Include (optional): `(ArticleId, ArticlePublicId, Title, Summary, CategoryId, CoverMediaId, AuthorUserId)`
  - Filter: `WHERE IsDeleted = 0 AND Status = 'Published'`

### 9.3 Filter by category
- **IX_Article_Category_Status_PublishedAt**
  - Key: `(CategoryId, Status, PublishedAt DESC)`
  - Include: `(ArticleId, ArticlePublicId, Title, Summary, CoverMediaId)`

### 9.4 Admin list by author
- **IX_Article_Author_CreatedAt**
  - Key: `(AuthorUserId, CreatedAt DESC)`
  - Include: `(ArticleId, ArticlePublicId, Status, Title)`

### 9.5 Filter by tag
- **IX_ArticleTag_TagId_ArticleId**
  - Key: `(TagId, ArticleId)`

### 9.6 Revisions and lifecycle history
- **IX_ArticleRevision_ArticleId_EditedAt**
  - Key: `(ArticleId, EditedAt DESC)`
- **IX_LifecycleEvent_ArticleId_OccurredAt**
  - Key: `(ArticleId, OccurredAt DESC)`
  - Include (optional): `(ArticleVersion, ActionType, FromStatus, ToStatus, ActorUserId, Reason)`
- **IX_LifecycleEvent_Actor_OccurredAt**
  - Key: `(ActorUserId, OccurredAt DESC)`
  - Include (optional): `(ArticleId, ArticleVersion, ActionType, Reason)`

---

## 10) Integration event baseline (Content V1)

### 10.1 Event type constants

V1 Phase 1 implementation only emits Article events. Category and Tag event names are reserved for later phases.

```csharp
public static class ContentIntegrationEventTypes
{
    public const string ArticleCreated = "content.article_created";
    public const string ArticleUpdated = "content.article_updated";
    public const string ArticlePublished = "content.article_published";
    public const string ArticleUnpublished = "content.article_unpublished";
    public const string ArticleArchived = "content.article_archived";
    public const string ArticleSoftDeleted = "content.article_soft_deleted";

    // Reserved for retention purge / physical deletion workflows, not emitted by V1 Phase 1.
    public const string ArticlePurged = "content.article_purged";

    public const string CategoryCreated = "content.category_created";
    public const string CategoryUpdated = "content.category_updated";
    public const string CategorySoftDeleted = "content.category_soft_deleted";

    public const string TagCreated = "content.tag_created";
    public const string TagUpdated = "content.tag_updated";
    public const string TagSoftDeleted = "content.tag_soft_deleted";
}
```

### 10.2 Outbox envelope baseline

```text
EventType     = content.article_published
AggregateType = Article
AggregateId   = {ArticlePublicId}
Version       = {Article.Version}
OccurredAtUtc = {now}
CorrelationId = {request/workflow correlation id}
PayloadJson   = {...}
```

### 10.3 Article published payload sample

```csharp
public sealed record ArticlePublishedIntegrationEventPayload(
    long ArticleId,
    string ArticlePublicId,
    long Version,
    string Title,
    string? Summary,
    long? CategoryId,
    long? AuthorUserId,
    long? CoverMediaId,
    DateTimeOffset PublishedAtUtc
);
```

### 10.4 Slug relationship

If slug is managed by SEO, Content events must not depend on slug as Article identity. A Content event may carry `SlugCandidate` as optional metadata where useful, but the stable identity remains `ArticlePublicId`.

---

## 11) Evolution rules (V1) — safe change over time (DDIA Ch4)

### 11.1 Add-only schema changes
- Prefer adding nullable fields or fields with defaults
- Avoid rename/type changes; deprecate instead

### 11.2 Rolling upgrade safety (avoid silent data loss)
- Use **partial updates** (update only changed columns) to avoid dropping newer fields
- Keep optimistic concurrency (`Version`) to detect lost updates
- Keep `ArticlePublicId` immutable across migrations

### 11.3 Event evolution
- Event payloads are add-only; consumers ignore unknown fields
- Use message envelope: `messageId/type/version/occurredAt/correlationId/payload`
- Do not remove `ArticlePublicId`, `AggregateId`, or `Version` from Article events

---

## 12) Retention & operational jobs (V1 policy)

### 12.1 Append-only histories
- `ArticleRevision` and `ArticleLifecycleEvent` are append-only.
- Retention window must be defined (policy-level) and enforced via purge/archive jobs.

### 12.2 Cleanup jobs
- Optional scheduled jobs:
  - purge soft-deleted content after retention (if required)
  - archive old revisions/events to cheaper storage (V2+)
  - compact or expire derived Redis entries; never treat cache cleanup as truth cleanup

---

## 13) V2 extension points (no breaking changes)
- Scheduled publishing: `PublishAt` or `ArticleSchedule`
- Localization: `ArticleTranslation`
- Contributors: `ArticleContributor`
- Workflow: extend `ActionType` values and policy
- Idempotency: `CommandDeduplication` unique on `IdempotencyKey`
- Revision storage: snapshot ↔ patch via ADR (keep backward compatibility)
- Slug aliases/redirect history in SEO module

---

## 14) ADR-aligned decisions to lock
- Unpublish semantics: Published→Draft (V1) vs Unpublished state (V2)
- Revision strategy: snapshot vs patch
- Soft delete vs archive boundary
- Cross-module FK enforcement: DB FK vs application contract (depends on deployment)
- Outbox is required for Article events with async side effects, per ADR-0013 and ADR-0018
- Public/cross-module Article identity is `ArticlePublicId`, per ADR-0014
- Article lifecycle event ordering is per aggregate using `Article.Version`

---

## 15) ERD (dbdiagram.io)

See: `../diagrams/erd/content-v1.dbml`

How to render:
1. Open dbdiagram.io
2. Copy DBML content from the file above
3. Paste into dbdiagram.io to view/export
