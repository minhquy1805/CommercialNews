# ADR-0014 — Public Identifier Strategy (Slug / UUID / ULID) (V1)

**Status:** Accepted  
**Date:** 2026-03-04  
**Decision owners:** Architecture / Platform  
**Scope:** System-wide (public API identifiers, URLs, cross-module contracts)  
**Related:**
- `../architecture/arc42/10-system-data.md`
- `../architecture/arc42/11-replication-v1.md`
- `../architecture/arc42/system-data/system-data-seo-v1.md`
- `../api-architecture/01-api-architecture-charter-v1.md`
- ADR-0011 (Replication topology), ADR-0012 (Data store placement), ADR-0013 (Outbox semantics)

---

## Context

CommercialNews is a public-facing system where:
- URLs are long-lived and must remain stable (SEO and sharing)
- internal storage keys may change over time (migration, sharding, multi-store)
- replication lag and caching can cause inconsistencies if identifiers are reused
- security and privacy require avoiding accidental data exposure via predictable IDs

We need a clear strategy for:
- what identifiers are exposed in public APIs and URLs
- what identifiers are internal-only
- how modules reference each other (IDs-only contracts)
- how to avoid identifier reuse that can corrupt caches/derived stores

---

## Decision

### 1) Separate identifiers into three classes

#### A) Internal primary keys (PKs)
- Used inside a module's database schema.
- May be numeric (e.g., `bigint`) for OLTP efficiency.
- **Never treated as stable public identifiers**.

#### B) Public opaque IDs (PublicId)
- Exposed via APIs for resources where a stable opaque identifier is needed.
- Format: **ULID (preferred)** or UUID.
- Non-guessable and non-reused.
- Safe for logs/caches/projections and across store migrations.

#### C) Human-readable slugs (Slug)
- Used in public URLs for SEO and sharing.
- Uniqueness is enforced per `(Scope, Slug)` in the SEO routing truth table.
- Slug resolution returns the corresponding resource PublicId/ArticleId.

---

### 2) Use ULID as the default public opaque ID (V1)
- **ULID** is the default for `PublicId` because it is:
  - globally unique
  - lexicographically sortable (operability/debugging benefit)
  - safe for distributed generation without coordination

UUID is allowed when ULID is not available, but ULID is preferred for new modules.

**Rule:** public IDs are immutable and never reused.

---

### 3) Slug routing is a sidecar truth in the SEO module
- Public URL uses: `/{scope}/{slug}` (or equivalent).
- SEO owns a routing table (truth) mapping:
  - `(Scope, Slug)` → `TargetType + TargetId` (or ArticleId/PublicId)
- Slug changes are handled as routing updates, not as primary identity changes.

**Rule:** SEO routing must never leak drafts/unpublished content (visibility enforced by Content truth).

---

### 4) Do not expose internal numeric IDs in public endpoints
Public endpoints MUST NOT rely on sequential numeric IDs for:
- primary routing
- authorization decisions by ID predictability
- caching keys shared across boundaries

Admin endpoints MAY accept internal IDs if strictly internal, but preferred is still `PublicId` for consistency.

---

### 5) Cross-module references use stable IDs only
Cross-module contracts use:
- `UserId` (opaque ID if exposed)
- `ArticleId`/`ArticlePublicId` (depending on API surface)
- `RoleId`, `PermissionKey`, etc.

**Rule:** cross-module references MUST NOT require joins on “public slug”; slug is for routing only.

---

### 6) Slug lifecycle and compatibility rules (V1)
- Slugs may change (editorial updates), but old slugs should be preservable by policy:
  - either keep old slugs as inactive aliases (301 redirect), or
  - keep only the current slug (simpler V1), depending on SEO policy.

**V1 default posture:**
- Support a single active slug per scope and article.
- Optional: store slug history for redirects in V2+.

---

## Rationale

This strategy reduces risk under replication lag and caching:
- non-reused opaque IDs prevent cache key collisions after failover/migrations
- slug routing is decoupled from heavy reads and can be cached aggressively
- public URLs remain stable while internal schemas evolve

---

## Alternatives considered

1) **Expose numeric auto-increment IDs publicly**
- Pros: short URLs and easy debugging.
- Cons: predictable enumeration risk; cache/projection corruption risk if IDs are reused; hard to migrate across stores.

2) **Slug as the only identifier**
- Pros: human friendly.
- Cons: slugs change; uniqueness constraints and conflict handling become hard; poor fit for cross-module references.

3) **UUID only (no ULID)**
- Pros: standard and widely supported.
- Cons: not sortable; less operator-friendly when debugging logs/backlogs. Allowed, but ULID preferred.

---

## Consequences

### Positive
- Stable public contracts independent of internal storage keys
- Safer caching/projections under replication lag (no key reuse)
- Clear separation: slug for routing, ULID/UUID for identity
- Easier migrations (store placement changes, sharding) without breaking consumers

### Negative / Trade-offs
- Resource representations may include both `PublicId` and `Slug` (slightly more complexity)
- Requires a slug registry and (optionally) redirect policy for changes
- Requires consistent generation and validation libraries for ULID/UUID

---

## Implementation notes (V1)

- For each public resource that needs opaque identity:
  - add `PublicId` (ULID/UUID) in the truth table
  - return it in API responses
- SEO routing table:
  - unique constraint on `(Scope, Slug)`
  - map to target `ArticlePublicId` (or internal ArticleId if strictly internal)
- Logging and tracing:
  - prefer PublicId in logs over numeric PKs for cross-system debugging
- Cache keys:
  - include namespace + PublicId (never numeric-only)
  - slug cache keys are `(scope, slug)` for routing only

---

## Follow-ups

- ADR-0015: Cache Policy & Invalidation (Redis) — especially SEO routing cache and redirect behavior
- Update module docs:
  - SEO: slug registry rules and conflict handling
  - Content: include PublicId and slug update events
  - API surfaces: ensure public endpoints accept/return PublicId consistently