# Interaction — Idempotency & Consistency (V1)

This document defines Interaction command idempotency and consistency rules for V1.
It focuses on async consumer safety, concurrency/version rules, counter consistency, moderation/report workflow safety, and recovery posture.

Related:

- `01-api-surface.md`
- `02-domain-contracts.md`
- `03-runtime-flows.md`
- `04-errors-status-codes.md`
- `05-security-abuse-controls.md`
- `07-observability-slos.md`
- `08-dependencies-and-ownership.md`
- `09-open-questions.md`
- `10-business-rules.md`
- ADR-0013 — Outbox & Delivery Semantics (V1)
- ADR-0018 — Transaction Boundaries & Consistency Model (V1)
- ADR-0019 — System Model and Fault Assumptions (V1)
- ADR-0020 — Timeout, Retry, and Failure Detection Policy (V1)
- ADR-0022 — Versioning and Fencing Strategy (V1)
- ADR-0025 — Batch Processing and Derived State Policy (V1)
- ADR-0027 — Stream Processing and Derived State Policy (V1)
- ADR-0028 — Consumer Idempotency, Replay, and Rebuild Policy (V1)

---

## 1) Consistency Posture

Interaction owns engagement and moderation workflow state around public articles.

Interaction V1 must remain correct under:

- client retry;
- API timeout after commit;
- concurrent user commands;
- concurrent moderator commands;
- duplicate message delivery;
- stale or out-of-order async messages;
- consumer restart;
- replay and bounded reconciliation;
- delayed Reading, Audit or Notifications processing.

### 1.1 Core correctness rule

```text
Interaction truth/workflow mutation succeeds when Interaction-owned state
and required outbox intent commit atomically.

Downstream projection, audit ingestion and email delivery are asynchronous
and may complete later.
```

### 1.2 No exactly-once assumption

Interaction does not assume exactly-once delivery.

The required outcome is:

```text
Effectively-once business outcome under at-least-once message delivery.
```

That outcome is achieved through:

- authoritative business invariants;
- conditional state transitions;
- durable message dedupe;
- explicit versions;
- known-value snapshot publication;
- reconciliation of derived state.

---

## 2) Truth, Workflow State and Derived State

### 2.1 Authoritative Interaction truth/workflow state

```text
ArticleLike
Comment
CommentReport
CommentModerationCase
CommentModerationActionHistory
```

| State | Correctness authority |
|---|---|
| `ArticleLike` | Whether one authenticated user currently has an active like for an article |
| `Comment` | Comment content and current status |
| `CommentReport` | Whether a valid report was submitted |
| `CommentModerationCase` | Whether one report-review cycle is open/resolved and whether alert intent was triggered |
| `CommentModerationActionHistory` | Required local operational record of moderation actions |

### 2.2 Derived and processing state

```text
ArticleInteractionTargetProjection
ArticleViewCount
ArticleInteractionStats
InteractionConsumedMessage
```

| State | Consistency posture |
|---|---|
| `ArticleInteractionTargetProjection` | Content-derived eligibility projection; may lag; fails closed when unsafe |
| `ArticleViewCount` | Durable accepted-view materialized count; no raw per-view rebuild in V1 |
| `ArticleInteractionStats` | Public counter snapshot state; may lag; published to Reading |
| `InteractionConsumedMessage` | Durable consumer processing/dedupe state |

### 2.3 Derived-state rule

Derived state must never be treated as the authority for:

- whether an article is published in Content;
- whether a user has an active like;
- whether a comment is visible;
- whether a report exists;
- whether a moderation case was resolved;
- whether an email alert intent was already created.

---

## 3) Consistency Classes

Interaction intentionally uses multiple consistency classes.

### 3.1 Strong local consistency

Required for:

```text
ArticleLike relationship mutation
Comment creation and status transition
CommentReport creation
CommentModerationCase creation/resolution/alert trigger
CommentModerationActionHistory append where required
Outbox intent committed with relevant local mutation
```

### 3.2 Version- and transition-sensitive consistency

Required for:

```text
Comment moderation transitions
Comment author deletion where concurrent moderation may occur
CommentModerationCase resolution
CommentModerationCase alert triggering
Content-derived eligibility projection apply
ArticleInteractionStats outbound snapshot publication
```

### 3.3 Eventual consistency

