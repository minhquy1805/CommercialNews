# System Data Model — Media (V1)

> **Recommended file:** `explanation/architecture/arc42/system-data/system-data-media-v1.md`  
> **Module:** Media  
> **Purpose:** Manage media assets safely and attach them to articles with deterministic primary selection and stable ordering.  
> **Design intent:** keep reusable file metadata in `MediaAsset`, keep per-article concerns (ordering/primary/caption) in `ArticleMedia`.

---

## 0) Data System fit (V1)

Media is a **supporting OLTP module** with a large “abuse surface” (uploads) and a strong impact on the read path.

- **Truth store:** SQL Server for metadata + attachments (`MediaAsset`, `ArticleMedia`)
- **Binary storage:** object/file storage (media bytes) — DB stores URLs/paths only
- **Read path rule:** missing/deleted media must degrade gracefully (never break article reads)
- **Async hooks (V2+):** scan/processing/variants can move to Worker without changing V1 contracts

**Non-negotiables (from Quality Requirements)**
- Read path availability: if media is unavailable, show fallback/placeholder
- Security: allowlist media types; avoid unsafe metadata; prevent injection through captions/alt
- Maintainability: attachments and ordering rules must be deterministic and testable

---

## 1) Scope & boundaries (V1)

### In scope (V1)
- Store media metadata (URL/path, size/type, alt text)
- Attach media to articles with:
  - stable ordering
  - deterministic primary selection (cover)
  - per-article caption/alt override
- Soft delete + restore hooks

### Out of scope (V2+)
- Virus scan / safety scan workflows
- Image/video processing pipeline (variants, EXIF stripping)
- Polymorphic attachments beyond articles (generalized owner model)

### Cross-module references
- Media references `Content.Article` by `ArticleId` (attachments)
- Media references `Identity.User` by `CreatedBy/DeletedBy` (actors)
- Content may optionally keep a nullable hook `Article.CoverMediaId` (sidecar convenience)

---

## 2) Capability → Entity mapping

### 2.1 Media management
**Entities**
- `MediaAsset` (file metadata + storage info)
- V2 hooks: `MediaVariant`, `MediaProcessingJob`, `MediaScanResult`

### 2.2 Attach media to articles
**Entities**
- `ArticleMedia` (association: sort order, primary flag, per-article alt/caption)

**Why `ArticleMedia` is separate**
- `MediaAsset` can be reused for other resources later (avatars, OG images, etc.)
- ordering/primary are **attachment concerns**, not file concerns

### 2.3 Delete / restore
**Entities**
- Soft delete on `MediaAsset` and optionally `ArticleMedia` (policy)
- V2 hook: retention/purge jobs

---

## 3) Workload & hot paths (V1) (DDIA Ch3)

### 3.1 Read path usage
- Article detail needs:
  - primary cover media
  - ordered attachment list (optional)
- Reads must be fast and bounded:
  - query by `ArticleId` and sort by `SortOrder`

### 3.2 Write patterns
- Upload: create `MediaAsset`
- Attach: create `ArticleMedia`
- Reorder: batch update `SortOrder` for a set of attachments
- Set primary: must be deterministic (0..1 per article)

### 3.3 Ops queries
- List deleted assets for restore/purge
- Find all articles using a given asset (investigation/cleanup)

---

## 4) Dataflows (V1) — REST / DB / Broker (DDIA Ch4)

### 4.1 Upload (sync OLTP metadata write)
- Binary upload goes to storage endpoint
- API stores `MediaAsset` metadata (URL/path/type/size) in SQL

**Failure behavior**
- If storage upload succeeds but DB write fails, the upload becomes orphaned (needs cleanup job/policy)
- If DB write succeeds but storage is missing, reads must degrade gracefully

### 4.2 Attach to article (sync)
- Insert `ArticleMedia (ArticleId, MediaId, SortOrder, IsPrimary=0)`
- Enforce no duplicate attachment by `(ArticleId, MediaId)` unique constraint

### 4.3 Set primary cover (atomic)
**Rule:** at most one active primary per article.

Recommended pattern:
- Transaction:
  1) set all attachments for ArticleId → `IsPrimary=0` (where not deleted)
  2) set the selected attachment → `IsPrimary=1`
- Enforced additionally by SQL Server filtered unique index:
  - `UNIQUE(ArticleId) WHERE IsPrimary=1 AND IsDeleted=0`

### 4.4 Reorder attachments (batched update)
- Perform reorder as a single batch operation:
  - update `SortOrder` for the set of `ArticleMediaId`s
- Avoid per-row chatty updates.

### 4.5 Async hooks (V2+)
- Publish events can trigger:
  - precompute primary media for read models
  - generate variants / scan assets
But V1 does not require async dependency for core reading.

---

## 5) Redis plan (Media V1) — optional, keep simple

Media is not the primary cache domain; caching usually belongs to the **Reading** facade.

