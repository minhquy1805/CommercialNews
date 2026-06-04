# SEO — Dependencies & Ownership (V1)

Related:
- `../../../../architecture/arc42/03-building-blocks-modularity.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 1) Ownership boundaries

SEO owns:
- routing truth
- SEO metadata truth
- slug conflict and active-route policy
- route/metadata versioning or revision markers where used
- rebuild/reconciliation of SEO-owned derived serving artifacts, if introduced

Content owns:
- publication state and lifecycle truth
- final truth of whether content is public

Reading owns:
- public response composition
- truth-safe visibility enforcement after routing
- final serve/no-serve decision on the public path using Content truth

**Rule:** SEO can answer **where a slug points**.  
It does not own **whether that target is publicly visible right now**.

**Rule:** Public and cross-module SEO contracts should use stable public identifiers such as `ResourcePublicId`, not internal database primary keys.

---

## 2) Allowed dependencies

- Consumes Content lifecycle events asynchronously:
  - `content.article_published`
  - `content.article_unpublished`
  - `content.article_archived`
  - `content.article_soft_deleted`
  - `content.article_restored`
  - `content.article_updated` where SEO auto-sync policy requires metadata evaluation
- Reading module calls SEO public APIs for slug routing
- SEO may emit cache/projection/search update signals
- SEO batch/rebuild workflows may read:
  - SEO truth
  - bounded Content truth inputs where reconciliation requires comparison
  - cache/derived serving state for mismatch detection
- downstream consumers may consume SEO-owned events for cache/search/projection refresh
- Audit may consume SEO-owned events for investigation/governance records if SEO event emission is adopted
- cache/search/sitemap refresh consumers may consume SEO-owned events as derived-state update signals
- Audit ingestion lag must not redefine SEO truth commit success

### 2.1 Allowed dependency shapes
Approved interaction patterns are:

- **sync read** for routing resolution
- **sync truth write** for SEO-owned slug/metadata changes
- **truth commit + outbox**
- **async consumer reaction**
- **bounded rebuild/reconciliation input consumption**

Not approved:
- synchronous dependency on downstream cache/projection completion
- hidden cross-module truth mutation
- treating a derived serving artifact as though it were SEO truth

---

## 3) Forbidden dependencies

- SEO must not change Content publication state.
- Reading must not require heavy metadata reads for routing; use `/resolve` first.
- SEO must not treat Content truth as if SEO owned it.
- SEO must not publish partial derived route-serving output as complete active output.
- No cross-module DB writes; ownership respected.
- SEO rebuild/repair workflows must not mutate another module’s truth because the data is physically reachable.
- SEO must not treat Redis/search/derived projections as the authority for routing truth.
- SEO must not override Content visibility truth just because a route resolves successfully.
- Downstream modules must not infer public visibility from SEO route presence alone.

---

## 4) Truth vs derived ownership

### 4.1 Truth owned by SEO
- active route mapping
- SEO metadata truth
- slug uniqueness and route-state legality

### 4.2 Derived outputs SEO may own
If introduced, SEO may own derived outputs such as:
- cache-backed route-serving datasets
- metadata projections
- search/index-supporting route artifacts
- reconciliation outputs for SEO-serving state

These must remain:
- explicitly documented as derived
- subordinate to SEO truth
- rebuildable
- observable
- safe under rerun and replay

### 4.3 Ownership consequence
A downstream artifact can be SEO-owned and still be **derived**.

Examples:
- a cache-backed route-serving dataset may be owned operationally by SEO
- a metadata projection may be maintained by SEO
- a search/index-supporting route artifact may be emitted by SEO

But none of those automatically become:
- SEO truth
- Content publication truth
- the final authority for public exposure

---

## 5) Event and stream ownership rules

### 5.1 Content events are causes SEO reacts to
SEO may consume:
- `content.article_published`
- `content.article_unpublished`
- `content.article_archived`
- `content.article_soft_deleted`
- `content.article_restored`
- `content.article_updated` where SEO auto-sync policy requires metadata evaluation

But those events do not transfer ownership of publication truth to SEO.

Content events are inputs for SEO convergence only.
They do not authorize SEO to write Content lifecycle state or infer final public visibility without truth validation.

SEO uses them to:
- converge routing behavior
- converge metadata/indexability behavior
- update derived serving artifacts
- emit downstream invalidation/update signals where needed

### 5.2 SEO-owned events are downstream propagation signals
SEO may emit events or signals for:
- cache invalidation/update
- metadata refresh
- search/index synchronization
- downstream serving refresh

SEO event emission is optional in V1.
If adopted, SEO truth mutation and the Outbox record must commit atomically.

Possible SEO-owned integration events:
- `seo.slug_route_changed`
- `seo.slug_route_deactivated`
- `seo.metadata_updated`

SEO owns those signals as propagation mechanisms.
It does not own the truth of Content lifecycle through them.

### 5.3 Ordering ownership
SEO owns:
- route/metadata versioning for SEO truth
- correctness of active route mapping in its own truth store

Downstream consumers of SEO-owned events own:
- dedupe state
- apply tracking
- cutover/publication of their derived outputs
- replay/rebuild of their own derived stores

---

## 6) Batch / rebuild / reconciliation ownership rules

SEO batch workflows may:
- rebuild route-serving derived outputs
- reconcile SEO truth against cache or serving artifacts
- repair missing/stale route/materialized metadata effects
- clean up or archive SEO-derived maintenance outputs by policy

SEO batch workflows may also compare against bounded Content truth input when:
- a route exists but Content visibility has changed
- an SEO-serving artifact may be stale relative to publication truth
- mismatch analysis requires truth-safe comparison

SEO batch workflows must not:
- redefine route truth
- redefine Content publication truth
- bypass slug uniqueness policy
- assume that serving artifacts are authoritative just because they are queryable
- apply unsafe singleton ownership shortcuts
- publish an older candidate over newer SEO truth or newer Content visibility truth

### 6.1 Recovery posture
If cache, serving artifacts, or projections drift:
- SEO truth is the authoritative rebuild source for SEO-owned route/metadata behavior
- Content truth remains authoritative for visibility safety
- replay/rebuild/reconciliation remains a derived-state recovery concern, not a truth-transfer mechanism

---

## 7) Publication and cutover ownership

If SEO publishes a correctness-sensitive derived serving output, SEO owns:
- candidate generation
- candidate validation
- cutover/publication policy
- freshness signals
- rebuild/reconciliation policy

But SEO still does not own:
- publication lifecycle truth
- final public visibility decision

### 7.1 Cutover safety rule
SEO-owned cutover must ensure:
- candidate output is bounded and validated
- stale candidates do not replace fresher truth-backed state
- route-serving publication does not create a path that Content truth would deny
- fallback to SEO truth remains possible if derived output is stale or unavailable

---

## 8) Reading rule (V1)

Reading may use SEO as the routing sidecar.

This means:
- `/resolve` is the approved hot-path routing seam
- Reading should not perform heavy SEO truth scans itself on the hot path
- Reading may use the resolved target as a routing aid

But even in V1:
- Reading must still enforce Content truth-backed visibility
- route resolution does not equal serve permission
- stale SEO-derived output must lose to Content truth
- safe not-found or safe deny is preferred over incorrect exposure

### 8.1 Safe serving rule
If SEO-derived route state is:
- stale
- missing
- inconsistent
- lagging after Content lifecycle change

then the public-serving path must prefer:
- SEO truth fallback
- Content truth visibility check
- safe degradation

over trusting stale derived routing confidence.

---

## 9) Coordination / ownership-sensitive workflow rule

SEO normally prefers:
- idempotent consumer execution
- truth-backed fallback
- rerun-safe rebuilds
- reconciliation over exclusive control

If a future workflow truly requires exclusive ownership
(for example one-current-owner route-partition rebuild),
then it must follow system-wide coordination rules:
- explicit ownership source
- generation/fencing token
- resource-side stale-owner rejection

Naive lock/leader assumptions are forbidden.

### 9.1 Ownership ambiguity rule
If ownership is ambiguous for a correctness-sensitive rebuild/publication workflow:
- delay is acceptable
- rerun later is acceptable
- truth fallback is acceptable
- unsafe dual publication is not acceptable

Safe non-progress beats stale or duplicate cutover of serving state.

---

## 10) Module dependency posture summary

### 10.1 What SEO may expect from others
SEO may expect:
- Content to remain the single writer of publication lifecycle truth
- Reading to enforce truth-safe visibility after route resolution
- downstream consumers to treat SEO-owned signals as at-least-once and handle them idempotently
- rebuild/reconciliation workflows to respect bounded-input and cutover rules

### 10.2 What others may expect from SEO
Other modules may expect:
- authoritative active route mapping in SEO truth
- authoritative SEO-owned metadata truth
- deterministic slug conflict handling at the SEO truth boundary
- bounded truth input for SEO-owned rebuild/reconciliation workflows
- async route/metadata propagation signals after SEO truth changes

### 10.3 What nobody may assume
No module may assume:
- SEO route presence proves content is public
- Redis is fresher or more authoritative than SEO truth
- SEO may mutate Content truth for convenience
- derived serving artifacts may silently become truth
- replay/rebuild output may override fresher truth-backed routing state without validation/cutover

---

## 11) V2 evolution

SEO may evolve toward:
- alias/redirect history
- richer search/index synchronization
- more formalized derived serving artifacts
- stronger rebuild/checkpoint workflows

If that happens, SEO must make explicit:
- which datasets are SEO truth
- which datasets are derived serving outputs
- how publication/cutover works
- how fallback to SEO truth remains available
- which rebuild/reconciliation workflows are operationally critical
- how Content visibility truth remains protected even as SEO serving becomes richer

### 11.1 V2 constraint that remains unchanged
Even if SEO evolves into more advanced serving and synchronization workflows:

- SEO truth remains separate from Content lifecycle truth
- derived serving outputs remain subordinate to SEO truth
- Content truth remains authoritative for public visibility
- replay/rebuild remains a recovery mechanism, not a truth-transfer mechanism
