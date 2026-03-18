# Media — Runtime Flows (V1)

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

Media participates in three runtime lanes:

### A) Synchronous truth lane
Used for:
- register media metadata
- attach / detach
- set primary
- reorder attachments
- soft delete / restore
- enforce attachment/order/primary invariants

### B) Async side-effect lane
Used for:
- audit emission
- optional cache invalidation
- optional derived media processing signals
- optional read-model refresh hooks
- optional derivative-generation or processing workflows

### C) Batch / cleanup / reconciliation lane
Used for:
- cleanup orphan or soft-deleted media by retention policy
- reconcile attachment truth vs storage/object references
- rebuild derived media metadata or lists
- repair primary/order inconsistencies if drift is detected
- process or reprocess derivative/thumbnail workflows if introduced later

**Rule:** Media owns attachment truth, ordering truth, primary truth, and deletion state.  
Binary delivery layers and derived media outputs may lag, but they do not redefine that truth.

**Rule:** Media success is defined by **truth commit**, not by CDN/cache/derivative completion.

**Rule:** Media-derived async processing is assumed **at-least-once**.  
Duplicates, replay, and delayed/stale worker execution must be tolerated safely.

---

## Flow A — Register media

### Goal
Register authoritative media metadata safely after upload/storage step succeeds by policy.

### Flow
1. Admin uploads file (implementation choice; may be direct upload).
2. Admin calls `POST /admin/media/items` to register metadata.
3. Media validates type/metadata and persists `MediaItem`.
4. Media emits `MediaRegistered` (optional) for audit/caching/derived processing.

### Runtime stream semantics
- registration truth is the authoritative source for media metadata existence
- storage/CDN presence alone is not media relationship truth
- replay of `MediaRegistered` or related downstream events must not create duplicate harmful derived effects

### Failure modes
- Upload validation failure: reject request (`400`).
- Storage outage: registration fails; must be observable.
- Timeout during register: reconcile from Media truth, not from client belief.
- Duplicate register attempt: converge by policy or reject deterministically.
- Derived processing lag: metadata truth exists, but derivative availability may lag.

### Batch / repair hooks
- later reconciliation may detect:
  - registered metadata pointing to missing storage object
  - storage object with no matching metadata record
  - stale or orphan artifacts requiring cleanup/reporting

### Runtime rules
- media metadata truth commits synchronously
- derived thumbnails/previews/caches are downstream only
- missing derivative or delivery artifact must not corrupt metadata truth

---

## Flow B — Attach and set primary

### Goal
Attach media to article truthfully and maintain primary invariant deterministically.

### Flow
1. Admin calls `POST /admin/media/articles/{articleId}/attachments` with `mediaId`.
2. Media validates:
   - media exists and is not deleted
   - attachment uniqueness
3. If `isPrimary=true`, Media enforces primary rule (unset previous primary).
4. Emit `MediaAttached` / `MediaPrimaryChanged` events (optional).

### Runtime stream semantics
- attach/primary decisions are truth-first and order-sensitive within article scope
- downstream consumers should treat `(ArticleId, Version)` or equivalent freshness markers as authoritative where used
- replay of old attach/primary events must not overwrite newer relation truth

### Failure modes
- Conflicting primary updates: enforce deterministically (transaction or unique constraint).
- If Content `articleId` is invalid: policy decision (validate via read-only check vs accept and rely on later cleanup). Prefer validating via allowed read-only check in V1.
- Timeout ambiguity: reconcile from Media truth, not from downstream cache or Reading output.
- Stale or replayed downstream primary update must not recreate older primary state.

### Batch / repair hooks
- reconciliation jobs may later detect:
  - orphan attachments
  - invalid primary state
  - duplicate relation drift if legacy/import paths ever bypassed invariants

### Runtime rules
- at most one primary in the defined scope
- deleted media must not remain active primary truth
- downstream lag must not redefine attachment or primary truth

---

## Flow C — Reorder attachments

### Goal
Apply final attachment order atomically for one article scope.

### Flow
1. Admin calls reorder with ordered list.
2. Media validates list matches attachments by policy.
3. Media updates ordering atomically and emits `MediaReordered` (optional).

### Runtime stream semantics
- reorder is a final-state set command, not a sequence of independent micro-updates
- downstream order-sensitive consumers must tolerate duplicates and reject stale order application where freshness markers exist
- replay or duplication of the same reorder command should converge to the same final truth

### Failure modes
- Partial reorder updates: must be atomic to avoid inconsistent orders.
- Stale reorder submission: reject or converge safely according to freshness policy.
- Timeout ambiguity: reconcile from current Media truth.
- Older reorder event arriving after newer order exists must not overwrite current truth-backed order.

