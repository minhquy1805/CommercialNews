# SEO Module — API Architecture (V1)

**Purpose**
- Provide stable, fast, and safe public entry points (slug routing) and store SEO metadata:
  - slug + canonical URL
  - meta title/description defaults
  - social preview fields
- Keep **routing truth** and SEO metadata truth explicit and authoritative within the SEO boundary.
- Support cache, projection, stream-style propagation, and bounded rebuild/reconciliation workflows without letting derived routing outputs become hidden truth.

**Why this module is critical**
- Slug resolution is a **public entry point** and often a hot path.
- SEO regressions have long-lived negative impact (traffic loss persists after incidents).
- Slug and canonical rules must be stable to avoid link breakage and duplicate indexing.
- SEO may serve fast routing, but must not become the final authority for publication visibility.
- Future projections and cache layers may help performance, but must remain subordinate to authoritative SEO and Content truth.
- SEO route and metadata changes may propagate asynchronously, so duplicate delivery, stale delivery, replay, and rebuild must be handled safely.

**Primary consumers**
- Public Query/Reading module (slug resolution + metadata retrieval)
- Content module (indirectly via events; Content is source of truth for publication state)
- Admin UI (edit SEO metadata)
- Edge/caching layers (optional)
- Future search/index or SEO-serving consumers
- Future rebuild/reconciliation workflows for routing and metadata outputs

**Non-goals (V1)**
- Sitemap/robots automation (V2)
- Slug alias/redirect strategy (V2, ADR hook)
- Full SEO analytics
- Treating cache/projection/search outputs as routing truth
- Coordination-heavy global routing ownership
- Full event sourcing for SEO truth
- Requiring synchronous downstream cache/projection completion for SEO write success

**Hard constraints**
- Slug uniqueness: **zero tolerance for collisions** (within defined scope).
- Slug routing must remain **fast and reliable** under peak read traffic.
- SEO visibility behavior must follow publication state (unpublished/archived not indexable by policy).
- Slug routing must not be coupled to heavy metadata reads.
- SEO truth is **primary and authoritative** for active slug mapping and SEO metadata.
- Public visibility must still be validated against Content truth.
- Redis/cache is acceleration only; truth-backed DB fallback must remain available.
- SEO async propagation is **at-least-once**; duplicates, replay, stale deliveries, and version gaps must be tolerated safely.
- Derived routing/metadata outputs must remain **observable**, **rebuildable**, and **subordinate to authoritative truth**.
- Rebuild/reconciliation workflows must be **bounded**, **observable**, and **rerun-safe**.
- Candidate routing or metadata outputs must not be exposed as active truth before validation/cutover where correctness matters.

**Truth vs derived posture**
- **Truth**:
  - active slug mapping
  - slug uniqueness within scope
  - canonical metadata
  - SEO metadata fields
  - local revision/version markers where used
- **Derived**:
  - routing cache
  - metadata cache
  - optional read-optimized SEO projections
  - future search/index outputs
  - reconciliation or mismatch reports
  - rebuilt serving artifacts awaiting cutover
- **Rule**: SEO truth decides route mapping, but public exposure still depends on Content truth.  
  Derived outputs may lag, replay, or be rebuilt, but they do not become final visibility authority.

**Stream / async posture**
- SEO participates in the standard V1 model:
  1. truth commits in the owning module
  2. outbox is written atomically
  3. Background Worker publishes asynchronously
  4. broker delivers at least once
  5. consumers converge derived state idempotently
- SEO consumes Content lifecycle events such as:
  - `ArticlePublished`
  - `ArticleUnpublished`
  - `ArticleArchived`
- SEO may emit downstream signals for:
  - cache invalidation/update
  - metadata refresh
  - future search/index synchronization
- Where ordering matters, downstream consumers should use:
  - `messageId` for dedupe
  - route/aggregate version markers for stale-event rejection and ordered apply
- If a consumer detects uncertainty or version gaps, truth resync or bounded rebuild is preferred over unsafe stale apply.

**Batch / rebuild / reconciliation posture**
- SEO may run bounded workflows for:
  - routing cache rebuild
  - metadata projection rebuild
  - slug conflict/drift detection
  - reconciliation between SEO truth and derived routing/materialized outputs
  - future sitemap/search-support generation
- These workflows must:
  - start from authoritative SEO truth and, where needed, Content truth
  - remain rerun-safe
  - avoid publishing stale routing state over fresher truth
  - validate important candidate outputs before publication/cutover where correctness matters
  - treat replay/rebuild as recovery for derived outputs, not as redefinition of routing truth

**Primary correctness posture**
- Slug resolution is hot-path truth inside SEO, but not enough by itself for public exposure.
- Content truth still decides whether the resolved target is publicly visible.
- Cache hit is not enough; stale cache must not re-expose non-public content.
- If uncertainty exists, prefer truth-backed fallback or safe not-found behavior over stale confidence.
- Safe routing degradation is preferable to incorrect public exposure.

**Consistency and ordering posture**
- Strong consistency is required at the SEO truth boundary for:
  - active slug ownership
  - uniqueness within scope
  - authoritative route target mapping
  - SEO-admin truth changes
- Eventual consistency is accepted for:
  - cache invalidation/update
  - metadata propagation
  - search/index refresh
  - derived serving artifacts
  - rebuild/reconciliation convergence
- Ordering is scoped to the relevant route/aggregate boundary, not globally across all SEO activity.
- Wall-clock timestamps are useful for observability, not authoritative for stale-write or stale-route resolution.

**Failure and recovery posture**
- Timeout does not prove route absence or write failure.
- Successful SEO truth commit does not prove cache refresh, broker publish, or downstream serving update already completed.
- Lag in routing cache or metadata propagation is acceptable only if:
  - it is observable
  - it is repairable
  - it cannot override Content visibility truth
- Important derived SEO outputs must have replay/rebuild/reconciliation paths.
- Safe non-progress beats stale route confidence or unsafe cutover of older serving artifacts.

**Key links**
- System-wide rules:
  - `../../01-api-architecture-charter-v1.md`
  - `../../02-contracts-and-standards.md`
  - `../../09-observability-and-slos.md`
- Arc42:
  - `../../../architecture/arc42/03-building-blocks-modularity.md`
  - `../../../architecture/arc42/04-runtime-view-v1.md` (Scenario 3 + SEO parts of 1–2)
  - `../../../architecture/arc42/05-quality-requirements.md`
  - `../../../architecture/arc42/13-transactions-and-consistency-v1.md`
  - `../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
  - `../../../architecture/arc42/19-stream-processing-runtime-v1.md`
  - `../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
  - `../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- System data model:
  - `../../../architecture/arc42/system-data/system-data-seo-v1.md`
- Quality profile:
  - `../../../architecture/arc42/quality/seo.md` (if present)

**ADR hooks**
- Slug stability + when slug changes
- Slug alias/redirect strategy (V2)
- Canonical rules (duplicates/near-duplicates)
- Scope definition for slug uniqueness (global vs per-category/locale)
- Routing cache/projection rebuild and cutover policy
- Reconciliation policy for SEO truth vs derived routing outputs
- Version-gap handling and resync policy for SEO consumers