If you cache at Media layer:
- `cn:article:{articleId}:primaryMedia` TTL 5–30m (optional)
- Invalidate on: set-primary, delete/restore attachment, reorder if it impacts rendering

> Prefer caching at the read facade (Reading module) to avoid duplicated caches.

---

## 6) Relationships (V1)

### 6.1 Article ↔ ArticleMedia (cross-module)
- `Article (1) → ArticleMedia (0..N)`
- Media owns `ArticleMedia`; Content owns `Article`
- An article may have no media (valid)

### 6.2 MediaAsset ↔ ArticleMedia
- `MediaAsset (1) → ArticleMedia (0..N)`
- ArticleMedia must always reference an existing MediaAsset (FK)
- Soft-deleting MediaAsset must degrade gracefully (no crashes)

### 6.3 Primary & ordering
- Primary: for each `ArticleId`, **0..1** attachment with `IsPrimary=1` (active only)
- Ordering: `SortOrder` stable within each `ArticleId`

---

## 7) Invariants (V1 rules)

### 7.1 Attachment integrity
- No orphan attachments: `ArticleMedia.ArticleId` and `ArticleMedia.MediaId` must exist
- No duplicate attachment: `(ArticleId, MediaId)` unique

### 7.2 Primary rule (deterministic)
- At most one primary per article
- Primary must not be deleted (`IsDeleted=0`)

### 7.3 Ordering semantics
- `SortOrder >= 0`
- Reorder is handled as a batched/atomic update

### 7.4 Delete/restore policy
- Soft delete is reversible within retention (if used)
- Soft-deleted assets must not break read path (fallback policy)

### 7.5 Safety (policy-level)
- Allowlist media types and enforce safe metadata (alt/caption validation)
- Avoid embedding raw HTML/unsafe markup in captions (application-level sanitization)

---

## 8) Entities (Logical schema) — SQL Server (V1)

### 8.1 `MediaAsset`
*(keep your existing table as-is)*

### 8.2 `ArticleMedia`
*(keep your existing table as-is)*

### 8.3 (V2) `MediaVariant`
*(keep your existing table as-is)*

---

## 9) Constraints & indexes — Media (V1)

### 9.1 PK / FK / UNIQUE / CHECK
*(keep your current list; it is solid)*

**Note on FK boundaries**
- If modules share one DB, DB-level FK to `Content.Article` is optional but helpful.
- If modules split DBs later, enforce cross-module integrity at application contract level.

### 9.2 Index guidance (DDIA Ch3)
- Optimize for:
  - list attachments by `(ArticleId, SortOrder)`
  - primary lookup by `(ArticleId)` with filtered unique index
  - ops list by `(IsDeleted, CreatedAt)`

---

## 10) Evolution rules (V1) — safe change over time (DDIA Ch4)

- Add-only fields + defaults
- Prefer soft delete over hard delete to preserve safety and investigation capability
- Avoid coupling read path to processing pipeline: scan/variants are V2 sidecar features

---

## 11) Retention & operational jobs (V1 policy)

### 11.1 Orphan cleanup (storage ↔ DB)
- Periodic job to detect orphaned uploads:
  - storage objects without `MediaAsset` record
  - DB records pointing to missing storage objects (mark as degraded)

### 11.2 Soft delete purge
- If retention is enabled:
  - purge `MediaAsset` and/or `ArticleMedia` after `RestoreUntil` (or policy window)
- Keep audit trail in Audit module (who deleted/restored)

---

## 12) V2 notes (scale safely)

- Virus scan / safe processing: store scan status; only publish assets after passing
- Variants/CDN: use `MediaVariant` + caching headers
- Polymorphic attachments: generalize beyond articles if needed

---

## 13) ADR candidates
- Storage provider strategy: local vs GCS/S3 and URL stability
- Primary strategy: enforce immutable cover vs editable cover
- Cross-module FK: physical FK vs application contract
- Retention/purge: windows and archival strategy
- Dedup uploads via `ContentHash` (V2)

---

## 14) Partitioning Readiness (V1/V2)

> This section captures **partitioning and read-support scale readiness** for Media.
> V1 remains **non-sharded by default**; priority is deterministic attachment behavior and graceful read-path degradation.

### 14.1 Why Media is a partitioning-risk module

Media is a **supporting OLTP module** with two distinct pressures:

* metadata/attachment lookups affect the public read path (detail rendering)
* upload/attachment operations and cleanup jobs create operational pressure (including abuse/orphans)

However, in V1 the main scale risks are usually:

* read composition latency (not table size alone)
* storage/DB consistency drift (orphans, missing objects)
* deterministic primary/ordering correctness under concurrent edits

**V1 principle:** optimize **query shape + indexes + graceful fallback** before shard complexity.

---

### 14.2 Primary access patterns (V1)

**Read-support hot paths**

* primary media lookup by `ArticleId`
* ordered attachment list by `(ArticleId, SortOrder)`

**Write paths**

* upload metadata insert (`MediaAsset`)
* attach asset to article (`ArticleMedia`)
* set primary (atomic)
* reorder attachments (batched)
* soft delete / restore

