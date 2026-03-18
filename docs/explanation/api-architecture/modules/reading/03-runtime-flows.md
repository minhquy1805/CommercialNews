# Reading — Runtime Flows (V1)

Primary runtime scenario:
- arc42 Scenario 3: public list + open by slug

Related:
- `../../../../architecture/arc42/04-runtime-view-v1.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- `../../../../decisions/adr-0015-cache-policy-and-invalidation-redis-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## Runtime posture in V1

Reading participates in all three runtime lanes:

### A) Synchronous read lane
Used for:
- public article listing
- public article detail by slug
- truth-safe composition of content + seo + media
- correctness-first fallback when derived enrichments are stale or missing

### B) Async side-signal lane
Used for:
- non-blocking view tracking signals
- optional telemetry/access signals
- eventual interaction aggregation
- future downstream read-side freshness or telemetry signals

### C) Batch / rebuild / reconciliation lane
Used for:
- rebuilding reading-side derived summaries
- generating trending inputs
- reconciling truth-backed published content with derived enrichments
- replaying or repairing missing derived outputs when async processing lagged or failed

**Rule:** Reading correctness is defined by truth-backed visibility and safe composition, not by freshness of derived enrichments.

**Rule:** Reading is a **truth-safe query facade**, not the owner of publication truth.

**Rule:** Derived enrichments, counters, projections, and summaries may lag, replay, or be rebuilt.  
They improve experience, but do not define whether content is readable.

---

## Flow A — List articles (read path)

### Goal
Return published article lists quickly while tolerating lag in non-critical enrichments.

### Sync path
1. Client calls `GET /api/v1/articles?page=...`.
2. Reading queries published articles from truth-backed sources or policy-approved derived read path.
3. Reading enriches with:
   - SEO slug (if needed)
   - cover media (if available)
   - counters (if available)
4. Returns response quickly.

### Runtime rules
- Publication visibility must remain truth-safe.
- Missing or stale enrichments must not fail the entire list response if core readable content is available.
- Reading may omit or degrade enrichments, but must not invent visibility or serve non-public content.
- If a derived listing/projection exists later, it remains subordinate to truth-backed visibility rules.

### Failure modes
- Interaction down: return content without counters (or stale counters).
- Media partial: return placeholder/empty cover.
- SEO partial: still return list; slug may be missing only if unavoidable (prefer stable slug from SEO store when possible).
- Cache/projection stale: fall back to truth-backed composition rather than failing the whole list response.
- Derived listing says content is visible but Content truth disagrees: truth denial wins.

### Observability notes
- Track list latency and error rate separately from detail.
- Track omitted-enrichment rate for:
  - missing counters
  - missing media
  - SEO fallback behavior
- Track truth-fallback rate when derived list-serving state is stale or unavailable.

---

## Flow B — Open article by slug (hot path)

### Goal
Resolve slug safely and return article detail without leaking non-public content.

### Sync path
1. Client opens `/articles/{slug}` (either via SEO resolve + detail, or via `/articles/slug/{slug}`).
2. Reading calls SEO `/resolve` to map slug → articleId.
3. Reading fetches article detail from Content (published-only / truth-safe visibility).
4. Reading composes response (media/seo metadata/counters).
5. Reading triggers view tracking signal (non-blocking; Interaction).

### Runtime rules
- Route resolution is a routing aid, not final serve authority.
- Content truth-backed visibility check must always win over stale routing/projection state.
- If slug resolves but content is no longer public, Reading must return safe 404 / safe deny.
- Detail response success is defined by truth-safe readable content, not by completion of counters, telemetry, or cache refresh.

### Failure modes
- SEO resolve fails: return safe 404.
- Content lookup returns non-published: return safe 404.
- Interaction fails: reading still succeeds; view counting lags.
- Media fails: fallback media.
- Derived enrichments stale/missing: omit or fallback safely; do not block detail response.
- Slug resolves but visibility changed: truth-backed visibility denial must win.
- Stale derived detail/projection still contains now-non-public content: it must lose to truth-backed read.

### Observability notes
- Track:
  - slug resolve latency/error
  - truth fallback rate
  - visibility-denied-after-resolve rate (internal only)
  - detail latency/error
  - interaction signal enqueue success/failure
  - derived-detail omitted-enrichment rate

---

## Flow C — Non-blocking view tracking signal

### Goal
Emit a retry-safe signal for later aggregation without coupling read latency to Interaction processing.

### Flow
1. Reading finishes truth-safe response composition.
2. Reading emits a lightweight view signal or enqueue request.
3. Interaction accepts the signal asynchronously by policy.
4. Downstream consumers aggregate later.

### Rules
- Read response does not depend on signal completion.
- Duplicate signals are tolerated by downstream policy.
- Signal failure must not change visibility or article detail correctness.
- View-signal timing may lag behind response timing; analytics correctness belongs to downstream event-time policy, not to synchronous read completion.

### Failure modes
- Signal enqueue fails: article response still returns successfully.
- Broker or consumer lag: counters/trending lag, but read response remains valid.
- Duplicate signal delivery: counters/aggregates must remain safe under downstream dedupe or recompute policy.

