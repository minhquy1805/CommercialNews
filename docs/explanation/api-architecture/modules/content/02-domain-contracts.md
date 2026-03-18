
---

## `docs/explanation/api-architecture/modules/content/02-domain-contracts.md`

```md
# Content — Domain Contracts (V1)

## 1) Ownership
Content is the source of truth for:
- article lifecycle state and timestamps
- taxonomy (categories/tags) and attach/detach rules
- edit history integrity

Other modules must not override lifecycle.

---

## 2) Entities (conceptual)

### 2.1 Article
- `ArticleId`
- `Title`, `Summary`, `Body`
- `Status`: `Draft | Published | Archived`
- `AuthorUserId`
- `CreatedAt`, `UpdatedAt`
- `PublishedAt?`, `UnpublishedAt?` (if tracked)
- `CategoryId`
- `CoverMediaId?` (optional reference; Media owns media objects)

### 2.2 Category
- `CategoryId`
- `Name` (unique by policy)
- `Slug?` (optional)
- `ParentCategoryId?` (optional V2)

### 2.3 Tag
- `TagId`
- `Name` (unique by policy)

### 2.4 ArticleTag (association)
- `ArticleId`
- `TagId`

### 2.5 ArticleRevision / NewsHistory (edit history)
- `RevisionId`
- `ArticleId`
- `EditedByUserId`
- `EditedAt`
- `OldTitle?`, `OldBody?`, `OldSummary?` (snapshot vs diff is ADR)
- `ChangeSummary?` (optional)

---

## 3) Lifecycle invariants (must hold)

### 3.1 Allowed transitions (baseline)
- Draft → Published
- Published → (Unpublished) → Draft (policy decision) OR Published → UnpublishedState (if introduced)
- Published → Archived
- Archived → Draft or Published? (policy decision; recommended: Archived → Draft then publish)

### 3.2 Governance invariants
- Publish/unpublish actions must be protected by authorization policies.
- Unpublish must record a reason (mandatory).

### 3.3 Edit history invariants
- Updates must create a traceable revision record (who/when/what changed).
- History should be tamper-evident at policy level (append-only intent).

### 3.4 Taxonomy invariants
- No orphan references:
  - Article must not reference non-existent CategoryId
  - ArticleTag must not reference missing Tag/Article
- Attach/detach rules must be deterministic.

### 3.5 Read correctness
- Public read must never expose Draft/Unpublished/Archived content.

---

## 4) Domain events (for async side effects)

Content emits:
- `ArticleCreated`
- `ArticleUpdated`
- `ArticlePublished`
- `ArticleUnpublished` (includes reason)
- `ArticleArchived` (optional)
- `CategoryCreated/Updated/Deleted` (optional, if needed)
- `TagCreated/Updated/Deleted` (optional)

Event envelope is standard (EventId/OccurredAt/CorrelationId/ActorUserId?/Version/Payload).
Payloads must be minimal and privacy-aware.

Example payloads:
- ArticlePublished: `{ ArticleId, PublishedAt }`
- ArticleUnpublished: `{ ArticleId, Reason, UnpublishedAt }`