Accepted for:

```text
ViewCount publication to Reading
LikeCount materialization
VisibleCommentCount materialization
Reading public counter projection
Audit event ingestion
Notifications email delivery
Counter reconciliation and republish
```

### 3.4 Freshness authority

```text
Explicit version/state-transition rules determine correctness freshness.

Timestamps do not determine ordered correctness.
```

Timestamps may be used only for:

- diagnostics;
- display;
- lag measurement;
- retention;
- investigation.

---

## 4) Core Invariant Matrix

| Concern | Authoritative invariant / guard |
|---|---|
| Article interaction eligibility | New interaction requires safe local enabled projection |
| Active like | At most one active like per `(ArticlePublicId, UserId)` |
| V1 comment creation | `ParentCommentId = NULL` |
| Comment moderation | Legal transition plus `Comment.Version` protection |
| Comment author deletion | Ownership plus legal/status/version handling |
| Comment report | At most one report per `(CommentId, ReporterUserId)` |
| Open moderation case | At most one `Open` case per comment |
| Admin alert intent | At most one alert business intent per open case in V1 |
| Content projection consume | Durable message dedupe plus `LastSourceVersion` apply |
| Counter snapshot publication | Monotonic `ArticleInteractionStats.StatsVersion` |
| Consumer delivery | Durable dedupe by `(ConsumerName, MessageId)` |
| Accepted view count | Atomic increment only after policy acceptance |

---

## 5) Article Eligibility Consistency

### 5.1 Local projection rule

Interaction accepts new public interactions only through local eligibility state:

```text
ArticleInteractionTargetProjection.IsInteractionEnabled = true
AND ArticleInteractionTargetProjection.RequiresResync = false
```

Interaction does not synchronously query Content or Reading for ordinary interaction commands in V1.

### 5.2 Inbound Content apply rule

Conceptual input:

```text
content.article_read_projection_published
```

Apply logic:

```text
If (ConsumerName, MessageId) was already applied:
    ignore duplicate delivery.

If IncomingSourceVersion <= LastSourceVersion:
    ignore stale/already-known source state.

If IncomingSourceVersion is valid forward progress:
    update ArticleInteractionTargetProjection.

If a correctness-significant gap or uncertainty is detected:
    mark/respect projection as unsafe;
    reject new public interaction;
    require reconciliation/resync.
```

### 5.3 Fail-closed behavior

If Interaction cannot safely prove current eligibility:

```text
Do not accept new view contribution.
Do not accept new like.
Do not accept new comment.
Do not accept new report.
```

### 5.4 Article becomes non-public

When the source projection marks an article as unpublished, archived or soft-deleted:

- disable new public interactions;
- preserve active likes unless users retract them;
- preserve comments and statuses;
- preserve reports, moderation cases and history;
- preserve `ArticleViewCount`;
- preserve `ArticleInteractionStats`;
- allow unlike of an existing user-owned like;
- allow deletion of an existing user-owned comment;
- allow authorized resolution of existing open moderation cases.

### 5.5 Counter preservation rule

```text
Article visibility lifecycle changes do not reset Interaction counters.

Counter lifetime follows stable article identity, not publish/unpublish cycles.
```

---

## 6) Article View Count Consistency

### 6.1 V1 model

Interaction V1 uses:

```text
ArticleViewCount
```

V1 does not store one durable row per individual page view.

### 6.2 View acceptance rule

A request contributes to `ArticleViewCount` only after:

- local article eligibility confirmation;
- request validation;
- configured rate-limit checks;
- configured repeat-view suppression / anti-inflation evaluation;
- applicable abuse rejection checks.

### 6.3 Atomic mutation rule

Accepted concurrent views are valid independent increments.

The authoritative write must be atomic:

```text
ViewCount = ViewCount + 1
ViewVersion = ViewVersion + 1
```

Unsafe pattern:

```text
SELECT current ViewCount
increment in application memory
UPDATE stale value
```

### 6.4 View retry posture

A client retry after timeout does not prove the earlier view attempt was not counted.

Because V1 does not model strict per-view idempotency as user-visible truth, retry handling is governed by configured anti-abuse/repeat-view suppression policy.

### 6.5 No per-view Reading propagation

Interaction must not publish one outbound Reading message per accepted view.

Instead:

