# System Data Model — Content (V1)

> **Recommended file:** `explanation/architecture/arc42/system-data/system-data-content-v1.md`  
> **Module:** Content Management  
> **Purpose:** Core content lifecycle (create/edit/publish/unpublish), taxonomy, and audit-grade history — optimized for **read path performance** and **safe evolution**.

---

## 0) Data System fit (V1)

Content is the **read-heavy OLTP core**. It must remain fast under bursts and correct under governance rules.

- **Truth store:** SQL Server (row-store; index-driven reads)
- **Async side effects (non-blocking):** Audit, SEO, Notifications via Outbox + broker (recommended)
- **Redis:** public feed + detail cache, plus slug routing cache (via SEO module)
- **Append-only:** revisions + lifecycle events (retention-driven)

**Non-negotiables (from Quality Requirements)**
- Read path priority (P95/P99) — interaction/audit/email must not degrade reading
- Governance actions must be traceable (publish/unpublish reasons)
- Evolvability: rolling upgrades require backward/forward compatible schema changes

---

## 1) Scope & boundaries (V1)

### In scope (V1)
- **Article** current state (Draft/Published/Archived)
- **Category** (taxonomy; optional hierarchy)
- **Tag** + attach/detach via **ArticleTag**
- **ArticleRevision** (append-only edit history)
- **ArticleLifecycleEvent** (append-only governance history; reasons required for sensitive actions)

### Cross-module references
- **Identity** owns users; Content stores IDs:
  - `Article.AuthorUserId`
  - `ArticleRevision.EditedBy`
  - `ArticleLifecycleEvent.ActorUserId`
- **Media/SEO** are external:
  - Content keeps **nullable hooks** such as `Article.CoverMediaId` (media)  
  - Slug/canonical lives in SEO module (recommended), accessed via contract

---

## 2) Capability → Data mapping

### 2.1 Create Articles (draft first)
- `Article` stores current state
- `Category` classifies
- `Tag` + `ArticleTag` attach safely

### 2.2 Edit Articles (draft save) + edit history
- `Article` stores current state
- `ArticleRevision` stores edit trail (append-only)
  - actor + timestamp + correlationId
  - V1: snapshot (investigation-friendly)
  - V2: patch/diff (`PatchJson`) if needed

### 2.3 Publish / Unpublish + record reasons
- `Article` stores `Status`, `PublishedAt`, `UnpublishedAt`, `ArchivedAt`
- `ArticleLifecycleEvent` stores governance actions separately:
  - from→to status, actor, correlationId
  - **Reason required for Unpublish**

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
- Detail: by `ArticleId` (V1) and/or by slug via SEO module (V1 routing)

### 3.2 Admin workflows (write-light, correctness-heavy)
- Draft list: by author/status/createdAt
- Publish/unpublish actions (governance + reason)
- View revision timeline and lifecycle timeline for investigations

### 3.3 Investigation queries (append-only logs)
- Revisions by `ArticleId` ordered by `EditedAt DESC`
- Lifecycle events by `ArticleId` ordered by `OccurredAt DESC`
- Lifecycle events by `ActorUserId` ordered by time

---

## 4) Dataflows (V1) — REST / DB / Broker (DDIA Ch4)

### 4.1 Create/Edit Draft (sync OLTP)
- API validates authorization (Authorization module)
- Writes:
  - `Article` update/insert
  - `ArticleRevision` insert (append-only) for meaningful edits (policy)

### 4.2 Publish (sync OLTP + async side effects)
**Sync (API → DB)**
1) Validate transition (Draft → Published)
2) Update `Article` (`Status='Published'`, `PublishedAt=now`, `Version++`)
3) Insert `ArticleLifecycleEvent` (`ActionType='Publish'`)
4) (Recommended) Insert `OutboxMessage` with event `ArticlePublished`

**Async (Worker/Consumers)**
- Audit consumes event → writes audit log
- SEO consumes event → ensures slug/canonical/meta are correct
- Notifications consumes event → optional “new article” emails

**Failure behavior**
- Publish succeeds even if audit/seo/notifications are delayed
- Backlogs are observable; consumers must be idempotent

### 4.3 Unpublish (sync OLTP + async side effects)
**Sync**
1) Validate transition (Published → Draft)
2) Update `Article` (`Status='Draft'`, `UnpublishedAt=now`, `Version++`)
3) Insert `ArticleLifecycleEvent` (`ActionType='Unpublish'`, `Reason` required)
4) (Recommended) Outbox event `ArticleUnpublished`