### Rules
- reorder is a final-state set operation
- partial procedural row-by-row order mutation must not leak as final truth
- retries of the same order must converge safely

---

## Flow D — Soft delete and restore

### Goal
Remove media from active truth safely without corrupting attachment invariants.

### Flow
1. Admin deletes media item (soft delete).
2. Media marks deleted and ensures it cannot be primary by policy.
3. Restore within retention window.

### Runtime stream semantics
- delete/restore is lifecycle truth inside Media
- downstream derived state may lag, but delete/restore legality and current active state remain Media-owned
- replay of older active-state events must not resurrect deleted media truth incorrectly

### Failure modes
- Deleted media referenced by Reading: Reading must degrade gracefully.
- Deleted media still primary: must be prevented or repaired by policy.
- Restore outside retention/legality window: deterministic reject.
- Delayed cache/CDN state may still serve old binaries temporarily, but that must not redefine Media truth.

### Batch /cleanup hooks
- retention workflows may purge eligible soft-deleted items later
- reconciliation may detect deleted-but-still-referenced artifacts needing repair/reporting

### Runtime rules
- deletion truth commits first
- purge/cleanup happens later by policy
- stale derived presence must lose to current deletion truth

---

## Flow E — Derived media processing / derivative generation (future-ready)

### Goal
Support optional downstream workflows such as thumbnail generation or transformed variants without making them part of core Media truth.

### Typical flow
1. Media truth commits.
2. Media emits event or processing intent.
3. Worker generates derived artifact:
   - thumbnail
   - transformed image
   - optimized variant
4. Derived output is recorded/published according to policy.

### Runtime stream semantics
- derivative availability is derived, not truth
- downstream handlers must be idempotent and safe under replay
- stale worker output must not overwrite fresher derivative or metadata state where freshness markers exist

### Rules
- derivative availability is derived, not truth
- missing derivative must degrade gracefully
- derivative generation must not redefine attachment/order/primary truth
- candidate derived output should not be published as active if workflow is incomplete

### Failure modes
- worker crash/replay causes duplicate processing
- old processing result arrives after newer variant already exists
- derivative publication fails but base media truth remains valid
- storage/CDN lag affects convenience, not relationship truth

---

## Flow F — Cleanup / reconciliation workflow

### Goal
Keep media/storage hygiene under control without weakening current truth.

### Typical workflow shape
1. Select bounded candidate set:
   - orphan media
   - soft-deleted expired items
   - broken storage references
   - inconsistent attachment/primary states
2. Re-read authoritative Media truth.
3. Produce candidate cleanup/repair output.
4. Validate candidate actions.
5. Apply cleanup/repair or publish report.
6. Record completion and cleanup temporary workflow state.

### Rules
- cleanup is bounded
- repair/reporting is derived workflow behavior, not new truth authority
- rerun on the same bounded input must be safe
- if live truth and derived repair candidate disagree, Media truth wins

### Typical outputs
- orphan cleanup candidate sets
- mismatch reports
- rebuilt derived media lists
- derivative repair candidates
- retention cleanup outcomes

### Failure modes
- repeated replay of the same bounded cleanup scope
- overlapping repair runs
- stale repair candidate trying to overwrite fresher truth-backed state
- partial candidate output being mistaken for active repaired state

---

## Flow G — Truth-safe serving under media lag

### Goal
Ensure article rendering remains correct when media delivery, cache, or derived variants lag or fail.

### Typical runtime shape
1. Reading requests article composition.
2. Media truth answers attachment/order/primary metadata.
3. Binary delivery, CDN, cache, or derivative may be:
   - missing
   - stale
   - delayed
4. Reading degrades safely:
   - placeholder
   - omit broken asset
   - continue article rendering where policy allows

### Rules
- Media truth decides what is attached, ordered, and primary
- delivery failure is a degraded rendering problem, not a relationship-truth rewrite
- safe omission beats stale or invented media state

---

## Summary

Media runtime in V1 is governed by ten rules:

1. Media owns metadata, attachment membership, order, primary selection, and deletion truth.  
2. Register/attach/reorder/delete/restore are truth-first synchronous operations.  
3. Media-derived async processing is at-least-once; duplicates and replay are normal.  
4. CDN/object storage and derivative outputs are downstream and derived.  
5. Primary invariants and reorder correctness must be enforced atomically.  
6. Reading must degrade gracefully when media delivery or derived artifacts fail.  
7. Cleanup/reconciliation workflows may depend on Media truth, but do not redefine it.  
8. Repair workflows must be bounded, observable, and rerun-safe.  
9. Derived media processing must not become hidden truth.  
10. Truth-backed media relationship state must remain correct even while delivery or derivative systems lag.