```text
ArticleViewCount
    -> ArticleInteractionStats materialization
    -> interaction.article_counters_projection_published
    -> Reading
```

### 6.6 View recovery limitation

Because V1 does not retain raw individual view history:

```text
ViewCount cannot be exactly recomputed from one-row-per-view truth in V1.
```

Correctness posture relies on:

- durable `ArticleViewCount`;
- atomic mutation;
- operational monitoring;
- backup/recovery;
- public counter snapshot republish where required.

A future bucketed or analytics-oriented view pipeline may be added without changing module ownership.

---

## 7) Like / Unlike Idempotency and Concurrency

### 7.1 Like truth

`ArticleLike` is the authoritative relationship state.

```text
At most one active ArticleLike per (ArticlePublicId, UserId).
```

### 7.2 New-like eligibility

Creating/reactivating a like requires:

- authenticated actor;
- trusted actor identity from request context;
- locally confirmed interaction-enabled article.

### 7.3 Concurrent like behavior

| Situation | Required outcome |
|---|---|
| Different users like the same article concurrently | Independent valid successes |
| Same user likes the same article concurrently | At most one active relationship |
| Same user retries after timeout | No duplicate active relationship or duplicate business event |

### 7.4 Idempotent like response posture

The API should use one stable policy:

```text
Already active like -> liked=true idempotent success
```

or a documented consistent conflict policy if required by the global API style.

The recommended user-facing V1 posture is idempotent success.

### 7.5 Unlike behavior

Unlike retracts only the acting user's active like.

Unlike may remain allowed after an article becomes non-public because it removes user-owned engagement instead of adding new public engagement.

Repeated unlike must converge safely:

```text
Already inactive / absent like -> liked=false idempotent success or documented no-op.
```

### 7.6 Counter behavior

Like/unlike truth changes do not require public counter freshness before API success.

```text
ArticleLike truth commits first.
LikeCount in ArticleInteractionStats converges asynchronously.
```

### 7.7 No article-wide truth lock

Many users liking one hot article must not require every request to perform a stale-write-sensitive update on one authoritative article-wide like total.

Like correctness is protected at the relationship boundary:

```text
(ArticlePublicId, UserId)
```

---

## 8) Comment Creation Idempotency

### 8.1 V1 creation rules

Comment creation requires:

```text
Authenticated actor
Interaction-enabled article
Valid content
ParentCommentId = NULL
Initial Status = Pending
```

### 8.2 Reply compatibility constraint

Although `Comment.ParentCommentId` may exist as a nullable reserved field:

```text
Every V1-created Comment must have ParentCommentId = NULL.
```

V1 create-comment APIs must not accept parent-comment input.

### 8.3 Timeout ambiguity

If the client times out after create-comment request:

```text
The comment may already exist in Pending status.
```

### 8.4 Recommended idempotency-key posture

Production-oriented V1 should support an idempotency key for comment creation.

If supported:

| Situation | Required behavior |
|---|---|
| Same key and same semantic payload replayed | Return same logical created-comment result |
| Same key reused with conflicting payload | Deterministic conflict |
| First request committed but response timed out | Retry returns committed result without new comment |

### 8.5 Counter behavior

Creating a `Pending` comment does not update `VisibleCommentCount`.

---

## 9) Comment Moderation Consistency

### 9.1 Comment statuses

```text
Pending
Visible
Rejected
Hidden
Deleted
```

### 9.2 Valid moderator transitions

| Current status | Command | Next status |
|---|---|---|
| `Pending` | Approve | `Visible` |
| `Pending` | Reject | `Rejected` |
| `Visible` | Hide | `Hidden` |
| `Hidden` | Restore | `Visible` |

### 9.3 State legality rule

A fresh version does not make an illegal transition legal.

Interaction must enforce both:

```text
Current status permits command
AND expected/current Version permits mutation
```

### 9.4 Moderation atomic commit

A successful moderation transition requiring propagation commits atomically:

```text
Comment status/version transition
+ required CommentModerationActionHistory row
+ required OutboxMessage
```

Examples:

| Command | Required local history | Event intent |
|---|---|---|
| Approve | `Approve` | `interaction.comment_approved` |
| Reject | `Reject` | `interaction.comment_rejected` |
| Hide without report case | `Hide` | `interaction.comment_hidden` |
| Restore | `Restore` | `interaction.comment_restored` |

