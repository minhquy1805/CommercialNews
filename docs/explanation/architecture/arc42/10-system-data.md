# System Data Model — Index (V1)

> Entry point for the **System Data Model** of CommercialNews (V1).  
> This doc defines **system-level data contracts** (ownership + references + evolution rules) and links to **module-level data models**.
>
> **DDIA lens**
> - **Ch1 (Goals):** reliability, scalability (read-heavy), maintainability, operability
> - **Ch2 (Fit):** choose relational/document/search by access patterns, not trends
> - **Ch3 (Workloads):** separate OLTP vs logs vs aggregates to protect read path
> - **Ch4 (Evolution):** backward/forward compatibility + DB/REST/broker/batch dataflows
> - **Ch5 (Replication):** truth vs derived, lag budgets, idempotency, ordering, repair
> - **Ch10 (Batch):** bounded inputs, derived outputs, rebuild/reconciliation, selective materialization, safe publication/cutover

---

## Related

- `../02-constraints.md`
- `../03-building-blocks-modularity.md`
- `../04-runtime-view-v1.md`
- `../09-architecture-style.md`
- `../11-replication-v1.md`
- `../13-transactions-and-consistency-v1.md`
- `../16-batch-processing-and-derived-data-v1.md`
- `../17-dataflow-and-batch-workflows-v1.md`
- `../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../decisions/adr-0015-cache-policy-and-invalidation-redis-v1.md`
- `../../decisions/adr-0025-batch-processing-and-derived-state-policy-v1.md`
- `../../decisions/adr-0026-batch-job-orchestration-and-materialization-policy-v1.md`

---

## 1) Purpose

The System Data Model provides a consistent, architecture-level view of data across modules:

- Shared vocabulary for **entities and relationships**
- Clear **data ownership** (who owns lifecycle and rules)
- Stable **cross-module references** (IDs-only contracts)
- Canonical guidance for **constraints and indexes** (system-contract level)
- Clear separation between **truth**, **append-only logs**, and **derived state**
- Safe foundation for **evolution** (V2+ without breaking V1)
- A data-contract basis for **batch / rebuild / reconciliation workflows**

This is **not** a full physical DB implementation guide.  
Each module page contains detailed schema, constraints, indexes, and DB-specific choices.

---

## 2) Quality-driven intent (V1)

CommercialNews (V1) is constrained by:

- **Read-heavy, bursty public workload** (read path priority)
- **Security-critical Identity flows** (verification/reset/session)
- **Governance & auditability** for admin actions
- **Async side effects** must not block core flows (audit/email/aggregation)
- **SEO correctness** (stable slug/canonical)
- **Derived-state rebuildability** for aggregates, summaries, projections, and repair workflows

**System-level rule:** core writes and public reads must remain functional during partial failures of non-critical subsystems.

**System-level data rule:** truth remains authoritative; derived state may lag, be rebuilt, be reconciled, and be replaced, but must not silently become hidden truth.

---

## 3) Scope (V1)

### Included

- Module-level logical models (entities + relationships)
- Invariants that must always hold (correctness, consistency, auditability)
- Key constraints/index guidance tied to core use-cases (read path first)
- Cross-module reference contracts (e.g., `AuthorUserId → Identity.UserId`)
- Evolution rules for rolling upgrades (compatibility)
- **Replication artifacts and contracts** (truth vs derived, outbox, checkpoints)
- **Derived-state categories** relevant to aggregation, replay, rebuild, reconciliation, and retention
- **Workflow-facing data distinctions** such as reusable datasets vs internal intermediate state

### Deferred (V2+)

- Full-text search pipelines (if introduced)
- Dedicated analytics/warehouse schemas (beyond minimal aggregates)
- Advanced moderation/workflow expansions
- Detailed lineage and archival beyond policy-level retention
- First-class projection-serving contracts that would elevate some derived outputs into more formal read-model responsibilities

---

## 4) System Data System Snapshot (V1)

> A compact “Figure 1-1” view for CommercialNews data systems.  
> Details live in module pages and ADRs.

### 4.1 Workload separation (DDIA Ch3 / Ch10)

