# Content — Dependencies & Ownership (V1)

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

Content is the single source of truth for:
- article lifecycle state
- publish/unpublish reasons and timestamps
- taxonomy and edit history
- public visibility truth
- per-article lifecycle version

Content does **not** own:
- routing truth outside Content-owned lifecycle/visibility truth
- notification delivery truth
- audit evidence truth
- interaction aggregate truth
- read-model / projection truth in other modules
- search/serving artifacts maintained by downstream modules

**Rule:** Content owns whether an article is public.  
It does not automatically own every derived representation of that article.

---

## 2) Allowed dependencies

- Authorization (policy enforcement in sync truth flows)
- Audit / SEO / Notifications: async consumers via events only
- Media: referenced by ID only (no workflow joining across truth boundaries)
- Reading: read-only access by policy in V1
- downstream rebuild/reconciliation workflows may consume Content truth as bounded input
- downstream projections may consume Content events using outbox + broker semantics
- bounded replay/rebuild workflows may use Content truth, Content revision/history, and Content-derived outbox history as authoritative inputs where policy allows

### 2.1 Allowed interaction shape
Approved dependency shapes are:

- **sync read / policy check** where explicitly allowed
- **truth commit + outbox**
- **async consumer reaction**
- **bounded rebuild/reconciliation input consumption**

Not approved:
- synchronous downstream completion as part of Content success
- hidden cross-module truth mutation inside Content handlers
- treating a downstream serving artifact as if it were Content truth

---

## 3) Forbidden dependencies

- Content must not synchronously call Notifications/Audit/SEO to complete publish/unpublish.
- Other modules must not write into Content-owned tables.
- Content must not mutate another module’s truth because it is physically reachable.
- Downstream repair/rebuild workflows must not redefine Content lifecycle truth.
- Partial downstream derived outputs must not be treated as Content-owned authoritative state.
- Content must not treat Redis/search/projection freshness as authority for lifecycle correctness.
- Content must not rely on RabbitMQ or any downstream broker state as the primary source of publication truth.
- Downstream modules must not infer “article is public” purely from stale events when Content truth disagrees.

---

## 4) Truth vs derived ownership

### 4.1 Truth owned by Content
- lifecycle state
- visibility legality
- lifecycle metadata
- revision/history
- ordered per-article version

### 4.2 Derived outputs that may depend on Content
Examples:
- SEO-serving route/materialization
- reading projections and summaries
- search/index artifacts
- notification candidate/reporting outputs
- governance/reporting summaries

These outputs may depend on Content truth, but remain:
- explicitly derived
- subordinate to Content truth
- rebuildable
- observable
- safe under rerun/replay

### 4.3 Rule: dependency on Content does not transfer ownership
A downstream module may derive value from Content truth, but that does not make the output Content-owned truth.

Examples:
- SEO may own derived serving artifacts, but not Content lifecycle truth
- Reading may own projections or summaries, but not Content visibility authority
- Notifications may own delivery truth, but not publication truth
- Audit may own append-only evidence truth, but not Content lifecycle truth

---

## 5) Event and stream ownership rules

### 5.1 Content owns the cause, not every downstream effect
Content owns:
- lifecycle change
- authoritative version change
- outbox emission of Content-derived events

Content does **not** own:
- downstream consumer apply state
- delivery status in Notifications
- audit storage state in Audit
- serving/index materialization state in SEO/Reading/Search-like consumers

### 5.2 Content-derived events are truth-following, not truth-replacing
Events such as:
- `ArticlePublished`
- `ArticleUnpublished`
- `ArticleArchived`
- `ArticleRestored`

exist to propagate committed truth.

They do not become a competing source of truth against Content tables.

### 5.3 Ordering ownership
Content owns the authoritative monotonic lifecycle version per article.

Downstream modules that consume Content-derived events must respect:
- `ArticleId`
- `Version`
- dedupe and stale-event rejection rules

But those downstream modules own their own:
- apply tracking
- dedupe stores
- projection publication/cutover
- rebuild/reconciliation workflows

---

## 6) Batch / replay / reconciliation ownership rules

Content may support downstream workflows by exposing authoritative bounded truth input.