### 9.5 Stale/retry protection

If a moderator retries after timeout or acts from stale UI:

- completed transition must not repeat;
- history must not be appended again for the same effective state change;
- outbox intent must not be emitted again for the same logical transition;
- stale or illegal command must return a deterministic conflict/already-applied outcome.

### 9.6 Counter behavior

| Transition | Derived counter effect |
|---|---:|
| `Pending -> Visible` | `VisibleCommentCount` eventually `+1` |
| `Pending -> Rejected` | No change |
| `Visible -> Hidden` | `VisibleCommentCount` eventually `-1` |
| `Hidden -> Visible` | `VisibleCommentCount` eventually `+1` |

---

## 10) Author Delete-Own-Comment Idempotency

### 10.1 Ownership rule

Only the authoritative comment author may delete their own comment through user-facing delete flow.

```text
CurrentUserId == Comment.AuthorUserId
```

### 10.2 Allowed transitions

| Current status | Next status |
|---|---|
| `Pending` | `Deleted` |
| `Visible` | `Deleted` |
| `Rejected` | `Deleted` |
| `Hidden` | `Deleted` |
| `Deleted` | Idempotent no-op |

### 10.3 Normal author-delete atomicity

For author deletion without an open report case, Interaction commits:

```text
Comment status/version transition to Deleted
+ required outbox intent where downstream effect exists
```

Ordinary author deletion without an active case does not have to create a moderation-history row in V1.

### 10.4 Author deletion with open moderation case

If an author deletes a `Visible` comment that has an `Open` case, the atomic mutation set is:

```text
Comment: Visible -> Deleted
CommentModerationCase: Open -> ClosedByAuthorDeletion
Pending CommentReports in case -> ClosedByAuthorDeletion
CommentModerationActionHistory: CloseCaseByAuthorDeletion
Required OutboxMessage(s)
```

### 10.5 Retry behavior

If author delete is retried after timeout and the comment was already deleted by the author:

- return idempotent success/no-op;
- do not close a case twice;
- do not append duplicate history;
- do not emit duplicate effects;
- do not decrement derived counters twice.

### 10.6 Counter behavior

Only deletion from `Visible` changes the derived public comment count:

```text
Visible -> Deleted => VisibleCommentCount eventually decreases.
```

---

## 11) Comment Report Idempotency

### 11.1 Report eligibility

A report requires:

```text
Authenticated reporter
Comment.Status = Visible
Article interaction-enabled
ReporterUserId != Comment.AuthorUserId
Valid ReasonCode / Description
No existing report by that reporter for that comment
```

### 11.2 Unique-report invariant

```text
At most one CommentReport per (CommentId, ReporterUserId).
```

This applies across all moderation cycles in V1.

### 11.3 Report is not moderation truth

A report:

- creates review workflow truth;
- does not prove policy violation;
- does not change `Comment.Status`;
- does not change public counters;
- does not auto-hide content.

### 11.4 Report creation atomicity

When no open case exists, a valid report must atomically commit:

```text
New CommentModerationCase with Status = Open
+ New CommentReport with Status = Pending
+ Possible alert-trigger metadata/outbox if immediate severity rule applies
+ Required report outbox intent
```

When an open case exists, a valid report must atomically commit:

```text
New CommentReport attached to current Open case
+ Possible first alert-trigger metadata/outbox if threshold is reached
+ Required report outbox intent
```

### 11.5 Retry behavior

A retry after ambiguous timeout must not:

- create a second report for the same reporter/comment;
- create a second open case;
- count the same reporter twice toward alert threshold;
- trigger duplicate admin-alert intent.

---

## 12) Moderation Case Consistency

### 12.1 Single-open-case invariant

```text
At most one Open CommentModerationCase per Comment.
```

This must be protected authoritatively at the database/transaction boundary, not only by application pre-check.

### 12.2 Case statuses

```text
Open
Dismissed
Actioned
ClosedByAuthorDeletion
```

### 12.3 Resolution transitions

| Command | Required current case state | Next case state |
|---|---|---|
| Dismiss reported case | `Open` | `Dismissed` |
| Hide reported comment | `Open` | `Actioned` |
| Author deletes reported comment | `Open` | `ClosedByAuthorDeletion` |

