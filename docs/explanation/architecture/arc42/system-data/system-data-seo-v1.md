# System Data Model — SEO (V1)

> **Recommended file:** `explanation/architecture/arc42/system-data/system-data-seo-v1.md`  
> **Module:** SEO & Discoverability  
> **Purpose:** Provide stable, fast, and safe public entry points (slug routing) and store SEO metadata (canonical/meta/social).  
> **Design intent:** slug routing must remain **fast and reliable** and must not be coupled to “heavy” metadata reads.

---

## 0) Data System fit (V1)

SEO is a **read-path sidecar**: it improves discoverability but must not become a SPOF.

- **Truth store:** SQL Server (small routing table + optional metadata table)
- **Hot path:** slug resolution (Scope, Slug) → ArticleId
- **Redis:** slug routing cache (mandatory for burst protection)
- **Async side effects:** SEO updates can lag behind publish events (non-blocking), but routing must remain safe.

**Non-negotiables (from Quality Requirements)**
- Read path performance and availability are prioritized (P95/P99).
- Missing SEO rows must not break article reads (safe defaults).
- Slug routing must not leak drafts/unpublished content.

---

## 1) Scope & boundaries

### In scope (V1)
- Slug generation support + **unique slug routing**
- Canonical URL per article (optional; derivable by policy)
- Meta title/description with deterministic defaults (policy)
- Social sharing preview fields (OG/Twitter)

### Out of scope (V2+)
- Sitemap publishing
- Robots rules
- Slug redirect/alias support (old slug → new slug)

### Cross-module reference
- SEO references **Content.Article** by `ArticleId`.
- SEO must not become a SPOF: missing SEO rows must not break read path.
- Public routing must always enforce publication visibility via Content (truth store).

---

## 2) Capability → Entity mapping

### 2.1 Slug & Canonical
**Entities**
- `SlugRegistry` — **(Scope, Slug) → ArticleId** (small, routing-optimized)
- `SeoMetadata` — canonical + meta + social fields (heavier)

**V2 hook:** `SlugAlias` for redirects (old slug routes safely)

### 2.2 Meta title/description + defaults
- Defaults applied by application policy:
  - if `MetaTitle` is null → fallback from `Article.Title`
  - if `MetaDescription` is null → fallback from `Article.Summary`

### 2.3 Social preview
- `SeoMetadata`: OG/Twitter fields
- `OgImageUrl` may reference Media (by URL) or MediaId in V2.

---

## 3) Workload & query patterns (V1) (DDIA Ch3)

### 3.1 Hot path: slug routing
- Input: `(Scope, Slug)` from public request
- Output: `ArticleId` (routing)
- Must be:
  - fast (index + cache)
  - safe (do not leak non-public content)

### 3.2 Metadata read (secondary)
- `SeoMetadata` by `ArticleId` when rendering detail pages
- Must be optional (missing row → safe defaults)

### 3.3 Maintenance writes (low volume)
- Insert/update slug for an article
- Insert/update metadata fields

---

## 4) Dataflows (V1) — REST / DB / Broker (DDIA Ch4)

### 4.1 Resolve slug → articleId (sync read path)
1) Read `SlugRegistry` by `(Scope, Slug)` (prefer Redis cache; fallback SQL)
2) If slug not found / inactive → safe 404
3) Fetch `Article` by `ArticleId` (Content truth store)
4) Enforce visibility: only Published content is returned to public routes

**Safety rule**
- SEO alone must not decide visibility. Visibility is owned by Content.

### 4.2 Publish/Unpublish effects (async side effects)
- On `ArticlePublished/Unpublished`, SEO can:
  - ensure slug exists/active for published content
  - deactivate slug (or keep but make safe by Content filter) per policy

**Failure behavior**
- SEO processing delays must not re-expose unpublished/draft content.
- Core publish/unpublish must not block on SEO writes (non-critical).

---

## 5) Redis plan (SEO V1) — slug routing cache

Slug routing is a perfect cache target (small, hot keys, high read frequency).

