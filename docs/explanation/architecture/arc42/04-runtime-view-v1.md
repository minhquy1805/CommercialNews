# 04 — Runtime View (V1)

This section describes the **key V1 runtime scenarios** for CommercialNews.
Each scenario highlights:
- modules involved
- synchronous vs asynchronous boundaries
- stream-style derived-state propagation where relevant
- batch / rebuild / reconciliation boundaries where relevant
- replication mechanics (Outbox → Broker → Consumers)
- failure modes and expected behavior
- observability notes (what to log/measure)

> Related:
> - `03-building-blocks-modularity.md` (module boundaries & dependency rules)
> - `05-quality-requirements.md` (system-level characteristics)
> - `06-measurement-guide.md` (SLIs/SLOs)
> - `11-replication-v1.md` (replication rules)
> - `13-transactions-and-consistency-v1.md` (truth transaction boundaries, outbox atomicity, read-your-writes, async side effects)
> - `16-batch-processing-and-derived-data-v1.md` (batch lane, derived state, rebuild/reconciliation posture)
> - `17-dataflow-and-batch-workflows-v1.md` (workflow stages, materialization, publication/cutover)
> - `18-stream-processing-and-derived-state-v1.md` (system-wide stream/derived-state policy)
> - `19-stream-processing-runtime-v1.md` (canonical stream-runtime shape, lag, duplicate delivery, repair/rebuild hooks)
> - `quality/` (module characteristic profiles)

---

## Runtime lanes in V1

CommercialNews V1 operates through three complementary runtime lanes:

### Lane A — Synchronous request/response
Used for:
- truth writes
- truth-sensitive reads
- immediate user/admin outcomes

Examples:
- create draft
- publish / unpublish
- login / refresh / verify / reset
- list / detail reads
- role / permission changes

### Lane B — Asynchronous event-driven side effects and derived-state convergence
Used for:
- audit ingestion
- email delivery
- view aggregation signals
- cache invalidation
- reactive metadata/projection updates
- selective stream-style maintenance of derived outputs

This lane uses the standard replication contract:
- truth change + outbox in one local transaction
- Background Worker publishes to broker
- consumers are at-least-once and idempotent
- derived systems converge behind committed truth

### Lane C — Batch / rebuild / reconciliation
Used for:
- bounded aggregation
- replay / repair
- reconciliation
- archival / cleanup
- rebuild of derived outputs

This lane does not define core business success.
It exists to:
- keep derived state healthy
- recover after lag or partial failure
- regenerate candidate derived outputs
- produce reusable summaries or serving artifacts
- apply retention policy safely

---

## Shared V1 replication and stream-runtime rules (applies to all scenarios)

- **Truth writes are synchronous** and commit to the **primary** store.
- Cross-module side effects are **asynchronous** using:
  - **Outbox** write in the same transaction as the truth change
  - Background Worker publishes Outbox messages to the broker (RabbitMQ)
  - Consumers are **at-least-once** and MUST be **idempotent**
- Important derived systems are followers of truth and:
  - may lag
  - may be rebuilt
  - must not become hidden truth
- Batch / rebuild / reconciliation workflows use:
  - **bounded input**
  - **derived outputs**
  - **rerun-safe** behavior
  - **selective materialization**
  - **publication/cutover** only after candidate validation where output correctness matters
- Events SHOULD carry:
  - `messageId`, `eventType`, `aggregateId`, `version` (when ordering matters), `occurredAt`, `correlationId`
- If a derived system is stale, missing, or uncertain:
  - prefer safe fallback to truth
  - do not expose stale confidence as authoritative correctness

---

## Scenario 1 — Admin creates a draft and publishes an article

### Goal
Publish content safely with governance (authorization + audit), without blocking on non-critical subsystems.

### Modules involved
- Content Management
- Authorization
- Audit Trail (async consumer)
- Notifications (optional async consumer)
- SEO (reacts via events)
- Media (attachments already exist or are added separately)

### Flow (sync path)
1. Admin authenticates (Identity) and calls `CreateDraft`.
2. **Authorization** enforces policy for content creation.
3. **Content** creates the draft and records metadata (author, timestamps, status).
4. Admin updates draft content and metadata (optional repeated steps).
5. Admin calls `Publish`.
6. **Authorization** enforces publish permission.
7. **Content** validates lifecycle transition and persists the new state (PublishedAt set).