### 12.4 Case resolution atomicity

#### Dismiss reported case

```text
Case: Open -> Dismissed
Pending Reports -> Dismissed
Comment remains Visible
CommentModerationActionHistory: DismissReportedCase
Outbox: interaction.comment_reports_dismissed
```

#### Hide reported comment

```text
Comment: Visible -> Hidden
Case: Open -> Actioned
Pending Reports -> Actioned
CommentModerationActionHistory: HideReportedComment
Outbox: interaction.comment_hidden with report-resolution metadata
```

#### Close by author deletion

```text
Comment: Visible -> Deleted
Case: Open -> ClosedByAuthorDeletion
Pending Reports -> ClosedByAuthorDeletion
CommentModerationActionHistory: CloseCaseByAuthorDeletion
Required OutboxMessage(s)
```

### 12.5 Version protection

`CommentModerationCase.Version` must protect:

- concurrent moderator resolution;
- report threshold alert triggering;
- author-delete racing with admin resolution;
- retry after timeout;
- stale UI actions.

---

## 13) Admin Alert Intent Idempotency

### 13.1 Escalation rule

V1 default configuration:

| Open-case condition | Alert result |
|---|---|
| Three distinct normal-severity reporters | Trigger one alert intent |
| One high-severity report | Trigger one alert intent |
| One critical-severity report | Trigger one alert intent |

### 13.2 One-alert rule

```text
One Open CommentModerationCase may trigger at most one
administrator-alert business intent in V1.
```

### 13.3 Durable case metadata

The case stores durable trigger evidence, such as:

```text
AlertTriggeredAtUtc
AlertLevel
AlertMessageId or equivalent correlation metadata
```

### 13.4 Atomic trigger pattern

When a report causes escalation policy to be met:

```text
Insert valid report
+ verify case remains Open
+ verify alert not already triggered
+ update case alert metadata/version atomically
+ write interaction.comment_report_alert_triggered to Outbox
+ commit
```

### 13.5 Email delivery boundary

Interaction does not send email directly.

Notifications consumes:

```text
interaction.comment_report_alert_triggered
```

Notifications must dedupe email delivery using a stable business-intent identity equivalent to:

```text
InteractionCommentReportAlert:{CommentModerationCasePublicId}
```

### 13.6 Email ambiguity

A provider timeout or consumer restart does not authorize Interaction to create a second alert intent for the same open case.

---

## 14) Transaction Boundary

### 14.1 Atomic Interaction boundary

For commands with asynchronous consequences, atomicity stops at:

```text
Interaction-owned state mutation
+ required local operational history
+ required local idempotency state where adopted
+ shared OutboxMessage
```

### 14.2 Atomic command examples

| Command | Atomic commit set |
|---|---|
| Like | `ArticleLike` truth effect + outbox |
| Unlike | `ArticleLike` truth effect + outbox |
| Create comment | `Comment` + optional request-idempotency marker + outbox |
| Approve/reject/hide/restore | `Comment` transition + history + outbox |
| Report comment | `CommentReport` + case create/join + possible alert metadata + outbox |
| Dismiss report case | Case + reports + history + outbox |
| Hide reported comment | Comment + case + reports + history + outbox |
| Delete own comment with open case | Comment + case + reports + history + outbox |

### 14.3 Outside Interaction transaction

Interaction command success must not wait for:

```text
RabbitMQ publish acknowledgement as downstream completion
Reading projection application
Audit ingestion
Notifications email delivery
Redis refresh as correctness condition
External API/provider calls
Counter reconciliation completion
Batch/rebuild completion
```

### 14.4 Shared database rule

A shared physical SQL deployment does not permit Interaction to write directly into module-owned business/projection state of:

```text
Content
Reading
Audit
Notifications
```

---

## 15) Consumer Dedupe and Apply Safety

### 15.1 Durable consumer state

Interaction async consumers use durable processing state equivalent to:

```text
InteractionConsumedMessage
```

### 15.2 Dedupe key

```text
Unique(ConsumerName, MessageId)
```

`ConsumerName` is required because separate Interaction consumer purposes may process different effects from the same originating source message.

### 15.3 Duplicate versus stale