### 5.1 Slug resolution cache
- Key: `cn:slug:{scope}:{slug}`
- Value: `articleId` (or a small object `{articleId,isActive}` if needed)
- TTL: 10–60 minutes (policy)
- Negative cache (optional): cache “not found” for a short TTL (10–60s) to protect from repeated misses

### 5.2 Invalidation policy
Invalidate on:
- slug created/changed
- slug deactivated/reactivated
- publish/unpublish if policy changes slug activity

> Note: even if cache is stale, Content visibility check prevents leakage.

---

## 6) Entities (V1)

### 6.1 `SeoMetadata` (0..1 per article)
**Role:** store metadata for SEO snippets and social sharing.

| Field | Type | Null | Default | Notes |
|---|---|---:|---|---|
| SeoId | BIGINT IDENTITY | NO |  | PK |
| ArticleId | BIGINT | NO |  | cross-module ref → Content.Article |
| CanonicalUrl | NVARCHAR(500) | YES |  | if null → derive by policy |
| MetaTitle | NVARCHAR(300) | YES |  | fallback from Article.Title |
| MetaDescription | NVARCHAR(500) | YES |  | fallback from Article.Summary |
| OgTitle | NVARCHAR(300) | YES |  | social |
| OgDescription | NVARCHAR(500) | YES |  | social |
| OgImageUrl | NVARCHAR(800) | YES |  | may point to Media |
| TwitterTitle | NVARCHAR(300) | YES |  | optional |
| TwitterDescription | NVARCHAR(500) | YES |  | optional |
| TwitterImageUrl | NVARCHAR(800) | YES |  | optional |
| UpdatedAt | DATETIME2(3) | NO | SYSUTCDATETIME() | |
| UpdatedBy | BIGINT | YES |  | actor (Identity.User) |

**V2 hooks**
- `RobotsIndex` (BIT), `RobotsFollow` (BIT)
- `StructuredDataJson` (NVARCHAR(MAX)) for schema.org

---

### 6.2 `SlugRegistry` (public entry point)
**Role:** enable extremely fast routing: **(Scope, Slug) → ArticleId**.

| Field | Type | Null | Default | Notes |
|---|---|---:|---|---|
| SlugId | BIGINT IDENTITY | NO |  | PK |
| ArticleId | BIGINT | NO |  | cross-module ref |
| Slug | NVARCHAR(200) | NO |  | unique within scope |
| Scope | VARCHAR(30) | NO | `'global'` | V1 policy: global unique |
| IsActive | BIT | NO | `1` | deactivate without deleting |
| CreatedAt | DATETIME2(3) | NO | SYSUTCDATETIME() | |
| CreatedBy | BIGINT | YES |  | actor |

**Why separate from SeoMetadata**
- `SlugRegistry` is the **read-path entry point**.  
  Keep it small, heavily indexed, and cache-friendly.

**V2 hooks**
- `Locale`, `CategoryId`, `Site` scoping
- Redirect strategy: `SlugAlias` or `RedirectToSlugId`

---

## 7) Relationships (V1)
- `Article (1) → SeoMetadata (0..1)` (optional)
- `Article (1) → SlugRegistry (0..1)` (optional in V1; drafts may not receive slug until publish by policy)
- `SlugRegistry (1) → SlugAlias (0..N)` (V2)

---

## 8) Invariants (V1 rules)

### 8.1 Slug correctness & stability
1) **Slug uniqueness**
- V1 recommended: global unique (`Scope='global'`)

2) **Slug stability policy**
- Title changes do **not** automatically change slug.
- Slug changes only via explicit action (ADR candidate).

3) **Safe resolution**
- Slug routing must not leak non-public content.
- Missing SEO rows must not break reads (fail-safe).

### 8.2 Canonical correctness
- Canonical derivation must be deterministic.
- Non-public content should follow a consistent indexability policy (V2: robots flags).

### 8.3 Meta defaults
- If `MetaTitle` is null → fallback `Article.Title`
- If `MetaDescription` is null → fallback `Article.Summary`

### 8.4 Social preview alignment
- Do not expose draft-only assets through public routes.

---

## 9) Constraints & indexes (SEO V1)

### 9.1 PK
- `PK_SeoMetadata(SeoId)`
- `PK_SlugRegistry(SlugId)`

