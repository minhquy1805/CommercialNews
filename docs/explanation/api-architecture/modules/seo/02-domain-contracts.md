
---

## `docs/explanation/api-architecture/modules/seo/02-domain-contracts.md`

```md
# SEO — Domain Contracts (V1)

## 1) Ownership
SEO owns:
- slug routing table (scope + slug → resource)
- canonical URL and metadata defaults policy
- social preview fields

SEO does not own publication state. It reacts to Content state.

---

## 2) Entities (conceptual)

### 2.1 SlugRoute (routing table)
- `Scope` (e.g., `public`)
- `Slug`
- `ResourceType` (Article)
- `ResourceId`
- `CanonicalUrl`
- `IsIndexable` (derived from publication state policy)
- `UpdatedAt`

**Invariant:** `(Scope, Slug)` is unique (zero collision tolerance).

### 2.2 SeoMetadata
- `SeoId`
- `ResourceType`, `ResourceId`
- `Slug` (may reference routing slug)
- `MetaTitle`, `MetaDescription`
- `OgTitle`, `OgDescription`, `OgImageUrl`

---

## 3) Domain concerns / invariants

### 3.1 Slug uniqueness
- Slugs must be unique under a defined scope.
- Collisions are not allowed.

### 3.2 Slug stability policy (ADR hook)
- Title changes do not automatically change slug (baseline).
- Slug changes are explicit admin actions.

### 3.3 Canonical correctness
- Canonical rules must be consistent to avoid duplicate indexing.

### 3.4 Publication coupling
- SEO visibility follows Content publication state:
  - published is indexable
  - unpublished/archived is not indexable (policy)

---

## 4) Domain events

### 4.1 Consumes events (from Content)
- `ArticlePublished`
- `ArticleUnpublished`
- `ArticleArchived`
- `ArticleUpdated` (only if policy requires meta/slug evaluation)

### 4.2 Emits events
- `SeoUpdated` (used for cache invalidation or read model updates)

Event payloads must be minimal and privacy-aware.
