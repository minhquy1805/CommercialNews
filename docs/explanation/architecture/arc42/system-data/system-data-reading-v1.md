# System Data Model — Reading Experience (V1)

> **Recommended file:** `explanation/architecture/arc42/system-data/system-data-reading-v1.md`  
> **Module:** Reading Experience  
> **Purpose:** Define the **read composition contracts** for listing, detail, and search — optimized for **read-path performance and availability**.  
> **Note:** Reading (V1) is primarily **query orchestration**. It does not own core tables; it composes data from other modules using `ArticleId`.

---

## 0) Data System fit (V1)

Reading is the **public hot path** (DDIA Ch1/3):
- **Goal:** fast, predictable list/detail under burst traffic (P95/P99 focus).
- **Rule:** do not couple reads to non-critical subsystems (interaction tracking, email, audit).
- **Stores used:** OLTP truth (Content), sidecars (SEO/Media/Interaction stats), and Redis caches.

**Degradation policy**
- Missing stats → show `0` or hide counters
- Missing media → fallback image/placeholder
- Comments are paged and may be loaded separately

---

## 1) Capability → data sources mapping

### 1.1 Article listing (feed)
**Data sources**
- **Content**: `Article` (truth for publication state + time sort)
- **Content**: `Category` (category filtering)
- **Content**: `ArticleTag` + `Tag` (tag filtering)
- **Interaction**: `ArticleInteractionStats` (popularity sort)
- **Media**: `ArticleMedia` + `MediaAsset` (cover/thumbnail)
- **SEO** (optional for listing routes): slug lookup if needed for links

**V1 caching**
- Redis feed cache (short TTL) is recommended for burst protection.

**V2 hooks**
- Trending window scores (24h/7d)
- Read-model projections for extreme traffic

---

### 1.2 Article details (detail page)
**Data sources**
- Content: `Article` + Category + Tags
- SEO: `SlugRegistry` (routing) + `SeoMetadata` (optional)
- Media: attachments + asset metadata (primary + ordered list)
- Interaction: `ArticleInteractionStats` + `Comment` (paged)

**V1 tactic**
- Split loading is allowed:
  - detail first
  - comments as a separate paged call

---

### 1.3 Search
**V1 options**
- Basic (small scope): `LIKE` on Title/Summary/Body
- Preferred on SQL Server: **Full-Text Search** on `(Title, Summary, Body)`

**V2**
- External search engine (Meilisearch/Elastic) + sync pipeline via outbox/worker

---

## 2) Read composition contracts (V1)

> Reading defines how modules are composed, not who owns data.

### 2.1 Listing composition
`Article (Published)`  
→ optional `Category` (filter)  
→ optional `ArticleTag/Tag` (filter)  
→ optional `ArticleInteractionStats` (popularity)  
→ optional `Media primary` (thumbnail)

### 2.2 Details composition
- `SlugRegistry (Scope,Slug) → ArticleId`
- `ArticleId → Content.Article (+Category +Tags)`
- `ArticleId → SEO.SeoMetadata` (optional)
- `ArticleId → Media.ArticleMedia/MediaAsset` (primary + ordered)
- `ArticleId → Interaction.ArticleInteractionStats` (optional)
- `ArticleId → Interaction.Comment` (paged; Visible only)

---

## 3) Read-path invariants (V1 rules)

### 3.1 Correctness & visibility
1. **Public only sees Published**
- Listing/detail/search must enforce:
  - `Article.Status = 'Published'`
  - `IsDeleted = 0` (if soft delete is used)

2. **Slug routing must not leak non-public**
- After resolving slug → ArticleId, Reading must still validate publication state in Content.
- If slug exists but article is not public → safe 404.

### 3.2 Sorting & filtering semantics
1. **Time sort**
- Feed sort key = `PublishedAt DESC` (not CreatedAt).

2. **Popularity sort**
- Source: `Interaction.ArticleInteractionStats.PopularityScore` (blended all-time in V1)
- If stats missing → treat score as `0` (graceful)

3. **Deterministic filters**
- Category/tag filtering must be consistent across endpoints.

### 3.3 Related (deterministic fallback)
- Rule 1: same category, exclude self, published only
- Rule 2: shared tags (optional)
- Fallback: newest published

### 3.4 Graceful degradation
- Missing stats → 0 / hide counters
- Missing cover → placeholder
- Comments slow → separate call + paging
- Interaction tracking failures must not impact reads

---

## 4) Redis caching policy (Reading V1)

> Redis is derived; it protects P95/P99 under bursts.