### Replication record (Outbox, atomic with publish)
8. In the same transaction as step 7, **Content writes an Outbox message**:
   - `ArticlePublished` with `messageId`, `articleId` (aggregateId), `version`, `occurredAt`, `correlationId`.

### Side effects (async path)
9. Background Worker publishes the outbox message to the broker and retries on transient failures.
10. **Audit Trail** consumes `ArticlePublished` and records an audit entry (actor, action, timestamp, correlationId).
11. **SEO** consumes `ArticlePublished` to ensure slug/canonical/meta correctness for the published article.
12. **Notifications** consumes `ArticlePublished` (optional) to notify subscribers of a new article.

### Batch / rebuild hooks
13. If reactive SEO or notification processing is delayed, bounded rebuild/reconciliation jobs may later:
   - detect missing derived effects
   - regenerate candidate derived output
   - apply repair or cutover safely

### Failure modes
- If broker publish fails: publish still succeeds; **outbox backlog grows** and must recover via retries.
- If Audit ingestion fails: publish still succeeds; audit backlog/lag becomes observable and must recover via retries/DLQ.
- If SEO processing is delayed: article may be readable, but SEO metadata may lag temporarily (policy-defined).
- If Notifications fail: publish succeeds; email is retried; **duplicates must be prevented** via idempotency keys or durable delivery records.
- If duplicate publish delivery occurs after retry/restart: downstream consumers must treat replay safely using `messageId` and/or `(aggregateId, version)`.
- If a later rebuild is needed: rebuild failure must not revert already committed publication truth.

### Observability notes
- Log `correlationId` across publish → outbox write → broker publish → async consumers.
- Metrics:
  - publish success/failure rate
  - outbox backlog count + **oldest pending age**
  - broker queue depth (ready/unacked) per consumer
  - audit ingestion success/failure + backlog
  - notification send success/failure + backlog + dedupe hits
  - SEO update lag / fallback rate (if measurable)
  - rebuild/reconciliation runs for delayed publish side effects (if implemented)

---

## Scenario 2 — Admin unpublishes an article with a reason

### Goal
Remove content from public view immediately, with governance and traceability.

### Modules involved
- Content Management
- Authorization
- Audit Trail (async)
- SEO (async)

### Flow (sync path)
1. Admin calls `Unpublish(articleId, reason)`.
2. **Authorization** enforces unpublish permission.
3. **Content** validates transition and sets article to a non-public state, recording the **reason** and timestamp.

### Replication record (Outbox, atomic with unpublish)
4. In the same transaction as step 3, **Content writes an Outbox message**:
   - `ArticleUnpublished` with `messageId`, `articleId`, `version`, `occurredAt`, `reason`, `correlationId`.

### Side effects (async path)
5. **Audit Trail** consumes `ArticleUnpublished` and records the action + reason.
6. **SEO** consumes `ArticleUnpublished` to remove/deprioritize indexability (policy-defined).

### Batch / rebuild hooks
7. If derived SEO-serving artifacts or caches remain stale beyond policy, bounded reconciliation/rebuild jobs may:
   - compare truth visibility against derived serving state
   - generate repair candidates
   - apply repair without redefining truth

### Failure modes
- **Audit/SEO delays must not re-expose unpublished content**.
- Even if async is delayed, the **read path MUST filter out unpublished content from the truth store** (visibility is enforced by truth, not by derived stores).
- If SEO cache/routing data lags, slug resolution must still not leak drafts/unpublished content.
- Reconciliation failure must not weaken the truth-backed hiding of unpublished content.
- If stale/unordered events arrive at a derived consumer, stale applies must be rejected or repaired, not accepted blindly.

### Observability notes
- Track unpublish reasons (controlled vocabulary if desired).
- Monitor audit completeness for unpublish actions.
- Metrics:
  - outbox backlog/age for unpublish events
  - SEO routing fallback rate (if applicable)
  - reconciliation mismatch count for visibility-related derived state (if implemented)

---

## Scenario 3 — Public user reads article list and opens an article by slug

### Goal
Provide fast and available reading under burst traffic while enforcing visibility correctness.

### Modules involved
- Reading Experience
- SEO (slug routing)
- Content Management (publication state)
- Media (primary media/attachments)
- Interaction (view tracking; must be non-blocking)

### Flow (sync path)
1. User requests article listing with paging/filter/sort.
2. **Reading** returns only public (Published) content, applying filter/sort semantics.
3. User opens an article by slug.
4. **SEO** resolves slug → articleId (fast routing path; cache-first by policy).
5. **Reading** fetches article detail (content + seo + media by policy) and renders response.

