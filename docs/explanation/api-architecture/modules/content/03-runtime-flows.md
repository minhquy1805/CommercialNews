# Content — Runtime Flows (V1)

This module supports arc42 scenarios:
- Scenario 1: create draft and publish
- Scenario 2: unpublish with reason

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

Content primarily participates in three runtime lanes:

### A) Synchronous truth lane
Used for:
- create draft
- update draft
- publish / unpublish
- archive / restore
- edit history / revision append
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

**Rule:** Content owns truth and visibility.  
Batch/rebuild workflows may depend on Content truth, but they do not replace or redefine it.

**Rule:** Content success is defined by **truth commit**, not by downstream completion.

**Rule:** Content-derived async processing is assumed **at-least-once**.  
Duplicates, retries, replay, and out-of-order delivery for the same article version must be tolerated safely downstream.

---

## Flow A — Create draft → Publish (governance boundary)

### Goal
Move an article from draft to public truth safely and durably.

### Sync path
1. Admin calls `POST /admin/content/articles` to create Draft.
2. Authorization enforces create policy.
3. Content persists Draft with metadata.

4. Admin calls `PUT /admin/content/articles/{id}` to update draft.
5. Content persists updates and creates an edit history entry.

6. Admin calls `POST /admin/content/articles/{id}:publish`.
7. Authorization enforces publish policy.
8. Content validates lifecycle transition and sets `PublishedAt` and `Status=Published`.
9. Content increments per-article `Version`.
10. Content writes Outbox in the same transaction.

### Async side effects
11. Content emits `ArticlePublished`.
12. Audit ingests and records publish action (eventual).
13. SEO reacts to ensure slug/canonical/meta correctness or refresh derived serving artifacts (eventual).
14. Notifications may send new-article notifications (optional, eventual).
15. Future reading/search projections may update from the same event stream.

### Runtime stream semantics
- `ArticlePublished` is a **truth-following event**, not an independent truth source.
- Downstream handlers should use:
  - `MessageId` for dedupe
  - `(ArticleId, Version)` for ordering-sensitive apply logic
- Replayed or duplicated `ArticlePublished` must not produce duplicate harmful effects.

### Failure modes
- Audit ingestion fails: publish still succeeds; backlog/lag observable; retries idempotent.
- SEO processing delayed: article readable; SEO or serving artifacts may lag temporarily (policy-defined).
- Notifications fail: publish succeeds; retries must not duplicate emails.
- Broker/outbox publish delayed: truth remains committed; downstream recovery happens through retry.
- Older/stale Content-derived event arrives after newer version downstream: consumer must reject stale apply or resync from truth.

### Batch / rebuild hooks
- If downstream derived systems lag or miss updates, bounded replay/reconciliation workflows may later:
  - detect missing derived effects
  - rebuild candidate derived output from Content truth
  - publish repaired output safely where needed

### Runtime rules
- Content truth defines whether an article is publicly visible.
- Publish does not wait for derived serving state to catch up.
- Derived serving lag is acceptable only if truth-safe fallback preserves correctness.

---

## Flow B — Unpublish with reason

### Goal
Remove an article from public visibility immediately at the truth boundary.

### Sync path
1. Admin calls `POST /admin/content/articles/{id}:unpublish` with reason.
2. Authorization enforces unpublish policy.
3. Content validates transition and sets article to non-public state immediately; records reason.
4. Content increments per-article `Version`.
5. Content writes Outbox in the same transaction.

### Async side effects
6. Content emits `ArticleUnpublished`.
7. Audit records action and reason (eventual).
8. SEO reacts to ensure non-indexable behavior or removal of derived serving artifacts (eventual).
9. Future reading/search projections may remove or mark content non-public.

### Runtime stream semantics
- `ArticleUnpublished` is ordering-sensitive.
- Downstream handlers must not allow an older publish version to re-expose the article after unpublish.
- Consumers should prefer version-aware update or truth resync over arrival-order trust.

### Failure modes
- Audit/SEO delay must not re-expose content publicly.
- Public Query must filter unpublished content by source of truth.
- Stale route/cache/projection may exist temporarily, but must lose to Content truth visibility checks.
- Replay of old publish event after unpublish must not resurrect visibility in a derived store.