- **OLTP (truth):** user-facing read/write (index-driven)
- **Append-only logs:** audit/login/view events (retention-driven)
- **Aggregates / derived outputs:** dashboard/trending reads, summaries, projections, rebuild artifacts
- **Workflow-private intermediate state:** temporary stage outputs used only inside one bounded workflow

### 4.2 Dataflow modes (DDIA Ch4 / Ch10)

- **REST:** clients/admin → Public API
- **DB:** API/Worker ↔ stores
- **Broker:** API/Worker ↔ message broker (async side effects)
- **Batch/dataflow:** Worker ↔ truth/log/derived datasets for bounded rebuild/reconciliation/aggregation/cleanup workflows

### 4.3 Reliability baseline (V1)

- Async handlers are **retry-safe** and **idempotent**
- Prefer **Outbox pattern** for side effects (audit/email/aggregation) so core flows do not block
- Redis is allowed for: **cache, counters, rate-limit, dedup** (not source of truth)
- Derived outputs are allowed to lag, but important ones must be:
  - observable
  - rebuildable
  - publish-safe
  - replaceable when workflow policy requires it

### 4.4 Replication baseline (DDIA Ch5, V1)

CommercialNews uses **single-leader truth + event-driven replication**:

- Truth tables are the **source of record** and enforce invariants.
- Derived stores may lag and must be **rebuildable** and **observable**.
- Cross-module replication uses **Outbox → Broker → Consumers**:
  - domain state change and outbox record are committed atomically (same transaction)
  - Background Worker publishes and consumes at-least-once
  - consumers must be idempotent; ordering is **per aggregate** when required

### 4.5 Batch / derived-state baseline (DDIA Ch10, V1)

CommercialNews treats batch as a first-class lane for:

- bounded aggregation
- replay / repair
- rebuild
- reconciliation
- archival / cleanup

The data model must therefore support:

- bounded input selection
- reusable derived outputs where justified
- internal intermediate state that is clearly non-authoritative
- candidate-before-publication for correctness-sensitive derived outputs
- recompute and replay where safer/simpler than fragile mutation

> Implementation choices (SQL/Mongo/Redis/RabbitMQ) should be captured as ADRs unless fixed by constraints.

---

## 5) Conventions

### 5.1 Data ownership

- Each entity/table belongs to exactly **one module** (the owner).
- Other modules may **reference** it but do not control its lifecycle.

### 5.2 Cross-module references

Cross-module references are recorded as IDs in the referencing module.

Examples:

- `Content.Article.AuthorUserId` → `Identity.User.UserId`
- `Content.Article.CoverMediaId` → `Media.Media.MediaId` *(nullable hook in V1)*
- `SEO.SeoMetadata.ArticleId` → `Content.Article.ArticleId`

**Rule:** cross-module FKs are enforced by **contracts and application policies**.  
DB-level foreign keys depend on deployment boundaries (shared DB vs split DB).

### 5.3 IDs & timestamps

- IDs are stable, non-reused primary identifiers.
- Use `CreatedAt/UpdatedAt` consistently.
- Use `SYSUTCDATETIME()` for system timestamps (UTC at rest).

### 5.4 History & audit

- History tables are **append-only** unless an explicit retention policy exists.
- Sensitive governance actions must be traceable with:
  - `ActorUserId`
  - `OccurredAt`
  - `CorrelationId`
  - `Reason` (when required)

### 5.5 Truth vs derived conventions

- **Truth** defines correctness and ownership.
- **Derived state** exists for acceleration, summarization, reporting, or maintenance support.
- Derived state may be:
  - stale
  - rebuilt
  - replaced
  - reconciled
  - temporarily unavailable
- Derived state must not quietly become hidden truth for:
  - publication visibility
  - security state
  - governance truth
  - owning-module invariants

### 5.6 Reusable dataset vs internal intermediate state

- A **reusable dataset** has independent value, identifiable ownership, and explicit contract/retention/freshness semantics.
- **Internal intermediate state** is stage-local, disposable, and not intended for broad reuse.
- Not every batch stage output should be promoted to a named reusable dataset.

### 5.7 Replication contracts (DDIA Ch5)