**Safety rule**
- Unpublish must not re-expose content even if async SEO is delayed (read path filters by truth store)

### 4.4 Read list/detail
- Read path first checks Redis caches (policy), falls back to SQL
- View tracking is non-blocking (Interaction module; async)

---

## 5) Redis plan (Content V1) — read path priority

> Redis is derived. Content remains the truth store.

### 5.1 Feed caches (short TTL)
- `cn:feed:published:{page}:{size}:{filtersHash}` TTL 30–120s
- `cn:feed:cat:{categoryId}:{page}:{size}` TTL 30–120s
- `cn:feed:tag:{tagId}:{page}:{size}` TTL 30–120s

### 5.2 Article detail cache (longer TTL + invalidate-on-write)
- `cn:article:{articleId}:detail` TTL 5–30m  
Invalidate on:
- edit (draft save) if the draft is visible in admin UI caches
- publish/unpublish/archive
- media primary/cover change
- seo canonical/slug change (policy-defined)

### 5.3 Notes (staleness policy)
- Feed caches are short-lived to avoid complex invalidation.
- Detail caches should be invalidated explicitly on publish/unpublish.

---

## 6) Entities (data dictionary)

> **Keep current state in `Article`, keep content edits in `ArticleRevision`, and keep governance actions in `ArticleLifecycleEvent`.**  
> This separation prevents audit confusion and simplifies future workflow changes.

### 6.1 Article (Aggregate Root)
*(unchanged — keep your table as-is)*

### 6.2 Category
*(unchanged)*

### 6.3 Tag
*(unchanged)*

### 6.4 ArticleTag
*(unchanged)*

### 6.5 ArticleRevision (append-only)
*(unchanged)*

### 6.6 ArticleLifecycleEvent (append-only)
*(unchanged)*

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

### 7.2 Consistency
- No orphan category: `Article.CategoryId` references existing `Category`
- No orphan tag link: `ArticleTag` references existing `Article` & `Tag`
- Unique attachment: `(ArticleId, TagId)` unique
- Category no self-parent; no-cycles enforced in app layer

### 7.3 Auditability
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
- Tag.Slug unique (`UQ_Tag_Slug`)
- Optional: Category.Slug unique if used in routes

### 8.4 CHECK
- Article.Status enum: `IN ('Draft','Published','Archived')`
- PublishedAt required when Published:
  - `(Status <> 'Published') OR (PublishedAt IS NOT NULL)`
- Category no self-parent:
  - `(ParentCategoryId IS NULL) OR (ParentCategoryId <> CategoryId)`
- Lifecycle reason for Unpublish:
  - `(ActionType <> 'Unpublish') OR (Reason IS NOT NULL AND LEN(LTRIM(RTRIM(Reason))) > 0)`

---

## 9) Indexes (query-driven)

> Chosen to match public feed, filtering, admin lists, and investigations.

### 9.1 Public feed (Published)
- **IX_Article_Status_PublishedAt**
  - Key: `(Status, PublishedAt DESC)`
  - Include (optional): `(ArticleId, Title, Summary, CategoryId, CoverMediaId, AuthorUserId)`

If using soft delete, prefer SQL Server filtered index:
- **IX_Article_Published_NotDeleted** (filtered)
  - Key: `(PublishedAt DESC)`
  - Filter: `WHERE IsDeleted = 0 AND Status = 'Published'`

### 9.2 Filter by category
- **IX_Article_Category_Status_PublishedAt**
  - Key: `(CategoryId, Status, PublishedAt DESC)`
  - Include: `(ArticleId, Title, Summary, CoverMediaId)`

### 9.3 Admin list by author
- **IX_Article_Author_CreatedAt**
  - Key: `(AuthorUserId, CreatedAt DESC)`
  - Include: `(ArticleId, Status, Title)`

### 9.4 Filter by tag
- **IX_ArticleTag_TagId_ArticleId**
  - Key: `(TagId, ArticleId)`

### 9.5 Revisions and lifecycle history
- **IX_ArticleRevision_ArticleId_EditedAt**
  - Key: `(ArticleId, EditedAt DESC)`
- **IX_LifecycleEvent_ArticleId_OccurredAt**
  - Key: `(ArticleId, OccurredAt DESC)`
  - Include (optional): `(ActionType, FromStatus, ToStatus, ActorUserId, Reason)`
