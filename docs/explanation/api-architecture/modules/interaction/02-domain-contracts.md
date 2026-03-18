
---

## `docs/explanation/api-architecture/modules/interaction/02-domain-contracts.md`

```md
# Interaction — Domain Contracts (V1)

## 1) Ownership
Interaction owns:
- view recording semantics (V1 simple; V2 unique)
- like/unlike semantics and totals
- comment lifecycle rules (V1 CRUD; V2 moderation)

Interaction does not own:
- publication state (Content owns)
- rendering/composition of reading responses (Reading owns)

---

## 2) Entities (conceptual)

### 2.1 View (log or counter input)
- `ViewId`
- `ArticleId`
- `UserId?` (nullable for anonymous)
- `VisitorKey?` (optional policy)
- `ViewedAt`
- `IPAddress?` (privacy policy)
- `UserAgent?` (privacy policy)

### 2.2 Like
- `LikeId`
- `ArticleId`
- `UserId`
- `LikedAt`

**Invariant:** `(ArticleId, UserId)` is unique (enables idempotency).

### 2.3 Comment
- `CommentId`
- `ArticleId`
- `UserId` (author)
- `Content`
- `CreatedAt`, `UpdatedAt?`
- `DeletedAt?` (soft delete optional)
- `Status?` (V2: Visible/Hidden/Pending/Spam)

---

## 3) Semantics and invariants

### 3.1 View semantics (V1)
- A “view” is recorded on read as a simple event/log/counter increment.
- V1 does not guarantee uniqueness.
- Must be non-blocking relative to reading.

### 3.2 Like semantics (V1)
- Like/unlike is idempotent.
- Totals must remain consistent (eventual consistency allowed for aggregates).

### 3.3 Comment lifecycle (V1)
- Create/edit/delete rules must be explicit.
- Object-level authorization:
  - author can edit/delete their own comments
  - admins/moderators can remove abusive comments (V2 extension)

### 3.4 Publication coupling
- Interaction should not be enabled for non-public articles by policy (ADR hook):
  - disable interactions when article is unpublished/archived
  - enforce via checks or via consuming Content events

---

## 4) Events

### 4.1 Emits events (optional but recommended for V2/read models)
- `ArticleViewed`
- `ArticleLiked` / `ArticleUnliked`
- `CommentCreated` / `CommentUpdated` / `CommentDeleted`

### 4.2 Consumes events (policy-driven)
- `ArticleUnpublished` / `ArticleArchived` to disable interaction (optional V1; recommended V2)