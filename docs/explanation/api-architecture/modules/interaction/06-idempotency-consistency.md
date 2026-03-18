# Interaction — Idempotency & Consistency (V1)

This document defines Interaction-specific idempotency, retry safety, aggregation posture, ordering scope, consistency rules for likes, comments, views, and derived counters, plus replay/rebuild/reconciliation posture for aggregate outputs.

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

## 0) Role of Interaction in the system

Interaction owns user interaction truth such as:
- like state
- comment truth
- optional raw interaction events if stored durably

Interaction does **not** own:
- Content publication truth
- Reading response truth
- aggregate counters as correctness boundary
- final public visibility rules

Therefore:
- Interaction must not slow or control public read correctness
- aggregate lag is acceptable
- user-visible interaction truth must remain deterministic
- derived counters must never become the only authority for correctness-sensitive behavior

---

## 1) Truth vs derived

### 1.1 Truth (Interaction store)
Interaction truth includes:
- user actions with integrity constraints:
  - likes (unique per user per article)
  - comments (append + moderation/status fields, optional edit trace)
  - optional raw view events if V1 persists them durably
- minimal state needed to enforce abuse rules and user-visible behavior
- local dedupe or idempotency markers where policy requires them

### 1.2 Derived (allowed to lag)
Derived/aggregated state includes:
- counters: `viewsTotal / likesTotal / commentsTotal`
- popularity score / trending signals
- read-optimized stats tables
- cached counters / cached reaction summaries
- batch-generated aggregate snapshots and mismatch reports

These are eventually consistent and may lag under backlog or replay.

**Rule:** Reading must tolerate stale or missing counters.  
Correctness does not depend on aggregate freshness.

### 1.3 Truth boundary for user-visible behavior
The authoritative answer to:
- “has this user liked this article?”
- “does this comment exist?”
- “was this comment deleted/edited?”

comes from Interaction truth, not from aggregate counters or cached summaries.

### 1.4 Consistency class for Interaction
Interaction intentionally uses multiple consistency classes:

#### Strong truth-backed consistency
Required for:
- current like truth
- comment existence/edit/delete/moderation truth
- any abuse or integrity rule enforced at the interaction truth boundary
- deterministic user-visible interaction state

#### Ordered / causality-sensitive consistency
Required for:
- like → unlike transitions per `(ArticleId, UserId)`
- comment edit/delete after comment create
- downstream side effects that depend on committed comment truth
- stale aggregate materialization rejection if strict freshness/version markers are introduced
- candidate aggregate publication that must not overwrite fresher derived state

#### Eventual consistency
Accepted for:
- counters
- popularity/trending signals
- cached summaries
- read-optimized aggregate views
- non-authoritative interaction enrichments
- rebuild/reconciliation convergence

---

## 2) Like / unlike idempotency (mandatory)

### 2.1 Like
- enforce uniqueness on `(ArticleId, UserId)` for active like state
- repeated like returns `liked=true` (idempotent success) or a consistent conflict policy if the API standard requires it

### 2.2 Unlike
- repeated unlike returns `liked=false` (idempotent success) or a consistent conflict policy if chosen system-wide

### 2.3 Required implementation posture
At minimum:
- use DB uniqueness constraints for `(ArticleId, UserId)` active-like semantics
- treat “already liked” / “already unliked” deterministically
- do not emit duplicate downstream effects for logically unchanged state

### 2.4 Timeout ambiguity posture
If the client times out during like/unlike:
- timeout does **not** prove the state change failed
- safe reconciliation must inspect Interaction truth
- retries must not create contradictory like state

### 2.5 Idempotency is preferred over singleton assumptions
Interaction correctness must not depend on:
- only one request arriving
- only one worker updating a reaction summary
- local ownership belief
- startup order
- only one rebuild/reconciliation worker being assumed current without explicit protection

Interaction should instead rely on:
- truth-store authority
- uniqueness constraints
- deterministic state transitions
- idempotent replay-safe downstream processing

---

## 3) View retry behavior (V1)