### Batch / rebuild hooks
- Reconciliation workflows may later:
  - detect stale derived outputs still referencing now-non-public content
  - generate bounded repair candidates
  - repair derived state without touching Content truth

### Runtime rules
- Unpublish takes effect **immediately at the truth boundary**.
- Async lag may affect derived serving freshness, not truth visibility correctness.
- If derived state is uncertain, safe fallback or safe negative response beats stale confidence.

---

## Flow C — Update draft with revision history

### Goal
Persist new editorial truth for a draft while preserving edit history.

### Sync path
1. Admin edits a draft.
2. Content validates edit command and freshness.
3. Content updates article truth.
4. Content appends revision/history entry according to policy.
5. Content updates `Version` where required by concurrency model.

### Failure modes
- Stale edit submission: conflict / optimistic concurrency reject.
- History append failure inside the same transaction: whole write fails, preserving consistency.
- External side effects are not required for success.
- Replay/retry of the same update command must not create hidden duplicate revision history if command-level idempotency is used by policy.

### Rules
- History is append-only by intent.
- Old revisions must not be silently rewritten.
- Draft update correctness does not depend on downstream systems.
- If draft-edit-derived projections exist later, they remain downstream and non-authoritative.

---

## Flow D — Archive / Restore (if enabled)

### Goal
Move content into or out of archival state safely without confusing public visibility rules.

### Sync path
1. Admin calls archive/restore action.
2. Authorization enforces policy.
3. Content validates lifecycle legality.
4. Content updates truth state and metadata.
5. Content increments `Version` and writes Outbox if downstream effects are needed.

### Async side effects
6. Content may emit lifecycle event for downstream serving, reporting, or audit consumers.
7. Derived stores may update later to reflect archive/restore behavior.

### Failure modes
- Repeated equivalent command should converge safely as no-op or documented conflict.
- Downstream lag must not weaken Content truth.
- Out-of-order derived updates must not move archived content back into active serving state incorrectly.

### Rules
- Archive/restore remains a truth-bound lifecycle decision.
- Derived outputs may lag, but lifecycle truth remains authoritative.
- Version-aware downstream processing is preferred when archive/restore affects serving behavior.

---

## Flow E — Downstream replay / resync from Content truth

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
- Content truth is the input authority.
- Replay/reconciliation outputs are derived artifacts, not Content truth.
- Partial recovery output must not be mistaken for complete active derived state.
- Replay jobs must be bounded and rerun-safe.
- If downstream state is too uncertain, full rebuild from Content truth is preferred over fragile partial mutation.

---

## Flow F — Truth-safe serving under derived lag

### Goal
Ensure Content visibility correctness survives stale cache, stale projection, or delayed downstream consumers.

### Typical runtime shape
1. Public-facing route or projection attempts to serve published content.
2. Derived route/projection/cache may answer quickly if fresh enough.
3. If derived state is missing, stale, or inconsistent:
   - runtime falls back to Content truth or truth-backed visibility check.
4. Response is served only if Content truth allows public visibility.

### Examples
- stale SEO artifact still references now-unpublished content
- lagging read projection has not yet removed archived content
- search/index result references content whose truth visibility changed

### Rules
- Content truth wins over derived serving state.
- Derived state may accelerate, but must not silently override lifecycle truth.
- Safe not-found or safe degradation is preferred over incorrect public exposure.

---

## Summary

Content runtime in V1 is governed by ten rules:

1. Content owns lifecycle and visibility truth.  
2. Publish/unpublish commit truth + version + outbox atomically.  
3. Side effects are downstream and eventual.  
4. Content-derived async processing is at-least-once; duplicates and replay are normal.  
5. Unpublish must take effect immediately at the truth boundary.  
6. Downstream consumers should use `MessageId` and `(ArticleId, Version)` semantics where relevant.  
7. Edit history is append-only by intent.  
8. Derived serving lag must lose to Content truth visibility checks.  
9. Batch/replay/reconciliation workflows may depend on Content truth, but do not redefine it.  
10. Derived-state recovery must remain bounded, observable, rerun-safe, and safe under replay.