### 4.1 Feed caches (short TTL)
- `cn:feed:published:{page}:{size}:{filtersHash}` TTL 30–120s
- `cn:feed:cat:{categoryId}:{page}:{size}` TTL 30–120s
- `cn:feed:tag:{tagId}:{page}:{size}` TTL 30–120s
- Optional: negative cache for empty results (very short TTL)

### 4.2 Slug routing cache (SEO)
- `cn:slug:{scope}:{slug}` → `articleId` TTL 10–60m
- Invalidate on slug change or deactivation

### 4.3 Detail cache (moderate TTL + invalidate on write)
- `cn:article:{articleId}:detail` TTL 5–30m
- Invalidate on:
  - publish/unpublish/archive
  - content edits that affect public detail
  - media primary change/reorder (if rendered in detail)
  - SEO metadata changes (policy)

**Staleness policy**
- Feed cache uses short TTL to avoid complex invalidation.
- Detail cache should be explicitly invalidated on publish/unpublish.

---

## 5) Owned entities (Reading V1)

### V1 must-have
- **No new tables are required** by default.

### V1 optional (early optimization only)
- `ArticleRelatedSnapshot(ArticleId, RelatedArticleId, Rank, GeneratedAt)`
- `ArticleSearchIndex(ArticleId, SearchText, UpdatedAt)` *(only if SQL Full-Text is not used)*

### V2 hooks
- Trending window stats (24h/7d)
- External `SearchDocument`
- Dedicated read-model cache tables/services

---

## 6) Index requirements for hot queries (V1)

> Constraints are enforced by owning modules. Reading documents the **required indexes** for public performance.

### 6.1 Listing by time (Content)
- `IX_Article_Status_PublishedAt (Status, PublishedAt DESC)`
- `IX_Article_Category_Status_PublishedAt (CategoryId, Status, PublishedAt DESC)`
- `IX_ArticleTag_TagId_ArticleId (TagId, ArticleId)`

### 6.2 Listing by popularity (Interaction)
- `IX_Stats_PopularityScore (PopularityScore DESC, ArticleId)`

**Query shapes**
- Popular global: `Article (Published)` → join `Stats` order by score desc
- Popular by category: filter Article by Category+Published → join Stats → order by score desc

**Scaling note (V2)**
If “popular by category” becomes slow:
- cached snapshots per category
- enrich stats projection (category-scoped aggregates)

### 6.3 Details hot path (supporting modules)
- Media: `IX_ArticleMedia_ArticleId_SortOrder`
- SEO: `UQ/IX_SlugRegistry_Scope_Slug` and `IX_SeoMetadata_ArticleId`
- Comments: `IX_Comment_ArticleId_Status_CreatedAt`

### 6.4 Search (V1)
**Option A: SQL Server Full-Text (preferred)**
- Full-text index on `Article(Title, Summary, Body)`
- Always filter by `Status='Published'` (and `IsDeleted=0` if used)

**Option B: LIKE-based (temporary)**
- `LIKE '%term%'` scales poorly; acceptable only for small scope or limited V1 usage.
- Move to Full-Text early if search becomes important.

---

## 7) Dataflow notes (V1) — non-blocking side effects

- View tracking is async/non-blocking (Interaction module)
- Audit/notifications/SEO updates can lag but must not break reads
- CorrelationId should propagate across list/detail calls for tracing

---

## 8) V2 hooks (Reading)
- Trending: add `PopularityScore24h/7d` + indexes
- Related snapshot: precompute & cache
- External search: sync search docs via outbox/worker
- Dedicated projections/read models for extreme traffic

---

## 9) Decisions locked for V1
- Popularity source: `Interaction.ArticleInteractionStats.PopularityScore`
- Search V1: SQL Full-Text if enabled; otherwise LIKE with limited scope
- Related: deterministic rule (category-first, tag-second, fallback newest)
- Reads enforce Content visibility regardless of SEO slug existence

---

## 10) Partitioning Readiness (V1/V2)

> This section captures **partitioning and read-path scale readiness** for Reading Experience.
> V1 remains **non-sharded by default**; priority is protected public read latency/availability and safe fallback behavior.

### 10.1 Why Reading is a partitioning-risk module

Reading is the **public hot path** and the main latency/SLO surface:

* burst traffic concentrates on listing and a few hot articles
* reads compose multiple modules (Content/SEO/Media/Interaction)
* sidecar lag/failure must not break user-visible reads

**V1 principle:** optimize **query shape + caching + graceful degradation** before introducing shard complexity.

---

### 10.2 Primary access patterns (V1)

**Hot paths**

* feed/listing (Published content, paging/filter/sort)
* detail by slug (SEO route -> Content truth -> optional sidecars)
* detail by `ArticleId` (if internal/public policy supports)

**Read composition pattern**