### 3.1 Semantics
Views may be retried; duplicates are acceptable in V1 semantics unless a stricter policy is explicitly introduced.

This means:
- read response must not depend on exact dedupe of view signals
- temporary view inflation risk is acceptable within V1 policy
- view tracking must remain non-blocking

### 3.2 Optional dedupe (if enabled)
If V1 introduces view dedupe to reduce inflation:
- define a stable `visitorKey` or `viewIdempotencyKey` policy explicitly
- example inputs may include:
  - `(ArticleId, UserId?, IP?, UserAgent?, timeWindow)`
- document whether dedupe is:
  - best-effort
  - durable
  - or analytics-only

### 3.3 Dedupe storage posture
Possible implementations:
- Redis TTL-based dedupe for best-effort suppression
- SQL/durable dedupe if stronger guarantees are needed

**Rule:** view tracking must never block the read response.

### 3.4 Views do not require global precision in V1
Exact global ordering or exact globally deduped view truth is not a V1 requirement.

The architecture favors:
- non-blocking ingestion
- bounded inflation risk
- replay-safe aggregation
- observability and later reconciliation where needed

### 3.5 Event-time note for views
If Interaction later computes:
- per-window summaries
- trending inputs
- hourly/daily rollups

then it should document clearly whether those workflows use:
- event time
- processing time
- bounded lateness / watermark policy

V1 does not require advanced stream-time correctness everywhere, but any introduced time-window logic must remain explicit and replay-safe.

---

## 4) Transaction boundary (V1)

### 4.1 Truth boundary
The Interaction transaction boundary stops at the Interaction-owned truth change.

Typical truth changes include:
- create/remove the current like state for a given `(ArticleId, UserId)`
- create/edit/delete a comment and update moderation/status fields
- persist raw interaction events if V1 stores them durably
- write local metadata required to preserve user-visible interaction state
- write the Outbox record when downstream aggregation or side effects are required

### 4.2 Atomic commit set
For Interaction commands, the module should commit atomically:
- the truth change for the interaction action
- any local metadata required to keep the action deterministic
- the Outbox record for downstream aggregation or fan-out side effects
- local idempotency marker where policy requires one

Typical examples:
- like: like truth change + Outbox
- unlike: active-like removal/deactivation + Outbox
- comment create: comment insert + Outbox
- comment edit/delete: comment truth update + Outbox
- raw view event persistence (if used): raw event write + Outbox

### 4.3 Outside the transaction
The following MUST NOT be required inside the Interaction truth transaction:
- counter aggregation completion
- popularity/trending recomputation
- Redis counter updates as a success condition
- public page rendering refresh
- notification sending
- broker publish
- external HTTP/API calls
- rebuild/reconciliation workflow completion

These are post-commit async effects.

### 4.4 Transaction duration rule
Interaction transactions must be short:
- no waiting on downstream consumers
- no synchronous aggregation in the critical path
- no open transaction across read-path rendering
- no retry loops over external dependencies inside the transaction

This is especially important for read-adjacent flows such as views and lightweight reactions.

### 4.5 Shared DB does not widen Interaction ownership
Even in a shared DB deployment, Interaction must not use the same transaction to directly mutate:
- Content lifecycle truth
- Reading projections
- Notification truth
- Audit downstream state

Interaction may write:
- Interaction-owned truth tables
- approved local replication artifacts such as Outbox
- local dedupe/idempotency records when policy requires them

### 4.6 No heterogeneous distributed transaction
Interaction does **not** attempt one atomic workflow across:
- Interaction truth DB
- RabbitMQ
- Redis
- notification providers
- external scoring systems
- other module-owned truth stores

Atomicity stops at:
- Interaction truth mutation
- required local deterministic metadata
- local idempotency marker where used
- Outbox intent

---

## 5) Concurrency and retry posture

### 5.1 Concurrency assumptions
Interaction must assume:
- client retries
- duplicate event delivery
- concurrent like/unlike attempts
- repeated comment submissions after timeout
- stale aggregate reads
- replay/reprocess in consumers
- overlapping rebuild/reconciliation runs if triggered more than once