### Side effects (async / non-blocking)
6. **Interaction** receives a view tracking signal (must not block the response).
7. Interaction writes an Outbox message (or enqueue signal) for aggregation (policy-defined), processed asynchronously.

### Batch / rebuild hooks
8. Bounded aggregation jobs may later:
   - roll up raw view signals into daily/hourly summaries
   - produce trending inputs or derived counters
   - repair missing aggregates if async consumers lagged or failed

### Failure modes
- If Interaction is down: reading still works; view counting lags.
- If Media is partially unavailable: degrade gracefully (fallback image/placeholder policy).
- If SEO resolution fails: return a safe not-found (must not leak drafts).
- **Consistent prefix rule:** if slug resolves but derived content/SEO metadata is not caught up,
  the system must **fallback to truth store** (still enforcing Published visibility) rather than returning misleading 404/empty.
- If aggregation/rebuild is delayed: counters or trending may lag, but public truth visibility must remain correct.
- If interaction analytics later use event-time windows, late events and replay must be handled by explicit policy, not hidden assumptions.

### Observability notes
- Metrics:
  - P95/P99 latency for list/detail
  - error rate for list/detail
  - SEO slug lookup latency/error rate
  - SEO DB fallback rate (cache miss / stale fallback)
  - interaction backlog/lag (if async)
  - aggregate freshness age / rebuild lag for reading-derived outputs (if implemented)

---

## Scenario 4 — User registration and email verification

### Goal
Secure onboarding without blocking on email delivery and with abuse controls.

### Modules involved
- Identity & Access
- Notifications (async)
- Audit Trail (optional async)

### Flow (sync path)
1. User calls `Register`.
2. Identity validates input and creates account in unverified state.
3. Identity commits the user and writes an Outbox message:
   - `UserRegistered` (or `VerificationRequested`) with `messageId`, `userId`, `correlationId`.

### Side effects (async path)
4. Notifications consumes the event and sends verification email (idempotent; duplicates prevented).
5. User clicks verification link; Identity verifies the account (sync) and writes an Outbox message:
   - `UserEmailVerified`.

### Batch / rebuild hooks
6. Replay/cleanup jobs may later:
   - identify stuck or repeatedly failed delivery attempts
   - summarize delivery behavior
   - clean retention-managed reset/verification artifacts by policy

### Failure modes
- If email sending fails: registration still succeeds; retries occur; duplicates prevented.
- If broker publish fails: registration still succeeds; outbox backlog grows and retries.
- Abuse: resend verification must be rate-limited.
- **Read-your-writes rule:** after verification succeeds, self-state reads must reflect verified status (primary read/bypass caches).
- If send retry/redelivery occurs after a worker crash, duplicate harmful sends must still be prevented.

### Observability notes
- Track:
  - register success/failure rate
  - resend verification rate-limit triggers
  - outbox backlog/age for identity events
  - email send success/failure + backlog + dedupe hits
  - replay/cleanup backlog for identity-related delivery artifacts (if implemented)

---

## Scenario 5 — Forgot password and reset password

### Goal
Account recovery is secure, rate-limited, and reliable under attack/burst conditions.

### Modules involved
- Identity & Access
- Notifications (async)

### Flow (sync path)
1. User calls `ForgotPassword`.
2. Identity creates a time-bound reset request and writes an Outbox message:
   - `PasswordResetRequested` (must not leak account existence by response shape).
3. Notifications sends reset email asynchronously (idempotent).
4. User submits `ResetPassword(token, newPassword)`.
5. Identity validates token and updates credentials (sync), then writes an Outbox message:
   - `UserPasswordChanged`.

### Batch / rebuild hooks
6. Cleanup/replay jobs may later:
   - expire old reset artifacts by policy
   - summarize delivery or failure patterns
   - repair retryable notification gaps where appropriate

### Failure modes
- Duplicate requests: must not leak account existence and must be rate-limited.
- Token misuse: token must expire and be single-use by policy.
- Broker publish failures: user reset still succeeds; outbox backlog grows and retries.
- **Read-your-writes rule:** after password change/reset, self-state and auth flows must reflect the new credential state.
- Cleanup/replay lag must not weaken identity/security truth.
- Duplicate email delivery attempts after retry/replay must be deduped durably.

### Observability notes
- Track reset request volume, rate-limit triggers, failures, and anomalies.
- Metrics:
  - outbox backlog/age for reset events
  - email send failures + dedupe hits
  - cleanup/replay lag for reset-related operational artifacts (if implemented)

