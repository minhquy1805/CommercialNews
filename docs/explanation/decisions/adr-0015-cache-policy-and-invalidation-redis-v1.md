# ADR-0015 — Cache Policy & Invalidation (Redis) (V1)

**Status:** Accepted  
**Date:** 2026-03-04  
**Decision owners:** Architecture / Platform  
**Scope:** System-wide (Redis usage, cache correctness, invalidation, fallbacks)  
**Related:**
- `../architecture/arc42/04-runtime-view-v1.md`
- `../architecture/arc42/06-measurement-guide.md`
- `../architecture/arc42/10-system-data.md`
- `../architecture/arc42/11-replication-v1.md`
- `../api-architecture/01-api-architecture-charter-v1.md`
- `../api-architecture/09-observability-and-slos.md`
- ADR-0011 (Replication topology), ADR-0012 (Data store placement), ADR-0013 (Outbox semantics), ADR-0014 (Public ID strategy)

---

## Context

CommercialNews is read-heavy and bursty. Redis is needed to protect the read path by:
- accelerating hot lookups (especially SEO slug routing)
- buffering burst traffic and reducing DB pressure
- supporting abuse controls (rate limiting)
- supporting at-least-once pipelines (dedupe/idempotency hints)

However, Redis is not a source of truth and is subject to:
- staleness (replication lag between truth and cache)
- eviction and data loss
- race conditions and out-of-order events

We need a strict cache policy that preserves correctness and security invariants.

---

## Decision

### 1) Redis is derived-only (never truth)
Redis may store:
- cache entries (hot-path acceleration)
- rate limit counters
- dedupe/idempotency keys (non-critical, TTL-bound)
- counters/aggregates where eventual accuracy is acceptable

Redis must NOT be used as the source of truth for:
- identity security state (verified, password reset, refresh token validity)
- authorization decisions (role/permission enforcement)
- content visibility correctness (published vs unpublished)
- slug uniqueness/routing truth (SQL truth owns uniqueness)

---

## 2) Cache patterns (V1)

### 2.1 Cache-aside is the default
- Read flow: check Redis → if miss, read truth store → set Redis → return.
- Write flow: write truth store → emit event/outbox → invalidate/update cache asynchronously.

**Reason:** simplifies correctness; cache can be dropped/rebuilt.

### 2.2 Write-through is allowed only for narrow, safe keys
Only allowed when:
- the cached value is fully derived from the write result
- and the write transaction already has the authoritative value

Use cases:
- small, immutable lookup data (rare in V1)
- carefully scoped admin-only caches (optional)

---

## 3) Invalidation strategy (V1)

### 3.1 Event-driven invalidation + TTL fallback
Primary invalidation mechanism:
- Outbox event → Worker consumer → invalidate/update Redis keys

TTL is a safety net:
- handles missed events, operational gaps, or rare edge cases
- TTL must not be the only correctness mechanism for sensitive paths

### 3.2 Namespace and versioned keys
All keys must be namespaced:
- `cn:{module}:{entity}:{keyParts}`

Where practical, include version/time for safe overwrites:
- `cn:seo:slug:{scope}:{slug}` → value includes `articleId/publicId` + `updatedAt/version`

### 3.3 No per-instance in-memory caches for shared read paths
To avoid monotonic read anomalies and divergence:
- prefer Redis/shared cache for shared endpoints
- in-memory caches are allowed only for local, non-shared configuration with safe invalidation

---

## 4) Correctness guardrails (non-negotiable)

### 4.1 SEO slug routing must be safe under staleness
- Cache may be stale, but must never leak drafts/unpublished content.
- Routing policy:
  1) resolve slug (cache-first)
  2) validate visibility via Content truth (Published) before returning public content
  3) on cache miss or suspicion of staleness → DB fallback for routing
  4) prefer safe "not found" over incorrect exposure

### 4.2 Admin and Identity self-state bypass stale caches
Endpoints requiring read-your-writes:
- MUST read from primary truth store (or return write result)
- MUST NOT depend on Redis freshness

### 4.3 Unpublish must take effect immediately
Even if caches lag:
- read path must filter unpublished in truth queries
- cache invalidation is best-effort but correctness is enforced by truth

---

## 5) Redis usage categories and TTL guidance (policy-level)

### 5.1 SEO routing cache
- Key: `cn:seo:slug:{scope}:{slug}`
- TTL: short-to-moderate (policy-defined; tuned after baseline)
- Invalidation: on `ArticlePublished`, `ArticleUnpublished`, `SlugChanged`, `SeoMetadataUpdated`
- Fallback: DB routing lookup + set cache

### 5.2 Read path response caches (optional)
- Only for endpoints with clear invalidation and acceptable staleness
- Must define:
  - cache key strategy
  - invalidation events
  - TTL as a safety net
  - correctness fallback

### 5.3 Rate limiting counters
- Key: `cn:rl:{routeGroup}:{clientKey}:{window}`
- TTL aligned to window
- Must be resilient to Redis restarts (rate limiting is best-effort, not a security boundary by itself)

### 5.4 Dedupe / idempotency keys
- Key: `cn:dedupe:{consumer}:{messageId}`
- TTL must cover maximum retry window
- Critical effects may require durable dedupe in SQL (Redis-only is acceptable for non-critical duplicates)

### 5.5 Counters / aggregates (optional)
- For views/likes: eventual accuracy acceptable
- Prefer commutative updates and periodic reconciliation from truth/logs if needed

---

## 6) Failure behavior & degrade strategy

If Redis is down or inconsistent:
- do not block core flows
- fall back to truth store reads (with safeguards)
- degrade gracefully for non-critical features (e.g., counters)

If cache invalidation lags:
- correctness is still enforced at truth store boundaries
- prefer safe responses over misleading data

---

## 7) Observability requirements

Minimum cache SLIs:
- Redis availability and latency
- cache hit rate (per key group)
- SEO DB fallback rate (%)
- stale/invalid reads detected (if measurable)
- eviction/memory pressure signals

These metrics support:
- tuning TTLs
- validating invalidation correctness
- detecting regressions under burst traffic

---

## Alternatives considered

1) **TTL-only caching (no event invalidation)**
- Pros: simple.
- Cons: staleness windows are uncontrolled; unacceptable for SEO/visibility-sensitive paths.

2) **Write-through everywhere**
- Pros: fewer misses.
- Cons: hard to maintain correctness under failures; high coupling to writes; not suitable under at-least-once pipelines.

3) **Per-instance memory caching**
- Pros: very fast.
- Cons: divergence across instances; monotonic read anomalies; hard invalidation.

---

## Consequences

### Positive
- Correctness-first cache posture (no security leaks)
- Operable under failures (fallbacks + observability)
- Clear rules for where caching is safe vs unsafe
- Compatible with Outbox-based replication

### Negative / Trade-offs
- Some reads may hit DB during cache outages (latency increase)
- Requires disciplined event-driven invalidation and key hygiene
- TTL tuning requires baseline traffic and measurement

---

## Implementation notes (V1)

- Standardize key naming and ownership per module.
- Keep cached payloads small and include a `lastUpdatedAt/version` when helpful.
- Implement invalidation consumers with idempotency (messageId dedupe).
- Ensure read paths validate visibility (Published) from truth for public content.

---

## Follow-ups

- Update module docs (`modules/{module}/06-idempotency-consistency.md`) with:
  - cache keys
  - invalidation events
  - TTL policy
  - fallback behavior
- Consider adding durable consumer checkpoints if projections become primary read sources (V2+).