### 9.2 FKs (cross-module)
- Optional DB-level FK if same database:
  - `SeoMetadata.ArticleId → Content.Article.ArticleId`
  - `SlugRegistry.ArticleId → Content.Article.ArticleId`
- If split DBs: enforce as application contract.

### 9.3 UNIQUE
- `UQ_SeoMetadata_ArticleId` (max 1 SEO record per article)
- `UQ_SlugRegistry_Scope_Slug` unique on `(Scope, Slug)`

### 9.4 CHECK
- `CK_SlugRegistry_Slug_NotEmpty`: `LEN(LTRIM(RTRIM(Slug))) > 0`
- `CK_SlugRegistry_Scope_Enum`: scope must be in a whitelist

### 9.5 Indexes (read-path optimized)
- `IX_SlugRegistry_Scope_Slug` (covered by unique index if implemented as unique index)
  - Key: `(Scope, Slug)`
  - Include: `(ArticleId, IsActive)`
- `IX_SlugRegistry_ArticleId`
  - Key: `(ArticleId)` (maintenance)
- `IX_SeoMetadata_ArticleId`
  - Key: `(ArticleId)`
  - Include: `(CanonicalUrl, MetaTitle, MetaDescription, OgTitle, OgDescription, OgImageUrl, UpdatedAt)`

---

## 10) Evolution rules (V1) — safe change over time (DDIA Ch4)

### 10.1 Add-only schema changes
- Prefer nullable fields/defaults; avoid rename/type changes.
- `SlugRegistry` should evolve cautiously (it is routing-critical).

### 10.2 Rolling upgrade safety
- Slug routing must remain compatible during deployments:
  - consumers ignore unknown fields
  - defaults for missing fields
- Redis cache TTL prevents stale entries from living forever.

### 10.3 Redirects (V2)
- Prefer introducing `SlugAlias` instead of mutating slugs aggressively.

---

## 11) Retention & operational notes (V1)
- `SlugRegistry` should avoid deletes; prefer `IsActive=0` for safe deactivation.
- `SeoMetadata` updates are low volume; keep audit trail in Audit module if required.

---

## 12) V2 extension notes
- Multi-language: uniqueness `(Site, Locale, Slug)`
- Category-scoped slug: `(CategoryId, Slug)` if URL structure is `/category/slug`
- Slug immutability + alias-only redirects (recommended for SEO stability)

---

## 13) ADR candidates
- Slug stability: allow changes or enforce immutable slugs?
- When to allocate slug: on draft creation vs on publish?
- Cross-module physical FK: keep or enforce via contracts only?
- Redirect strategy: `SlugAlias` table vs `RedirectToSlugId` on SlugRegistry
- Cache strategy: TTL + invalidation rules for slug routing

---

## 14) Partitioning Readiness (V1/V2)

> This section captures **partitioning and routing-readiness** for SEO.
> V1 remains **non-sharded by default**; priority is ultra-fast slug routing, cache correctness, and safe visibility enforcement.

### 14.1 Why SEO is a partitioning-risk module

SEO has a classic partitioning tension:

* **hot, exact-key read path**: `(Scope, Slug) -> ArticleId` must be extremely fast
* **secondary reads**: metadata by `ArticleId` (optional)
* **low-volume writes**: slug/metadata maintenance

If traffic grows, slug routing becomes a natural candidate for distribution. But in V1:

* Redis cache already handles the primary burst pressure
* correctness must be preserved under cache staleness and async lag

**V1 principle:** cache-first + index-first + safe fallback beats sharding complexity.

---

### 14.2 Primary access patterns (V1)

**Hot path (public routing)**

* get `ArticleId` by `(Scope, Slug)`
* validate `IsActive` (if stored)
* always confirm visibility via Content truth (Published only)

**Secondary reads (optional)**

* fetch `SeoMetadata` by `ArticleId` (missing row => safe defaults)

**Maintenance writes**

* upsert `SlugRegistry` for published content
* upsert `SeoMetadata` on admin edits or policy recalculation

---

### 14.3 What would partition (if we ever need to)

SEO has two entities with different scale characteristics.

#### A) `SlugRegistry` (routing-critical)