| Condition | Meaning | Required behavior |
|---|---|---|
| Same `MessageId` delivered again to same consumer | Duplicate | Do not repeat effect |
| Different message with lower/equal source version arrives later | Stale | Do not overwrite newer projection/state |

### 15.4 Apply-decision diagnostics

Recommended apply decisions:

```text
Applied
DuplicateIgnored
StaleIgnored
GapDetected
ResyncRequired
Failed
```

### 15.5 Inbound eligibility consumer

For Content-derived article eligibility:

```text
Dedupe by (ConsumerName, MessageId)
Apply by LastSourceVersion
Fail closed and resync when eligibility is uncertain
```

---

## 16) Counter Materialization and Reading Publication

### 16.1 Public counter snapshot

Interaction publishes:

```text
interaction.article_counters_projection_published
```

### 16.2 Counter snapshot state

```text
ArticleInteractionStats
- ArticlePublicId
- ViewCount
- LikeCount
- VisibleCommentCount
- StatsVersion
```

### 16.3 Counter sources

| Public counter | Source |
|---|---|
| `ViewCount` | Durable `ArticleViewCount` materialized state |
| `LikeCount` | Active `ArticleLike` truth |
| `VisibleCommentCount` | `Comment.Status = Visible` truth |

### 16.4 Known-value snapshot rule

Reading receives known-value snapshots, not raw deltas.

```text
Correct:
    set ViewCount = incoming ViewCount
    set LikeCount = incoming LikeCount
    set VisibleCommentCount = incoming VisibleCommentCount
    only if incoming StatsVersion is newer

Incorrect:
    increment Reading LikeCount for each raw liked event
    decrement Reading comment count for each raw hidden event
    increment Reading ViewCount for each page-view message
```

### 16.5 StatsVersion rule

```text
ArticleInteractionStats.StatsVersion is monotonic per article snapshot.

A new public counter snapshot is applied downstream only when:
IncomingStatsVersion > CurrentAppliedStatsVersion.
```

### 16.6 Counter lag rule

Public counters may lag without invalidating Interaction truth or article visibility.

Reading may display older counters temporarily, but:

- must not use counters to decide article visibility;
- must not infer comment moderation truth from counters;
- must not infer a user's like state from aggregate counts.

---

## 17) Counter Reconciliation and Repair

### 17.1 Recomputable values

The following derived values can be reconciled from authoritative Interaction truth:

```text
LikeCount = COUNT(active ArticleLike for ArticlePublicId)

VisibleCommentCount = COUNT(Comment
                            WHERE ArticlePublicId = target
                              AND Status = Visible)
```

### 17.2 View-count posture

In V1:

```text
ViewCount is read from durable ArticleViewCount materialized state.
It is not exactly recomputed from raw individual view events.
```

### 17.3 Bounded repair flow

```text
Select bounded article scope
    -> Read ArticleViewCount
    -> Recompute LikeCount from ArticleLike truth
    -> Recompute VisibleCommentCount from Comment truth
    -> Compose candidate public counter snapshot
    -> Compare with ArticleInteractionStats
    -> If outward snapshot differs:
         update ArticleInteractionStats
         increment StatsVersion
         write interaction.article_counters_projection_published outbox
         commit
    -> Record repair diagnostics
```

### 17.4 Repair rule

```text
Reconciliation repairs derived state from truth/materialized accepted-view state.

It must not mutate truth to match an incorrect historical counter.
```

### 17.5 Batch posture

V1 requires a documented bounded reconciliation path.

V1 does not require initial implementation of:

- full scheduled rebuild orchestration;
- trending pipelines;
- raw view replay pipelines;
- singleton-owned counter rebuild infrastructure.

If future exclusive aggregation/rebuild ownership is introduced, it must use approved fencing/ownership policy.

---

## 18) Timeout and Safe Reconciliation

### 18.1 Timeout rule

```text
Timeout means the caller stopped waiting.
It does not prove the mutation failed.
```

### 18.2 Command ambiguity table

| Operation | Safe reconciliation source |
|---|---|
| Like / Unlike | Current `ArticleLike` truth |
| Create Comment | Idempotency record/result if supported; otherwise authoritative comment query posture |
| Delete Own Comment | Current `Comment.Status` and ownership |
| Approve/Reject/Hide/Restore | Current `Comment.Status` and `Version` |
| Report Comment | Existing `(CommentId, ReporterUserId)` report truth |
| Resolve Case | Current `CommentModerationCase.Status` and `Version` |
| Alert Trigger | Durable case alert metadata and Notifications business-intent dedupe |
| Counter publication | Current `ArticleInteractionStats.StatsVersion` |

