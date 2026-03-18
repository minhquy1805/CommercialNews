# Reading — Dependencies & Ownership (V1)

Related:
- `../../../../architecture/arc42/03-building-blocks-modularity.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../decisions/adr-0015-cache-policy-and-invalidation-redis-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 1) Ownership

Reading owns:
- public query semantics and response composition
- cache usage policy and degradation behavior on the read path
- truth-safe public response composition
- omission/fallback behavior for stale or missing enrichments
- reading-side derived outputs only when explicitly introduced as Reading-owned summaries, fragments, or read-facing enrichments
- bounded rebuild/reconciliation of Reading-owned derived outputs, if introduced

Reading does **not** own:
- publication truth
- slug-routing truth
- media truth
- interaction truth
- batch truth for other modules
- final truth of whether a piece of content exists or is public

**Rule:** Reading owns **how** the public response is composed and degraded.  
It does not own **whether** the content is public.

---

## 2) Allowed dependencies (V1)

- SEO `/resolve` for slug routing
- Content read-only access by policy
- Media read-only access by policy
- Interaction for non-blocking view signals / counters (eventual)
- Redis/cache as acceleration only
- batch/rebuild/reconciliation workflows that read truth or derived inputs to produce Reading-owned summaries or enrichments

### 2.1 Allowed dependency shapes
Approved interaction patterns are:

- **sync read** from truth-owning modules
- **truth-backed composition**
- **optional async side signal** (view/telemetry)
- **cache-aside / fallback behavior**
- **bounded rebuild/reconciliation input consumption**

Not approved:
- synchronous dependence on downstream aggregation success
- hidden truth mutation in another module
- treating route/cache/projection presence as if it were publication truth

---

## 3) Forbidden dependencies

- Reading must not write into Content/SEO/Media/Interaction stores as business truth.
- Reading must not require audit/notifications/interaction aggregation to respond.
- Reading must not treat cache/projection/summary state as hidden truth.
- Reading must not publish partially built derived outputs as complete active state.
- Reading must not rely on naive singleton ownership for rebuild/repair workflows.
- No gateway loopback for internal calls.
- Reading must not infer public visibility from SEO route resolution alone.
- Reading must not trust stale derived fragments over Content visibility truth.
- Reading must not mutate another module’s truth under the excuse of “repair” or “fallback optimization.”

---

## 4) Truth vs derived ownership

### 4.1 Truth owned elsewhere
- Content owns publication visibility truth
- SEO owns slug-routing truth
- Media owns attachment/media truth
- Interaction owns interaction event/counter truth and aggregation policy

### 4.2 Derived outputs that Reading may own
If introduced, Reading may own derived outputs such as:
- read-facing summary fragments
- bounded trending inputs for reading use
- response-composition accelerators
- future projection-backed read enrichments

If Reading owns such outputs, they must remain:
- explicitly documented as derived
- rebuildable
- observable
- subordinate to truth

### 4.3 Ownership consequence
A Reading-owned derived fragment may improve:
- latency
- composition efficiency
- ranking/trending display
- partial response completeness

But it does not become:
- publication truth
- routing truth
- media truth
- interaction truth

**Rule:** Reading-owned derived output may shape response presentation, but must not override truth-sensitive allow/deny decisions.

---

## 5) Route and visibility ownership rule

### 5.1 SEO resolves, Reading validates, Content decides visibility
The ownership split is:

- **SEO** owns route mapping truth
- **Reading** owns response composition
- **Content** owns final public visibility truth

Therefore:
- a slug may resolve successfully in SEO
- Reading may receive a valid target identifier
- but Reading must still validate visibility against Content truth before serving public content

### 5.2 Route success is not serve authority
Reading must not treat:
- cache hit
- route resolve success
- projection presence
- summary presence

as proof that content is publicly readable.

**Rule:** Content visibility truth wins over all read-side convenience layers.

---

## 6) Batch / rebuild / reconciliation ownership rules

If Reading introduces batch workflows, they may:
- aggregate bounded interaction input for reading-facing summaries
- reconcile truth-visible content against reading-side derived outputs
- rebuild Reading-owned enrichments
- repair missing or stale reading-side derived state
- regenerate trending inputs or response-composition fragments

Such workflows must not:
- redefine content visibility truth
- modify another module’s truth because it is physically reachable
- bypass module ownership under the excuse of “repair”
- assume that active derived output may replace truth-sensitive checks
- publish older derived state over fresher truth-backed readable state

### 6.1 Recovery posture
If a Reading-derived output is stale, corrupted, or missing:
- truth-backed response composition remains the safe fallback
- rebuild/reconciliation restores quality and performance
- recovery output remains derived, not authoritative truth

---

## 7) Publication and cutover ownership

If Reading publishes a correctness-sensitive derived output, Reading owns:
- candidate generation
- candidate validation
- cutover/publication policy
- freshness signals
- rerun/rebuild policy

But Reading still does not own:
- underlying publication truth
- SEO truth
- media truth

### 7.1 Cutover safety rule
Reading-owned cutover must ensure:
- candidate output is bounded and validated
- stale candidates do not replace fresher truth-backed behavior
- active read-side derived output does not contradict Content visibility truth
- truth fallback remains available if a derived output is stale, missing, or cutover is delayed

---

## 8) Cache and enrichment ownership rule

### 8.1 Cache is operationally owned by Reading only when explicitly configured
Reading may own:
- cache usage policy
- cache-aside behavior
- safe omission policy for enrichments
- refresh-after-truth-read behavior

But Reading does not own truth merely because it can cache a response.

### 8.2 Enrichments remain subordinate
Counters, metadata fragments, recommendation/trending fragments, and future projection-backed enrichments remain:
- optional for correctness
- derived
- laggable
- omittable if freshness is uncertain

### 8.3 Safe degradation rule
Reading owns the policy for:
- omit enrichment
- use placeholder/default
- fallback to truth
- return core readable content without optional enrichments

It does not own the right to expose content that truth would deny.

---

## 9) Coordination / ownership-sensitive workflow rule

Reading normally prefers:
- idempotent execution
- bounded rerun
- truth fallback
- repair/reconciliation over exclusive control

If a future workflow truly requires exclusive ownership
(for example one-current-owner rebuild of a partition),
then the workflow must follow system-wide coordination rules:
- explicit ownership source
- generation/fencing token
- resource-side stale-owner rejection

Naive lock/leader assumptions are forbidden.

### 9.1 Ownership ambiguity rule
If ownership is ambiguous for a Reading-derived rebuild/publication workflow:
- delayed rebuild is acceptable
- stale-owner rejection is acceptable
- operator retry is acceptable
- continued truth-safe serving is acceptable

Unsafe dual publication or stale derived overwrite is not acceptable.

---

## 10) Module dependency posture summary

### 10.1 What Reading may expect from others
Reading may expect:
- SEO to provide route resolution truth
- Content to provide authoritative visibility truth
- Media to provide media truth
- Interaction to accept optional view signals asynchronously
- derived inputs to sometimes lag, requiring fallback behavior

### 10.2 What others may expect from Reading
Other modules may expect:
- Reading to enforce truth-safe visibility on the public path
- Reading not to block on optional side effects
- Reading to degrade safely when enrichments lag
- Reading-owned derived outputs, if introduced, to follow rebuild/reconciliation and cutover discipline

### 10.3 What nobody may assume
No module may assume:
- Reading can decide publication truth
- route resolution alone is enough to serve content
- cache/projection presence proves readability
- Reading rebuild output may silently replace truth-sensitive checks
- derived response quality is more important than truth-safe correctness

---

## 11) V2 evolution

Reading evolves into a Read Model consuming events and building projections to reduce cross-module reads.

If that happens, Reading must make explicit:
- which derived datasets are now first-class Reading-owned outputs
- how freshness is measured
- how publication/cutover works
- what truth fallback remains in place
- which reconciliation/rebuild workflows are now operationally critical

### 11.1 V2 constraint that remains unchanged
Even if Reading grows into a richer projection/read-model layer:

- Content still owns publication truth
- SEO still owns routing truth
- Media still owns media truth
- Reading-owned outputs remain derived unless explicitly reclassified by ADR
- truth-safe fallback remains the guardrail against stale read-model confidence