### 5.2 Required protections
At minimum, the design must prevent:
- duplicate active likes for the same `(ArticleId, UserId)`
- contradictory like state after concurrent retries
- duplicate durable comment creation when idempotency is promised
- stale aggregates becoming a correctness dependency
- rebuilt aggregate outputs overwriting fresher aggregate truth incorrectly

Typical implementation options include:
- DB uniqueness constraints for like state
- durable idempotency records for retriable comment creation
- conditional state transitions
- append-log + async aggregation where eventual consistency is acceptable

### 5.3 Duplicate delivery vs duplicate intent
Interaction must distinguish:

#### A) Duplicate processing of the same action
Example:
- same like event delivered twice
- same comment-create request retried with same idempotency key

Must converge to one logical action.

#### B) Legitimately distinct actions
Example:
- two different comments with different intent
- a later unlike after a like
- a later comment edit after create

Must not be incorrectly deduped away.

### 5.4 Timestamp is not freshness authority
Interaction must not use:
- `UpdatedAt`
- “latest event time wins”
- cache update time
- client-supplied timestamps

as the primary authority for correctness-sensitive interaction freshness.

Truth and explicit version/freshness markers are authoritative when needed.

---

## 6) Comment creation and edit posture

### 6.1 Comment creation
If comment creation is exposed to timeout-prone clients, consider `Idempotency-Key`.

Goal:
- prevent duplicate comment creation under retry ambiguity
- allow safe reconciliation after timeout

If idempotency is promised for comment create:
- the same semantic request with the same key must converge to one comment truth result
- conflicting payload reuse should return a deterministic conflict

### 6.2 Comment edits
Edits should:
- update `UpdatedAt`
- preserve current truth correctly
- remain retry-safe if the same edit is replayed

If stale edit risk exists, use:
- optimistic concurrency
- version/revision checks
- explicit stale-update conflict handling

### 6.3 Comment delete/moderation
Delete/moderation transitions must be deterministic and truth-first.  
Derived counters may lag behind those truth changes.

### 6.4 State legality and freshness are complementary
Comment/version checks alone are not enough where moderation/delete/edit rules matter.

Interaction must enforce both:
- freshness/stale-write rejection where needed
- legality of the requested state transition

---

## 7) Counter and aggregate posture

### 7.1 Counters are derived, not correctness truth
Counters and scores are not the primary correctness boundary.

Therefore:
- counter lag is acceptable
- duplicate delivery must be tolerated safely
- aggregation must be replay-safe
- UI/read paths must not depend on counters being exact at commit time

### 7.2 Commutative aggregation preferred
Where possible, aggregation should prefer:
- commutative updates
- monotonic increments/decrements with reconciliation support
- replay-safe recomputation

This reduces sensitivity to:
- duplicate delivery
- out-of-order arrival
- replay after consumer failure

### 7.3 Exact counter truth is out of scope unless explicitly documented
If exact counters are ever required for a specific use case, that use case must document:
- stronger truth boundary
- stronger concurrency rules
- stronger reconciliation guarantees

Default V1 posture is eventual aggregates.

### 7.4 No global aggregate order is assumed
Interaction aggregates do **not** assume:
- one total order of all interaction events
- one globally linearizable counter stream

Where ordering matters, it is scoped to the interaction subject that actually needs it.

### 7.5 Rebuildable aggregate outputs
Aggregate outputs such as:
- counters
- popularity scores
- trending inputs
- summary tables

should be treated as rebuildable derived state by policy.

### 7.6 Candidate-before-publication
If a rebuild/reconciliation workflow produces a correctness-sensitive derived aggregate output:
- build candidate first
- validate candidate
- publish/cut over explicitly
- do not treat partial candidate state as complete active output

### 7.7 Late-arriving event handling
If a view/comment/like-derived event arrives late:
- it must not silently corrupt a newer aggregate result
- if logic is commutative, convergence should remain safe
- if logic is version- or window-sensitive, bounded recompute/reconciliation is preferred over trusting late arrival blindly

