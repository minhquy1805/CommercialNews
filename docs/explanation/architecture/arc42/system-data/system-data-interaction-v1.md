# System Data Model — Interaction (V1)

> **Recommended file:** `explanation/architecture/arc42/system-data/system-data-interaction-v1.md`  
> **Module:** Interaction  
> **Purpose:** Track views/likes/comments **without blocking** the public read path, and expose stable counters for popularity sorting.

---

## 0) Data System fit (V1)

Interaction is the **read-path risk module**: it can produce high write volume and must never degrade public list/detail.

- **Truth store:** SQL Server (likes/comments as OLTP entities; stats projection as stable read support)
- **High-write log:** `ArticleViewEvent` is append-only and retention-driven (keep indexes minimal)
- **Redis:** counters (views/likes/comments deltas), short-lived dedup/throttle keys, and optional “already viewed recently” keys
- **Async:** aggregation updates `ArticleInteractionStats` (eventual consistency)

**Non-negotiables (from Quality Requirements)**
- Read path remains fast under burst traffic.
- Interaction failures must not break reading (graceful degradation).
- Background processing must be retry-safe and idempotent.

---

## 1) Scope & boundaries (V1)

### In scope (V1)
- View tracking (non-blocking)
- Like/unlike (idempotent per user)
- Comments (basic lifecycle; moderation hooks later)
- A stable projection for totals + popularity sorting

### Cross-module references
- Interaction references `Content.Article` by `ArticleId`.
- Interaction references `Identity.User` by `UserId` (for likes/comments; views can be anonymous).

---

## 2) Capability → Entity mapping

### 2.1 View tracking
**Entities**
- `ArticleViewEvent` — append-only, high-write log
- `ArticleInteractionStats` — materialized counters updated async

**V2 hook**
- Unique view policy: `ViewUniqueKey` / `ViewMeasurementPolicy`

---

### 2.2 Likes (like/unlike + totals)
**Entities**
- `ArticleLike` — unique per `(ArticleId, UserId)`; idempotent
- `ArticleInteractionStats` — totals (likes)

**V2 hook**
- `Reaction` (like/dislike/emoji)

---

### 2.3 Comments (CRUD + moderation V2)
**Entities**
- `Comment` — lifecycle + visibility
- V2 hooks: `CommentModerationAction`, `CommentReport/SpamSignal`

---

## 3) Workload & hot paths (V1) (DDIA Ch3)

### 3.1 Public read dependency policy
- Public read path should depend only on:
  - `Article` (Content truth)  
  - optional: `ArticleInteractionStats` (safe fallback to 0 when missing)
- Public read must **not** depend on `ArticleViewEvent`.

### 3.2 Hot writes
- Views can spike heavily (hot articles).
- Likes/comments are lower than views but still potentially bursty.

### 3.3 Admin/moderation (V2+)
- In V1, moderation is minimal; keep lifecycle fields for future governance.

---

## 4) Dataflows (V1) — REST / DB / Broker (DDIA Ch4)

### 4.1 View tracking (non-blocking)
**Goal:** record a view without slowing article detail.

**Recommended V1 tactic**
- API emits a view signal asynchronously (queue/event) OR writes a lightweight record and returns immediately.
- If Interaction is down, article read still succeeds.

**Pipeline options (pick one; both are compatible)**
- **Option A (simplest):** insert `ArticleViewEvent` async in Worker (recommended)
- **Option B (direct write):** API inserts `ArticleViewEvent` synchronously but with strict time budget and minimal indexes

### 4.2 Like/unlike (idempotent)
- API upserts like state:
  - Like: ensure row exists and `IsActive=1`, set `LikedAt`
  - Unlike: set `IsActive=0`, set `UnlikedAt` (no delete)
- Stats update happens asynchronously (eventual).

### 4.3 Comment create/edit/delete (soft)
- Create comment → visible/pending based on policy
- Edit increments `EditCount`, sets `UpdatedAt`
- Delete is soft (`Status='Deleted'`, `DeletedAt`, `DeletedBy`)
- Public reads only `Status='Visible'`.

### 4.4 Stats projection update (async aggregation)
- Worker aggregates deltas from:
  - view events or redis counters
  - active likes
  - visible comments
- Updates `ArticleInteractionStats` periodically (e.g., every 1–5 minutes) or event-driven.

**Failure behavior**
- If aggregation is delayed: counters are stale; reads still succeed.
- If stats row missing: treat totals as 0.

---

## 5) Redis plan (Interaction V1) — protect OLTP and enable burst handling

> Redis is derived; it reduces write pressure and supports safe retries.

### 5.1 Counters (recommended for views)
- `cn:article:{articleId}:views` INCR (hot path)
- (optional) `cn:article:{articleId}:likes_delta` INCR/DECR
- (optional) `cn:article:{articleId}:comments_delta` INCR

**Flush policy**
- Worker flushes counters to `ArticleInteractionStats` in batches (1–5 minutes)
- After flush: reset counters (atomic get+del pattern)

