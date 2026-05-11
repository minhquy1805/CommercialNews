# Content — Runtime Flows (V1)

This module supports arc42 scenarios:

- Scenario 1: create draft and publish
- Scenario 2: unpublish with reason

Related:

- `../../../../architecture/arc42/04-runtime-view-v1.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0020-timeout-retry-and-failure-detection-policy-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## Runtime posture in V1

Content primarily participates in three runtime lanes:

### A) Synchronous truth lane

Used for:

- create draft
- update article content
- publish / unpublish
- archive
- soft-delete
- revision append
- lifecycle legality enforcement
- authoritative visibility changes

### B) Async side-effect and derived-state lane

Used for:

- audit ingestion
- SEO reactions
- notifications
- cache invalidation triggers
- future projection/read-model updates
- future search/index updates

### C) Batch / replay / reconciliation support lane

Used for:

- resyncing downstream derived state from Content truth
- replay/reconciliation when downstream consumers lag or fail
- bounded rebuild support for read models, SEO-serving artifacts, search artifacts, or reporting outputs that depend on Content truth

### Core runtime rules

**Rule:** Content owns lifecycle and visibility truth.
Batch/rebuild workflows may depend on Content truth, but they do not replace or redefine it.

**Rule:** Content success is defined by **truth commit**.
When async side effects are required, success means:
- Content truth committed
- async intent/outbox committed

It does **not** mean:
- audit is already queryable
- SEO is already refreshed
- notifications are already sent
- projections/caches are already caught up

**Rule:** Content-derived async processing is assumed **at-least-once**.
Duplicates, retries, replay, and out-of-order delivery for the same Article aggregate version must be tolerated safely downstream.

---

## Flow A — Create draft → Publish

### Goal

Move an article from draft to public truth safely and durably.

### Sync path

1. Admin calls `POST /api/v1/admin/content/articles` to create `Draft`.
2. Authorization enforces create policy.
3. Content persists draft truth, initializes `Article.Version = 1`, and writes `OutboxMessage` with event `content.article_created` in the same transaction.

4. Admin calls `PUT /api/v1/admin/content/articles/{articleId}` to update draft content.
5. Content validates update legality and freshness.
6. Content persists article updates and appends revision/history entry according to policy.

7. Admin calls `POST /api/v1/admin/content/articles/{articleId}:publish`.
8. Authorization enforces publish policy.
9. Content validates lifecycle transition.
10. Content sets `Status = Published` and `PublishedAt`.
11. Content increments `Article.Version`.
12. Content appends `ArticleLifecycleEvent` with `ActionType='Publish'` and `ArticleVersion=Article.Version`.
13. Content writes `OutboxMessage` with event `content.article_published` in the same transaction.

### Async event emitted

Content emits:

- `content.article_published`

### Event semantics

- emitted only after truth commit
- carries stable `MessageId`
- envelope uses `AggregateType = Article`, `AggregateId = ArticlePublicId`, `Version = Article.Version`
- downstream consumers should use:
  - `MessageId` for message-level dedupe
  - `ArticlePublicId + Version` / `AggregateId + Version` for ordering-sensitive apply logic
- replayed or duplicated `content.article_published` must not produce duplicate harmful effects

### Async side effects

14. Audit ingests and records publish action *(eventual)*.
15. SEO reacts to ensure slug/canonical/meta correctness or refresh derived serving artifacts *(eventual)*.
16. Notifications may send new-article notifications *(optional, eventual)*.
17. Future reading/search projections may update from the same event stream.

### Failure modes

- audit ingestion fails:
  - publish still succeeds
  - backlog/lag must be observable
  - retries must be idempotent
- SEO processing delayed:
  - article truth is already public
  - SEO or serving artifacts may lag temporarily by policy
- notifications fail:
  - publish succeeds
  - retries must not duplicate emails
- broker/outbox publish delayed:
  - truth remains committed
  - downstream recovery happens through retry
- older/stale content-derived event arrives after newer version downstream:
  - consumer must reject stale apply or resync from truth

### Batch / rebuild hooks

If downstream derived systems lag or miss updates, bounded replay/reconciliation workflows may later:

- detect missing derived effects
- rebuild candidate derived output from Content truth
- publish repaired output safely where needed

### Runtime rules

- Content truth defines whether an article is publicly visible
- publish does not wait for derived serving state to catch up
- derived serving lag is acceptable only if truth-safe fallback preserves correctness

---

## Flow B — Unpublish with reason

### Goal

Remove an article from public visibility immediately at the truth boundary.

### Sync path

1. Admin calls `POST /api/v1/admin/content/articles/{articleId}:unpublish` with reason.
2. Authorization enforces unpublish policy.
3. Content validates transition legality.
4. Content V1 sets `Status = Draft`, records `UnpublishedAt`, and records mandatory reason.
5. Content increments `Article.Version`.
6. Content appends `ArticleLifecycleEvent` with `ActionType='Unpublish'`, required `Reason`, and `ArticleVersion=Article.Version`.
7. Content writes `OutboxMessage` with event `content.article_unpublished` in the same transaction.

### Async event emitted

Content emits:

- `content.article_unpublished`

### Event semantics

- ordering-sensitive for downstream serving/search/SEO behavior
- carries stable `MessageId`
- envelope uses `AggregateType = Article`, `AggregateId = ArticlePublicId`, `Version = Article.Version`
- downstream consumers must not allow an older publish version to re-expose the article after unpublish
- consumers should prefer version-aware apply or truth resync over arrival-order trust

### Async side effects

8. Audit records action and reason *(eventual)*.
9. SEO reacts to ensure non-indexable behavior or removal of derived serving artifacts *(eventual)*.
10. Reading/search projections may remove or mark content non-public *(eventual)*.

### Failure modes

- audit/SEO delay must not re-expose content publicly
- public read must filter unpublished content (`Status = Draft` after unpublish) by Content truth or truth-backed visibility check
- stale route/cache/projection may exist temporarily, but must lose to Content truth visibility checks
- replay of old publish event after unpublish must not resurrect visibility in a derived store

### Batch / rebuild hooks

Reconciliation workflows may later:

- detect stale derived outputs still referencing now-non-public content
- generate bounded repair candidates
- repair derived state without touching Content truth

### Runtime rules

- unpublish takes effect **immediately at the truth boundary**
- async lag may affect derived serving freshness, not truth visibility correctness
- if derived state is uncertain, safe fallback or safe negative response beats stale confidence

---

## Flow C — Update article with revision history

### Goal

Persist new editorial truth while preserving append-only history.

### Sync path

1. Admin edits an article according to lifecycle policy.
2. Content validates edit command and freshness.
3. Content updates article truth.
4. Content appends revision/history entry according to policy.
5. Content increments `Article.Version` for meaningful changes.
6. Content writes `OutboxMessage` with event `content.article_updated` in the same transaction for meaningful changes.

### Async event emitted

Content emits for meaningful changes:

- `content.article_updated`

The update event uses `AggregateType = Article`, `AggregateId = ArticlePublicId`, and `Version = Article.Version`.

### Async side effects

7. Audit records edit action *(eventual)*.
8. SEO may recompute metadata defaults or slug candidates *(eventual)*.
9. Reading/search/cache projections may invalidate or refresh if a published article changed *(eventual)*.

### Failure modes

- stale edit submission:
  - conflict / optimistic concurrency reject
- history append failure inside the same transaction:
  - whole write fails, preserving consistency
- external side effects are not required for success after truth + outbox commit
- replay/retry of the same update command must not create hidden duplicate revision history if command-level idempotency is used by policy

### Rules

- history is append-only by intent
- old revisions must not be silently rewritten
- revision history is historical content evidence, not current live truth
- update correctness does not depend on downstream systems
- published article updates may require public cache/projection invalidation
- if draft/article-derived projections exist later, they remain downstream and non-authoritative

---

## Flow D — Archive article

### Goal

Move content into archival state safely without confusing public visibility rules.

### Sync path

1. Admin calls `POST /api/v1/admin/content/articles/{articleId}:archive`.
2. Authorization enforces policy.
3. Content validates lifecycle legality.
4. Content sets `Status = Archived` and records `ArchivedAt`.
5. Content increments `Article.Version`.
6. Content appends `ArticleLifecycleEvent` with `ActionType='Archive'` and `ArticleVersion=Article.Version`.
7. Content writes `OutboxMessage` with event `content.article_archived` in the same transaction.

### Async event emitted

Content emits:

- `content.article_archived`

### Async side effects

8. Audit may record lifecycle action.
9. Derived stores may later update to reflect archive behavior.

### Failure modes

- repeated equivalent command should converge safely as no-op or documented conflict
- downstream lag must not weaken Content truth
- out-of-order derived updates must not move archived content back into active serving state incorrectly

### Rules

- archive remains a truth-bound lifecycle decision
- Archived restore is out of scope for V1 unless a later lifecycle policy explicitly enables `Archived -> Draft`
- derived outputs may lag, but lifecycle truth remains authoritative
- version-aware downstream processing is preferred when archive affects serving behavior

---

## Flow E — Soft-delete article

### Goal

Remove an article from public eligibility through Content truth without physically deleting it.

### Sync path

1. Admin calls `POST /api/v1/admin/content/articles/{articleId}:soft-delete`.
2. Authorization enforces soft-delete policy.
3. Content validates command legality.
4. Content sets `IsDeleted=1`, `DeletedAt`, and `DeletedBy`.
5. Content increments `Article.Version`.
6. Content appends `ArticleLifecycleEvent` with `ActionType='SoftDelete'` and `ArticleVersion=Article.Version`.
7. Content writes `OutboxMessage` with event `content.article_soft_deleted` in the same transaction.

### Async event emitted

Content emits:

- `content.article_soft_deleted`

### Async side effects

8. Audit records soft-delete action *(eventual)*.
9. SEO, Reading, search, and cache projections remove or mark the article non-public *(eventual)*.

### Failure modes

- downstream cache/projection lag must not re-expose soft-deleted content
- older publish/update events must not overwrite newer soft-delete visibility state
- repeated equivalent soft-delete commands should converge safely as no-op or documented idempotent success

### Rules

- soft-delete is not physical deletion
- soft-delete does not have to change `Status`
- public reads must stop serving the article immediately because visibility requires `Status='Published' AND IsDeleted=0`
- if downstream state is uncertain, safe fallback or safe negative response beats stale confidence

---

## Flow F — Downstream replay / resync from Content truth

### Goal

Support bounded recovery when downstream modules missed or delayed Content-derived side effects.

### Typical workflow shape

1. Select bounded Content truth input:
   - article window
   - changed-since-version/checkpoint scope
   - suspect aggregate set
   - reconciliation mismatch set
2. Re-read authoritative lifecycle truth from Content.
3. Produce derived replay/reconciliation candidate set.
4. Hand off to downstream workflow or publish bounded recovery output.
5. Record completion and cleanup.

### Typical uses

- SEO rebuild or route reconciliation
- reading projection repair
- search/index repair
- notification recovery candidate generation
- governance/reporting consistency checks

### Rules

- Content truth is the input authority
- replay/reconciliation outputs are derived artifacts, not Content truth
- partial recovery output must not be mistaken for complete active derived state
- replay jobs must be bounded and rerun-safe
- if downstream state is too uncertain, full rebuild from Content truth is preferred over fragile partial mutation

---

## Flow G — Truth-safe serving under derived lag

### Goal

Ensure Content visibility correctness survives stale cache, stale projection, or delayed downstream consumers.

### Typical runtime shape

1. Public-facing route or projection attempts to serve published content.
2. Derived route/projection/cache may answer quickly if fresh enough.
3. If derived state is missing, stale, or inconsistent:
   - runtime falls back to Content truth or a truth-backed visibility check exposed to the serving path
4. Response is served only if Content truth allows public visibility.

### Examples

- stale SEO artifact still references content unpublished back to Draft
- lagging read projection has not yet removed archived content
- search/index result references content whose truth visibility changed

### Rules

- Content truth wins over derived serving state
- derived state may accelerate, but must not silently override lifecycle truth
- safe not-found or safe degradation is preferred over incorrect public exposure

---

## Summary

Content runtime in V1 is governed by the following rules:

1. Content owns lifecycle and visibility truth.
2. Publish/unpublish/archive/soft-delete commit truth + lifecycle event + version + outbox atomically.
3. Successful writes mean truth committed, and where needed, async intent/outbox committed.
4. Side effects are downstream and eventual.
5. Content-derived async processing is at-least-once; duplicates and replay are normal.
6. Unpublish must take effect immediately at the truth boundary.
7. Downstream consumers should use `MessageId` for dedupe and `ArticlePublicId + Version` / `AggregateId + Version` for stale-event protection.
8. Edit history is append-only by intent and does not replace current truth.
9. Derived serving lag must lose to Content truth visibility checks.
10. Batch/replay/reconciliation workflows may depend on Content truth, but do not redefine it.
11. Derived-state recovery must remain bounded, observable, rerun-safe, and safe under replay.