Such workflows may:
- rebuild derived outputs from Content truth
- reconcile derived visibility/pathing against Content truth
- replay missed downstream effects using Content + Outbox as causal sources
- generate candidate repair sets based on Content truth/version scope

Such workflows must not:
- widen Content’s ownership into other modules
- treat derived repair output as Content truth
- overwrite newer Content truth using stale replay assumptions
- assume exclusive ownership without explicit coordination semantics
- publish a derived output that represents an older Content version over newer truth

### 6.1 Recovery posture
If downstream state is corrupted, stale, or missing:
- Content remains the authoritative source to rebuild from
- downstream modules remain responsible for their own derived output correctness
- batch/replay/reconciliation remains a derived-state recovery concern, not a Content truth mutation concern

---

## 7) Publication and cutover ownership

Content owns publication of **truth** lifecycle state.

Content does **not** own publication/cutover of:
- SEO-derived serving outputs
- reading projections
- search/index artifacts
- notification summaries
- audit reports

Those belong to their owning modules.

However, Content remains the authoritative truth source those modules must respect.

### 7.1 Rule: downstream cutover must not outrun Content truth
A downstream module may publish a derived output only if that publication:
- does not contradict current Content truth
- does not expose stale visibility assumptions
- is safe under replay/rerun/version drift

If derived publication is uncertain, truth-safe fallback beats cutover confidence.

---

## 8) Public Query rule (V1)

Reading module may perform read-only access by policy (explicitly allowed) until V2 projections exist.

Even in V1:
- Reading must not redefine Content visibility truth
- routing success does not override Content truth
- stale derived output must lose to Content truth
- projection freshness does not grant authority over lifecycle legality

### 8.1 Safe serving rule
If Reading or SEO derived state is:
- stale
- missing
- inconsistent
- lagging behind Content version

then public-serving logic must prefer:
- Content truth-backed visibility check
- safe degradation
- safe not-found

over exposing possibly invalid derived visibility.

---

## 9) Coordination / ownership-sensitive workflow rule

Content normally prefers:
- truth-store authority
- optimistic concurrency
- outbox durability
- idempotent consumer behavior
- bounded replay/reconciliation

If a future workflow truly requires exclusive ownership
(for example one-current-owner scheduled publication or partition rebuild),
then it must follow system-wide coordination rules:
- explicit ownership source
- generation/fencing token
- resource-side stale-owner rejection

Naive leader/lock assumptions are forbidden.

### 9.1 Ownership ambiguity rule
If ownership is ambiguous for a Content-dependent derived workflow:
- delay is acceptable
- replay later is acceptable
- rebuild later is acceptable
- unsafe dual apply is not acceptable

Safe non-progress beats stale or duplicate publication of derived state.

---

## 10) Module dependency posture summary

### 10.1 What Content may expect from others
Content may expect:
- Authorization to enforce policy synchronously where required
- downstream modules to consume Content events asynchronously and idempotently
- derived-state owners to rebuild/reconcile from Content truth when needed

### 10.2 What others may expect from Content
Other modules may expect:
- authoritative lifecycle/visibility truth
- stable per-article version for ordering-sensitive downstream logic
- bounded truth input for replay/rebuild/reconciliation
- outbox-emitted lifecycle events after truth commit

### 10.3 What nobody may assume
No module may assume:
- Content will synchronously finish downstream work
- Content events arrive exactly once
- Content events arrive globally ordered
- a derived serving artifact is safer than Content truth
- replay/rebuild output can silently replace truth-owned decisions

---

## 11) V2 evolution

Content may later interact with:
- richer read models
- scheduled publication flows
- stronger rebuild/checkpoint workflows
- more formalized content-reporting outputs
- dedicated search/index or projection components

If that happens, the architecture must keep explicit:
- what remains Content truth
- what is derived from Content truth
- how downstream publication/cutover works
- how replay/reconciliation preserves truth-first correctness
- which module owns active serving artifacts versus lifecycle authority

### 11.1 V2 constraint that remains unchanged
Even if CommercialNews evolves into richer projections or more explicit stream pipelines:

- Content remains owner of lifecycle and visibility truth
- derived systems remain subordinate
- rebuild/reconciliation remains a recovery mechanism, not a truth-transfer mechanism