---

## 8) Replication mechanics (Outbox + async aggregation)

Interaction supports a synchronous entry plus async aggregation.

### 8.1 Events emitted (V1)
Typical events:
- `ArticleViewed` (optional)
- `ArticleLiked`
- `ArticleUnliked`
- `CommentCreated`
- `CommentEdited`
- `CommentDeleted`

Events are published via Outbox:
- action write + outbox record committed atomically

### 8.2 Aggregation consumer posture
Counters and scores are updated asynchronously by Worker:
- handler is at-least-once
- duplicates are expected
- stale delivery is possible
- aggregation must be replay-safe

### 8.3 Duplicate delivery vs stale delivery
Aggregation must handle both:

#### Duplicate delivery
Same message arrives again.  
Protection:
- `messageId` dedupe
- replay-safe update logic

#### Stale delivery
Older event arrives after newer aggregate state is already materialized.  
Protection:
- if strict ordering matters, use version/freshness metadata
- otherwise rely on commutative aggregation + reconciliation
- do not let stale aggregate materialization become correctness truth

### 8.4 Outbox is the causal boundary
For Interaction, Outbox is the durable bridge between:
- committed interaction truth
- downstream counter or side-effect propagation

This means:
- notifications, aggregates, and derived summaries must derive from committed truth
- delayed derived updates do not redefine whether the interaction really happened
- replay and reconciliation start from truth + Outbox, not from timing assumptions

### 8.5 Replay is a normal recovery path
If aggregation, counters, or trending fall behind:
- replay from outbox/raw inputs
- bounded rebuild
- reconciliation against truth/raw logs

are all valid and expected recovery tools.

They must remain:
- bounded
- observable
- rerun-safe
- subordinate to interaction truth

---

## 9) Ordering & conflict posture

### 9.1 Likes
Per `(ArticleId, UserId)`, like/unlike operations are ordered by truth state:
- only one active like state should exist at a time

If events include version/freshness metadata:
- consumers may enforce stronger ordering
otherwise:
- idempotency + convergent truth rules must still produce stable final state

### 9.2 Comments
Comments are append-first; edits/deletes are later truth transitions.

If projections are added later:
- use per-comment or per-thread ordering/freshness metadata where needed
- avoid naive timestamp-only overwrite logic

### 9.3 Prefix-consistency with downstream consumers
If Notifications or other consumers react to comments:
- comment truth must commit before downstream effect is sent
- if projection/update lags, fallback to truth should remain possible

### 9.4 No global total order across interactions
Interaction ordering guarantees do not imply:
- one total order across all likes and comments
- one total order across all articles
- one global sequence shared with other modules

Ordering is scoped to the interaction subject that actually needs it.

### 9.5 Rebuild/cutover ordering must not outrun fresher truth
If a candidate aggregate/summarization output is built from bounded input:
- cutover must not publish older knowledge over fresher interaction truth or fresher already-applied derived state
- stale candidates should be rejected, rerun, or explicitly bounded by snapshot semantics
- safe non-progress is preferred over unsafe stale overwrite

---

## 10) Eventual consistency for counters (expected)

Counters may lag because aggregation is async.

Policy:
- UI may display counters as eventually consistent
- prefer stable UX over exact real-time counts
- do not fail page render because counters are missing or stale

Lag budget (policy-level):
- counters can lag seconds to minutes under spikes
- backlog must be observable
- reconciliation must be possible

### 10.1 Derived summaries follow the same rule
Trending inputs, popularity scores, and summary tables are also eventually consistent unless explicitly promoted by future ADR.

They remain:
- derived
- rebuildable
- subordinate to interaction truth
- safe to omit if freshness is uncertain

---

## 11) Safe reconciliation after ambiguity

### 11.1 Timeout does not prove action failure
If a client times out during:
- like
- unlike
- comment create
- comment edit/delete

that timeout does **not** prove the truth change did not commit.

