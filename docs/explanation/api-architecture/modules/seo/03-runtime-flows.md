# SEO — Runtime Flows (V1)

Supports arc42 scenarios:
- Scenario 3: public user opens article by slug
- SEO side effects from Scenario 1–2 (publish/unpublish/archive/soft-delete/restore)

Related:
- `../../../../architecture/arc42/04-runtime-view-v1.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## Runtime posture in V1

SEO participates in all three runtime lanes:

### A) Synchronous routing / admin truth lane
Used for:
- slug resolution on the hot path
- SEO-admin truth changes
- route/metadata reads from SEO truth
- uniqueness enforcement for SEO-owned routing/metadata truth

### B) Asynchronous side-effect and derived-state lane
Used for:
- reacting to Content lifecycle events
- cache invalidation / refresh signals
- downstream metadata update signals
- eventual propagation to derived routing/metadata consumers
- version-aware convergence of serving artifacts after Content or SEO truth changes

### C) Batch / rebuild / reconciliation lane
Used for:
- rebuilding derived routing-serving artifacts
- reconciling SEO truth against cache or derived outputs
- repairing missing route/materialized metadata effects after consumer lag/failure
- cleanup / archival / maintenance of derived SEO outputs if introduced

**Rule:** SEO owns routing truth and SEO metadata truth, but not publication visibility truth.

**Rule:** SEO success is defined by **SEO truth commit** or **SEO truth-backed hot-path decision**, not by downstream cache/projection freshness.

**Rule:** SEO-derived async processing is assumed **at-least-once**.  
Duplicates, retries, replay, and stale deliveries must be tolerated safely.

---

## Flow A — Public slug routing (hot path)

### Goal
Resolve slug to target safely and quickly, without turning stale routing into public exposure.

### Sync path
1. User requests article by slug.
2. Reading module calls `GET /api/v1/seo/resolve?scope=public&slug=...`.
3. SEO attempts cache-first route lookup if enabled.
4. On miss or suspected staleness, SEO falls back to SEO truth store.
5. SEO returns `resourcePublicId` + `canonicalUrl` + indexable/status flags.
6. Reading uses `resourcePublicId` to fetch Content/Reading truth and render response.
7. Reading still validates Content truth before public response is returned.

### Runtime rules
- Slug resolution is a **routing helper**, not the final authority for public visibility.
- Cache presence or route presence does not prove content is public.
- Content truth-backed visibility checks remain authoritative at serve time.
- Safe 404 / safe deny beats stale route confidence.

### Failure modes
- If slug cannot be resolved from SEO truth: return safe 404.
- If cache is stale: DB fallback is required.
- If SEO store is degraded:
  - reading by slug may fail; monitor and prioritize recovery (hot path).
- If slug resolves but Content truth denies visibility:
  - safe deny/404 must win; no leak is allowed.
- If a stale derived route survives after unpublish:
  - it must lose to truth-backed visibility denial.

### Observability notes
- Track:
  - resolve latency / error
  - cache hit ratio
  - DB fallback rate
  - resolved-but-denied-by-visibility rate (internal)
  - stale route / truth mismatch indicators where measurable

---

## Flow B — Publish triggers SEO update (async)

### Goal
Make routing and metadata converge after Content truth publishes an article.

### Async flow
1. Content publishes an article (sync).
2. Content emits `content.article_published`.
3. SEO consumer receives `content.article_published`.
4. SEO checks message-level dedupe by `MessageId`.
5. SEO checks `AggregateVersion` against stored `SourceAggregateVersion` for the `AggregateId` / `ResourcePublicId`.
6. SEO applies idempotent upsert for `SlugRegistry` / `SeoMetadata`.
7. SEO records `LastAppliedMessageId`, `SourceAggregateVersion`, and `LastSyncedAtUtc`.
8. SEO may emit SEO integration events for cache/sitemap/search refresh if adopted, such as:
   - `seo.slug_route_changed`
   - `seo.metadata_updated`

### Runtime stream semantics
- `content.article_published` is a truth-following event for SEO, not a replacement for Content truth.
- SEO consumers should use:
  - `MessageId` for dedupe
  - `(AggregateId / ResourcePublicId, AggregateVersion)` for ordered apply or stale-event rejection
- Duplicate delivery must converge safely.
- Stale publish events must not overwrite newer SEO truth or newer derived serving state.

### Failure modes
- If SEO consumer is delayed:
  - article may be readable by ID/listing, but slug routing/metadata may lag temporarily.
  - backlog/lag must be observable.
- Duplicate event delivery must converge safely.
- Stale delivery must not overwrite newer SEO truth.
- Version gap should trigger resync or bounded rebuild rather than blind apply.

### Batch / rebuild hooks
- Reconciliation or rebuild workflows may later:
  - detect missing route or metadata effects
  - rebuild candidate derived SEO-serving state
  - publish repaired output safely where relevant

### Runtime rules
- Content publish truth exists before SEO-derived serving state becomes meaningful.
- SEO lag is acceptable only if truth-safe fallback preserves correctness.
- Where a full rebuild is simpler than partial repair, rebuild is preferred.

---

## Flow C — Unpublish/archive/soft-delete triggers de-indexability (async)

### Goal
Ensure SEO-derived behavior converges after Content truth removes or changes public visibility.

### Async flow
1. Content unpublishes/archives/soft-deletes/restores (sync; source of truth changes immediately).
2. Content emits one of:
   - `content.article_unpublished`
   - `content.article_archived`
   - `content.article_soft_deleted`
   - `content.article_restored`
3. SEO checks message-level dedupe by `MessageId`.
4. SEO rejects or ignores stale `AggregateVersion` values for the `AggregateId` / `ResourcePublicId`.
5. SEO deactivates the route or marks it non-indexable idempotently when Content truth removes public visibility.
6. For restore events, SEO re-checks Content truth and does not automatically make content indexable unless the article is currently publicly visible.
7. SEO records `LastAppliedMessageId`, `SourceAggregateVersion`, and `LastSyncedAtUtc`.
8. SEO may emit SEO integration events if adopted, such as:
   - `seo.slug_route_deactivated`
   - `seo.slug_route_changed`
   - `seo.metadata_updated`

### Runtime stream semantics
- `content.article_unpublished`, `content.article_archived`, `content.article_soft_deleted`, and `content.article_restored` are ordering-sensitive for downstream SEO state.
- Older publish-derived state must not win after a later unpublish/archive/soft-delete/restore.
- Consumers should prefer version-aware update or truth resync over arrival-order trust.

### Non-negotiable
- Even if SEO processing lags, public read must not expose unpublished, archived, or soft-deleted content (Content remains source of truth).
- Stale route cache presence must not defeat truth-backed visibility denial.
- A stale or replayed publish event must not resurrect now-non-public content in SEO-derived state.
- Restored content must not automatically become indexable unless Content truth says the article is publicly visible.

### Batch / rebuild hooks
- Reconciliation workflows may later:
  - compare SEO truth against derived route-serving state
  - repair stale route entries
  - verify non-indexability convergence after delayed processing

### Runtime rules
- Unpublish/archive/soft-delete truth wins immediately.
- Restore truth only reopens SEO-derived route/indexability when Content truth confirms public visibility.
- SEO serving lag is operationally important, not truth-authoritative.
- Safe deny beats stale routing confidence.

---

## Flow D — SEO admin updates slug or metadata (sync truth path)

### Goal
Safely mutate SEO-owned truth without stale overwrite or uniqueness drift.

### Sync path
1. Admin submits slug or metadata change.
2. SEO validates command semantics and conflict rules.
3. SEO updates routing truth and/or metadata truth.
4. SEO writes Outbox in the same transaction if downstream invalidation/update events are adopted.
5. SEO returns success based on SEO truth commit only.
6. After commit, relay/workers may publish SEO integration events such as `seo.slug_route_changed`, `seo.slug_route_deactivated`, or `seo.metadata_updated`.

### Rules
- `(Scope, Slug)` active uniqueness is enforced at SEO truth boundary.
- Version/revision checks are preferred for stale-write protection.
- Redis/cache update is not part of the truth transaction.
- Downstream cache/projection refresh follows SEO truth asynchronously.
- If a slug/routing change impacts public serving, downstream artifacts must still remain subordinate to Content truth for visibility.

### Failure modes
- Slug conflict: clear conflict response.
- Stale admin update: explicit conflict/reject if optimistic concurrency is used.
- Broker/cache failure after commit: SEO truth still succeeds; downstream lag is repaired asynchronously.
- Duplicate or replayed SEO integration events downstream must not regress current SEO-serving state.

---

## Flow E — SEO rebuild / reconciliation workflow (batch lane)

### Goal
Restore or validate derived SEO-serving state from bounded truth input after lag, failure, or drift.

### Typical workflow shape
1. Select bounded input:
   - e.g. all published articles in scope S
   - or all changed SEO truth rows since checkpoint C
   - or all suspect route entries in bounded set B
2. Normalize / partition by target/article/slug scope
3. Compare truth and derived route-serving state
4. Generate candidate rebuild or repair set
5. Validate candidate output
6. Publish / cut over rebuilt output or apply bounded repair safely
7. Cleanup temporary workflow state

### Typical outputs
- rebuilt routing-serving datasets
- repaired cache-backed route maps
- reconciliation mismatch reports
- metadata repair candidate sets

### Rules
- Output is derived state unless explicitly defined as SEO truth.
- Partial candidate output must not be treated as completed active output.
- Workflow rerun on the same bounded input must be harmless or explicitly controlled.
- Rebuild/publication must not publish older route assumptions over newer SEO truth or newer Content visibility truth.
- If uncertainty exists, truth re-read/resync is preferred over unsafe cutover.

### Failure modes
- Rebuild failure: SEO truth remains authoritative; DB fallback still exists.
- Candidate publication failure: previous active derived output remains valid if one exists.
- Duplicate run / overlapping execution: must be safe under rerun policy and ownership rules.
- Stale source snapshot used too late: rebuild must be rejected, rerun, or cut over only under explicit bounded semantics.

---

## Flow F — Cache refresh / invalidation workflow

### Goal
Keep route cache hot without letting cache become routing authority.

### Flow
1. SEO truth changes or receives downstream trigger.
2. Event-driven invalidation/update is attempted.
3. Cache is refreshed or invalidated asynchronously.
4. On future resolve requests, cache miss/staleness falls back to SEO truth.

### Rules
- TTL is a safety net, not the correctness mechanism.
- Cache refresh lag is acceptable if truth fallback works.
- Cache refresh must not overwrite fresher route knowledge with stale event/state.
- Cache remains derived acceleration only, never the sole authority for route correctness or visibility correctness.

### Failure modes
- Cache refresh delayed: hot-path latency may increase due to DB fallback.
- Duplicate refresh/invalidation: must be harmless.
- Stale cache update after fresher truth exists: must be rejected or naturally lose through freshness/version rules.

---

## Flow G — Truth-safe serving under route/projection lag

### Goal
Preserve correct public behavior when SEO-derived serving state is stale, missing, or inconsistent.

### Typical runtime shape
1. Route cache / serving artifact resolves a slug or returns a route hint.
2. Reading follows the SEO result as a routing aid.
3. Before public response is committed, Reading checks Content truth-backed visibility.
4. If truth and SEO-derived state disagree:
   - truth wins
   - derived state is treated as stale and recoverable
5. Recovery may happen later via replay/rebuild/reconciliation.

### Examples
- stale route still points to unpublished content
- canonical/meta projection lags after publish
- rebuilt route dataset is missing a newly published article
- old serving artifact survives after archive/unpublish

### Rules
- SEO truth and derived serving state support routing and metadata.
- Content truth remains authoritative for public visibility.
- Safe not-found or safe degrade is preferred over incorrect exposure.
- Truth fallback is a valid degraded behavior, not a correctness failure.

---

## Summary

SEO runtime in V1 is governed by ten rules:

1. SEO owns routing truth and metadata truth, but not publication truth.  
2. Slug resolution is hot-path critical, but still subordinate to Content visibility checks.  
3. Cache is acceleration only; DB fallback is mandatory.  
4. Async publish/unpublish/archive/soft-delete/restore reactions must be idempotent and version-aware.
5. Duplicate delivery, replay, and stale event arrival are normal and must converge safely.  
6. Batch workflows may rebuild, reconcile, and repair derived SEO-serving outputs.  
7. Partial candidate output must not be published as complete active state.  
8. Rebuild/cutover must not publish older route assumptions over newer truth.  
9. Truth-safe fallback beats stale routing confidence.  
10. Rebuild/reconciliation support SEO operability, but do not redefine truth ownership.