- **IX_LifecycleEvent_Actor_OccurredAt**
  - Key: `(ActorUserId, OccurredAt DESC)`
  - Include (optional): `(ArticleId, ActionType, Reason)`

---

## 10) Evolution rules (V1) — safe change over time (DDIA Ch4)

### 10.1 Add-only schema changes
- Prefer adding nullable fields or fields with defaults
- Avoid rename/type changes; deprecate instead

### 10.2 Rolling upgrade safety (avoid silent data loss)
- Use **partial updates** (update only changed columns) to avoid dropping newer fields
- Keep optimistic concurrency (`Version`) to detect lost updates

### 10.3 Event evolution (if using Outbox)
- Event payloads are add-only; consumers ignore unknown fields
- Use message envelope: `messageId/type/version/occurredAt/correlationId/payload`

---

## 11) Retention & operational jobs (V1 policy)

### 11.1 Append-only histories
- `ArticleRevision` and `ArticleLifecycleEvent` are append-only.
- Retention window must be defined (policy-level) and enforced via purge/archive jobs.

### 11.2 Cleanup jobs
- Optional scheduled jobs:
  - purge soft-deleted content after retention (if required)
  - archive old revisions/events to cheaper storage (V2+)

---

## 12) V2 extension points (no breaking changes)
- Scheduled publishing: `PublishAt` or `ArticleSchedule`
- Localization: `ArticleTranslation`
- Contributors: `ArticleContributor`
- Workflow: extend `ActionType` values and policy
- Idempotency: `CommandDeduplication` unique on `IdempotencyKey`
- Revision storage: snapshot ↔ patch via ADR (keep backward compatibility)

---

## 13) ADR candidates (explicit decisions to lock)
- Unpublish semantics: Published→Draft (V1) vs Unpublished state (V2)
- Revision strategy: snapshot vs patch
- Soft delete vs archive boundary
- Cross-module FK enforcement: DB FK vs application contract (depends on deployment)
- Outbox adoption for publish/unpublish events (recommended)

---

# System Data Model — Content (V1)

> **Recommended file:** `explanation/architecture/arc42/system-data/system-data-content-v1.md`  
> **Module:** Content Management  
> **Purpose:** Core content lifecycle (create/edit/publish/unpublish), taxonomy, and audit-grade history — optimized for **read path performance** and **safe evolution**.

---

## 0) Data System fit (V1)

Content is the **read-heavy OLTP core**. It must remain fast under bursts and correct under governance rules.

- **Truth store:** SQL Server (row-store; index-driven reads)
- **Async side effects (non-blocking):** Audit, SEO, Notifications via Outbox + broker (recommended)
- **Redis:** public feed + detail cache, plus slug routing cache (via SEO module)
- **Append-only:** revisions + lifecycle events (retention-driven)

**Non-negotiables (from Quality Requirements)**
- Read path priority (P95/P99) — interaction/audit/email must not degrade reading
- Governance actions must be traceable (publish/unpublish reasons)
- Evolvability: rolling upgrades require backward/forward compatible schema changes

---

## 1) Scope & boundaries (V1)

### In scope (V1)
- **Article** current state (Draft/Published/Archived)
- **Category** (taxonomy; optional hierarchy)
- **Tag** + attach/detach via **ArticleTag**
- **ArticleRevision** (append-only edit history)
- **ArticleLifecycleEvent** (append-only governance history; reasons required for sensitive actions)

### Cross-module references
- **Identity** owns users; Content stores IDs:
  - `Article.AuthorUserId`
  - `ArticleRevision.EditedBy`
  - `ArticleLifecycleEvent.ActorUserId`
- **Media/SEO** are external:
  - Content keeps **nullable hooks** such as `Article.CoverMediaId` (media)  
  - Slug/canonical lives in SEO module (recommended), accessed via contract

---

## 2) Capability → Data mapping

### 2.1 Create Articles (draft first)
- `Article` stores current state
- `Category` classifies
- `Tag` + `ArticleTag` attach safely

### 2.2 Edit Articles (draft save) + edit history
- `Article` stores current state
- `ArticleRevision` stores edit trail (append-only)
  - actor + timestamp + correlationId
  - V1: snapshot (investigation-friendly)
  - V2: patch/diff (`PatchJson`) if needed