### 11.2 Reconciliation posture
Safe reconciliation should rely on:
- current like truth
- comment truth by authoritative id
- durable idempotency records if used
- raw events where stored durably
- aggregate state only as derived hint, not authority

### 11.3 Do not infer from counters
Do not use:
- likesTotal/commentsTotal
- cached reaction summaries
- delayed notification side effects
- stale popularity/trending outputs

as authority for whether the originating interaction truth committed.

Interaction truth is authoritative.

### 11.4 Rerun safety
Important aggregate replay/rebuild/reconciliation workflows must be safe to rerun on the same bounded input.

### 11.5 Full rebuild is acceptable when safer than partial repair
If a derived counter/summary set is cheap enough to regenerate from:
- raw views
- current like truth
- comment truth
- retained durable inputs

then full rebuild is preferred over fragile partial repair logic.

---

## 12) Coordination and ownership posture (Interaction)

### 12.1 Interaction does not require global singleton coordination by default
Ordinary Interaction correctness must not depend on:
- one global aggregation leader
- one process being “the only counter updater”
- startup order deciding ownership
- timeout-only assumptions about who is current

Interaction correctness should instead be achieved through:
- truth-store authority
- idempotent consumers
- replay-safe aggregation
- commutative updates where possible
- stale-event rejection where needed

### 12.2 If future ownership-sensitive workflows are introduced
If a future Interaction workflow truly requires one current owner
(for example exclusive rebuild of a hot aggregate partition or one-current trending calculator),
that workflow must define:
- ownership source of truth
- monotonic generation/fencing token
- resource-side rejection of stale owner actions

Naive leader/lock patterns are not acceptable.

### 12.3 Safe non-progress beats unsafe stale aggregate apply
If ownership is ambiguous for a correctness-sensitive rebuild/publication workflow, Interaction must prefer:
- delayed rebuild
- stale-owner rejection
- operator retry
- continued truth-first behavior

over unsafe dual publication of aggregates or stale overwrite.

---

## 13) Observability signals (Interaction-specific)

Minimum signals:
- like/unlike success/failure rate
- uniqueness-constraint violation rate (mapped to idempotent success or expected conflict)
- comment create/edit/delete rates and failures
- durable idempotency hits/conflicts for comment create if implemented
- view tracking enqueue/backlog if async
- aggregation backlog/lag and processing latency (P95/P99)
- dedupe hit rate for consumers
- stale-event reject/resync indicators if implemented
- reconciliation/rebuild activity for aggregates if used operationally
- candidate publication/cutover failures for important derived outputs
- ownership-generation mismatch count if future ownership-sensitive workflows are introduced

Logs should include:
- `correlationId`
- `actorUserId` where available (avoid unnecessary PII)
- `ArticleId`
- interaction action type
- `messageId` for async events
- idempotency outcome (`applied`, `deduped`, `conflict`, `replayed`) where relevant

---

## 14) Summary

Interaction correctness in V1 rests on fifteen rules:

1. Interaction truth owns likes/comments/raw events, while counters remain derived.  
2. Like/unlike must converge deterministically under retries and concurrency.  
3. View tracking is non-blocking and duplicates are acceptable unless stricter dedupe is explicitly introduced.  
4. Comment creation retries need explicit idempotency if duplicate comments are unacceptable.  
5. Aggregates must be replay-safe and must never become correctness truth.  
6. Timeout does not prove an interaction action failed.  
7. Safe reconciliation must query Interaction truth, not infer from counters or downstream effects.  
8. No global ordering or distributed transaction is assumed for Interaction workflows.  
9. Causal ordering matters only where a later effect depends on committed interaction truth.  
10. Aggregate outputs are rebuildable derived state.  
11. Important rebuild/reconciliation workflows must be rerun-safe.  
12. Candidate aggregate output must be validated before publication when correctness matters.  
13. Replay, duplicate delivery, and late arrival are normal and must remain safe.  
14. Full rebuild is acceptable when safer than fragile partial repair.  
15. Singleton/ownership semantics are not relied on unless explicitly protected by authoritative generation/fencing rules.