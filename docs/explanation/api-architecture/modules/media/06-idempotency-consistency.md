# Media — Idempotency & Consistency (V1)

This document defines Media-specific idempotency, invariants, stale-write protection, ordering-sensitive state transitions, replication-lag posture, and cleanup/reconciliation rules for derived media outputs.

System-wide rules live in:
- `../../../../architecture/arc42/11-replication-v1.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/14-distributed-systems-assumptions-v1.md`
- `../../../../architecture/arc42/15-consistency-ordering-and-consensus-v1.md`
- `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- ADR-0013 (Outbox & delivery semantics)
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

## 0) Role of Media in the system

Media owns:
- media asset metadata
- article-media attachment truth
- attachment order
- primary-media selection truth

Media does **not** own:
- Content lifecycle truth
- public visibility truth
- CDN/object storage cache correctness as business truth
- Reading page composition truth

Therefore:
- Media truth defines which asset is attached, primary, deleted, and in what order
- CDN/object storage serve binary content, but do not define authoritative relationship truth
- Reading must degrade gracefully if media delivery/enrichment lags or assets are missing

---

## 1) Truth vs derived

### 1.1 Truth (Media store)
Media truth includes:
- media asset metadata (URL/reference, type, status, soft-delete flags)
- attachment relations (`ArticleId ↔ MediaId`) with ordering and primary marker
- invariants:
  - no duplicate attachment where policy forbids it
  - at most one primary media per article (or per media type/scope if policy requires)

Binary blobs in object storage/CDN are not the OLTP truth for attachment relationships.
Media truth stores the authoritative metadata and links.

### 1.2 Derived (allowed to lag)
Derived/cached layers may include:
- CDN caching of media URLs/content
- response caches for media lists
- thumbnail/transformation outputs
- projections/read models for read optimization
- cleanup candidate sets
- mismatch/repair reports
- derived media summaries or snapshots

These may lag or fail independently.

**Rule:** Media correctness invariants are enforced by Media truth, not by CDN/cache freshness.

### 1.3 Missing derived assets must not corrupt business truth
If an image or derived media variant is missing or stale:
- that is a degraded delivery problem
- not a reason to corrupt or reinterpret attachment truth

Reading/UI should degrade gracefully:
- placeholder
- omit broken enrichment
- keep article response functional if policy allows

### 1.4 Derived delivery and processing remain subordinate
Derived outputs may be:
- replayed
- rebuilt
- replaced
- delayed
- partially unavailable

They must not become authority for:
- attachment membership
- primary-media selection
- attachment order
- deletion/restore truth

### 1.5 Consistency class for Media
Media intentionally uses multiple consistency classes:

#### Strong truth-backed consistency
Required for:
- attachment membership truth
- current order truth
- primary-media truth
- soft-delete / restore truth
- media metadata state that affects authoritative relationship behavior

#### Ordered / causality-sensitive consistency
Required for:
- reorder operations per article
- primary selection changes
- delete/restore effects when they interact with current attachment/primary state
- downstream order-sensitive projections
- media-processing state transitions when later state depends on earlier committed state
- candidate publication where stale derived output must not overwrite fresher derived state

#### Eventual consistency
Accepted for:
- CDN invalidation/propagation
- thumbnail/transformation availability
- cached media lists
- non-authoritative derived media materializations
- cleanup/reconciliation convergence

---

## 2) Idempotent operations and business guards

### 2.1 Attach
Repeated attach should return success without duplication.

### 2.2 Set primary
Setting the same primary again should converge safely when the request is based on current version.
If the request carries a stale `expectedVersion`, it should fail deterministically with `409 Conflict`.

### 2.3 Reorder
Submitting the same order list again should either:
- converge safely when protected by idempotency/replay policy, or
- fail deterministically with `409 Conflict` when `expectedVersion` is stale.

It must never partially apply or corrupt order truth.

### 2.4 Soft delete / restore
- repeated delete returns success
- repeated restore returns success within the allowed policy window

### 2.5 Register media
If the same semantic register command is retried after timeout ambiguity:
- it should converge deterministically by policy
- it should not create duplicate harmful metadata truth
- it should not emit duplicate meaningful side effects for unchanged state

### 2.6 Implementation posture
Recommended implementation posture:
- use DB uniqueness constraints to prevent duplicate attachments where appropriate
  - for example `(ArticleId, MediaId)` unique
- treat “already in desired state” as idempotent success or documented conflict if that is the module-wide API style

**Rule:** logically unchanged commands must not emit duplicate downstream side effects.

### 2.7 Idempotency is preferred over singleton assumptions
Media correctness must not depend on:
- only one admin submitting reorder
- only one worker updating a derived media view
- startup order
- local ownership belief
- only one reconciliation/repair workflow being assumed exclusive without explicit protection

Media should instead rely on:
- truth-store authority
- deterministic final-state commands
- uniqueness/invariant constraints
- stale-write rejection where needed
- replay-safe downstream processing

### 2.8 Business-level idempotency table

| Operation | Message-level guard | Business-level guard | Expected behavior |
|---|---|---|---|
| Register media | Outbox `MessageId` for emitted event | `Idempotency-Key` where provided | Same semantic retry converges safely |
| Update metadata | Outbox `MessageId` | Same final state is no-op/equivalent success | No duplicate meaningful side effect |
| Attach media | Outbox `MessageId` | Unique active `(ArticleId, MediaId)` + optional `Idempotency-Key` | Repeated attach does not duplicate membership |
| Detach media | Outbox `MessageId` | Current attachment truth | Repeated detach converges safely |
| Reorder | Outbox `MessageId` | `expectedVersion` + final-state set validation | Stale reorder returns `409` |
| Set primary | Outbox `MessageId` | `expectedVersion` + one-primary invariant | Stale primary update returns `409` |
| Soft delete | Outbox `MessageId` | Current asset state | Repeated delete converges safely |
| Restore | Outbox `MessageId` | Current asset state + retention policy | Repeated restore converges where policy allows |

### 2.9 Idempotency-Key policy

`Idempotency-Key` is used to protect same-intent retries after client timeout or network ambiguity.

Required / strongly recommended usage:

- `POST /items`
- `POST /articles/{articleId}/attachments`

V1 posture:

- `Idempotency-Key` is strongly recommended and may become required for production clients.
- The key must be scoped by actor/client, endpoint, and semantic request.
- Reusing the same key with the same semantic request should return the original/equivalent result.
- Reusing the same key with a different semantic request must return `409 Conflict`.
- Malformed keys must return `400 Bad Request`.
- Idempotency keys must not bypass authorization.
- Idempotency keys must not be treated as global secrets.

Same semantic request includes:

- same actor/client scope
- same endpoint/operation
- same target resource where applicable
- same important payload fields

For `POST /items`, semantic request should include:

- `url` or canonical storage reference
- `type`
- safe metadata summary where policy requires it

For `POST /articles/{articleId}/attachments`, semantic request should include:

- `articleId`
- `mediaId`
- `isPrimary`

---

## 3) Consistency expectations

### 3.1 Strong consistency in Media truth
Attachment updates must be strongly consistent in Media truth:
- attach/detach/primary/reorder commit atomically
- admin reads after write reflect the new truth immediately (primary read or write-return)

### 3.2 Eventual effects through caches/object delivery (allowed)
Reading may observe eventual effects through:
- CDN caching
- response caching
- transformed asset propagation
- future projections

Policy:
- UI must tolerate eventual cache propagation
- correctness invariants (single primary, attachment membership, order truth) must be enforced by truth, not caches

### 3.3 Timeout ambiguity posture
If a client times out during:
- attach
- detach
- set primary
- reorder
- soft delete / restore
- register media
- update media metadata

that timeout does **not** prove the Media truth change failed.

Safe reconciliation must inspect Media truth:
- current attachment set
- current primary
- current order
- current deletion state
- current metadata registration truth

### 3.4 No global ordering assumption
Media does **not** assume:
- one total global order across all media mutations
- one cluster-wide sequence for all attachments and assets

**Rule:** ordering is scoped per article, per media aggregate, or per order-sensitive relation boundary as needed.

### 3.5 Cause-before-effect rule
A later externally meaningful media-derived state must not outrun required committed causes.

Examples:
- a derivative must not be treated as authoritative before base media truth exists
- a stale worker must not publish older derivative state over fresher truth-backed metadata
- a delayed cache entry must not override newer delete/restore or primary/order truth

---

## 4) Transaction boundary (V1)

### 4.1 Truth boundary
The Media transaction boundary stops at the Media-owned truth change.

Typical truth changes include:
- register/update media asset metadata
- attach or detach media from an article
- reorder media attachments for an article
- set or unset primary media
- soft delete / restore media assets
- update local relation metadata required for stable rendering/order semantics
- write the Outbox record when downstream side effects are required

### 4.2 Atomic commit set
For Media commands, the module should commit atomically:
- the Media truth change
- attachment relation updates
- primary/order state changes required by the command
- register: media metadata insert + Outbox
- update metadata: metadata update + version increment + Outbox
- revision/version marker where used for stale-write protection
- the Outbox record for downstream cache/projection/audit side effects

Typical examples:
- attach: relation insert + Outbox
- detach: relation removal/deactivation + Outbox
- set primary: unset old primary + set new primary + Outbox
- reorder: apply the submitted order set + Outbox
- soft delete: media status update + any primary/attachment policy change required + Outbox

### 4.3 Outside the transaction
The following MUST NOT be required inside the Media truth transaction:
- CDN purge/invalidation completion
- broker publish
- response cache refresh
- projection rebuild
- external object storage/CDN side effects as a success condition
- Content truth mutation
- cleanup/reconciliation workflow completion

These are post-commit async effects or belong to other module boundaries.

### 4.4 Transaction duration rule
Media transactions must be short:
- no waiting for CDN/cache propagation
- no waiting for downstream consumers
- no interactive multi-step transaction across requests
- no retry loops over external dependencies inside the transaction

### 4.5 Shared DB does not widen Media ownership
Even in a shared DB deployment, Media must not use the same transaction to directly mutate:
- Content lifecycle truth
- Reading projections
- Notification state
- Audit downstream state

Media may write:
- Media-owned truth tables
- Media attachment relations
- approved local replication artifacts such as Outbox

### 4.6 No heterogeneous distributed transaction
Media does **not** attempt one atomic workflow across:
- Media truth DB
- object storage
- CDN
- RabbitMQ
- derived projections
- other module-owned truth stores

Atomicity stops at:
- Media truth mutation
- required attachment/order/primary state change
- local revision/version marker where used
- Outbox intent

---

## 5) Concurrency and stale-write protection

### 5.1 Concurrency expectations
Media write flows must assume:
- concurrent attach/detach attempts
- concurrent set-primary requests
- repeated reorder submissions after timeout
- stale admin forms
- duplicate event delivery to downstream consumers
- later-running repair/reconciliation workflows over already-changed truth
- stale derivative workers resuming after pause/restart

### 5.2 Required protections
At minimum, the design must prevent:
- duplicate `(ArticleId, MediaId)` attachments
- two active primaries in the same primary scope
- partial reorder application
- stale admin writes silently overriding a newer attachment/primary/order decision

Typical implementation options include:
- DB uniqueness constraints for attachment identity
- filtered unique constraints for primary invariants
- optimistic concurrency for reorder and set-primary workflows
- deterministic “set desired final state” semantics instead of procedural incremental updates

### 5.3 Version/revision over timestamp
Where stale admin writes are plausible, freshness should be protected by:
- version
- revision
- rowversion
- compare-and-set semantics

Media must not use:
- `UpdatedAt`
- “largest timestamp wins”

as the sole authority for correctness-sensitive final-state mutation.

### 5.4 Resource-side protection
The Media truth boundary must verify authoritative current state itself.

A caller saying:
- “this is still the latest order”
- “this is still the current primary”

is not sufficient.

The store or write boundary must verify:
- expected version/revision for operations requiring it
- current primary scope
- current attachment membership
- final set validity before commit

### 5.5 State legality and freshness are complementary
Version/revision checks alone are not enough.

Media must also enforce:
- primary-scope legality
- delete/restore legality
- attachment membership legality
- reorder completeness/validity according to policy

**Rule:** versioning prevents stale overwrite; invariant/state rules prevent illegal final state.

### 5.6 Stale derived state must not weaken fresher truth
If caches, projections, derivative metadata, or delayed workers still reflect older media assumptions:
- they must not reintroduce deleted media as active
- they must not restore an older primary selection over current truth
- they must not publish older ordering over fresher truth-backed order
- they must be rejected, ignored, or rebuilt from authoritative Media truth

### 5.7 ExpectedVersion policy

`expectedVersion` is required for:

- `POST /articles/{articleId}/attachments:reorder`
- `POST /articles/{articleId}/attachments:set-primary`

Rules:

- missing `expectedVersion` returns `400 Bad Request`
- version mismatch returns `409 Conflict`
- `UpdatedAt` must not be used as the concurrency authority
- version check must be performed against current Media attachment-set truth
- passing version check does not bypass invariant validation

Attach and detach do not require `expectedVersion` in V1:

- attach is protected by active `(ArticleId, MediaId)` uniqueness and optional `Idempotency-Key`
- detach converges safely from current attachment truth

---

## 6) Reorder semantics rule

### 6.1 Reorder is a set operation
Reorder should be treated as a set operation on the target article scope:
- validate that all submitted media belong to the article
- validate completeness/ownership per policy
- then apply the final order atomically

### 6.2 Do not model reorder as loose partial updates
Do not treat reorder as a sequence of independent row updates that may partially succeed and leave inconsistent state.

### 6.3 Retry-safe reorder
Retrying the same reorder command should converge to the same final order.

### 6.4 Projection ordering
If projections/events later materialize order-sensitive views:
- include `(ArticleId, Version)` or equivalent ordering metadata
- reject stale reorder updates in projections where strict final-state preservation matters

### 6.5 No cross-article ordering guarantee
Reorder guarantees apply per article scope.
They do **not** imply one global order across all media operations in the system.

---

## 7) Replication mechanics (events and side effects)

Media changes trigger Audit ingestion in V1.

Media changes may trigger additional downstream effects in later phases:
- cache invalidation / CDN purge
- search/index projection updates
- read-model refreshes
- derivative processing workflows

If downstream effects are required:
- Media writes an Outbox record atomically with the truth change
- Worker publishes and consumers execute side effects

Typical events:
- `media.asset_registered`
- `media.asset_updated`
- `media.asset_soft_deleted`
- `media.asset_restored`
- `media.article_media_attached`
- `media.article_media_detached`
- `media.article_media_reordered`
- `media.article_primary_media_set`

Consumers must be idempotent (`MessageId` dedupe).

### 7.1 Duplicate delivery vs stale delivery
Media consumers must distinguish:
- **duplicate delivery**: same message again
- **stale delivery**: older event arrives after newer Media truth already exists

Protection:
- dedupe by `MessageId`
- version-aware stale rejection where order-sensitive projections exist
- truth resync if gaps are detected and exact order matters

### 7.2 Outbox is the causal boundary
For Media, Outbox is the durable bridge between:
- committed Media truth
- downstream cache/projection/audit propagation

This means:
- CDN/cache invalidation derives from committed Media truth
- delayed derived updates do not redefine attachment/order/primary truth
- replay and reconciliation start from Media truth + Outbox, not from timing assumptions

### 7.3 At-least-once downstream posture
Media assumes downstream consumers may:
- crash and replay
- redeliver duplicates
- apply out of order
- lag behind current truth
- resume after long pause

Therefore derived media outputs must remain subordinate to current truth and safe under replay.

### 7.4 V1 consumer posture

In V1, Media events are required to be consumed by Audit.

Audit consumer rules:

- dedupe by `MessageId`
- append audit record only if not already processed
- ack only after durable append or durable dedupe decision
- duplicate delivery must not create duplicate audit records
- Audit lag must not redefine Media truth
- Audit consumer failure must not be reported as a synchronous Media API failure after truth commit

Reading, SEO, CDN invalidation, scan, and variant generation are future consumers.

Future projection consumers must:

- dedupe by `MessageId`
- use `AggregateId + Version` where ordering matters
- ignore stale versions or resync from Media truth
- avoid blind overwrite by arrival order

---

## 8) Primary rule enforcement (non-negotiable invariant)

Must prevent two primaries simultaneously.

Preferred enforcement:
- DB constraint + transactional update

Recommended pattern:
1. unset existing primary for `(ArticleId, Type/Scope)`  
2. set new primary  
3. commit atomically

If supported, enforce with a unique filtered constraint such as:
- unique `(ArticleId)` where `IsPrimary = 1` and `IsDeleted = 0`

### 8.1 Deleted media cannot remain primary
Deleted media cannot remain primary.

V1 policy:

- if a deleted media asset is active primary in any article attachment, primary selection is cleared in the same Media truth transaction where feasible
- the system does not automatically select fallback primary media
- if primary selections were cleared, `media.asset_soft_deleted` payload may include `primarySelectionsCleared = true` and affected article IDs where safe
- deleted media must not be newly attached
- deleted media must not be selected as primary

---

## 9) Deleted media behavior (policy)

Soft-deleted media:
- must not remain active for new truth-sensitive selection
- may still exist historically for audit/recovery policy
- must not break article detail response if asset delivery later fails

Reading/UI should degrade gracefully:
- placeholder
- omit unavailable asset
- preserve article response correctness

Missing asset delivery is a degraded rendering issue, not a reason to corrupt attachment truth.

### 9.1 Restore behavior
Restore must:
- re-enter active truth only when policy allows
- not silently violate current primary/order rules
- not resurrect stale derived artifacts as authoritative state

---

## 10) Retry safety

Clients may retry after timeout ambiguity.
Operations must converge to the same final truth state or fail safely according to policy.

This applies especially to:
- register media
- update media metadata
- attach
- detach
- set primary
- reorder
- soft delete / restore

If Media publishes events for side effects:
- DB commit must not depend on broker availability
- outbox backlog/lag must be observable
- replay must not create duplicate or stale derived effects

### 10.1 Retry-safe design beats exclusive execution assumptions
Media correctness must not depend on:
- one worker “surely” being the current invalidator
- one process being the only reorder/materialization applier
- local ownership belief
- only one cleanup/repair run being assumed current without explicit protection

If future ownership-sensitive workflows are introduced, they must use authoritative generation/fencing checks rather than naive singleton assumptions.

### 10.2 Same-intent replay policy

Media distinguishes:

- duplicate message replay: same `MessageId`
- same-intent replay: different `MessageId`, same effective business intent

Same-intent replay risks:

- duplicate media metadata registration
- duplicate attachment intent
- repeated primary changes after timeout ambiguity
- repeated audit evidence for unchanged state

V1 protection:

- `Idempotency-Key` for register and attach where provided
- unique active `(ArticleId, MediaId)` for attach
- `expectedVersion` for reorder and set-primary
- repeated delete/restore converge by current state
- Audit dedupes by `MessageId`

Same-intent replay must not rely on timeout assumptions.
If ambiguity exists, reconcile from Media truth.

---

## 11) Media processing and causal-chain posture

### 11.1 Processing pipelines are causal, not one global transaction
If Media processing includes steps such as:
- upload/register
- validation/scan
- thumbnail/derivative generation
- ready/available marking

those steps should be treated as a causal chain of state transitions, not one heterogeneous distributed transaction.

### 11.2 Cause-before-effect rule
A later externally meaningful media state must not appear before required prior causes are durable.

Examples:
- a “ready” state must not appear before required validation/processing truth says it is ready
- a derived thumbnail URL must not be treated as authoritative attachment truth
- a stale worker must not re-mark older processing state after newer truth already exists

### 11.3 Stale worker protection
If a future media-processing workflow uses ownership transfer or exclusive workers,
stale-worker writes must be rejectable using:
- generation/fencing token
- authoritative resource-side validation

Naive leader/lock patterns are not acceptable.

### 11.4 Partial derived output rule
Partially generated derivatives, incomplete variant sets, or half-finished repair outputs must not masquerade as complete active derived state.

---

## 12) Reconciliation / cleanup posture

### 12.1 Media truth is the authoritative rebuild source
When derived media lists, snapshots, reports, or repair outputs need rebuild or repair, authoritative input must come from:
- current Media truth
- bounded truth snapshots
- versioned outbox/event history where applicable

### 12.2 Derived outputs remain derived
Thumbnail outputs, derived media lists, cleanup candidate sets, and mismatch reports are derived artifacts.

They may be:
- rebuilt
- replaced
- reconciled
- delayed

But they do not become Media truth.

### 12.3 Candidate-before-publication
If a cleanup/reconciliation workflow produces an important derived output:
- build candidate first
- validate candidate
- publish/cut over explicitly
- do not treat partial candidate output as complete active state

### 12.4 Rerun safety
Important cleanup/reconciliation workflows must be safe to rerun on the same bounded input without corrupting truth or active derived outputs.

### 12.5 Rebuild over fragile repair when simpler
If a derived media output is cheap enough to rebuild from current truth, full rebuild is often safer than clever partial repair logic.

---

## 13) Coordination and ownership posture (Media)

### 13.1 Media does not require global singleton coordination by default
Ordinary Media correctness must not depend on:
- one global media leader
- one process being “the only processor”
- startup order deciding which worker is current
- timeout-only assumptions about ownership

Media correctness should instead be achieved through:
- truth-store authority
- invariant enforcement
- deterministic final-state commands
- stale-write rejection
- idempotent downstream consumers

### 13.2 If future ownership-sensitive workflows are introduced
If a future Media workflow truly requires one current owner
(for example exclusive rebuild of derivative assets for a partition or one-current maintenance owner),
that workflow must define:
- ownership source of truth
- monotonic generation/fencing token
- resource-side rejection of stale owner actions

Naive leader/lock patterns are not acceptable.

### 13.3 Safe non-progress beats unsafe stale media output
If ownership is ambiguous for a correctness-sensitive repair/materialization workflow, Media must prefer:
- delayed repair
- stale-owner rejection
- operator retry
- continued truth-first behavior

over unsafe dual publication or stale overwrite.

---

## 14) Observability signals (Media-specific)

Minimum signals:
- attach/detach/primary/reorder success/failure
- invariant violation attempts (multiple primary prevented, duplicate attach prevented)
- expectedVersion / stale-update rejects for reorder and set-primary
- idempotency-key reuse count
- idempotency conflict count
- outbox backlog/age for Media events
- Media event dedupe hits by `MessageId`
- Audit dedupe hit count for Media events
- CDN/cache invalidation failures if used
- degraded reads due to missing media (placeholder rate)
- reorder conflict/resync indicators for order-sensitive projections if implemented
- truth fallback usage when derived media data is stale or missing
- cleanup/reconciliation activity for media artifacts
- candidate publication/cutover failures for important derived outputs
- version-gap / stale-event rejection indicators where applicable
- same-intent replay rejects where measurable
- ownership-generation mismatch count if future ownership-sensitive workflows are introduced

Logs should include:
- `correlationId`
- `MessageId`
- `eventType`
- `ArticleId`
- `MediaId`
- `expectedVersion`
- `actualVersion`
- `idempotencyKeyHash` where used
- action and outcome (`applied`, `no-op`, `idempotent`, `conflict`, `stale-rejected`)
- version/revision markers where relevant

Do not log raw idempotency keys.

---

## 15) Summary

Media correctness in V1 rests on fifteen rules:

1. Media truth owns attachment membership, order, primary selection, and deletion state.  
2. CDN/object storage serve binary content but do not define relationship truth.  
3. Register, update, attach, detach, set-primary, reorder, delete/restore must converge deterministically or fail safely under retry.
4. Reorder is a final-state set operation, not a loose partial update sequence.  
5. Primary invariants must be enforced by truth and protected under concurrency.  
6. Timeout does not prove a Media mutation failed.  
7. Derived delivery and cache lag may affect presentation, but must not corrupt Media truth.  
8. Async downstream processing is at-least-once; duplicates, replay, and stale workers are normal conditions.  
9. No global ordering or distributed transaction is assumed for Media workflows.  
10. Media-processing state should follow causal truth transitions rather than naive timestamp or worker-belief ordering.  
11. Stale derived media state must never weaken fresher truth-backed attachment/order/primary state.  
12. Cleanup and reconciliation workflows are derived and subordinate to Media truth.  
13. Important repair/materialization workflows must be rerun-safe.  
14. Candidate derived output must be validated before publication when correctness matters.  
15. Singleton/ownership semantics are not relied on unless explicitly protected by authoritative generation/fencing rules.