### 5.2 View throttling / “recent view” keys (optional V1)
To reduce spam/bots inflating views:
- `cn:viewed:{articleId}:{fingerprint}` TTL 30–300s  
Where fingerprint can be:
- userId (if logged in), else
- sessionId/cookie id, else
- ip hash + user-agent hash (privacy-aware)

If key exists → skip counting view (policy).

### 5.3 Dedup for async processing (required)
- `cn:msg:processed:{messageId}` TTL 7–30 days  
Used if views/likes/comments events are processed via broker/worker.

---

## 6) Identify entities (V1)

### V1 must-have
1. `ArticleViewEvent`
2. `ArticleLike`
3. `Comment`

### V1 highly recommended
1. `ArticleInteractionStats` *(projection)*

### V2 hooks
- `ViewUniqueKey`, `CommentModerationAction`, `CommentReport`, `Reaction`

---

## 7) Relationships (V1)

- `Article (1) → ArticleViewEvent (0..N)` (UserId nullable)
- `Article (1) → ArticleLike (0..N)` unique per `(ArticleId, UserId)`
- `Article (1) → Comment (0..N)` with optional `ParentCommentId`
- `Article (1) → ArticleInteractionStats (0..1)` optional projection (missing = zeros)

---

## 8) Invariants (V1 rules)

### 8.1 Non-blocking read path
- Tracking failures must not break list/detail.
- Public pages must not depend on view log availability.

### 8.2 View semantics
- V1 view: one count per “detail load” (optionally throttled).
- View events are append-only (except retention policy).

### 8.3 Like semantics (idempotent)
- Double-like does not create duplicates.
- Unlike when not liked is a no-op.
- Uniqueness: `(ArticleId, UserId)` at most one row.

### 8.4 Comment lifecycle
- Status ∈ `Visible | Hidden | Deleted | Pending`
- Public reads only `Visible`
- Hard delete discouraged; keep governance hooks.

### 8.5 Counter consistency
- Stats are eventually consistent
- Counters must be non-negative
- Views should be monotonic in projection (policy)

---

## 9) Fields (Logical schema) — SQL Server (V1)

### 9.1 `ArticleViewEvent`
*(unchanged — keep your table as-is)*

### 9.2 `ArticleLike`
*(unchanged — toggle IsActive is a good V1 tactic)*

### 9.3 `Comment`
*(unchanged)*

### 9.4 `ArticleInteractionStats` (projection)
*(unchanged)*

---

## 10) Constraints & indexes — Interaction (V1)

### 10.1 PK / FK / UNIQUE / CHECK
*(keep your current list; it is solid)*

### 10.2 Index guidance (DDIA Ch3)
- Keep `ArticleViewEvent` indexes minimal (high-write).
- Ensure `ArticleLike` unique constraint for idempotency.
- Ensure `Comment` index supports paging by article and status.
- Stats index supports popularity sort.

---

## 11) Aggregation strategy (V1) — from signals to stats

### 11.1 Minimal viable pipeline (recommended)
- Views:
  - `Redis INCR` on read
  - Worker flushes into `ArticleInteractionStats.ViewsTotal`
- Likes:
  - compute delta based on `ArticleLike.IsActive` changes (event-driven) or periodic recount
- Comments:
  - increment on create; decrement if hidden/deleted (policy)

### 11.2 Correctness trade-offs
- Exact real-time stats are not required in V1.
- Prefer predictable read performance over perfect immediacy.

---

## 12) Evolution rules (V1) — safe change over time (DDIA Ch4)

- Add-only fields + defaults
- If introducing unique-view policies, add `UniqueKeyHash` without breaking existing counters
- Never change meanings of existing counters; add new metrics instead (e.g., `ViewsUniqueTotal`)

---

## 13) Retention & operational jobs (V1 policy)

### 13.1 View log retention
`ArticleViewEvent` will grow quickly. Define:
- retention window (e.g., 7–30 days raw)
- purge job by `ViewedAt` (uses `IX_ViewEvent_ViewedAt`)

### 13.2 Login/audit alignment
If audit requires interaction governance later (moderation actions), store moderation logs append-only (V2).

---

## 14) V2 hooks (interaction evolution)
- Unique views: `UniqueKeyHash` from (UserId/session/window) + policy version
- Moderation: `CommentModerationAction` append-only
- Anti-spam: reports/signals + throttling + shadow-ban
- Reactions: generalize to `Reaction(Type)`

---

## 15) Partitioning Readiness (V1/V2)

> This section captures **partitioning and hotspot-readiness** for Interaction.
> V1 remains **non-sharded by default**; the goal is to protect the read path and define safe scale options.

### 15.1 Why Interaction is a partitioning-risk module

Interaction is the highest-risk module for **write bursts** and **hot keys**:

* hot articles can generate large view spikes in minutes
* likes/comments may burst during trending events
* counters are user-visible and frequently requested by read paths

**V1 principle:** optimize for **read-path protection** first, not perfect real-time counters.

---

### 15.2 Primary access patterns (V1)

