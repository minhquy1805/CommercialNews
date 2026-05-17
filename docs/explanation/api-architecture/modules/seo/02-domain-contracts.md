# SEO — Domain Contracts (V1)

## 1) Ownership

SEO owns:

- slug routing table: `Scope + Slug -> Resource`
- canonical URL policy
- SEO metadata for resources
- social preview fields
- SEO route/indexability derived state
- SEO sync state for Content-derived projections

SEO does not own:

- article publication state
- article lifecycle truth
- article body/content truth
- final public visibility decisions

Content remains the source of truth for article lifecycle and publication visibility.

SEO reacts to Content events and maintains derived routing/metadata state. SEO state may lag behind Content truth and must be safe under duplicate, stale, replayed, or out-of-order events.

---

## 2) Entities / Aggregates

### 2.1 SlugRegistry / SlugRoute

Represents route ownership and route resolution.

Fields:

- `SlugRouteId` / `SlugRegistryId`
- `Scope`  
  Example: `public`
- `Slug`
- `ResourceType`  
  Example: `Article`
- `ResourcePublicId`
- `CanonicalUrl`
- `IsActive`
- `IsIndexable`
- `SourceAggregateId`
- `SourceAggregateVersion`
- `LastAppliedMessageId`
- `LastSyncedAtUtc`
- `CreatedAtUtc`
- `UpdatedAtUtc`
- `Version`

Invariants:

- `(Scope, Slug)` must be unique.
- `(Scope, ResourceType, ResourcePublicId)` should identify at most one active/current route.
- Public route responses must expose stable public identifiers, not internal database primary keys.
- Route resolution is not final authority for public exposure.
- Public Reading flows must validate Content truth before returning public content.

---

### 2.2 SeoMetadata

Represents SEO metadata for a resource.

Fields:

- `SeoMetadataId`
- `Scope`
- `ResourceType`
- `ResourcePublicId`
- `Slug`
- `CanonicalUrl`
- `MetaTitle`
- `MetaDescription`
- `OgTitle`
- `OgDescription`
- `OgImageUrl`
- `Robots`
- `IsManualOverride`
- `SourceAggregateId`
- `SourceAggregateVersion`
- `LastAppliedMessageId`
- `LastSyncedAtUtc`
- `CreatedAtUtc`
- `UpdatedAtUtc`
- `Version`

Invariants:

- `(Scope, ResourceType, ResourcePublicId)` must be unique for the current metadata record.
- Manual SEO metadata must not be overwritten by automatic Content event sync unless explicitly allowed by policy.
- Metadata freshness may lag behind Content truth.
- Metadata must not be treated as authority for public visibility.
- Admin metadata edits require stale-write protection through `Version`, `RowVersion`, or equivalent compare-and-set semantics.

---

## 3) Domain Concerns / Invariants

### 3.1 Slug uniqueness

Slugs must be unique within a defined scope.

Required invariant:

- `Scope + Slug` is unique.

Collision behavior:

- slug collision must return a domain conflict
- collision must not be resolved silently
- final slug ownership is determined only by a truth-bound SEO write commit

Utility endpoints such as slug generation or slug availability checks are advisory only and do not reserve ownership.

---

### 3.2 Slug stability

Title changes do not automatically change slug in V1.

Slug changes are explicit admin actions.

When a slug changes:

- the new slug must pass uniqueness validation
- the route version must advance
- downstream cache/search/sitemap effects, if any, are post-commit async effects

Redirect history is out of scope for baseline V1 unless introduced by a separate policy.

---

### 3.3 Canonical correctness

Canonical URL must be deterministic for a resource and scope.

Canonical rules must avoid duplicate indexing.

Canonical URL changes must be treated as SEO truth changes and should advance metadata/route version.

---

### 3.4 Publication coupling

SEO follows Content publication truth but does not own it.

Baseline policy:

- published content may be indexable
- unpublished content is not indexable
- archived content is not indexable
- soft-deleted content is not indexable

However, SEO route state must not be the final authority for public visibility.

A stale active route must not expose non-public content. Public Reading flows must validate Content truth before returning public content.

---

### 3.5 Manual override protection

Admin-edited metadata is treated as a manual override.

Automatic sync from Content events must not overwrite manual fields unless policy explicitly allows it.

Examples:

- if admin manually edits `MetaTitle`, Content title updates must not overwrite it by default
- if admin manually edits `MetaDescription`, Content summary updates must not overwrite it by default
- route/indexability state may still be updated from Content lifecycle events

---

### 3.6 Version-aware derived state

SEO projections must be version-aware when applying Content lifecycle events.

For Content-derived updates:

- incoming events must include `AggregateId` / `ResourcePublicId`
- incoming events should include `AggregateVersion`
- SEO must track `SourceAggregateVersion`
- stale events must not overwrite newer SEO state
- duplicate events must be harmless
- version gaps or ambiguity should trigger retry, defer, or truth resync

Approved apply behavior:

- apply only if incoming version is newer
- ignore stale version
- resync from Content truth when forward progress cannot be established safely

---

## 4) Events

### 4.1 Consumed events from Content

SEO may consume the following Content integration events:

- `content.article_published`
- `content.article_unpublished`
- `content.article_archived`
- `content.article_soft_deleted`
- `content.article_restored`
- `content.article_updated`  
  Optional; used only if SEO auto-sync policy requires metadata evaluation.

Consumed events must carry stable identity:

- `MessageId`
- `EventType`
- `AggregateId` / `ResourcePublicId`
- `AggregateVersion` when lifecycle ordering matters
- `OccurredAtUtc`
- `CorrelationId`

---

### 4.2 Emitted events from SEO

SEO event emission is optional in V1.

If adopted, SEO truth change and the outbox record must commit atomically.

Possible SEO integration events:

- `seo.slug_route_changed`
- `seo.slug_route_deactivated`
- `seo.metadata_updated`

These events may be consumed by:

- Audit
- cache invalidation
- sitemap refresh
- search/index refresh
- future read-model or projection workflows

Event payloads must be minimal, privacy-aware, and must not include sensitive internal-only fields.

---

## 5) Idempotency and Replay Contract

SEO consumers must be safe under:

- duplicate message delivery
- consumer restart after partial work
- retry after timeout
- replay/rebuild workflows
- stale or out-of-order Content events

Required protection:

- message-level dedupe by `MessageId`
- business-level idempotency by `Scope + Slug`
- business-level idempotency by `Scope + ResourceType + ResourcePublicId`
- version-aware apply using `SourceAggregateVersion`
- safe ignore or resync for stale events

Duplicate `content.article_published` events must not create duplicate slug routes or duplicate metadata records.

Stale lifecycle events must not reactivate routes or metadata for content that has already moved to a newer non-public state.

---

## 6) Recovery / Rebuild Posture

SEO is a derived system and must have a documented recovery path.

Approved recovery strategies:

- rebuild SlugRegistry from Content truth
- rebuild SeoMetadata defaults from Content truth where manual override does not exist
- replay retained Content lifecycle events where available
- reconcile SEO state against Content published/non-public truth

Recovery must not redefine Content ownership.

When SEO state is uncertain, stale, or missing, public correctness must prefer safe deny / safe not-found over incorrect public exposure.