---

## Flow D — Bounded reading aggregation workflow (batch lane)

### Goal
Generate reading-side derived summaries from bounded interaction input.

### Typical workflow shape
1. Select bounded input:
   - e.g. all view events in window W
2. Normalize / partition by key:
   - `ArticleId`
   - `(ArticleId, Date)`
3. Aggregate into candidate summaries
4. Validate candidate output
5. Publish / replace active derived summary if policy requires
6. Cleanup temporary workflow state

### Typical outputs
- daily article view summaries
- trending inputs
- bounded summary tables
- repairable derived reading enrichments

### Rules
- Output is derived state, not publication truth.
- Workflow rerun on the same bounded input must be harmless or explicitly controlled.
- Partial candidate output must not be exposed as completed active state.
- If event-time semantics matter, lateness policy must be explicit at the workflow/module level.
- If a full rebuild is simpler than fragile partial repair, rebuild is preferred.

### Failure modes
- Aggregation failure: reading still works; summaries lag.
- Candidate publication failure: previous active derived summary remains valid if one exists.
- Replay/rebuild overlap: must be safe under rerun/duplicate execution policy.
- Late or replayed interaction input must not silently corrupt active derived summaries.

---

## Flow E — Reading reconciliation / repair workflow

### Goal
Detect and repair divergence between truth-backed readable content and reading-side derived outputs.

### Typical cases
- published content exists but reading-derived enrichment missing
- counters/trending input missing after consumer lag
- stale derived summary beyond freshness policy
- cache/projection mismatch detected by reconciliation policy
- derived read model contains content that truth would now deny

### Typical workflow shape
1. Select bounded truth and/or derived comparison scope
2. Detect mismatches
3. Generate bounded repair candidate set
4. Validate repair candidate
5. Apply repair safely or publish repaired derived output
6. Record completion / cleanup

### Rules
- Repair workflow must not redefine truth visibility.
- Truth-safe fallback must remain available even while repair is pending.
- Repair output must follow candidate-before-apply discipline where correctness matters.
- If derived state is too uncertain, bounded rebuild from truth-backed inputs is preferred over fragile patch mutation.

### Recovery posture
- Reading repair workflows are allowed to:
  - replay missing enrichments
  - rebuild summaries
  - regenerate read-side candidates
  - correct drift against truth-backed sources
- They are not allowed to:
  - declare content readable when truth would deny it
  - bypass visibility checks because a derived candidate “looks complete”

---

## Flow F — Reading-side cache refresh / fallback behavior

### Goal
Use cache as acceleration only, never as hidden truth.

### Flow
1. Attempt cache-backed read path where configured.
2. On miss or suspected stale data, fall back to truth-backed sources.
3. Optionally refresh cache after safe truth-backed read.
4. Return response based on correctness-first composition.

### Rules
- Cache hit must not bypass truth-sensitive visibility enforcement.
- Cache refresh is optional and must not stretch the read path unboundedly.
- Reading must prefer safe omission over stale invented enrichment.
- Cache refresh/update must be harmless under duplicate signals or repeated requests.
- Stale cache must lose to truth-backed visibility every time.

### Failure modes
- Cache miss: latency may increase, correctness must hold.
- Cache stale after unpublish/archive: truth denial must win.
- Cache refresh fails: response may still succeed from truth-backed composition.
- Replayed cache update after fresher truth exists: stale cache content must not regain authority.

---

## Flow G — Truth-safe serving under projection or enrichment lag

### Goal
Preserve correct public behavior when read-side projections, counters, summaries, or enrichments are behind.

### Typical runtime shape
1. Reading receives a public request.
2. A derived projection, cache, or enrichment source may provide partial fast-path data.
3. Reading validates visibility and core readable content against truth-backed sources.
4. If derived data is stale, missing, or inconsistent:
   - core readable content still serves if truth allows
   - stale enrichments are omitted, degraded, or repaired later
5. Recovery happens through replay/rebuild/reconciliation workflows as needed.

### Examples
- article is published, but counters are stale
- article is published, but trending enrichment is missing
- projection still shows content after unpublish
- SEO route resolves, but Content truth says non-public

### Rules
- Truth-backed visibility comes before enrichment freshness.
- Core readable content may still succeed while enrichments lag.
- Safe degrade is preferable to blocking on derived freshness.
- Derived read convenience is never authority for public exposure.

---

## Summary

Reading runtime in V1 is governed by nine rules:

1. Truth-backed visibility comes first.  
2. Reading is a truth-safe facade, not the owner of publication truth.  
3. Async side signals are optional for read success.  
4. Derived enrichments may lag or be omitted safely.  
5. Route resolution does not override Content visibility truth.  
6. Batch workflows support summaries, replay, rebuild, and repair — not truth ownership.  
7. Candidate derived output must be validated before publication when output correctness matters.  
8. Replay, rerun, and lag are normal for derived reading outputs and must remain safe.  
9. Truth fallback beats stale derived confidence every time.