
---

## `docs/explanation/api-architecture/modules/media/02-domain-contracts.md`

```md
# Media — Domain Contracts (V1)

## 1) Ownership
Media owns:
- media objects (metadata)
- attachment policy and ordering
- primary media rule
- soft delete/restore policy

Media does not own:
- article lifecycle (Content owns)
- SEO state (SEO owns)

---

## 2) Entities (conceptual)

### 2.1 MediaItem
- `MediaId`
- `Url` (or path)
- `Type` (Image|Video|File)
- `AltText?`
- `Metadata` (sanitized, optional)
- `Status` (Active|Deleted)
- `CreatedAt`, `UpdatedAt`

### 2.2 ArticleMedia (attachment)
- `ArticleId`
- `MediaId`
- `Order` (integer)
- `IsPrimary` (bool)
- `AttachedAt`

**Invariant:** `(ArticleId, MediaId)` unique.

---

## 3) Invariants and policies

### 3.1 Attachment integrity
- Cannot attach non-existent media.
- Cannot reference deleted media as primary (policy: block or auto-unset).
- Reading must degrade gracefully if media becomes unavailable.

### 3.2 Primary media rule
- At most one primary per article.
- Primary can be none by policy.

### 3.3 Ordering semantics
- Order must be deterministic and stable.
- Reorder must result in a valid total order (no duplicates, no gaps required by policy).

### 3.4 Delete/restore policy
- Soft delete in V1.
- Restore allowed within retention window (policy-level).
- Hard delete (if any) is out of scope in V1.

### 3.5 Safety and abuse surface
- Allowed media types and size constraints must be defined.
- Metadata must be sanitized to prevent injection/unsafe processing.

---

## 4) Domain events

### 4.1 Emits events (optional, useful for caching/read models)
- `MediaRegistered`
- `MediaAttached`
- `MediaDetached`
- `MediaPrimaryChanged`
- `MediaReordered`
- `MediaSoftDeleted`
- `MediaRestored`

### 4.2 Consumes events (optional policy)
- `ArticleArchived` / `ArticleDeleted` (cleanup policies in V2)