### 2.3 Publish / Unpublish + record reasons
- `Article` stores `Status`, `PublishedAt`, `UnpublishedAt`, `ArchivedAt`
- `ArticleLifecycleEvent` stores governance actions separately:
  - from→to status, actor, correlationId
  - **Reason required for Unpublish**

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
- Detail: by `ArticleId` (V1) and/or by slug via SEO module (V1 routing)

### 3.2 Admin workflows (write-light, correctness-heavy)
- Draft list: by author/status/createdAt
- Publish/unpublish actions (governance + reason)
- View revision timeline and lifecycle timeline for investigations

### 3.3 Investigation queries (append-only logs)
- Revisions by `ArticleId` ordered by `EditedAt DESC`
- Lifecycle events by `ArticleId` ordered by `OccurredAt DESC`
- Lifecycle events by `ActorUserId` ordered by time

---

## 4) Dataflows (V1) — REST / DB / Broker (DDIA Ch4)

### 4.1 Create/Edit Draft (sync OLTP)
- API validates authorization (Authorization module)
- Writes:
  - `Article` update/insert
  - `ArticleRevision` insert (append-only) for meaningful edits (policy)

### 4.2 Publish (sync OLTP + async side effects)
**Sync (API → DB)**
1) Validate transition (Draft → Published)
2) Update `Article` (`Status='Published'`, `PublishedAt=now`, `Version++`)
3) Insert `ArticleLifecycleEvent` (`ActionType='Publish'`)
4) (Recommended) Insert `OutboxMessage` with event `ArticlePublished`

**Async (Worker/Consumers)**
- Audit consumes event → writes audit log
- SEO consumes event → ensures slug/canonical/meta are correct
- Notifications consumes event → optional “new article” emails

**Failure behavior**
- Publish succeeds even if audit/seo/notifications are delayed
- Backlogs are observable; consumers must be idempotent

### 4.3 Unpublish (sync OLTP + async side effects)
**Sync**
1) Validate transition (Published → Draft)
2) Update `Article` (`Status='Draft'`, `UnpublishedAt=now`, `Version++`)
3) Insert `ArticleLifecycleEvent` (`ActionType='Unpublish'`, `Reason` required)
4) (Recommended) Outbox event `ArticleUnpublished`

**Safety rule**
- Unpublish must not re-expose content even if async SEO is delayed (read path filters by truth store)

### 4.4 Read list/detail
- Read path first checks Redis caches (policy), falls back to SQL
- View tracking is non-blocking (Interaction module; async)

---

## 5) Redis plan (Content V1) — read path priority

> Redis is derived. Content remains the truth store.

### 5.1 Feed caches (short TTL)
- `cn:feed:published:{page}:{size}:{filtersHash}` TTL 30–120s
- `cn:feed:cat:{categoryId}:{page}:{size}` TTL 30–120s
- `cn:feed:tag:{tagId}:{page}:{size}` TTL 30–120s

### 5.2 Article detail cache (longer TTL + invalidate-on-write)
- `cn:article:{articleId}:detail` TTL 5–30m  
Invalidate on:
- edit (draft save) if the draft is visible in admin UI caches
- publish/unpublish/archive
- media primary/cover change
- seo canonical/slug change (policy-defined)

### 5.3 Notes (staleness policy)
- Feed caches are short-lived to avoid complex invalidation.
- Detail caches should be invalidated explicitly on publish/unpublish.

---

## 6) Entities (data dictionary)

> **Keep current state in `Article`, keep content edits in `ArticleRevision`, and keep governance actions in `ArticleLifecycleEvent`.**  
> This separation prevents audit confusion and simplifies future workflow changes.

### 6.1 Article (Aggregate Root)
*(unchanged — keep your table as-is)*

### 6.2 Category
*(unchanged)*

### 6.3 Tag
*(unchanged)*

### 6.4 ArticleTag
*(unchanged)*

### 6.5 ArticleRevision (append-only)
*(unchanged)*

### 6.6 ArticleLifecycleEvent (append-only)
*(unchanged)*

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

### 7.2 Consistency
- No orphan category: `Article.CategoryId` references existing `Category`
- No orphan tag link: `ArticleTag` references existing `Article` & `Tag`
- Unique attachment: `(ArticleId, TagId)` unique
- Category no self-parent; no-cycles enforced in app layer

### 7.3 Auditability
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
- Tag.Slug unique (`UQ_Tag_Slug`)
- Optional: Category.Slug unique if used in routes

