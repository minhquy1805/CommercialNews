# Reading — Idempotency & Consistency (V1)

This document defines Reading-specific consistency guarantees for the public read path and how it behaves under replication lag, stale caches, ambiguous dependency outcomes, delayed enrichments, stream-style derived-state lag, and bounded rebuild/reconciliation workflows.

System-wide rules live in:
- `../../../../architecture/arc42/11-replication-v1.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/14-distributed-systems-assumptions-v1.md`
- `../../../../architecture/arc42/15-consistency-ordering-and-consensus-v1.md`
- `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- ADR-0014 (Public ID / slug strategy)
- ADR-0015 (Redis cache policy)
- ADR-0018 (Transaction boundaries & consistency model)
- ADR-0019 (System model and fault assumptions)
- ADR-0020 (Timeout, retry, and failure detection policy)
- ADR-0021 (Clock, time, and ordering policy)
- ADR-0022 (Versioning and fencing strategy)
- ADR-0023 (Consistency, ordering, and consensus boundaries)
- ADR-0024 (Distributed coordination and singleton work policy)
- ADR-0025 (Batch processing and derived state policy)
- ADR-0026 (Batch job orchestration and materialization policy)
- ADR-0027 (Stream processing and derived state policy)
- ADR-0028 (Consumer idempotency, replay, and rebuild policy)

---

## 0) Truth vs derived (read facade)

### 0.1 Reading is a truth-composing facade
Reading does not own publication truth, slug-routing truth, or media truth.

Reading is a facade over truth owned by other modules:

- **Content truth**: publication state and visibility rules (`Draft / Published / Unpublished / Archived`)
- **SEO truth**: slug routing truth `(Scope, Slug) -> ArticleId/PublicId`
- **Media truth**: attachments + primary media metadata

Reading is therefore correctness-sensitive even though it is read-only in business terms.

Its job is not merely “fetch fast.”  
Its job is:
- compose truth-backed public responses
- tolerate lag in derived inputs
- never leak non-public content
- degrade safely when dependencies are stale or ambiguous

### 0.2 Derived inputs (allowed to lag)
Reading may include derived data such as:
- SEO metadata projections
- interaction counters (`views / likes / comments`)
- Redis/CDN/response caches if enabled
- future read-model enrichments
- batch-generated summaries or trending inputs

Derived inputs may lag truth.

**Rule:** Reading correctness must not depend on derived freshness.

This means:
- enrichments may be stale or missing
- visibility correctness may not be stale or missing
- fallback to truth is preferred over stale confidence

### 0.3 Knowledge limits in the read path
Reading does not directly “know” truth from one place only.
It composes from multiple sources and therefore must assume:
- caches may be stale
- SEO routing may resolve a slug whose visibility has changed
- enrichments may arrive later than truth transitions
- batch-derived summaries may lag async signals
- dependency timeouts do not prove a target is absent

Because of that, Reading must:
- validate visibility from truth
- distinguish truth-backed absence from derived uncertainty
- prefer safe, explicit fallback behavior over optimistic assumptions

### 0.4 Consistency class for Reading
Reading intentionally uses multiple consistency classes:

#### Strong truth-backed consistency
Required for:
- public visibility correctness
- final allow/deny decision on public article exposure
- safe handling of slug resolution followed by visibility validation
- any truth-sensitive read that could otherwise leak non-public content

#### Ordered / causality-sensitive consistency
Required for:
- slug resolve -> visibility check -> response composition
- cause-before-effect behavior where a derived signal must not outrun truth
- version-aware handling of derived enrichments where freshness markers exist
- avoiding stale derived responses after a newer truth transition such as unpublish

#### Eventual consistency
Accepted for:
- counters
- non-critical SEO metadata enrichments
- cached fragments
- recommendations/related content
- batch-generated summaries
- response-shaping enrichments that do not define visibility truth

---

## 1) Idempotency

### 1.1 Public reads are naturally idempotent
List/detail/read endpoints are naturally idempotent:
- repeated identical reads must not mutate business truth
- repeated identical reads should return the same visibility result given the same truth state

### 1.2 Read-side signals must be retry-safe
If Reading emits view or telemetry signals, they must be retry-safe:

- the read response must not depend on view tracking success
- a duplicate signal must not break correctness
- Interaction handles dedupe/idempotency by policy
- telemetry write failure must not change the public visibility result

### 1.3 Read success must not depend on side-effect completion
A public read remains successful even if:
- view tracking enqueue fails
- broker is slow
- aggregation is behind
- telemetry persistence is delayed
- a later batch summary build has not caught up

These side effects are optional with respect to read correctness.

### 1.4 Idempotency is preferred over singleton assumptions
Reading correctness must not depend on:
- only one renderer or composer instance being active
- one cache warmer being “the current owner”
- startup order or local process leadership
- one worker “surely” handling a read-side signal
- one rebuild worker being assumed exclusive without explicit protection

Reading should instead rely on:
- truth-backed composition
- safe cache-aside behavior
- retry-safe optional side signals
- deterministic visibility enforcement
- rerun-safe rebuild/reconciliation policy

### 1.5 Derived read outputs must also be safe under replay
If Reading consumes or maintains derived outputs such as:
- counters
- summary fragments
- trending inputs
- future projection-backed read fragments

then duplicate input, replay, rerun, and late-arriving derived updates must be tolerated safely.

**Rule:** read-side convenience must remain idempotent or safely replaceable under replay.

---

## 2) Consistency expectations (non-negotiable)

### 2.1 Publication-state correctness must be strict
Reading must never expose drafts or unpublished content to public callers.

Policy:
- every public detail/list query MUST enforce Content visibility from truth
- cached, projected, or precomputed data must never bypass truth visibility rules
- if any dependency is stale or ambiguous, prefer safe denial over incorrect exposure

### 2.2 Consistent-prefix / cause-before-effect rule
Reading must prevent “effect before cause” anomalies.

Examples:
- a slug may resolve while the corresponding article is not yet publicly visible
- a cache entry may still exist after unpublish
- a derived enrichment may imply content exists even though truth now denies it
- a batch-generated summary may still mention content that truth no longer exposes

Therefore, after resolving `slug -> articleId`, Reading MUST confirm visibility from Content truth before returning content.

### 2.3 Eventual consistency is allowed only for enrichments
The following may be eventually consistent:
- counters
- non-critical SEO metadata enrichments
- derived read-model fields
- cache freshness
- recommendation/related-item enrichments if introduced
- batch-generated reading summaries or ranking inputs

Reading must degrade gracefully:
- missing counters -> display `0`, `unknown`, or omit section (policy-defined)
- missing SEO metadata -> fallback to safe defaults
- missing non-critical enrichments -> omit rather than fail the whole read

### 2.4 Timeout ambiguity does not prove non-existence
If a dependency times out during read composition, Reading must not conclude:
- “the article definitely does not exist”
- “the slug definitely does not map”
- “the media definitely is absent”
- “the summary definitely is empty”

Instead, Reading should apply the configured fallback path:
- retry inside bounded local policy if appropriate
- fall back from cache to truth
- or return a safe result consistent with truth-backed certainty

**Rule:** truth-backed absence and ambiguous dependency failure are not the same thing.

### 2.5 No global ordering assumption
Reading does **not** assume:
- one total global order across all read-enrichment inputs
- one globally linearizable read-model view across all modules
- one cluster-wide sequencing truth for all public reads

**Rule:** Reading ordering is scoped to the minimal causal chain required for safe composition.

### 2.6 Route success is not visibility success
A successful slug resolution means:
- SEO found a target

It does **not** mean:
- the target is public
- the target is safe to serve
- a stale route is acceptable to trust

**Rule:** visibility must be confirmed after routing and before response publication.

---

## 3) Transaction boundary (V1)

### 3.1 Reading does not own business truth writes
Reading is a read facade and does not define the primary transactional boundary for:
- article lifecycle truth
- slug uniqueness/routing truth
- media attachment truth
- interaction aggregate truth
- identity or authorization truth

Its primary responsibility is:
- correct composition
- truth-first visibility enforcement
- safe fallback across module-owned truth sources

### 3.2 Read-path composition is not a distributed transaction
A public read may compose data from:
- Content
- SEO
- Media
- optional derived sources such as counters/cache
- batch-generated summaries if introduced

Reading does not attempt a distributed atomic transaction across these sources.

Instead, it preserves correctness with:
- truth-first visibility checks
- refusal to trust stale derived data for visibility/security decisions
- safe fallback when one source is stale or unavailable

### 3.3 Allowed write-side effects in Reading flows
Reading may emit lightweight side signals, such as:
- view tracking signal/enqueue
- access telemetry
- cache-aside fill after a safe truth-backed read

These must not be required for read success.

### 3.4 Outside the critical read path
The following MUST NOT be required for a successful public read:
- interaction tracking completion
- counter aggregation completion
- cache invalidation completion
- SEO consumer catch-up
- broker publish completion
- downstream projection refresh
- batch summary rebuild completion

Public reads must remain available even when these subsystems lag or fail.

### 3.5 Transaction duration rule
If Reading performs any local write-side signal inside the request path, it must remain short and bounded:
- no waiting for downstream systems
- no open transaction across external calls
- no coupling between read latency and async side-effect completion
- no retry loops that stretch the read path indefinitely

### 3.6 Visibility-before-enrichment rule
Reading must validate visibility from truth before treating derived enrichments as usable.

Examples:
- slug resolves -> still verify `Published` truth before returning detail
- counters present -> may be shown if available, but must not override visibility rules
- missing media/SEO enrichment -> degrade gracefully, not leak or fail incorrectly
- batch summary present -> may shape response, but must not outrun truth visibility

### 3.7 Cache is acceleration only
Any cache used by Reading is an optimization layer only.

A cache hit must never be trusted blindly for:
- publication visibility
- authorization-sensitive admin reads
- security-sensitive self-state
- truth-sensitive route resolution without visibility confirmation

Where correctness is at risk, Reading must fall back to truth.

### 3.8 No heterogeneous distributed transaction
Reading does **not** attempt one atomic workflow across:
- Content truth
- SEO truth
- Media truth
- Redis/cache
- interaction counters
- telemetry/view tracking systems
- batch-generated summaries

Correctness is achieved through:
- truth-backed composition
- causal validation order
- safe fallback
- optional side effects outside the correctness boundary

---

## 4) Fallback behavior under lag or ambiguity (required)

### 4.1 Slug resolution fallback
Routing can be cache-first, but must be correct:

1. Resolve `(scope, slug)` via Redis cache if enabled  
2. On miss or suspicion of staleness, fall back to SEO truth store  
3. After obtaining target id, validate Content visibility from truth  
4. Only then compose the public response

### 4.2 Missing derived state fallback
If projections/caches/summaries are stale or missing:
- fall back to truth stores (`Content / SEO / Media`) for correctness
- never return misleading 404/empty when truth indicates `Published` and readable
- never return content when truth indicates non-public

### 4.3 Safe 404 vs misleading 404
Reading distinguishes between:

#### Safe 404
Used when authoritative truth indicates:
- slug does not map to a visible target
- article is not public
- resource truly should not be exposed

#### Misleading 404
Would happen if Reading trusted:
- a stale cache miss
- a lagging projection
- an unavailable enrichment source
- a stale batch-generated summary
without checking truth

**Rule:** misleading 404 must be avoided where truth fallback can determine a correct public result.

### 4.4 Non-critical subsystems must not block reads
- interaction tracking failures must not degrade reading correctness
- counter aggregation backlog must not affect article detail response
- missing derived metadata must not force full read failure if truth-backed rendering is still possible
- stale batch summaries must not block truth-safe response composition

### 4.5 Safe omission beats stale invention
If a non-critical enrichment is unavailable or suspiciously stale:
- omit it
- fallback to a safe default
- or render the core truth-backed article response without it

Do not invent or over-trust derived values to make the response “look complete.”

### 4.6 Truth fallback is valid degraded behavior
A truth-backed slower path is still a correct path.

Reading should treat:
- cache miss + truth success
- stale projection + truth success
- missing summary + truth-backed core response

as degraded-but-correct behavior, not as correctness failure.

---

## 5) Cache consistency (policy hook)

### 5.1 Cache posture
Reading caches must follow system cache policy:
- cache-aside as default
- event-driven invalidation where feasible + TTL fallback
- no blind trust of cached visibility-sensitive data

### 5.2 Suggested cache groups (if introduced)
Possible cache groups:
- list endpoints (short TTL)
- detail response fragments
- SEO routing cache (owned by SEO, hot-path relevant)
- non-critical enrichment fragments
- bounded summary fragments if they remain clearly derived

### 5.3 Visibility-sensitive cache rule
Article detail/list caches must respect visibility changes immediately at the truth boundary.

Therefore:
- cache lag must never override truth-backed visibility checks
- unpublish must win over stale cache presence
- public correctness beats cache hit ratio

### 5.4 Canary/rollout guardrail
Caches must not hide regressions during rollouts.

During canary or staged releases:
- monitor fallback rates
- monitor truth-read amplification
- monitor error rates and latency
- ensure stale caches are not masking broken routing or composition behavior

### 5.5 Cache refresh safety
Cache refresh or cache-aside refill must be safe under:
- repeated requests
- replayed refresh signals
- late-arriving derived updates
- overlapping rebuild/reconciliation activity

A stale cache write must not regain authority over fresher truth-backed composition.

---

## 6) Freshness and ordering of derived enrichments

### 6.1 Derived enrichment freshness is secondary to visibility truth
Reading may consume enrichments that are behind current truth.

That is acceptable if:
- visibility is still validated from truth
- enrichment omission is safe
- stale enrichment does not create incorrect exposure

### 6.2 Timestamps are informational, not freshness authority
If enrichments carry `UpdatedAt` or `OccurredAt`, those timestamps are useful for:
- debugging
- approximate chronology
- telemetry

They are not sufficient authority for:
- deciding public visibility
- resolving truth conflicts
- overriding authoritative module truth

### 6.3 Version-aware enrichments are preferred when available
If a projection or enrichment source carries:
- aggregate version
- last applied version
- revision marker

Reading should prefer those signals over timestamps when deciding whether derived data is stale enough to ignore or refresh.

### 6.4 Causal ordering beats global recency
Reading does not need one globally newest view of all modules at once.
It does need:
- visibility checked after route resolution
- enrichments not trusted ahead of authoritative truth
- stale derived fragments not allowed to outrun truth transitions

This is a causal-composition rule, not a global-order rule.

### 6.5 Batch-generated enrichments follow the same rule
If Reading consumes outputs from bounded aggregation/rebuild workflows, those outputs remain:
- derived
- lag-tolerant
- subordinate to truth
- safe to omit if freshness is uncertain

### 6.6 Late-arriving enrichments must not outrun newer truth
If a counter, summary, projection fragment, or rebuild candidate arrives later:
- it must not make content appear readable if truth would deny it
- it must not overwrite fresher already-known derived state blindly
- it should be ignored, replaced, or rebuilt according to module policy

---

## 7) Rebuild / replay / reconciliation posture (Reading)

### 7.1 Rebuildable reading-derived outputs
Reading-side derived outputs such as:
- counters
- trending inputs
- summary enrichments
- future projection-backed read fragments

should be treated as rebuildable by policy.

### 7.2 Reconciliation over blind trust
If truth and derived reading outputs diverge, Reading prefers:
- truth-backed fallback for immediate correctness
- bounded reconciliation workflows for later repair
- candidate-before-publication for repaired or rebuilt derived outputs where needed

### 7.3 Rerun safety
Important rebuild/reconciliation workflows affecting reading-derived outputs must be safe to rerun on the same bounded input.

### 7.4 Partial rebuild output is not active truth
Partially built summaries, counters, or projections must not be treated as complete active outputs if their workflow has not successfully completed publication/cutover.

### 7.5 Full rebuild is acceptable when safer than partial repair
If a derived reading output is cheap enough to regenerate from bounded truth/log inputs:
- full rebuild is preferred over fragile partial patching
- bounded recompute is preferred over hidden exactly-once assumptions
- explicit repair is preferred over silent divergence

### 7.6 Recovery outputs remain derived
Mismatch reports, repaired summary candidates, rebuilt projection fragments, and regenerated trending inputs remain derived outputs.

They improve quality and performance.
They do not become publication truth.

---

## 8) Coordination and ownership posture (Reading)

### 8.1 Reading does not require global singleton coordination by default
Ordinary Reading correctness must not depend on:
- one global read-model leader
- one process being “the only composer”
- startup order deciding which renderer is current
- timeout-only assumptions about ownership

Reading correctness should instead be achieved through:
- truth-backed composition
- safe cache fallbacks
- deterministic visibility enforcement
- retry-safe optional side signals

### 8.2 If future ownership-sensitive workflows are introduced
If a future Reading workflow truly requires one current owner
(for example exclusive rebuild of a read-model partition or one-current cache repair owner),
that workflow must define:
- ownership source of truth
- monotonic generation/fencing token
- resource-side rejection of stale owner actions

Naive leader/lock patterns are not acceptable.

### 8.3 Safe non-progress beats unsafe double-apply
If ownership is ambiguous for a correctness-sensitive rebuild or publication workflow, Reading must prefer:
- delayed rebuild
- operator retry
- stale-owner rejection
- continued truth-safe fallback

over unsafe dual publication or unsafe repair mutation.

---

## 9) Observability signals (Reading-specific)

### 9.1 Minimum signals
Minimum signals include:
- P95/P99 latency and error rate for list/detail
- SEO slug resolve latency + error rate
- SEO DB fallback rate (%)
- truth fallback rate for stale/missing derived inputs
- “visibility denied after slug resolve” rate for debugging (must not leak to clients)
- interaction enqueue/backlog signals (non-blocking)
- counter freshness lag indicators (if available)
- misleading-404 prevention signals where measurable
- omitted-enrichment rate due to stale/missing derived inputs
- rebuild/reconciliation mismatch count for reading-derived outputs
- active-summary freshness age where summaries exist
- candidate publication/cutover failures for important derived outputs
- ownership-generation mismatch count if future ownership-sensitive workflows are introduced

### 9.2 Health layering for Reading
Reading observability should distinguish:
- API process health
- dependency health (SEO truth, Content truth, Redis)
- business-flow health (public list/detail correctness and latency)
- derived-path lag (counters/projections/cache fallback)
- rebuild/reconciliation health for reading-derived outputs

### 9.3 Logging requirements
Logs should include:
- `correlationId / traceId`
- scope + slug (when safe)
- resolved target id
- visibility outcome (`published` vs denied)
- fallback path taken (`cache hit`, `DB fallback`, `truth fallback`)
- whether enrichments were omitted due to staleness or failure
- rebuild/reconciliation workflow identifiers where applicable

Logs must help operators answer:
- was this truly not found?
- was it denied by truth?
- did we fall back correctly?
- did a derived store lag or fail?
- is the issue in request composition or in a delayed rebuild/reconciliation path?

---

## 10) Summary

Reading correctness in V1 rests on fourteen rules:

1. Reading is a facade over truth owned elsewhere; it must compose, not invent truth.  
2. Visibility correctness comes from Content truth and is never delegated to stale derived data.  
3. Slug resolution is not enough; visibility must still be confirmed before returning content.  
4. Missing or stale enrichments are acceptable; incorrect exposure is not.  
5. Timeout or cache miss does not automatically prove absence; fallback to truth is the safe path.  
6. Read latency may degrade under lag, but public correctness must not.  
7. No global ordering or distributed transaction is assumed for Reading composition.  
8. Causal validation order matters more than globally freshest derived data.  
9. Reading-side signals are optional and retry-safe.  
10. Reading-derived outputs are rebuildable and subordinate to truth.  
11. Reconciliation and rebuild support reading quality, but do not redefine publication truth.  
12. Replay, rerun, and late-arriving derived updates are normal and must remain safe.  
13. Truth-backed degraded behavior is acceptable; stale derived confidence is not.  
14. Singleton/ownership semantics are not relied on unless explicitly protected by authoritative generation/fencing rules.