# Reading Module (Public Query) — API Architecture (V1)

**Purpose**
- Provide the public reading experience as a **query facade**:
  - article listing with paging/filter/sort
  - article detail (by ID and/or by slug)
  - related articles
  - basic keyword search (scope-dependent)
- Compose public responses from module-owned truth while keeping **publication visibility correctness** explicit.
- Support cache, enrichment, stream-style derived-state maintenance, projection, and bounded rebuild/reconciliation workflows without turning derived outputs into hidden serving truth.

**Why this module is critical**
- It is the **read path** and must remain fast/available under burst traffic (hot articles).
- It must strictly enforce publication-state correctness (no draft/unpublished exposure).
- It must degrade gracefully when non-critical subsystems fail (Interaction counters, telemetry pipelines, derived enrichments, etc.).
- It often depends on multiple upstream sources, so truth-vs-derived discipline is essential.
- Future read models may improve performance, but must not silently replace truth-backed correctness without explicit promotion rules.
- Route resolution, cache hits, and derived summaries are useful accelerators, but none of them are final authority for public visibility.

**Primary consumers**
- Public web/mobile clients (anonymous + authenticated)
- Edge caching layers (optional)
- Internal monitoring/synthetics
- Future read-model rebuild/reconciliation workflows
- Future projection/read-fragment consumers owned by Reading

**Non-goals (V1)**
- Full projections/read models as the only serving source (V2+)
- Advanced search ranking and personalization (future)
- Guaranteed strongly consistent counters (Interaction is eventual)
- Treating cache, projection, or aggregate enrichment as publication truth
- Coordination-heavy global serving ownership
- Requiring downstream aggregation, telemetry, or cache refresh to complete before a read succeeds

**Hard constraints**
- Only show published content publicly.
- Sorting/filter semantics must be predictable and documented.
- Related articles must have deterministic fallback behavior.
- Interaction tracking must not block responses.
- Reading must validate visibility from authoritative truth before returning public content.
- Reading-side async signals and derived enrichments are **at-least-once / eventually consistent** concerns and must remain non-blocking.
- Derived outputs must remain **observable**, **rebuildable where practical**, and **subordinate to truth-backed visibility rules**.
- Rebuild/reconciliation workflows must be **bounded**, **observable**, and **rerun-safe**.
- Candidate projection or rebuilt output must not be exposed as active serving truth before validation/cutover where correctness matters.
- Safe degraded behavior is acceptable; incorrect public exposure is not.

**Truth vs derived posture**
- **Truth used by Reading**:
  - Content truth for publication state and visibility
  - SEO truth for slug routing
  - Media truth for attachment/primary metadata
- **Derived inputs used by Reading**:
  - cached article responses
  - interaction counters
  - related-article summaries
  - projection/read-model outputs
  - search/materialized query outputs
  - trending inputs and batch-generated summaries
  - mismatch/reconciliation reports for public-serving outputs
- **Rule**: Reading is a truth-composing facade.  
  Derived inputs may lag, replay, or be rebuilt, but they must not override truth-backed visibility correctness.

**Stream / async posture**
- Reading primarily serves synchronously, but may emit non-blocking side signals such as:
  - view tracking
  - access telemetry
  - optional cache-aside refresh signals
- Those signals are:
  - post-composition
  - retry-safe
  - non-authoritative for public correctness
- Reading may consume derived inputs that are maintained asynchronously by other modules:
  - counters
  - summary fragments
  - future read-model enrichments
- Replay, lag, duplicate delivery, and stale enrichments are normal runtime realities for those inputs.
- Reading must prefer truth-backed fallback over trusting stale derived confidence.

**Batch / rebuild / reconciliation posture**
- Reading may participate in bounded workflows for:
  - projection/read-model rebuild
  - related-content recomputation
  - search/materialized query rebuild
  - cache warm/rebuild operations
  - reconciliation between truth-backed reads and derived serving outputs
  - trending and summary regeneration
- These workflows must:
  - start from authoritative module truth or approved durable input
  - remain rerun-safe
  - avoid exposing partial or stale candidate output as active public truth
  - validate important candidate outputs before publication/cutover where correctness matters
  - treat rebuild/replay as recovery for derived read-side quality, not as redefinition of publication truth

**Primary correctness posture**
- Slug resolution is not enough; visibility must still be confirmed from truth.
- Cache hit is not enough; stale cache must not leak non-public content.
- Missing or stale enrichments degrade gracefully; they do not redefine visibility truth.
- If uncertainty exists, prefer truth-backed fallback or safe not-found behavior over stale confidence.
- Truth-backed degraded behavior is acceptable; stale derived certainty is not.

**Consistency and ordering posture**
- Strong truth-backed consistency is required for:
  - final public allow/deny decision
  - safe slug-resolve → visibility-check → response chain
  - avoiding draft/unpublished exposure
- Eventual consistency is accepted for:
  - counters
  - related-content hints
  - search/materialized outputs
  - cached fragments
  - read-facing summaries
- Reading does not require one globally freshest derived view.
- Reading does require causal correctness:
  - route first
  - visibility check second
  - enrich only if safe
- Version-aware enrichments are preferred over timestamp-only freshness judgments when available.

**Failure and recovery posture**
- Timeout does not prove content absence.
- Cache miss does not prove truth absence.
- Route success does not prove visibility success.
- Successful public read does not require view tracking, aggregation, cache refill, or rebuild completion.
- If derived read-side state becomes stale or broken, truth-backed fallback preserves correctness while replay/rebuild/reconciliation restores quality.

**Key links**
- System-wide rules:
  - `../../01-api-architecture-charter-v1.md`
  - `../../02-contracts-and-standards.md`
  - `../../09-observability-and-slos.md`
- Arc42:
  - `../../../architecture/arc42/03-building-blocks-modularity.md` (Public Query module)
  - `../../../architecture/arc42/04-runtime-view-v1.md` (Scenario 3)
  - `../../../architecture/arc42/05-quality-requirements.md` (Read path performance/availability)
  - `../../../architecture/arc42/13-transactions-and-consistency-v1.md`
  - `../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
  - `../../../architecture/arc42/19-stream-processing-runtime-v1.md`
  - `../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
  - `../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- Upstream modules:
  - Content (publication state source of truth)
  - SEO (slug routing hot path)
  - Media (cover/attachments)
  - Interaction (views/likes/comments; non-blocking)
- System data model:
  - `../../../architecture/arc42/system-data/system-data-reading-v1.md` (if present)

**ADR hooks**
- Popularity definition (views vs likes vs blended)
- Search capability level (basic vs full-text) and upgrade path
- Read caching policy (TTL, invalidation signals, bypass for canary)
- Projection/read-model promotion rules (when a derived read source may serve as primary public source)
- Rebuild/reconciliation policy for public-serving derived outputs
- Event-time vs processing-time policy for reading-side summaries/trending inputs