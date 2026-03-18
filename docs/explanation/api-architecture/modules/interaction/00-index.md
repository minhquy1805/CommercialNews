# Interaction Module — API Architecture (V1)

**Purpose**
- Provide high-traffic interaction features:
  - view tracking
  - like/unlike
  - comments CRUD (with moderation hooks in V2)
- Keep **interaction truth** explicit and authoritative for likes/comments and optional raw interaction events.
- Support async aggregation, stream-style propagation, replay, rebuild, and reconciliation workflows without letting counters or summaries become hidden truth.

**Why this module is critical**
- It is a "hot" feature area: traffic spikes during hot articles.
- It is abuse-prone (bots/spam), especially comments.
- It must not degrade the public read experience (non-blocking requirement).
- Derived counters and popularity outputs may lag, but must never control read correctness.
- Interaction writes are a mix of:
  - deterministic truth mutations (likes/comments)
  - non-blocking ingestion signals (views)
- Rebuild/reconciliation workflows may grow over time and must stay bounded, rerun-safe, and subordinate to truth.
- Duplicate delivery, replay, late arrivals, and retry ambiguity are normal for the async side of Interaction and must be handled safely.

**Primary consumers**
- Public clients (web/mobile)
- Reading module (signals views; consumes counters optionally)
- Worker (async aggregation, replay, rebuild, reconciliation, if implemented in V1/V2)
- Optional reporting or trend-analysis workflows
- Future read-facing summary/trending consumers

**Non-goals (V1)**
- Unique view counting (V2)
- Full moderation workflows (approve/hide/spam scoring) (V2)
- Real-time counters with strict consistency (eventual by design)
- Treating counters, summaries, or trending outputs as interaction truth
- Coordination-heavy global aggregation ownership
- Requiring aggregation/counter refresh to complete before reads or writes succeed

**Hard constraints**
- View tracking must be non-blocking for read endpoints.
- Like/unlike must be idempotent; totals must remain consistent by truth-first policy.
- Comments must support create/edit/delete with governance expansion in V2.
- Abuse controls (rate limit hooks) must exist in V1.
- Interaction truth is **primary and authoritative** for like state, comment truth, and any stored raw interaction events.
- Async aggregation is **at-least-once**; duplicates, replay, stale delivery, and late arrival must be tolerated safely.
- Counters and aggregate outputs are **derived**, **rebuildable**, and **allowed to lag**.
- Rebuild/reconciliation workflows must be **bounded**, **observable**, and **rerun-safe**.
- Partial or candidate aggregate outputs must not be exposed as if they were final active truth.
- Reading must remain correct even when Interaction-derived outputs are stale or unavailable.

**Truth vs derived posture**
- **Truth**:
  - like state
  - comment truth
  - optional raw interaction events
  - local idempotency markers where policy requires them
- **Derived**:
  - counters (`viewsTotal`, `likesTotal`, `commentsTotal`)
  - popularity/trending signals
  - read-optimized summaries
  - aggregate snapshots
  - mismatch/reconciliation reports
  - cleanup candidate sets
- **Rule**: interaction truth answers whether an interaction happened; counters and summaries may lag, replay, or be rebuilt, but do not become correctness authority.

**Stream / async posture**
- Interaction uses two different runtime styles:
  1. **truth-first writes** for likes/comments
  2. **non-blocking signal ingestion** for views
- When async fan-out or aggregation is needed, the standard model is:
  1. interaction truth or durable raw input is accepted
  2. outbox/event is written atomically where applicable
  3. broker delivers at least once
  4. consumers update derived counters/summaries asynchronously
- Aggregation consumers must tolerate:
  - duplicate delivery
  - replay
  - late arrival
  - stale delivery
- Where possible, commutative aggregation is preferred over fragile ordering assumptions.
- If strict ordering or window correctness matters later, that policy must be documented explicitly.

**Batch / replay / reconciliation posture**
- Interaction may run bounded workflows for:
  - counter rebuild
  - replay of raw interaction inputs
  - reconciliation between truth/raw input and aggregate outputs
  - popularity/trending recomputation
  - cleanup/retention of raw interaction artifacts by policy
- These workflows must:
  - start from authoritative Interaction truth or approved durable input
  - remain rerun-safe
  - avoid overwriting fresher truth with stale aggregate output
  - validate important candidate outputs before publication/cutover where correctness matters
  - treat replay/rebuild as recovery for derived outputs, not as redefinition of interaction truth

**Primary correctness posture**
- Reading must not depend on exact counter freshness.
- Timeouts do not prove interaction truth failed.
- Missing or stale counters degrade gracefully; they do not redefine like/comment truth.
- If uncertainty exists, reconcile from Interaction truth rather than infer from aggregates.
- Safe degraded behavior is acceptable; stale aggregate certainty is not.

**Consistency and ordering posture**
- Strong consistency is required at the Interaction truth boundary for:
  - like state
  - comment truth
  - integrity constraints such as unique active like state
  - deterministic comment edit/delete/moderation transitions where applicable
- Eventual consistency is accepted for:
  - counters
  - popularity/trending signals
  - cached summaries
  - read-facing aggregate enrichments
- Interaction does not require one global total order across all events.
- Ordering is scoped to the subject that needs it:
  - `(ArticleId, UserId)` for likes
  - per comment/thread where needed
  - bounded aggregate window/scope for rebuild workflows
- Event time vs processing time for analytics-style workflows is a policy hook, not an implicit assumption.

**Failure and recovery posture**
- Timeout does not prove a like/comment/view action failed.
- Successful truth commit does not prove counters or summaries are already updated.
- Worker lag, replay, or duplicate delivery is acceptable if:
  - truth remains authoritative
  - aggregates remain rebuildable
  - stale derived state does not become correctness truth
- Full rebuild is acceptable when safer than fragile partial repair.
- Safe non-progress beats stale aggregate overwrite or unsafe cutover.

**Key links**
- System-wide rules:
  - `../../01-api-architecture-charter-v1.md`
  - `../../02-contracts-and-standards.md`
  - `../../07-security-threat-modeling.md`
  - `../../09-observability-and-slos.md`
- Arc42:
  - `../../../architecture/arc42/03-building-blocks-modularity.md`
  - `../../../architecture/arc42/04-runtime-view-v1.md` (Scenario 3)
  - `../../../architecture/arc42/05-quality-requirements.md` (Read-path protection)
  - `../../../architecture/arc42/13-transactions-and-consistency-v1.md`
  - `../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
  - `../../../architecture/arc42/19-stream-processing-runtime-v1.md`
  - `../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
  - `../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- Upstream/downstream:
  - Content (publication state source of truth)
  - Reading (must not block)
  - Audit (optional async ingestion of interaction events)

**ADR hooks**
- View counting semantics (V1 simple vs V2 unique view + privacy)
- Comment moderation model (V2) and enforcement boundaries
- Counter aggregation approach (sync counters vs async aggregation/read model)
- Replay/rebuild policy for interaction-derived aggregates
- Publication/cutover policy for important aggregate outputs
- Event-time vs processing-time policy for trending/popularity workflows