### 8.4 CHECK
- Article.Status enum: `IN ('Draft','Published','Archived')`
- PublishedAt required when Published:
  - `(Status <> 'Published') OR (PublishedAt IS NOT NULL)`
- Category no self-parent:
  - `(ParentCategoryId IS NULL) OR (ParentCategoryId <> CategoryId)`
- Lifecycle reason for Unpublish:
  - `(ActionType <> 'Unpublish') OR (Reason IS NOT NULL AND LEN(LTRIM(RTRIM(Reason))) > 0)`

---

## 9) Indexes (query-driven)

> Chosen to match public feed, filtering, admin lists, and investigations.

### 9.1 Public feed (Published)
- **IX_Article_Status_PublishedAt**
  - Key: `(Status, PublishedAt DESC)`
  - Include (optional): `(ArticleId, Title, Summary, CategoryId, CoverMediaId, AuthorUserId)`

If using soft delete, prefer SQL Server filtered index:
- **IX_Article_Published_NotDeleted** (filtered)
  - Key: `(PublishedAt DESC)`
  - Filter: `WHERE IsDeleted = 0 AND Status = 'Published'`

### 9.2 Filter by category
- **IX_Article_Category_Status_PublishedAt**
  - Key: `(CategoryId, Status, PublishedAt DESC)`
  - Include: `(ArticleId, Title, Summary, CoverMediaId)`

### 9.3 Admin list by author
- **IX_Article_Author_CreatedAt**
  - Key: `(AuthorUserId, CreatedAt DESC)`
  - Include: `(ArticleId, Status, Title)`

### 9.4 Filter by tag
- **IX_ArticleTag_TagId_ArticleId**
  - Key: `(TagId, ArticleId)`

### 9.5 Revisions and lifecycle history
- **IX_ArticleRevision_ArticleId_EditedAt**
  - Key: `(ArticleId, EditedAt DESC)`
- **IX_LifecycleEvent_ArticleId_OccurredAt**
  - Key: `(ArticleId, OccurredAt DESC)`
  - Include (optional): `(ActionType, FromStatus, ToStatus, ActorUserId, Reason)`
- **IX_LifecycleEvent_Actor_OccurredAt**
  - Key: `(ActorUserId, OccurredAt DESC)`
  - Include (optional): `(ArticleId, ActionType, Reason)`

---

## 10) Evolution rules (V1) — safe change over time (DDIA Ch4)

### 10.1 Add-only schema changes
- Prefer adding nullable fields or fields with defaults
- Avoid rename/type changes; deprecate instead

### 10.2 Rolling upgrade safety (avoid silent data loss)
- Use **partial updates** (update only changed columns) to avoid dropping newer fields
- Keep optimistic concurrency (`Version`) to detect lost updates

### 10.3 Event evolution (if using Outbox)
- Event payloads are add-only; consumers ignore unknown fields
- Use message envelope: `messageId/type/version/occurredAt/correlationId/payload`

---

## 11) Retention & operational jobs (V1 policy)

### 11.1 Append-only histories
- `ArticleRevision` and `ArticleLifecycleEvent` are append-only.
- Retention window must be defined (policy-level) and enforced via purge/archive jobs.

### 11.2 Cleanup jobs
- Optional scheduled jobs:
  - purge soft-deleted content after retention (if required)
  - archive old revisions/events to cheaper storage (V2+)

---

## 12) V2 extension points (no breaking changes)
- Scheduled publishing: `PublishAt` or `ArticleSchedule`
- Localization: `ArticleTranslation`
- Contributors: `ArticleContributor`
- Workflow: extend `ActionType` values and policy
- Idempotency: `CommandDeduplication` unique on `IdempotencyKey`
- Revision storage: snapshot ↔ patch via ADR (keep backward compatibility)

---

## 13) ADR candidates (explicit decisions to lock)
- Unpublish semantics: Published→Draft (V1) vs Unpublished state (V2)
- Revision strategy: snapshot vs patch
- Soft delete vs archive boundary
- Cross-module FK enforcement: DB FK vs application contract (depends on deployment)
- Outbox adoption for publish/unpublish events (recommended)

---

## 14) ERD (dbdiagram.io)

See: `../diagrams/erd/content-v1.dbml`

How to render:
1. Open dbdiagram.io
2. Copy DBML content from the file above
3. Paste into dbdiagram.io to view/export