### 18.3 Invalid reconciliation sources

Do not use the following as proof that an Interaction command committed:

```text
Reading counter display
Cached counters
Email delivered/not delivered observation
Audit arrival/non-arrival
Timestamp comparison alone
```

---

## 19) Observability Requirements

Minimum Interaction consistency/idempotency signals should include:

```text
interaction_like_idempotent_hits_total
interaction_like_unique_conflicts_total
interaction_unlike_idempotent_hits_total

interaction_comment_create_idempotency_hits_total
interaction_comment_version_conflicts_total
interaction_comment_invalid_transition_total

interaction_report_duplicate_rejected_total
interaction_moderation_case_open_conflicts_total
interaction_moderation_case_version_conflicts_total

interaction_report_alert_triggered_total
interaction_report_alert_suppressed_total

interaction_consumer_duplicate_ignored_total
interaction_consumer_stale_ignored_total
interaction_consumer_gap_detected_total
interaction_eligibility_resync_required_total

interaction_view_accepted_total
interaction_view_rejected_total
interaction_view_atomic_update_failures_total

interaction_stats_materialization_lag_seconds
interaction_stats_reconciliation_runs_total
interaction_stats_reconciliation_repairs_total
interaction_counter_projection_published_total
```

Logs should carry where applicable:

```text
CorrelationId
MessageId
ConsumerName
ArticlePublicId
CommentPublicId
CommentModerationCasePublicId
ActorUserId
ActionType
IdempotencyOutcome
CurrentVersion
IncomingVersion / ExpectedVersion
ApplyDecision
```

Sensitive data and comment/report content must not be logged unnecessarily.

---

## 20) V1 Deferred Consistency Concerns

The following are deferred and must not be implied as currently implemented:

| Capability | Deferred consistency concern |
|---|---|
| Reply comments | Parent/child transition, moderation and count semantics |
| Comment editing | Revision/re-moderation/idempotency semantics |
| Auto-hide after report threshold | Abuse-resistant automated decision safety |
| Bulk moderation | Batch command atomicity and per-item outbox semantics |
| Raw per-view history | Exact view rebuild and analytics semantics |
| Trending pipeline | Windowing, event-time and late-arrival semantics |
| Full scheduled rebuild | Ownership/fencing and candidate cutover strategy |

`ParentCommentId` may exist in schema only as future compatibility; V1-created comments must always set it to `NULL`.

---

## 21) Summary

Interaction V1 idempotency and consistency are governed by the following rules:

1. `ArticleLike`, `Comment`, `CommentReport`, `CommentModerationCase` and required local moderation history are Interaction-owned truth/workflow state.
2. `ArticleInteractionTargetProjection`, `ArticleViewCount` and `ArticleInteractionStats` are derived/materialized state with explicit safety posture.
3. New public interactions require safe local eligibility projection; unknown eligibility fails closed.
4. Unpublish/archive/soft-delete disables new interaction but never resets historical counters or workflow state.
5. Views use `ArticleViewCount` atomic accepted-count mutation; V1 stores no raw per-view event history.
6. Likes are protected per `(ArticlePublicId, UserId)`; concurrent likes by different users are independent.
7. V1 comments are top-level only; `ParentCommentId` is reserved but must be `NULL` for new comments.
8. Comment moderation is status-legal and version-protected.
9. Comment authors may delete their own comments directly and idempotently.
10. Reports never auto-hide comments and are unique per `(CommentId, ReporterUserId)`.
11. At most one moderation case may be open for a comment.
12. One open moderation case may trigger at most one administrator-alert business intent in V1.
13. Required Interaction mutation/history/outbox state commits atomically.
14. Async consumers dedupe by `(ConsumerName, MessageId)` and reject stale applies by version.
15. Reading consumes only versioned known-value public counter snapshots.
16. Counter lag is acceptable; counter state must never replace truth.
17. Like and visible-comment totals are reconcilable from truth; view count relies on durable materialized count in V1.
18. Timeout never proves failure; reconciliation uses Interaction truth and durable workflow state.