- **Truth vs derived:** truth tables define correctness; derived data may be stale.
- **Lag is expected:** modules must define lag budgets and fallback behavior.
- **At-least-once delivery:** consumers must be idempotent; duplicates are normal.
- **Per-aggregate ordering:** when state transitions are ordered, events carry `AggregateId + Version`.
- **Causality / prefix safety:** derived “effects” (notify/index/cache warm) must not appear before “causes” (truth exists).

### 5.8 Batch/dataflow contracts (DDIA Ch10)

- **Bounded input:** important workflows must define bounded input explicitly.
- **Derived output:** batch outputs are derived by default, not authoritative truth.
- **Selective materialization:** materialize when reuse/checkpoint/publication justifies it.
- **Candidate-before-cutover:** correctness-sensitive derived output should be validated before publication.
- **Rerun safety:** important rebuild/reconciliation workflows must tolerate rerun/replay safely.

### 5.9 Evolution & versioning (DDIA Ch4)

- V1 docs use suffix `-v1`.
- Rolling upgrades imply old+new code coexist; therefore:
  - **Backward compatible:** new code reads old data
  - **Forward compatible:** old code tolerates new data
- Breaking schema changes require V2 docs (or ADR if an exception is justified).

---

## 6) Replication artifacts and workflow-facing data structures (V1)

This section lists **system-level replication and workflow-supporting data structures** and how they are used.

### 6.1 Outbox (required pattern)

- Outbox is the authoritative **replication log** for async side effects.
- Outbox records are written in the same transaction as the domain change.
- Workers publish outbox messages to the broker and retry safely.

Recommended fields (contract-level):

- `MessageId` (unique)
- `EventType`
- `AggregateType`, `AggregateId`
- `Version` (when ordering matters)
- `OccurredAt`
- `CorrelationId`
- `Status`, `NextRetryAt`, `AttemptCount` (operability)

### 6.2 Inbox / dedup store (recommended)

To prevent duplicate effects under at-least-once delivery:

- Consumers maintain a dedupe mechanism keyed by `MessageId` (or a business idempotency key).
- Storage can be:
  - a small table (preferred for durability), or
  - Redis set with TTL (acceptable for non-critical effects)

### 6.3 Ingestion checkpoints (V2 hook; optional in V1)

For projections/materialized views:

- `IngestionCheckpoint(ConsumerName, LastMessageId/LastVersion, UpdatedAt)`

This enables:

- bootstrap new consumers (snapshot + catch-up)
- measuring freshness precisely (checkpoint age)

### 6.4 Candidate output / publication markers (policy-level)

For correctness-sensitive derived outputs, the architecture may use candidate/publication state such as:

- candidate version markers
- active output version markers
- cutover timestamps
- publication status

These allow:

- build candidate first
- validate candidate output
- publish/cut over safely
- reason about active derived output version

### 6.5 Workflow-private temporary state (policy-level)

For multi-stage workflows, temporary state may exist as:

- staging rows
- temporary grouping outputs
- internal repair candidate sets
- bounded work partitions

These are valid architectural concepts but remain:

- non-authoritative
- private to the workflow unless explicitly promoted
- retention-managed or disposable

### 6.6 Repair & reconciliation (policy-level)

- **Read repair** for caches: refresh/overwrite stale cache on read when safe.
- **Reconciliation jobs** for rarely-read derived data.
- **Repair candidate sets** may be produced and validated before application where correctness-sensitive derived state is involved.

---

## 7) Module map (V1)

Module-level System Data Model documents:

- [Content — system-data-content-v1](./system-data/system-data-content-v1.md)
- [SEO — system-data-seo-v1](./system-data/system-data-seo-v1.md)
- [Media — system-data-media-v1](./system-data/system-data-media-v1.md)
- [Identity — system-data-identity-v1](./system-data/system-data-identity-v1.md)
- [Authorization — system-data-authorization-v1](./system-data/system-data-authorization-v1.md)
- [Interaction — system-data-interaction-v1](./system-data/system-data-interaction-v1.md)
- [Reading Experience — system-data-reading-v1](./system-data/system-data-reading-v1.md)
- [Notifications — system-data-notifications-v1](./system-data/system-data-notifications-v1.md)
- [Audit — system-data-audit-v1](./system-data/system-data-audit-v1.md)

