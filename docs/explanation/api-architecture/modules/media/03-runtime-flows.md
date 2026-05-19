# Media — Runtime Flows (V1)

Related:

* `../../../../architecture/arc42/04-runtime-view-v1.md`
* `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
* `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
* `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
* `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
* `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
* `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
* `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
* `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`
* `../../../../decisions/adr-0018-transaction-boundaries-and-consistency-model-v1.md`
* `../../../../decisions/adr-0020-timeout-retry-and-failure-detection-policy-v1.md`

---

## Runtime posture in V1

Media participates in four runtime lanes:

### A) Synchronous truth + outbox lane

Used for:

* register media metadata
* update media metadata
* attach / detach
* set primary
* reorder attachments
* soft delete / restore
* enforce attachment/order/primary invariants
* write `OutboxMessage` in the same local transaction

### B) Outbox publication lane

Used for:

* worker polling eligible Media outbox messages
* claiming messages safely
* publishing to RabbitMQ
* marking outbox as `Published` / `Failed` / `Dead`

### C) Consumer side-effect lane

Used for:

* Audit ingestion in V1
* future Reading/SEO/CDN/processing consumers

### D) Batch / cleanup / reconciliation lane

Used for:

* orphan cleanup
* storage-vs-DB reconciliation
* soft-delete retention jobs
* future projection/variant repair

**Rule:** Media owns attachment truth, ordering truth, primary truth, and deletion state. Binary delivery layers and derived media outputs may lag, but they do not redefine that truth.

**Rule:** Media success is defined by **truth commit**, not by CDN/cache/derivative completion, Outbox publication, or consumer completion.

**Rule:** Media events use the integration event names defined by API Surface and Domain Contracts:

* `media.asset_registered`
* `media.asset_updated`
* `media.asset_soft_deleted`
* `media.asset_restored`
* `media.article_media_attached`
* `media.article_media_detached`
* `media.article_media_reordered`
* `media.article_primary_media_set`

**Rule:** Media-derived async processing is assumed **at-least-once**. Duplicates, replay, and delayed/stale worker execution must be tolerated safely.

---

## Flow A — Register media

### Goal

Register authoritative media metadata safely after upload/storage step succeeds by policy.

### Flow

1. Admin uploads file (implementation choice; may be direct upload).
2. Admin calls `POST /api/v1/admin/media/items` to register metadata.
3. Media validates type/metadata and safety policy.
4. Media persists `MediaAsset`.
5. Media writes `OutboxMessage` with event type `media.asset_registered` in the same local transaction.
6. Commit.
7. Return registration result/version.

### Runtime stream semantics

* Registration truth is the authoritative source for media metadata existence.
* Storage/CDN presence alone is not media relationship truth.
* `media.asset_registered` is not optional for governance/audit actions in V1.
* Replay of `media.asset_registered` or related downstream events must not create duplicate harmful derived effects.

### Failure modes

* Upload validation failure: reject request (`400`).
* Storage outage: registration fails; must be observable.
* Timeout during register: reconcile from Media truth, not from client belief.
* Duplicate register attempt: converge by idempotency policy or reject deterministically.
* Derived processing lag: metadata truth exists, but derivative availability may lag.

### Batch / repair hooks

Later reconciliation may detect:

* registered metadata pointing to missing storage object
* storage object with no matching metadata record
* stale or orphan artifacts requiring cleanup/reporting

### Runtime rules

* Media metadata truth commits synchronously.
* Derived thumbnails/previews/caches are downstream only.
* Missing derivative or delivery artifact must not corrupt metadata truth.

---

## Flow B — Update media metadata

### Goal

Update safe media metadata without changing binary storage identity.

### Flow

1. Admin calls `PATCH /api/v1/admin/media/items/{mediaId}`.
2. Media validates safe mutable fields.
3. Media loads current `MediaAsset` truth.
4. Media updates `AltText` and sanitized metadata.
5. Media writes `media.asset_updated` to Outbox in the same transaction.
6. Commit.
7. Return updated result/version.

### Failure modes

* Media item not found: return `404`.
* Unsafe metadata field: reject request (`400`).
* Attempt to change immutable storage identity fields: reject unless explicitly allowed by policy.
* Timeout during update: reconcile from Media truth, not from Audit or downstream projections.

### Runtime rules

* `url`, storage path, size, and type should not change unless policy explicitly allows.
* Metadata update truth commits synchronously.
* Audit ingestion may lag.
* Repeated update with the same final state should converge safely.

---

## Flow C — Attach media to article

### Goal

Attach media to article truthfully and maintain primary invariant if the attach request makes the media primary.

### Flow

1. Admin calls `POST /api/v1/admin/media/articles/{articleId}/attachments` with `mediaId` and `isPrimary`.
2. Media validates:
   * article is valid by application contract
   * media exists and is not deleted
   * attachment uniqueness
3. If `isPrimary = true`, Media attaches the media and sets it as primary in the same transaction.
4. Media writes `media.article_media_attached` to Outbox in the same transaction.
5. Commit.
6. Return attach result/version.

### Event behavior

On success:

* emit `media.article_media_attached`
* include `isPrimary = true` when the request attached as primary
* include `primaryChanged = true` if primary changed

### Runtime stream semantics

* Attach/primary decisions are truth-first and order-sensitive within article scope.
* Downstream consumers should treat `(ArticleId, Version)` or equivalent freshness markers as authoritative where used.
* Replay of old attach/primary events must not overwrite newer relation truth.

### Failure modes

* Conflicting primary updates: enforce deterministically with transaction and/or DB constraint.
* If Content `articleId` is invalid: reject by application contract or policy.
* Timeout ambiguity: reconcile from Media truth, not from downstream cache or Reading output.
* Stale or replayed downstream primary update must not recreate older primary state.

### Batch / repair hooks

Reconciliation jobs may later detect:

* orphan attachments
* invalid primary state
* duplicate relation drift if legacy/import paths ever bypassed invariants

### Runtime rules

* At most one primary exists in the defined article scope.
* Deleted media must not remain active primary truth.
* Downstream lag must not redefine attachment or primary truth.

---

## Flow D — Set primary media

### Goal

Set exactly one active primary media for an article.

### Flow

1. Admin calls `POST /api/v1/admin/media/articles/{articleId}/attachments:set-primary` with `mediaId` and `expectedVersion`.
2. Media validates `expectedVersion`.
3. Media validates the media is attached and active.
4. Media unsets previous primary and sets selected media as primary atomically.
5. Media writes `media.article_primary_media_set` to Outbox in the same transaction.
6. Commit.
7. Return `primarySet = true` and new version.

### Failure modes

* Missing `expectedVersion`: return `400`.
* Version mismatch: return `409`.
* Media not attached: return `400` or `404` by policy.
* Media deleted: return `409` or `400` by policy.
* Concurrent primary update: DB constraint/transaction prevents invalid state.

### Runtime rules

* The primary invariant is enforced atomically.
* If the selected media is already primary and version is current, the operation converges safely.
* Stale or replayed downstream projection/cache updates must not recreate an older primary state.
* `expectedVersion` is required for set-primary.
* Missing `expectedVersion` returns `400 Bad Request`.
* Version mismatch returns `409 Conflict`.

---

## Flow E — Detach media from article

### Goal

Remove media from an article attachment set without breaking primary/order invariants.

### Flow

1. Admin calls `DELETE /api/v1/admin/media/articles/{articleId}/attachments/{mediaId}`.
2. Media loads current attachment truth.
3. If already detached, converge safely.
4. If detached media is primary, clear primary.
5. Media writes `media.article_media_detached` to Outbox in the same transaction.
6. Commit.
7. Return `detached = true`, `version`, and `primaryCleared`.

### Runtime rules

* V1 does not auto-select fallback primary.
* If primary was cleared, event payload includes `primaryCleared = true`.
* Derived Reading/SEO/cache may lag.
* Detach truth is immediate inside Media after commit.

### Failure modes

* Timeout ambiguity: reconcile from Media truth.
* Concurrent detach: converge safely by current attachment truth.
* Older derived cache/projection state must not restore detached membership.

---

## Flow F — Reorder attachments

### Goal

Apply final attachment order atomically for one article scope.

### Flow

1. Admin calls `POST /api/v1/admin/media/articles/{articleId}/attachments:reorder` with `expectedVersion` and ordered `mediaIds`.
2. Media loads current `ArticleMediaSet` version.
3. If `expectedVersion` is missing, return `400`.
4. If version mismatch, return `409`.
5. Validate provided list matches current active attachment set.
6. Apply reorder atomically.
7. Write `media.article_media_reordered` to Outbox in the same transaction.
8. Commit.
9. Return reordered result/version.

### Runtime stream semantics

* Reorder is a final-state set command, not a sequence of independent micro-updates.
* Downstream order-sensitive consumers must tolerate duplicates and reject stale order application where freshness markers exist.
* Replay or duplication of the same reorder command should converge to the same final truth when version policy allows it.

### Failure modes

* Partial reorder updates: must be atomic to avoid inconsistent orders.
* Missing `expectedVersion`: return `400`.
* Stale reorder submission: return `409`.
* Timeout ambiguity: reconcile from current Media truth.
* Older reorder event arriving after newer order exists must not overwrite current truth-backed order.

### Runtime rules

* Reorder is a final-state set operation.
* Partial procedural row-by-row order mutation must not leak as final truth.
* Retrying the same order must converge safely or fail deterministically by version policy.

---

## Flow G — Soft delete and restore

### Goal

Remove media from active truth safely, and restore only where retention/legal policy allows, without corrupting attachment invariants.

### Soft delete flow

1. Admin calls `DELETE /api/v1/admin/media/items/{mediaId}`.
2. Media loads current `MediaAsset` truth.
3. Media marks `MediaAsset` deleted.
4. Media clears affected primary selections where feasible.
5. Media writes `media.asset_soft_deleted` to Outbox in the same transaction.
6. Commit.
7. Return delete result/version.

### Restore flow

1. Admin calls `POST /api/v1/admin/media/items/{mediaId}:restore`.
2. Media verifies retention/legal policy allows restore.
3. Media restores `MediaAsset`.
4. Media does not automatically reselect primary.
5. Media writes `media.asset_restored` to Outbox in the same transaction.
6. Commit.
7. Return restore result/version.

### Event payload notes

* `media.asset_soft_deleted` payload may include `primarySelectionsCleared` and `affectedArticleIds`.
* `media.asset_restored` payload records restore identity/time but does not imply primary reselection.

### Runtime stream semantics

* Delete/restore is lifecycle truth inside Media.
* Downstream derived state may lag, but delete/restore legality and current active state remain Media-owned.
* Replay of older active-state events must not resurrect deleted media truth incorrectly.

### Failure modes

* Deleted media referenced by Reading: Reading must degrade gracefully.
* Deleted media still primary: must be prevented or repaired by policy.
* Restore outside retention/legality window: deterministic reject.
* Delayed cache/CDN state may still serve old binaries temporarily, but that must not redefine Media truth.

### Batch / cleanup hooks

* Retention workflows may purge eligible soft-deleted items later.
* Reconciliation may detect deleted-but-still-referenced artifacts needing repair/reporting.

### Runtime rules

* Deletion truth commits first.
* Purge/cleanup happens later by policy.
* Stale derived presence must lose to current deletion truth.

---

## Flow H — Publish Media outbox messages

### Goal

Publish committed Media events to RabbitMQ without blocking Media write requests.

### Flow

1. Media write commits truth + `OutboxMessage`.
2. Outbox worker polls eligible messages.
3. Worker claims message atomically.
4. Worker publishes event to RabbitMQ.
5. If broker handoff succeeds, mark message `Published`.
6. If publish fails or times out, mark `Failed` with `NextRetryAt`.
7. If retry policy is exhausted, mark `Dead`.

### Runtime rules

* `Published` means broker handoff succeeded.
* `Published` does not mean Audit has consumed the event.
* Producer-side failure is tracked in Outbox.
* Consumer-side failure is handled by consumer retry/DLQ policy.
* Outbox publication lag does not roll back committed Media truth.

### Failure modes

* Broker temporarily unavailable: retry through Outbox policy.
* Worker crashes after claiming: claim timeout/retry policy must make the message eligible again.
* Duplicate broker handoff: consumers must dedupe by `MessageId`.
* Poison publication failure: mark `Dead` after retry policy is exhausted and alert.

---

## Flow I — Audit consumes Media events

### Goal

Create asynchronous audit evidence for Media governance actions.

### Flow

1. RabbitMQ delivers Media event.
2. Audit consumer reads envelope.
3. Audit dedupes by `MessageId`.
4. If already processed, ignore safely.
5. If new, append `AuditLog`.
6. Ack message after successful processing.

### Runtime rules

* Audit append is idempotent by `MessageId`.
* Duplicate delivery must not create duplicate audit records.
* Audit lag does not redefine Media truth.
* Consumer failure is not an Outbox publication failure.
* Audit is the required V1 consumer for Media governance evidence.

### Failure modes

* Audit write fails: consumer retries or sends to DLQ by consumer policy.
* Duplicate event delivery: dedupe by `MessageId`.
* Event replay: append only missing audit records.
* Audit lag: Media write success remains valid because Media truth and Outbox were committed.

---

## Flow J — Derived media processing / derivative generation (future-ready)

### Goal

Support optional downstream workflows such as thumbnail generation or transformed variants without making them part of core Media truth.

### Typical flow

1. Media truth commits and writes an outbox event.
2. Outbox publisher publishes the event.
3. Future processing consumer receives a selected Media event.
4. Worker generates derived artifact:
   * thumbnail
   * transformed image
   * optimized variant
5. Derived output is recorded/published according to policy.

### Runtime stream semantics

* Derivative availability is derived, not truth.
* Downstream handlers must be idempotent and safe under replay.
* Stale worker output must not overwrite fresher derivative or metadata state where freshness markers exist.

### Rules

* Derivative availability is derived, not truth.
* Missing derivative must degrade gracefully.
* Derivative generation must not redefine attachment/order/primary truth.
* Candidate derived output should not be published as active if workflow is incomplete.
* Reading, SEO, CDN invalidation, scan, and variant workflows are downstream extensions and are not required for Media truth success in V1.

### Failure modes

* Worker crash/replay causes duplicate processing.
* Old processing result arrives after newer variant already exists.
* Derivative publication fails but base media truth remains valid.
* Storage/CDN lag affects convenience, not relationship truth.

---

## Flow K — Cleanup / reconciliation workflow

### Goal

Keep media/storage hygiene under control without weakening current truth.

### Typical workflow shape

1. Select bounded candidate set:
   * orphan media
   * soft-deleted expired items
   * broken storage references
   * inconsistent attachment/primary states
2. Re-read authoritative Media truth.
3. Produce candidate cleanup/repair output.
4. Validate candidate actions.
5. Apply cleanup/repair or publish report.
6. Record completion and cleanup temporary workflow state.

### Rules

* Cleanup is bounded.
* Repair/reporting is derived workflow behavior, not new truth authority.
* Rerun on the same bounded input must be safe.
* If live truth and derived repair candidate disagree, Media truth wins.

### Typical outputs

* orphan cleanup candidate sets
* mismatch reports
* rebuilt derived media lists
* derivative repair candidates
* retention cleanup outcomes

### Failure modes

* Repeated replay of the same bounded cleanup scope.
* Overlapping repair runs.
* Stale repair candidate trying to overwrite fresher truth-backed state.
* Partial candidate output being mistaken for active repaired state.

---

## Flow L — Truth-safe serving under media lag

### Goal

Ensure article rendering remains correct when media delivery, cache, or derived variants lag or fail.

### Typical runtime shape

1. Reading requests article composition.
2. Media truth answers attachment/order/primary metadata.
3. Binary delivery, CDN, cache, or derivative may be:
   * missing
   * stale
   * delayed
4. Reading degrades safely:
   * placeholder
   * omit broken asset
   * continue article rendering where policy allows

### Rules

* Media truth decides what is attached, ordered, and primary.
* Delivery failure is a degraded rendering problem, not a relationship-truth rewrite.
* Safe omission beats stale or invented media state.

---

## Summary

Media runtime in V1 is governed by twelve rules:

1. Media owns metadata, attachment membership, order, primary selection, and deletion truth.
2. Register/update/attach/detach/reorder/set-primary/delete/restore are truth-first synchronous operations.
3. Media writes Outbox messages in the same local transaction as truth changes.
4. Outbox publication is separate from consumer processing.
5. `Published` means broker handoff, not Audit consumption.
6. Audit consumes Media events in V1 and dedupes by `MessageId`.
7. Media-derived async processing is at-least-once; duplicates and replay are normal.
8. CDN/object storage and derivative outputs are downstream and derived.
9. Primary invariants and reorder correctness must be enforced atomically.
10. Reading must degrade gracefully when media delivery or derived artifacts fail.
11. Cleanup/reconciliation workflows may depend on Media truth, but do not redefine it.
12. Truth-backed media relationship state must remain correct even while delivery, publication, consumer, or derivative systems lag.