* Content truth is the visibility authority
* SEO/Media/Interaction stats are sidecars
* missing sidecar data must degrade safely (not fail the read)

**Search (V1)**

* SQL Full-Text preferred (or bounded LIKE fallback)
* always enforce Published visibility in Content

---

### 10.3 Secondary-index-heavy queries (present and future)

Reading is not write-heavy, but it is **query-shape sensitive**.

**V1**

* feed by `PublishedAt DESC`
* feed by category/tag + time
* feed by popularity via `ArticleInteractionStats.PopularityScore`
* comments paging by `(ArticleId, Status, CreatedAt)`
* slug resolution by `(Scope, Slug)` (SEO sidecar)

**V2+**

* trending windows (24h/7d)
* richer related-content ranking
* external search queries / read-model projections

**Implication**
Reading scale problems often appear first as **query/index/caching issues**, not immediate DB sharding issues.

---

### 10.4 Candidate partitioning strategy (future)

Reading V1 owns no truth tables, so partitioning readiness is mostly about **query isolation** and **read-model evolution**.

#### A) V1 (preferred)

* bounded queries + required indexes in owner modules
* Redis feed/detail/slug caches
* split loading (detail first, comments separately)
* graceful fallback for sidecars

#### B) V2+ read-model/projection path (most likely first scale step)

Introduce dedicated read projections when:

* burst traffic or query complexity exceeds OLTP join/caching capacity
* popularity/category combinations become expensive
* search becomes a dominant workload

This aligns with the Query Facade -> Read Model evolution path.

#### C) Data partitioning (later, component-specific)

If read-model/projection stores become large/hot, partitioning may apply to those derived stores (not necessarily to Content truth first).

---

### 10.5 Hotspot and skew risks (V1)

#### A) Hot article skew

* repeated detail reads for a few articleIds/slugs
* repeated stats/media/SEO lookups for the same article

#### B) Feed burst skew

* same feed pages/filters repeatedly requested during spikes (page 1, top category)

#### C) Cross-module fan-out cost

* read composition can amplify latency if sidecar lookups are slow or uncached

---

### 10.6 V1 mitigations (no sharding yet)

CommercialNews V1 already has the correct baseline mitigations for Reading:

* **Redis feed/detail caches** (short TTL / explicit invalidation where needed)
* **SEO slug cache** + SQL fallback
* **Visibility enforcement in Content truth** (never trust SEO alone for visibility)
* **Graceful degradation** for missing stats/media/comments delays
* **Split loading** (detail first, comments paged separately)
* **Required indexes documented in owner modules**

These are preferred before introducing shard complexity.

---

### 10.7 V2+ scale options (selective)

Introduce stronger partitioning/read-model strategies only when sustained signals justify it.

#### Option A — Read-model/projection for hot listings (recommended first)

* precomputed feed slices / category snapshots / trending windows
* category-scoped popularity projections
* cache-friendly read shapes with explicit freshness policy

#### Option B — External search engine

* move search workload out of OLTP joins / LIKE queries
* sync via outbox/worker pipeline
* accept eventual consistency with measurable freshness and fallback behavior

#### Option C — Derived-store partitioning

If projections/search docs become the bottleneck, partition those derived stores before touching core truth stores.

---

### 10.8 Rebalancing and routing readiness (future)

Reading itself is mostly an API/query composition module, but future scale may require:

* routing to read-model partitions/projection shards
* cache key distribution and hotspot controls
* worker-owned projection rebuild lanes (via Background Worker)

**Guardrail**
Any scale/rebalance change must preserve public read P95/P99 and visibility correctness.

---

### 10.9 Partition-readiness observability signals (Reading)

Use existing V1 measurement signals to decide when stronger partitioning/read-model strategies are needed:

* list/detail P95/P99 latency
* list/detail error rate (5xx/timeouts)
* TTFB for key read endpoints
* cache hit ratio for feed/detail (if measured)
* SEO slug cache hit ratio / DB fallback rate
* sidecar fallback/degradation rates (stats/media/SEO)
* query latency for popularity/category combinations (if measured)

**Scale trigger (policy-level)**
Consider stronger read-model/projection partitioning when sustained pressure causes:

* protected read path latency degradation despite current cache/index/query tactics
* repeated expensive join/query shapes under burst traffic
* fallback/degradation becoming frequent enough to impact UX or SLOs

---

## 11) ERD (dbdiagram.io)

See: `../diagrams/erd/reading-composition-v1.dbml`

How to render:

1. Open dbdiagram.io
2. Copy DBML content from the file above
3. Paste into dbdiagram.io to view/export

> Note: This is a composition view (Reading owns no tables in V1).