> If a module is not implemented yet, keep the file with a short placeholder:  
> scope + planned entities + known cross-module references.

---

## 8) System ERD Overview (V1)

> **Goal:** module-boundary view (ownership + cross-module references).  
> **How to render:** copy DBML into dbdiagram.io.  
> **Note:** this is an overview diagram, not a full physical ERD and not a store-placement diagram.

```dbml
// =====================================================
// CommercialNews — System Overview (V1) — Module Boundary Map
// =====================================================

// Content
Table Content_Article {
  ArticleId bigint [pk]
  CategoryId bigint
  AuthorUserId bigint
  Status varchar
  PublishedAt datetime
  IsDeleted bit
}

Table Content_Category {
  CategoryId bigint [pk]
  ParentCategoryId bigint
}

Table Content_Tag {
  TagId bigint [pk]
  Slug nvarchar
}

Table Content_ArticleTag {
  ArticleId bigint
  TagId bigint

  Indexes {
    (ArticleId, TagId) [pk]
  }
}

// SEO
Table Seo_SlugRegistry {
  SlugId bigint [pk]
  ArticleId bigint
  Scope varchar
  Slug nvarchar
  IsActive bit

  Indexes {
    (Scope, Slug) [unique]
  }
}

Table Seo_SeoMetadata {
  SeoId bigint [pk]
  ArticleId bigint
}

// Media
Table Media_MediaAsset {
  MediaId bigint [pk]
  Url nvarchar
  MediaType varchar
  IsDeleted bit
}

Table Media_ArticleMedia {
  ArticleMediaId bigint [pk]
  ArticleId bigint
  MediaId bigint
  SortOrder int
  IsPrimary bit
  IsDeleted bit
}

// Interaction
Table Interaction_Stats {
  ArticleId bigint [pk]
  ViewsTotal bigint
  LikesTotal bigint
  CommentsTotal bigint
  PopularityScore float
}

Table Interaction_Comment {
  CommentId bigint [pk]
  ArticleId bigint
  UserId bigint
  ParentCommentId bigint
  Status varchar
}

// Identity
Table Identity_UserAccount {
  UserId bigint [pk]
  EmailNormalized nvarchar
  Status varchar
  IsEmailVerified bit
}

Table Identity_RefreshToken {
  RefreshTokenId bigint [pk]
  UserId bigint
  TokenHash varbinary
}

Table Identity_LoginHistory {
  LoginId bigint [pk]
  UserId bigint
  Succeeded bit
  AttemptedAt datetime
}

// Authorization
Table Auth_Role {
  RoleId bigint [pk]
  Name nvarchar
}

Table Auth_Permission {
  PermissionId bigint [pk]
  Key nvarchar
}

Table Auth_UserRole {
  UserId bigint
  RoleId bigint

  Indexes {
    (UserId, RoleId) [pk]
  }
}

Table Auth_RolePermission {
  RoleId bigint
  PermissionId bigint

  Indexes {
    (RoleId, PermissionId) [pk]
  }
}

// Audit
Table Audit_AuditLog {
  AuditLogId bigint [pk]
  PublicId char(26) [unique]
  MessageId char(26) [unique]
  EventType nvarchar
  SourceModule nvarchar
  Action nvarchar
  ActionCategory nvarchar
  ActorInternalId bigint
  ActorUserId char(26)
  ResourceType nvarchar
  ResourceId nvarchar
  Outcome varchar
  Severity varchar
  RiskLevel varchar
  Summary nvarchar
  CorrelationId nvarchar
  OccurredAtUtc datetime
  IngestedAtUtc datetime
  CreatedAtUtc datetime
  SanitizedPayloadJson nvarchar

  Indexes {
    (PublicId) [unique]
    (MessageId) [unique]
  }
}

// Notifications
Table Notif_OutboxMessage {
  OutboxId bigint [pk]
  MessageKey uuid
  EventType nvarchar
  AggregateType nvarchar
  AggregateId nvarchar
  Status varchar
  Priority int
  NextRetryAt datetime

  Indexes {
    (MessageKey) [unique]
  }
}

Table Notif_EmailDelivery {
  EmailDeliveryId bigint [pk]
  MessageKey uuid
  UserId bigint
  ToEmail nvarchar
  TemplateKey nvarchar
  Status varchar
  AttemptCount int
  SentAt datetime

  Indexes {
    (MessageKey) [unique]
  }
}

// Relationships (cross-module refs)
Ref: Content_Article.CategoryId > Content_Category.CategoryId
Ref: Content_ArticleTag.ArticleId > Content_Article.ArticleId
Ref: Content_ArticleTag.TagId > Content_Tag.TagId

Ref: Seo_SlugRegistry.ArticleId > Content_Article.ArticleId
Ref: Seo_SeoMetadata.ArticleId > Content_Article.ArticleId

Ref: Media_ArticleMedia.ArticleId > Content_Article.ArticleId
Ref: Media_ArticleMedia.MediaId > Media_MediaAsset.MediaId

Ref: Interaction_Stats.ArticleId > Content_Article.ArticleId
Ref: Interaction_Comment.ArticleId > Content_Article.ArticleId
Ref: Interaction_Comment.ParentCommentId > Interaction_Comment.CommentId

Ref: Content_Article.AuthorUserId > Identity_UserAccount.UserId
Ref: Interaction_Comment.UserId > Identity_UserAccount.UserId

Ref: Identity_RefreshToken.UserId > Identity_UserAccount.UserId
Ref: Identity_LoginHistory.UserId > Identity_UserAccount.UserId

Ref: Auth_UserRole.UserId > Identity_UserAccount.UserId
Ref: Auth_UserRole.RoleId > Auth_Role.RoleId
Ref: Auth_RolePermission.RoleId > Auth_Role.RoleId
Ref: Auth_RolePermission.PermissionId > Auth_Permission.PermissionId

Ref: Audit_AuditLog.ActorUserId > Identity_UserAccount.UserId

Ref: Notif_EmailDelivery.MessageKey > Notif_OutboxMessage.MessageKey
Ref: Notif_EmailDelivery.UserId > Identity_UserAccount.UserId
```

