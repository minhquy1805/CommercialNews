# Media — Dependencies & Ownership (V1)

Related:
- `../../../../architecture/arc42/03-building-blocks-modularity.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0018-transaction-boundaries-and-consistency-model-v1.md`
- `../../../../decisions/adr-0020-timeout-retry-and-failure-detection-policy-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 1) Ownership boundaries

Media owns:
- `MediaAsset` metadata
- `ArticleMedia` attachment truth
- attachment rules and ordering
- primary-media truth
- soft delete / restore truth
- cleanup/reconciliation/repair workflows for Media-owned truth and derived media artifacts

Media does **not** own:
- Content lifecycle truth
- Reading composition truth
- notification delivery truth
- audit evidence truth
- binary object-store/CDN behavior as business truth

**Rule:** Media owns **relationship truth** and media lifecycle truth inside its module boundary.  
Storage/CDN/derivative availability is operationally important, but it is not the authority for attachment, order, primary, or delete/restore truth.

---

## 2) Allowed dependencies

Media may depend on:

- Content:
  - `ArticleId` reference
  - optional read-only article validation by approved application contract
  - no Content truth mutation from Media
- Outbox:
  - required V1 causal bridge for Media integration events
  - Media writes Outbox records in the same local transaction as Media truth changes
- Audit:
  - required V1 async consumer for Media governance events
  - Audit owns audit evidence truth
- Object/file storage:
  - stores binary content
  - Media stores stable URL/path/reference metadata only
  - storage availability does not define attachment/order/primary truth
- Reading:
  - public composition owner
  - reads/composes Media truth where needed
  - must degrade gracefully when media delivery or derived state is missing
- SEO:
  - no required V1 dependency
  - may consume selected Media events in later phases if `og:image` or preview image derives from primary media
- Cleanup/reconciliation workflows:
  - may consume Media truth and bounded storage-reference checks where policy allows

### 2.1 Allowed dependency shapes
Approved interaction patterns are:
- sync truth mutation inside Media boundary
- Outbox write in the same local transaction as Media truth
- async event emission after truth commit
- async downstream cache/CDN/projection/derivative consumers
- bounded cleanup/reconciliation/reporting over Media-owned truth and derived artifacts
- read-only validation against Content where policy explicitly allows it

### 2.2 Dependency rule
No synchronous dependency on Outbox publication, CDN purge, derivative generation, cache invalidation, scan completion, or Audit ingestion is required for Media truth success.

Content validation, if used, must be read-only and must not widen the Media transaction boundary into Content truth.

Media must not mutate Content Article state when attaching, detaching, reordering, or setting primary media.

For commands that emit integration events, Media success requires Media truth commit and Outbox intent commit, not broker publication or consumer completion.

---

## 3) Forbidden dependencies

- Media must not join into Content workflows synchronously.
- Reading must not be blocked by media operations.
- No cross-module DB writes outside Media-owned schema.
- Media must not treat CDN/object storage/thumbnail outputs as hidden truth.
- Cleanup/reconciliation workflows must not redefine attachment/order/primary truth.
- Partial derived outputs must not be published as if they were authoritative active truth.
- Media must not mutate Content, Reading, Notifications, or Audit truth because the data is physically reachable.
- Derived repair output must not overwrite fresher Media truth.
- Media must not synchronously wait for Audit ingestion before returning success.
- Media must not synchronously wait for Outbox publication before returning success.
- Media must not synchronously wait for CDN/cache/variant/scan workflows before returning success.
- Media must not treat object storage timeout as proof that no object operation happened.

---

## 4) Truth vs derived ownership

### 4.1 Truth owned by Media
- media metadata
- attachment membership
- order truth
- primary truth
- deletion/restore state
- local revision/version markers where implemented

### 4.2 Derived outputs Media may own
Media may own derived outputs such as:
- thumbnails / transformed variants
- cached media lists
- attachment/order snapshots
- cleanup candidate sets
- orphan/mismatch reports
- repair candidate outputs

These must remain:
- explicitly documented as derived
- subordinate to Media truth
- rebuildable or reproducible where practical
- observable
- safe under rerun/replay

### 4.3 Ownership consequence
A derived thumbnail, transformed variant, cache entry, or mismatch report may help:
- rendering
- performance
- maintenance
- reporting
- recovery

It does **not** become:
- attachment-membership authority
- primary-media authority
- authoritative order truth
- delete/restore lifecycle authority

---

## 5) Async side-effect ownership rule

### 5.1 Media owns the cause
Media owns:
- metadata registration truth
- attachment truth
- order truth
- primary truth
- delete/restore truth

### 5.2 Downstream systems own their own derived state
Other systems may own:
- CDN/cache invalidation state
- derivative-generation operational state
- audit evidence truth
- read-model/projection truth if introduced later

### 5.3 Ownership consequence
A successful Media command means:
- Media truth committed
- Outbox event intent committed for governance/audit-producing commands

It does **not** mean:
- CDN is already updated
- thumbnails/variants are already generated
- Outbox message has already been published to RabbitMQ
- Audit has consumed the event
- audit evidence is already queryable
- read-facing derived outputs are already caught up

### 5.4 V1 async ownership

V1 async scope:

- Media emits integration events through Outbox.
- Worker publishes Media Outbox events to RabbitMQ.
- Audit consumes Media events for governance evidence.

Media owns:

- the cause event
- event payload correctness
- writing Outbox intent atomically with Media truth

Outbox/Worker owns:

- publication attempts
- retry/backoff/dead-state publication handling

Audit owns:

- consumer-side idempotency
- audit evidence storage
- audit ingestion retry/DLQ behavior

Rules:

- Outbox `Published` means broker handoff succeeded.
- It does not mean Audit has processed the event.
- Audit lag does not redefine Media truth.
- Consumer failure must not be reported as synchronous Media API failure after Media truth commit.

---

## 6) Batch / cleanup / reconciliation ownership rules

Some cleanup/reconciliation workflows may be operational policies rather than fully implemented V1 APIs.

Media batch-light workflows may:
- clean orphan or expired media artifacts
- reconcile truth vs storage references
- rebuild derived media outputs
- detect and report drift in attachment/order/primary state
- repair derived outputs from Media truth

Media batch-light workflows must not:
- redefine attachment membership truth
- redefine primary/order truth
- override fresher truth with stale repair output
- assume exclusive ownership without explicit coordination semantics
- publish stale repair candidates as active derived state

### 6.1 Recovery posture
If derived media outputs are unhealthy:
- Media truth remains authoritative
- replay/rebuild is a recovery mechanism
- Reading degrades gracefully where needed
- safe non-progress is preferable to stale truth corruption

---

## 7) Publication and cutover ownership

If Media publishes an important derived output, Media owns:
- candidate generation
- candidate validation
- publication/cutover policy
- freshness signals
- rerun/rebuild policy

But Media still does not own:
- Content lifecycle truth
- Reading response truth
- object storage/CDN as relationship truth

### 7.1 Publication consequence
For important derived outputs such as:
- derivative catalogs
- attachment snapshots
- repaired media lists
- mismatch/remediation views

Media must make explicit:
- when candidate output is considered complete
- when it becomes active
- how stale candidate output is rejected
- how truth fallback remains available if derived output is missing or behind

---

## 8) Evaluation ownership rule

### 8.1 Media owns final relationship evaluation
Media owns final truth for:
- whether an asset is attached
- which asset is primary
- what order attachments have
- whether an asset is deleted/restored and eligible for active use

### 8.2 Derived acceleration is subordinate
Caches, transformed variants, snapshots, summaries, and cleanup outputs may assist operations and rendering, but they must not become hidden authority for live relationship decisions.

### 8.3 Truth-sensitive uncertainty rule
If a derived view is:
- stale
- missing
- ambiguous
- inconsistent with current truth markers

then Media or its consumers must:
- read from authoritative Media truth
- or degrade safely without inventing or overriding relationship truth

---

## 9) Coordination / ownership-sensitive workflow rule

Media normally prefers:
- truth-store authority
- invariant enforcement
- deterministic final-state commands
- bounded cleanup/reconciliation
- rerun-safe derived workflows

If a future workflow truly requires exclusive ownership
(for example one-current-owner derivative rebuild or storage-repair worker),
then it must follow system-wide coordination rules:
- explicit ownership source
- generation/fencing token
- resource-side stale-owner rejection

Naive leader/lock assumptions are forbidden.

### 9.1 Ownership ambiguity rule
If ownership is ambiguous for a correctness-sensitive repair/materialization workflow:
- delayed rebuild is acceptable
- stale-owner rejection is acceptable
- operator retry is acceptable
- truth-first live media relationship evaluation is acceptable

Unsafe stale overwrite is not acceptable.

---

## 10) Module dependency posture summary

### 10.1 What Media may expect from others
Media may expect:
- Content to remain a separate lifecycle-truth owner
- Reading to compose from Media truth without turning derived artifacts into authority
- Audit to consume events asynchronously
- cleanup/reconciliation/reporting to be normal operational tools for derived artifacts

### 10.2 What others may expect from Media
Other modules may expect:
- authoritative attachment/order/primary truth via explicit contracts
- async event emission after truth commit where needed
- deterministic invariants under retry/concurrency
- graceful degradation when delivery layers fail
- no dependence on CDN/derivative completion for truth success

### 10.3 What nobody may assume
No module may assume:
- object storage presence is the same as attachment truth
- CDN/thumbnail presence is the same as primary/order truth
- a cleanup report is stronger than live Media truth
- one current worker/leader is safe without explicit authoritative coordination
- partial derived repair output is authoritative active state

---

## 11) V2 evolution

Media may later evolve toward:
- richer derivative processing
- more formalized storage hygiene workflows
- larger-scale derived media catalogs
- stronger repair and reconciliation pipelines

If that happens, the architecture must keep explicit:
- what remains live Media truth
- what is derived media output
- how publication/cutover works for important derived outputs
- how cleanup/reconciliation preserves truth-first behavior

### 11.1 V2 constraint that remains unchanged
Even if Media becomes richer:
- attachment/order/primary/delete truth still belongs to Media
- storage/CDN/derivative layers still remain subordinate
- derived media outputs still require explicit publication and rerun-safe recovery