**Hot paths**

* `TrackView(articleId, ...)` (non-blocking)
* `Like/Unlike(articleId, userId)` (idempotent)
* `CreateComment/EditComment/DeleteComment` (OLTP)

**Read dependencies**

* Public read path may read `ArticleInteractionStats` (optional)
* Public read path must **not** read `ArticleViewEvent`

**Admin/moderation (V2+)**

* comment lifecycle review / moderation history / abuse signals (secondary-index-heavy later)

---

### 15.3 Secondary-index-heavy queries (present and future)

**V1**

* comment paging by `(ArticleId, Status, CreatedAt)`
* like existence lookup by `(ArticleId, UserId)` (idempotency path)

**V2+**

* moderation queues (by `Status`, `Flag`, `CreatedAt`)
* abuse investigation/search (user/article/time-window)
* reaction analytics/trending projections

**Implication**

* V1 stays OLTP + projection-based
* V2+ may require dedicated moderation/search projections before any truth-store sharding

---

### 15.4 Candidate partitioning strategy (future)

Partitioning choices depend on **sub-workload**, not one rule for all Interaction data.

#### A) `ArticleViewEvent` (append-only, high-write)

**Likely fit:** **range/hybrid**

* range by time (retention/purge/replay friendly), with hotspot caution
* hybrid option (bucket + time) if current-range hotspot appears

**Risk**

* pure time-range partitioning can create a hot “current” partition during spikes

#### B) `ArticleInteractionStats` (projection)

**Likely fit:** workload-driven / projection partitioning

* often better solved by Redis + async aggregation lanes before DB sharding
* if scaled later, partition by article/bucket depending query patterns

#### C) `ArticleLike` and `Comment` (OLTP)

**Likely fit:** defer DB partitioning in V1

* prioritize indexes, idempotency constraints, bounded reads
* consider hybrid/projection strategies first for heavy analytics/moderation paths

---

### 15.5 Hotspot and skew risks (V1)

#### A) Hot keys (most important)

* viral article → `articleId` becomes a hot key
* hashing alone does **not** solve this if all writes target the same `articleId`

#### B) Write skew

* view spikes concentrated on a small number of hot articles
* retry storms can amplify pressure if async consumers fail/retry aggressively

#### C) Read skew

* top/trending articles repeatedly request the same counters/stats rows

---

### 15.6 V1 mitigations (no sharding yet)

CommercialNews V1 already applies the correct mitigations for Interaction:

* **Non-blocking tracking** (tracking failure must not break reading)
* **Redis counters** for burst absorption (`INCR` on hot path)
* **Async flush/aggregation** into `ArticleInteractionStats`
* **Minimal indexes** on `ArticleViewEvent` (high-write table)
* **Idempotent processing + dedup keys** for at-least-once workflows
* **Safe fallback semantics**: missing stats row => zeros (read path still works)

These tactics are preferred before introducing shard complexity.

---

### 15.7 V2+ scale options (selective)

Introduce stronger partitioning only when signals justify it.

#### Option A — Sharded counters / salted keys (for hot articles)

For extremely hot `articleId`s:

* split one logical counter into multiple shards (e.g., `views:{articleId}:{bucket}`)
* aggregate asynchronously or on read with cache

**Trade-off**

* write scale improves
* reads/aggregation become more complex

#### Option B — Aggregation lanes / ownership partitioning

Partition async aggregation work by:

* `articleId` hash bucket, or
* logical lane id

This is often the **first scalable step** before DB sharding.

#### Option C — Time/bucket partitioning for raw view events

Use range/hybrid partitioning if:

* retention/purge/replay operations become too expensive
* raw event volume materially impacts operability/recovery

---

### 15.8 Rebalancing and routing readiness (future)

Interaction scaling will likely require **workload rebalancing** before truth-table sharding.

**Likely rebalance unit**

* aggregation lane / bucket (worker ownership)
* later: event partitions or time buckets

**Routing requirement**

* authoritative mapping for `lane/bucket -> worker owner`
* safe reassignment (throttled, observable)

**Guardrail**

* rebalance or concurrency increases must not degrade public read P95/P99

---

### 15.9 Partition-readiness observability signals (Interaction)

Use existing V1 measurement signals to decide when stronger partitioning is needed:

* interaction backlog / aggregation lag
* worker processing latency P95/P99
* consumer failure/retry rate
* dedupe hits / idempotency anomalies
* read path P95/P99 during hot-article spikes
* Redis counter flush delays / batch lag (if measured)
* missing stats fallback frequency (if measured)

**Scale trigger (policy-level)**
Consider stronger workload/data partitioning when sustained spikes cause:

* growing lag/backlog that does not self-recover
* read-path degradation despite current Redis + async aggregation tactics
* recovery/replay times becoming operationally unsafe

---

## 16) ERD (dbdiagram.io)

See: `../diagrams/erd/interaction-v1.dbml`

How to render:

1. Open dbdiagram.io
2. Copy DBML content from the file above
3. Paste into dbdiagram.io to view/export