## 9) Cross-module data contracts (summary)

This section captures the most important **system-level data dependencies**.  
Implementation details live in each module page.

### 9.1 Identity ↔ Content

- Content references Identity for author/editor/actor fields.
- Identity does not depend on Content.

### 9.2 Content ↔ SEO

- SEO references Content for slug/canonical/meta/social metadata.
- Content must not depend on SEO to write core content (SEO is a sidecar concern).

### 9.3 Content ↔ Media

- Content references Media for cover/thumbnail via nullable hooks.
- Media references Content for ownership/attachment (policy-defined).

### 9.4 Content ↔ Interaction / Reading

- Interaction references Content for comments/likes.
- Reading references Content for public rendering and view-tracking signals.

### 9.5 Notifications / Audit

- Notifications and Audit subscribe to events across modules (async).
- Core writes must not fail if these subsystems are unavailable.

### 9.6 Truth / derived relationships

- Derived aggregates may summarize truth and append-only logs.
- Rebuild/reconciliation workflows may compare truth against derived outputs.
- Derived outputs remain subordinate to truth unless a later architecture decision explicitly formalizes a stronger serving role.

---

## 10) How to use these docs (workflow)

1. Read this index to understand system-level contracts.
2. Open the module file you are working on (e.g., Content).
3. Implement schema, constraints, and indexes based on:
   - module invariants
   - query patterns
   - operational requirements (read path priority)
   - truth vs derived role of the dataset
   - replay/rebuild/reconciliation expectations where relevant
4. Record non-trivial trade-offs in ADRs under `explanation/decisions/`.

---

## 11) Open decisions (ADR candidates)

- Store placement policy: OLTP truth store, append-only logs, Redis usage rules.
- Outbox pattern adoption: required in V1 vs phased rollout.
- Cross-module DB foreign keys: enforce at DB or at application boundary?
- Unpublish semantics: revert to Draft vs introduce Unpublished state?
- Snapshot vs patch strategy for revisions (storage vs audit readability).
- Soft-delete vs archive split and retention policies.
- Checkpoint strategy: if/when projections are introduced, define checkpoint schema + freshness SLOs.
- Candidate/publication marker strategy: if/when versioned derived outputs become more prominent.