**Ops paths**

* deleted assets listing
* orphan cleanup checks (storage vs DB)
* usage lookup: articles referencing a media asset

---

### 14.3 Secondary-index-heavy queries (present and future)

**V1**

* attachment list by `(ArticleId, SortOrder)`
* primary lookup by `ArticleId` (filtered unique primary)
* deleted/ops views by `(IsDeleted, CreatedAt)`
* reverse usage lookup by `MediaId`

**V2+**

* scan status / processing pipelines (`MediaScanResult`, `MediaProcessingJob`)
* variants (`MediaVariant`) lookups by asset/type/size
* broader polymorphic owner queries (beyond articles)

**Implication**
Media scale pressure in V1 is typically solved by indexes and read-facade caching (Reading) before DB partitioning.

---

### 14.4 Candidate partitioning strategy (future)

Media partitioning should distinguish between metadata/attachments (SQL) and binary storage (object store).

#### A) `MediaAsset` / `ArticleMedia` (OLTP truth metadata)

**Likely fit:** defer DB partitioning in V1

* prioritize deterministic constraints (primary/order/uniqueness)
* prioritize indexes for article-based reads
* preserve simple transactions for set-primary and reorder operations

#### B) Ops/cleanup and processing workloads (V2+)

**Likely fit:** workload partitioning before DB sharding

* orphan cleanup batches by time/bucket
* processing/scan lanes by asset id/hash/type
* variant generation lanes (async sidecar)

#### C) Large-scale media metadata (later)

If metadata grows significantly (multi-owner, variants, processing), partitioning may apply to derived processing tables first, not necessarily `ArticleMedia` core truth.

---

### 14.5 Hotspot and skew risks (V1)

#### A) Read skew (article-level)

* hot articles repeatedly request the same primary media / ordered attachments
* repeated joins can impact detail latency if uncached

#### B) Write contention (editorial updates)

* concurrent set-primary / reorder operations on the same `ArticleId`
* batched updates help, but concurrency control still matters

#### C) Upload abuse / orphan growth

* failed upload/DB write combinations create orphan cleanup pressure
* storage-DB drift can increase ops load even when SQL write volume is moderate

---

### 14.6 V1 mitigations (no sharding yet)

CommercialNews V1 already has the correct baseline mitigations for Media:

* **Deterministic constraints** (no duplicate attachment, one active primary per article)
* **Query-driven indexes** for `(ArticleId, SortOrder)` and primary lookup
* **Batched reorder updates** (avoid chatty per-row writes)
* **Graceful degradation** when media is missing/deleted (placeholder/fallback)
* **Read-facade caching in Reading** (preferred over duplicating Media-layer caches)
* **Orphan cleanup jobs** for storage/DB drift

These are preferred before shard complexity.

---

### 14.7 V2+ scale options (selective)

Introduce stronger partitioning only when sustained signals justify it.

#### Option A — Read-facade / projection optimization (recommended first)

* cache primary media and attachment summaries in Reading/detail caches
* precompute lightweight attachment projections if repeated joins become expensive

#### Option B — Workload partitioning for processing and cleanup

* async lanes for scan/variant generation/orphan cleanup
* bucket by `MediaId`, `ContentHash`, or created-time windows

#### Option C — Media metadata partitioning (later)

Consider only when:

* SQL metadata tables become a measured bottleneck
* article-based attachment queries remain expensive despite indexing/caching
* processing-side growth (variants/scan tables) materially impacts operability

---

### 14.8 Rebalancing and routing readiness (future)

Media V1 is not a strong candidate for truth-table partitioning early, but future async processing may require:

* worker-owned lanes for scan/variant generation
* cleanup/reconciliation buckets
* routing by asset id/hash/bucket

**Guardrails**

* must preserve deterministic primary/ordering semantics
* must not degrade article detail read P95/P99
* must keep missing/failed media behavior graceful and observable

---

### 14.9 Partition-readiness observability signals (Media)

Use existing V1 and operational signals to decide when stronger partitioning is needed:

* article detail latency impact attributable to media lookups (if measured)
* attachment query latency by `ArticleId`
* set-primary/reorder write latency and contention indicators (if measured)
* missing media fallback rate / placeholder rate
* orphan cleanup backlog and job duration
* storage-vs-DB mismatch counts (if tracked)
* async processing backlog (V2+, if scan/variant sidecars are introduced)

**Scale trigger (policy-level)**
Consider stronger workload/data partitioning when sustained pressure causes:

* repeated media lookup latency degrading article detail SLOs despite indexes/caching
* cleanup/reconciliation workloads becoming operationally unsafe
* processing/variant pipelines (V2+) dominating worker capacity

---

## 15) ERD (dbdiagram.io)

See: `../diagrams/erd/media-v1.dbml`

How to render:

1. Open dbdiagram.io
2. Copy DBML content from the file above
3. Paste into dbdiagram.io to view/export