* naturally partitionable by key
* read pattern is exact match

#### B) `SeoMetadata` (heavier but not routing-critical)

* reads by `ArticleId`
* can lag without breaking routing

**Implication**
If partitioning happens, treat them differently:

* keep routing table (`SlugRegistry`) optimized for lookup and caching
* treat metadata as optional and evolvable

---

### 14.4 Candidate partitioning strategies (future)

#### Strategy 1 — Partition by hash of `(Scope, Slug)` (most natural for routing)

* distributes slug lookups evenly
* avoids “range hotspot” issues from lexicographic slug distributions
* works well with consistent hashing for node changes

**Trade-off**

* range scans by slug prefix become expensive (usually not needed for routing)

#### Strategy 2 — Partition by key range on `Slug` (rarely needed)

* useful if you need prefix/range operations (admin tools, sitemap generation)
* risk of hotspots if popular slugs cluster lexicographically

#### Strategy 3 — Separate routing vs metadata stores

* keep `SlugRegistry` in a small, fast store (SQL table + Redis)
* keep `SeoMetadata` in a store optimized for optional reads (same SQL in V1; flexible later)

**Recommendation (V2+)**
If SEO ever needs scale beyond V1, start with **workload partitioning** and cache strategy, not immediate DB sharding.

---

### 14.5 Skew and hotspot risks

#### A) Slug popularity skew

* a few “hot” slugs dominate reads

**Mitigation**

* Redis cache + negative cache
* safe TTL + invalidation

#### B) Cache stampedes (same hot slug expiring)

**Mitigation**

* jitter TTL, request coalescing (single-flight), stale-while-revalidate policy (optional)

#### C) Write hotspot is unlikely

* writes are low volume; events are manageable

---

### 14.6 V1 mitigations (no sharding yet)

V1 already has strong anti-hotspot tactics:

* **Redis slug cache**: key-based, high hit ratio
* **SQL unique index** on `(Scope, Slug)` with include `(ArticleId, IsActive)`
* **DB fallback** for correctness when cache misses
* **visibility enforcement via Content truth** (never trust SEO for visibility)
* **safe defaults** when SeoMetadata missing
* **async updates** (publish does not block on SEO writes)

These measures are preferred before any partitioning.

---

### 14.7 V2+ scale options (selective)

Introduce partitioning only when sustained signals justify it.

#### Option A — Workload partitioning (recommended first)

* multiple routing cache layers (edge/CDN + Redis)
* route handling replicas behind a load balancer
* single-flight for cache rebuild on misses

#### Option B — Partition `SlugRegistry` (hash) if routing DB becomes bottleneck

* shard by hash of `(Scope, Slug)`
* consistent hashing to reduce rebalancing cost

#### Option C — Add `SlugAlias` and keep routing safe under evolution

* redirects increase read traffic; keep alias lookup fast and cacheable

---

### 14.8 Rebalancing and routing readiness

If/when SEO is partitioned, it needs a stable request-routing mechanism:

* route to the correct partition based on hash of `(Scope, Slug)`
* minimize rebalancing impact when partitions/nodes change
* maintain correctness under partial failures:

  * DB fallback must remain available
  * Content visibility check remains mandatory

---

### 14.9 Partition-readiness observability signals (SEO)

Use V1 signals to decide if partitioning is needed:

* `/seo/resolve` P95/P99 latency
* `/seo/resolve` error rate (separate 404 vs 5xx/timeouts)
* Redis cache hit ratio and DB fallback rate
* top-N slug concentration (hot key distribution)
* cache stampede indicators (burst of DB fallbacks around TTL boundaries)
* time-to-consistency after publish/unpublish (SEO lag, if measurable)

**Scale trigger (policy-level)**
Consider stronger workload/data partitioning when sustained pressure causes:

* DB fallback rate remains high and impacts latency
* routing latency violates SLO even with high cache hit ratio
* hot-key concentration causes cache/DB saturation

---

## 15) ERD (dbdiagram.io)

See: `../diagrams/erd/seo-v1.dbml`

How to render:

1. Open dbdiagram.io
2. Copy DBML content from the file above
3. Paste into dbdiagram.io to view/export