---

## Scenario 6 — Admin assigns a role/permission and audit is recorded

### Goal
Enforce least privilege and maintain governance traceability.

### Modules involved
- Authorization
- Identity (userId reference)
- Audit Trail (async)

### Flow (sync path)
1. Admin calls `AssignRole(userId, role)` or `GrantPermission(role, permission)`.
2. Authorization validates admin permission to change governance state.
3. Authorization persists the change (sync) and writes an Outbox message:
   - `RoleAssigned` / `PermissionGranted` with `messageId`, `aggregateId`, `version`, `correlationId`.

### Side effects (async path)
4. **Audit Trail** consumes the event and persists an audit record (idempotent, retry-safe).

### Batch / rebuild hooks
5. Reconciliation/reporting jobs may later:
   - detect governance changes missing from derived audit/reporting views
   - archive or summarize governance audit windows
   - repair delayed derived visibility of governance actions

### Failure modes
- If audit ingestion is delayed: governance changes still apply, but backlog must be visible and recoverable.
- **Read-your-writes for admin governance:** admin reads after changes must reflect current policy (primary read/bypass caches).
- Derived reporting lag must not affect current governance truth.
- If stale or out-of-order governance events arrive in derived consumers, they must be rejected or resynced safely.

### Observability notes
- Policy coverage for governance endpoints
- Audit completeness for role/permission changes
- Metrics:
  - outbox backlog/age for governance events
  - audit ingestion failures + DLQ rate
  - reconciliation mismatch count for governance-derived outputs (if implemented)

---

## Scenario 7 — Scheduled aggregation / rebuild / reconciliation workflow

### Goal
Maintain derived state quality, freshness, and recoverability without redefining truth or blocking user-facing flows.

### Modules involved
- Background Worker
- Reading Experience / Interaction (for aggregates and derived counters)
- SEO (for rebuild/reconciliation of derived serving artifacts)
- Audit / Notifications (for archival, replay, cleanup, and summary workflows)
- Content / Identity / Authorization (truth sources when bounded snapshots are needed)

### Flow (batch lane)
1. Scheduler or operator triggers a bounded workflow.
2. Workflow selects explicit bounded input:
   - time window, snapshot boundary, or bounded candidate set.
3. Workflow performs one or more internal stages:
   - normalize / prepare
   - group / partition
   - aggregate / compare / transform
   - generate candidate output
4. Candidate output is validated according to workflow policy.
5. If validation succeeds, workflow publishes/cuts over derived output or applies bounded repair.
6. Temporary internal state is cleaned up or retained according to policy.

### Examples
- aggregate article views for a day
- generate trending inputs
- reconcile published articles against derived SEO-serving state
- archive or summarize audit windows
- replay / cleanup notification delivery artifacts
- repair missing derived entries after consumer lag or outage

### Failure modes
- Workflow failure must not corrupt truth.
- Partial candidate output must not be treated as complete active output.
- If ownership/exclusivity is required, workflow must follow ADR-0024:
  - explicit ownership
  - stale-owner rejection
  - safe non-progress over unsafe double-apply
- If a workflow is rerun on the same bounded input, behavior must be harmless or explicitly controlled by publication/versioning rules.
- Repair/rebuild remains a **derived-state recovery lane**; it must not silently redefine truth ownership.

### Observability notes
- Metrics:
  - run success/failure count
  - run duration
  - current stage / last completed stage
  - records selected / processed / skipped / repaired
  - candidate publication success/failure
  - freshness age of active derived output
  - replay/rebuild backlog age
  - stale-owner rejection / duplicate-run detection where applicable

---

## Runtime posture summary (V1)

Across all scenarios, CommercialNews V1 follows these runtime rules:

- **Truth commits define core success**
- **Outbox is the standard async boundary**
- **Consumers are at-least-once and idempotent**
- **Read-your-writes is explicit for truth-sensitive flows**
- **Derived state may lag, but must be observable and recoverable**
- **Important derived systems must have a replay, rebuild, or reconciliation posture**
- **Batch uses bounded input and produces derived output**
- **Candidate output must be validated before publication when output correctness matters**
- **Partial derived output must not be treated as complete**
- **Truth-safe fallback beats stale derived confidence**
- **If ownership is ambiguous, prefer safe non-progress over unsafe dual apply**
- **RabbitMQ delivery does not replace truth-owned history or rebuild discipline**
