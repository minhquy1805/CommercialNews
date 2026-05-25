# Interaction — Runtime Flows (V1)

This document defines the runtime flows for Interaction in V1.
It focuses on request handling, async consumption/publication, moderation, reporting, counter materialization, and recovery/reconciliation behavior.

Related:

- `01-api-surface.md`
- `02-domain-contracts.md`
- `04-errors-status-codes.md`
- `05-security-abuse-controls.md`
- `06-idempotency-consistency.md`
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

## 1) Runtime Posture

Interaction participates in three runtime lanes.

### 1.1 Synchronous Interaction command lane

Used for authoritative Interaction mutations:

```text
Like / Unlike
Create Comment
Delete Own Comment
Create Comment Report
Approve / Reject / Hide / Restore Comment
Dismiss / Action Reported-Comment Case
```

A successful command means:

```text
Interaction-owned state committed
+ required local operational history committed
+ required outbox intent committed
```

A successful command does not mean:

```text
RabbitMQ publication finished
Reading already updated counters
Audit already ingested moderation evidence
Notifications already sent an email
```

### 1.2 Async event and derived-state lane

Used for:

```text
Content eligibility projection consumption
Like/comment counter materialization
Public counter snapshot publication for Reading
Audit-facing moderation facts
Notifications-facing admin alert intent
```

### 1.3 Reconciliation and recovery lane

Used for:

```text
Eligibility projection resync
LikeCount reconciliation
VisibleCommentCount reconciliation
Public counter snapshot republish
Consumer replay/retry diagnostics
Future bounded counter repair workflows
```

### 1.4 Core runtime rules

```text
Interaction truth and workflow state remain authoritative inside Interaction.

Article eligibility projection and public counters are derived state.

Derived state may lag, but must not overwrite truth or silently redefine ownership.

Async delivery is at-least-once and must tolerate duplicates, replay and stale delivery.

Version, not wall-clock timestamp, determines ordered freshness.
```

---

## 2) Runtime Data Roles

### 2.1 Truth and workflow state

```text
ArticleLike
Comment
CommentReport
CommentModerationCase
CommentModerationActionHistory
```

### 2.2 Derived and processing state

```text
ArticleInteractionTargetProjection
ArticleViewCount
ArticleInteractionStats
InteractionConsumedMessage
```

### 2.3 Public counter output

```text
interaction.article_counters_projection_published
```

Reading consumes only this known-value counter snapshot from Interaction.

Reading does not consume raw like, comment moderation or individual view behavior to calculate public counters.

---

## 3) Flow A — Content Public-State Projection to Interaction Eligibility

### Goal

Maintain local article-interaction eligibility without synchronous request-path calls from Interaction to Content.

### Source

Conceptual inbound Content event:

```text
content.article_read_projection_published
```

The exact event type and payload field names must match the finalized Content outbound contract.

### Flow

```text
Content commits public article projection change
    -> Content writes OutboxMessage in its local transaction
    -> Worker publishes Content projection message
    -> Interaction consumer receives message
    -> Interaction checks durable dedupe state
    -> Interaction checks incoming source version
    -> Interaction updates ArticleInteractionTargetProjection
    -> Interaction commits consume/apply state
```

### Required projection outcome

```text
ArticleInteractionTargetProjection
- ArticlePublicId
- SourceStatus
- IsInteractionEnabled
- LastSourceVersion
- LastSourceMessageId
- LastSyncedAtUtc
- RequiresResync
```

### Apply decisions

| Situation | Interaction behavior |
|---|---|
| `(ConsumerName, MessageId)` already processed | Ignore duplicate safely |
| `IncomingVersion <= LastSourceVersion` | Ignore stale/already-applied state |
| Valid newer source version | Apply eligibility projection |
| Gap or unsafe uncertainty detected | Mark/respect projection as unsafe; require resync |
| Projection missing | Fail closed for new interaction |

### Eligibility behavior

| Local eligibility state | New public interactions |
|---|---:|
| `IsInteractionEnabled = true` and `RequiresResync = false` | Allowed according to each command rule |
| Disabled | Rejected or safely ignored |
| Missing | Rejected or safely ignored |
| Requires resync | Rejected or safely ignored |

### Unpublish/archive/soft-delete behavior

When Content communicates that an article is no longer public:

```text
ArticleInteractionTargetProjection.IsInteractionEnabled = false
```

Interaction must then:

- stop accepting new views contributing to counters;
- reject new likes;
- reject new comments;
- reject new reports;
- preserve existing likes, comments, reports, moderation cases and moderation history;
- preserve `ArticleViewCount`;
- preserve `ArticleInteractionStats`.

Allowed user-owned retractions remain possible:

- user may unlike their own existing active like;
- comment author may delete their own existing comment.

Authorized moderators may still resolve already-open moderation cases.

---

## 4) Flow B — Track Article View

### Goal

Accept countable article views without blocking public article reading and without publishing one async message per page view to Reading.

### Runtime boundary

Public article rendering belongs to Reading.

View tracking is an Interaction call made independently from successful article rendering. A failure to record a view must not make public article reading fail.

### Flow

```text
Reader opens a public article
    -> Public article response succeeds independently
    -> Client submits track-view request to Interaction
    -> Interaction checks local article eligibility
    -> Interaction applies validation and anti-abuse policy
    -> If view is accepted:
         atomically increment ArticleViewCount.ViewCount
         increment ArticleViewCount.ViewVersion
         update LastAcceptedViewAtUtc
    -> Return accepted/no-content response according to API contract
    -> Counter snapshot materialization/publication occurs asynchronously or in a coalesced manner
```

### View acceptance rules

A view contributes to `ArticleViewCount` only when:

- article is locally interaction-enabled;
- request is valid;
- request passes rate limiting and repeat-view policy;
- request is not rejected by configured abuse controls.

### View persistence rule

Interaction V1 uses:

```text
ArticleViewCount
```

It does not store one durable row per individual page view.

### Concurrent accepted views

Many readers viewing the same article concurrently are valid independent count contributions.

The persistence operation must use atomic increment semantics.

Disallowed:

```text
Read ViewCount
Increment in application memory
Overwrite a stale count
```

### Counter publication rule

Interaction must not publish:

```text
one outbound Reading message per accepted view
```

Instead:

```text
ArticleViewCount
    -> counter materialization/coalescing
    -> ArticleInteractionStats
    -> interaction.article_counters_projection_published
    -> Reading
```

### Failure behavior

| Failure | Safe behavior |
|---|---|
| Interaction unavailable | Reading still succeeds; view is not counted or counting lags |
| Eligibility projection unavailable/unsafe | Do not count new view |
| Abuse rule rejects request | Do not count new view |
| Counter publication lag | Public counter may temporarily remain stale |
| Retry from client | Must be bounded by anti-abuse/repeat-view policy |

### Recovery limitation

Because V1 does not keep raw per-view history:

- `ViewCount` is durable materialized state;
- exact reconstruction from historical individual views is not supported;
- monitoring, atomic mutation, backup/recovery and snapshot republication are required.

---

## 5) Flow C — Like Article

### Goal

Persist one active like relationship per authenticated user and article while allowing public counters to converge asynchronously.

### Preconditions

```text
Authenticated actor exists
ArticleInteractionTargetProjection confirms interaction-enabled article
```

### Flow

```text
Authenticated user requests Like
    -> Interaction obtains UserId from trusted request context
    -> Validate local article eligibility
    -> Attempt to create/reactivate ArticleLike for (ArticlePublicId, UserId)
    -> Enforce one-active-like invariant authoritatively
    -> Write interaction.article_liked outbox message if a new active-like effect occurred
    -> Commit Interaction transaction
    -> Return liked state from committed truth
```

### Invariant

```text
At most one active ArticleLike per (ArticlePublicId, UserId).
```

### Concurrent behavior

| Situation | Required outcome |
|---|---|
| User A and User B like same article concurrently | Both may succeed independently |
| Same user sends duplicate like requests concurrently | At most one active like is created |
| Same user retries after timeout | No duplicate active like or duplicate business effect |

### Derived counter behavior

`ArticleLike` is truth.

`LikeCount` in `ArticleInteractionStats` is derived and may update later through the counter materialization flow.

### Timeout behavior

If the request times out after commit:

- client must not assume like failed;
- a repeated like must remain safe;
- client may query current like state from Interaction truth.

---

## 6) Flow D — Unlike Article

### Goal

Allow a user to remove their own active like safely and idempotently.

### Preconditions

```text
Authenticated actor exists
An active ArticleLike for (ArticlePublicId, UserId) may exist
```

### Flow

```text
Authenticated user requests Unlike
    -> Interaction obtains UserId from trusted request context
    -> Locate or conditionally transition the user's active ArticleLike
    -> If active relationship exists:
         deactivate/remove it safely
         write interaction.article_unliked outbox message
    -> If already inactive/not present:
         return documented idempotent result or no-op result
    -> Commit Interaction transaction where mutation occurred
```

### Article unavailable behavior

Unlike may remain allowed after the article is unpublished, archived or soft-deleted because it retracts the user's existing relationship rather than creates new public engagement.

### Derived counter behavior

When an unlike truth mutation occurs:

```text
ArticleInteractionStats.LikeCount eventually decreases
```

The counter must never become negative.

---

## 7) Flow E — Create Top-Level Comment

### Goal

Persist a user-submitted top-level comment for moderation before public exposure.

### Preconditions

```text
Authenticated actor exists
Article is locally interaction-enabled
Comment content is valid and safe to persist/render later
```

### V1 reply rule

The `Comment` model may contain:

```text
ParentCommentId nullable
```

but V1 supports top-level comments only.

```text
Every V1-created comment must use ParentCommentId = NULL.
```

The V1 create-comment API must not accept a parent-comment identifier.

### Flow

```text
Authenticated user submits comment
    -> Interaction checks local article eligibility
    -> Validate content and rate-limit policy
    -> Create Comment:
         Status = Pending
         ParentCommentId = NULL
         AuthorUserId = current user
    -> Write interaction.comment_created outbox message where required
    -> Commit Interaction transaction
    -> Return created pending comment identity/state
```

### Public visibility behavior

A newly created comment:

- is not public;
- does not increase `VisibleCommentCount`;
- may appear in authorized admin moderation queries.

### Timeout and duplicate submission

A timeout may occur after the comment has committed.

Production-oriented V1 should use a create-comment idempotency key or document duplicate-submission handling explicitly.

---

## 8) Flow F — Public Read Visible Comments

### Goal

Return only comments currently eligible for public display.

### Query ownership

Public comment list querying belongs to Interaction in V1.

Reading does not need to project complete comment bodies.

### Flow

```text
Client requests comments for public article
    -> Interaction confirms article/public query eligibility according to contract
    -> Query Comment records where:
         ArticlePublicId matches
         Status = Visible
         ParentCommentId = NULL in V1
    -> Return public-safe comment response
```

### Non-public exclusion

Public APIs must not expose:

```text
Pending comments
Rejected comments
Hidden comments
Deleted comments
Report details
Moderation case metadata
Moderation notes
Moderator identity
Reporter identity
```

### Article unavailable behavior

A comment row may remain `Visible` in Interaction history while its article is unpublished.

Such a comment must not be publicly returned while the article is not public.

---

## 9) Flow G — Admin Approve Pending Comment

### Goal

Make an eligible pending comment publicly visible.

### Preconditions

```text
Actor has Interaction.Comments.Moderate
Comment.Status = Pending
Expected Comment.Version matches current truth
```

### Flow

```text
Moderator approves pending comment
    -> Authorize moderation permission
    -> Load current comment truth/version
    -> Validate Pending -> Visible transition
    -> Update Comment:
         Status = Visible
         Version = Version + 1
    -> Append CommentModerationActionHistory:
         ActionType = Approve
         FromStatus = Pending
         ToStatus = Visible
    -> Write interaction.comment_approved outbox message
    -> Commit Interaction transaction
    -> Return committed comment state
```

### Async effects

After commit:

```text
Audit may ingest approval fact asynchronously
Counter materialization eventually increases VisibleCommentCount
Reading eventually receives newer counter snapshot
```

### Retry behavior

If the approve request is retried after commit:

- comment must not be approved twice;
- history must not be appended twice for the same effective transition;
- counter effects must not be applied twice;
- stale/version conflict or documented already-applied outcome is returned.

---

## 10) Flow H — Admin Reject Pending Comment

### Goal

Prevent a pending comment from becoming public.

### Preconditions

```text
Actor has Interaction.Comments.Moderate
Comment.Status = Pending
Expected Comment.Version matches current truth
Valid required rejection reason is supplied
```

### Flow

```text
Moderator rejects pending comment
    -> Authorize permission
    -> Validate Pending -> Rejected transition
    -> Update Comment status/version
    -> Append CommentModerationActionHistory:
         ActionType = Reject
         FromStatus = Pending
         ToStatus = Rejected
         ReasonCode / Note
    -> Write interaction.comment_rejected outbox message
    -> Commit Interaction transaction
```

### Counter behavior

Rejecting a pending comment does not change `VisibleCommentCount`.

---

## 11) Flow I — Admin Hide Visible Comment

### Goal

Remove an already visible comment from public display.

### Preconditions

```text
Actor has Interaction.Comments.Moderate
Comment.Status = Visible
Expected Comment.Version matches current truth
Required hide reason is supplied
```

### Flow without report case

```text
Moderator hides visible comment
    -> Validate Visible -> Hidden transition
    -> Update Comment status/version
    -> Append CommentModerationActionHistory:
         ActionType = Hide
         FromStatus = Visible
         ToStatus = Hidden
         ReasonCode / Note
    -> Write interaction.comment_hidden outbox message
    -> Commit Interaction transaction
```

### Async effects

```text
Audit consumes moderation fact asynchronously
VisibleCommentCount eventually decreases
Interaction eventually publishes newer counter snapshot for Reading
```

### Retry behavior

A hidden comment must not be hidden again as a new effective transition.

---

## 12) Flow J — Admin Restore Hidden Comment

### Goal

Return a hidden comment to eligible visible state.

### Preconditions

```text
Actor has Interaction.Comments.Moderate
Comment.Status = Hidden
Expected Comment.Version matches current truth
```

### Flow

```text
Moderator restores hidden comment
    -> Validate Hidden -> Visible transition
    -> Update Comment status/version
    -> Append CommentModerationActionHistory:
         ActionType = Restore
         FromStatus = Hidden
         ToStatus = Visible
    -> Write interaction.comment_restored outbox message
    -> Commit Interaction transaction
```

### Article unavailable behavior

If the article is currently not public, restoring the comment may restore its Interaction status to `Visible`, but the comment is still not publicly returned until the article is public again.

### Async effects

```text
Audit consumes restore fact asynchronously
VisibleCommentCount eventually increases
Reading receives updated counter snapshot later
```

---

## 13) Flow K — Author Deletes Own Comment

### Goal

Allow a comment author to remove their own submitted content directly.

### Preconditions

```text
Authenticated actor exists
Comment.AuthorUserId = current user
Comment is not already deleted, or retry is treated idempotently
```

### Normal deletion flow without open case

```text
Author requests delete own comment
    -> Validate authoritative ownership
    -> If current status is Pending / Visible / Rejected / Hidden:
         update Comment.Status = Deleted
         increment Comment.Version
         set DeletedAtUtc
         write interaction.comment_deleted_by_author outbox message where required
         commit
    -> If already Deleted by author:
         return idempotent success/no-op
```

### Counter behavior

| Previous comment status | Counter effect |
|---|---:|
| `Pending` | No visible-counter change |
| `Visible` | `VisibleCommentCount` eventually decreases |
| `Rejected` | No visible-counter change |
| `Hidden` | No visible-counter change |

### Deletion with open moderation case

```text
Author deletes visible comment with Open moderation case
    -> Update Comment: Visible -> Deleted
    -> Update Case: Open -> ClosedByAuthorDeletion
    -> Update Pending reports -> ClosedByAuthorDeletion
    -> Append CommentModerationActionHistory:
         ActionType = CloseCaseByAuthorDeletion
    -> Write required outbox messages
    -> Commit one Interaction transaction
```

### Idempotency rule

Retry after ambiguous timeout must not:

- delete twice;
- close the case twice;
- append duplicate case-close history;
- emit duplicate business effects;
- decrement derived public counter twice.

---

## 14) Flow L — Submit Comment Report

### Goal

Allow an authenticated non-author user to request moderator review of a visible comment.

### Preconditions

```text
Authenticated actor exists
Article is interaction-enabled
Comment.Status = Visible
ReporterUserId != Comment.AuthorUserId
No existing report for (CommentId, ReporterUserId)
Valid ReasonCode and bounded Description supplied
```

### Flow when no open case exists

```text
User reports visible comment
    -> Validate actor, comment visibility, article eligibility and uniqueness
    -> Create CommentModerationCase:
         Status = Open
         Version = initial version
    -> Create CommentReport linked to new case:
         Status = Pending
    -> Evaluate alert escalation policy
    -> If first report already satisfies severity-based alert rule:
         persist alert-trigger metadata on case
         write interaction.comment_report_alert_triggered outbox message
    -> Write interaction.comment_reported outbox message where required
    -> Commit Interaction transaction
```

### Flow when an open case already exists

```text
User reports visible comment
    -> Validate report uniqueness and eligibility
    -> Load current Open case
    -> Create CommentReport linked to existing case
    -> Recalculate/evaluate threshold from valid reports in the case
    -> If alert policy becomes satisfied and no alert was previously triggered:
         update case alert-trigger metadata atomically
         write interaction.comment_report_alert_triggered outbox message
    -> Write interaction.comment_reported outbox message where required
    -> Commit Interaction transaction
```

### Report behavior

Creating a report:

- does not hide the comment;
- does not change `VisibleCommentCount`;
- does not publish a counter snapshot merely because a report exists;
- makes the report/case available for authorized moderation queries.

### Duplicate behavior

```text
At most one CommentReport per (CommentId, ReporterUserId).
```

A retry after timeout must not create:

- duplicate report;
- duplicate open case;
- duplicate alert intent.

---

## 15) Flow M — Admin Alert Trigger for Reported Comment

### Goal

Notify admin/moderator attention asynchronously when a report case reaches configured urgency.

### Default V1 escalation policy

| Condition in one open case | Alert behavior |
|---|---|
| Three distinct authenticated normal-severity reporters | Trigger one async admin alert |
| One high-severity report | Trigger one async admin alert |
| One critical-severity report | Trigger one async admin alert |

### Trigger flow

```text
Report creation causes open case to meet alert policy
    -> In the same Interaction transaction:
         verify Case.Status = Open
         verify AlertTriggeredAtUtc is null
         set AlertTriggeredAtUtc
         set AlertLevel
         set AlertMessageId/correlation metadata where used
         increment Case.Version safely
         write outbox:
             interaction.comment_report_alert_triggered
    -> Commit
```

### Boundary rule

Interaction does not send email directly.

```text
Interaction
    -> interaction.comment_report_alert_triggered
    -> Notifications consumer
    -> durable notification intent/delivery record
    -> email provider send/retry
```

### Business-intent dedupe

Notifications should protect email delivery using a stable intent equivalent to:

```text
InteractionCommentReportAlert:{CommentModerationCasePublicId}
```

### No automatic hide

An alert indicates moderator urgency only.

```text
Alert trigger never automatically changes Comment.Status.
```

---

## 16) Flow N — Admin Dismisses Reported-Comment Case

### Goal

Resolve reports while leaving the comment visible.

### Preconditions

```text
Actor has Interaction.CommentReports.Resolve
Case.Status = Open
Comment.Status = Visible
Expected Case.Version matches current truth
Valid resolution reason/note supplied
```

### Flow

```text
Moderator dismisses reported-comment case
    -> Authorize report-resolution permission
    -> Validate Open case/version
    -> Keep Comment.Status = Visible
    -> Update Case: Open -> Dismissed
    -> Update all Pending reports in case -> Dismissed
    -> Append CommentModerationActionHistory:
         ActionType = DismissReportedCase
         CommentModerationCaseId = current case
         ReasonCode / Note
    -> Write interaction.comment_reports_dismissed outbox message
    -> Commit Interaction transaction
```

### Counter behavior

No public counter changes occur because the comment remains visible.

### Retry behavior

A dismissed case must not be dismissed again as a new business effect.

---

## 17) Flow O — Admin Hides Reported Comment

### Goal

Resolve an open report case by removing the reported comment from public display.

### Preconditions

```text
Actor has Interaction.CommentReports.Resolve
Actor has required comment-moderation authority
Case.Status = Open
Comment.Status = Visible
Expected Comment.Version and Case.Version match current truth
Valid hide/resolution reason supplied
```

### Flow

```text
Moderator hides reported comment
    -> Authorize permissions
    -> Validate Comment Visible -> Hidden
    -> Validate Case Open -> Actioned
    -> Update Comment status/version
    -> Update Case status/version and resolution metadata
    -> Update all Pending reports in case -> Actioned
    -> Append CommentModerationActionHistory:
         ActionType = HideReportedComment
         FromStatus = Visible
         ToStatus = Hidden
         CommentModerationCaseId = current case
         ReasonCode / Note
    -> Write interaction.comment_hidden outbox message containing report-resolution metadata
    -> Commit Interaction transaction
```

### Suggested hidden-event metadata

```text
ResolutionSource = Report
CommentModerationCasePublicId
ResolvedReportCount
ReasonCode
ModeratorUserId
```

### Async effects

```text
Audit consumes moderation fact asynchronously
VisibleCommentCount eventually decreases
Interaction eventually publishes newer public counter snapshot to Reading
```

### Event simplification

A separate:

```text
interaction.comment_reports_actioned
```

event is not required in V1 if `interaction.comment_hidden` contains the required case-resolution metadata.

---

## 18) Flow P — Async Counter Materialization

### Goal

Maintain public-facing interaction counters without making like/comment moderation truth commands depend on Reading freshness.

### Derived snapshot state

```text
ArticleInteractionStats
- ArticlePublicId
- ViewCount
- LikeCount
- VisibleCommentCount
- StatsVersion
- LastMaterializedAtUtc
- LastPublishedMessageId nullable
- LastPublishedAtUtc nullable
```

### Source posture

| Counter | Source |
|---|---|
| `ViewCount` | `ArticleViewCount.ViewCount` |
| `LikeCount` | Active `ArticleLike` relationships |
| `VisibleCommentCount` | `Comment.Status = Visible` |

### Typical flow

```text
Interaction truth or accepted view state changes
    -> Counter materialization work is triggered/coalesced according to implementation policy
    -> Materializer determines current known public counter snapshot
    -> Compare against current ArticleInteractionStats
    -> If public snapshot changed:
         update ArticleInteractionStats values
         increment StatsVersion
         write outbox:
             interaction.article_counters_projection_published
         commit derived-state/publication transaction
    -> If no public snapshot changed:
         no downstream publication required
```

### Snapshot publication

```text
interaction.article_counters_projection_published
```

Conceptual payload:

```json
{
  "articlePublicId": "01J...",
  "viewCount": 1250,
  "likeCount": 48,
  "visibleCommentCount": 12,
  "statsVersion": 29,
  "projectedAtUtc": "2026-05-25T10:35:00Z"
}
```

### Reading contract boundary

Reading later consumes only the counter snapshot.

```text
Reading does not increment counters from raw like/comment/view events.
```

### Consistency behavior

- `ArticleInteractionStats` may lag behind Interaction truth.
- Reading may lag behind `ArticleInteractionStats`.
- Counter lag is acceptable.
- Counter lag must not determine article visibility or comment truth.

---

## 19) Flow Q — Consumer Dedupe and Version-Aware Apply

### Goal

Protect Interaction async consumers from duplicate and stale message application.

### Durable processing state

```text
InteractionConsumedMessage
- ConsumerName
- MessageId
- ProducerModule
- EventType
- AggregateId
- AggregateVersion nullable
- ApplyDecision
- CorrelationId nullable
- ReceivedAtUtc
- ProcessedAtUtc nullable
```

### Dedupe invariant

```text
Unique(ConsumerName, MessageId)
```

### Generic apply flow

```text
Interaction consumer receives message
    -> Check/attempt durable consume registration for (ConsumerName, MessageId)
    -> If duplicate:
         record/log duplicate outcome as applicable
         do not repeat effect
    -> If new message and ordered aggregate applies:
         compare incoming Version with current LastAppliedVersion
    -> If stale:
         ignore safely and record stale outcome
    -> If safe forward progress:
         apply derived/projection effect
         persist apply decision
    -> If gap/unsafe uncertainty:
         defer/fail closed/resync according to projection role
```

### Important distinction

```text
Duplicate message:
    same MessageId delivered again.

Stale message:
    different older message arrives after newer state was already applied.
```

Both must be handled.

---

## 20) Flow R — Counter Reconciliation and Repair

### Goal

Repair derived public counters when materialization lags, fails or is suspected to have drifted.

### Reconciliable counters

```text
LikeCount = COUNT(active ArticleLike for article)

VisibleCommentCount = COUNT(Comment
                            WHERE ArticlePublicId = target
                              AND Status = Visible)
```

### ViewCount posture

```text
ViewCount comes from durable ArticleViewCount.
V1 does not recalculate ViewCount from raw individual view history.
```

### Bounded repair flow

```text
Operator or approved recovery workflow selects bounded article scope
    -> Read Interaction truth/materialized view state
    -> Recompute candidate LikeCount and VisibleCommentCount
    -> Read durable ArticleViewCount
    -> Compose candidate public counter snapshot
    -> Compare candidate with ArticleInteractionStats
    -> If repair is required:
         update ArticleInteractionStats
         increment StatsVersion
         write interaction.article_counters_projection_published outbox message
         commit
    -> Emit repair metrics/logs
```

### Rules

- Reconciliation updates derived state only.
- Reconciliation must not rewrite like/comment truth to match a stale counter.
- Rerunning the same bounded repair must be safe.
- Repaired snapshots must publish with a newer `StatsVersion` only when public output changes.

---

## 21) Flow S — Truth-Safe Behavior Under Lag or Failure

### Goal

Keep system behavior safe while projections, counters, Audit or Notifications lag.

### Typical cases

| Situation | Safe behavior |
|---|---|
| Content eligibility projection not yet available after publish | Reject new Interaction commands temporarily |
| Article becomes non-public but counter snapshot remains stored | Do not reset counters; no new interaction |
| Counter materialization delayed | Show older counter later through Reading; truth remains correct |
| Reading has stale counter snapshot | Article visibility must still be controlled by Content-derived Reading state |
| Audit consumer delayed | Moderation transaction remains committed; local history remains available |
| Notifications email delayed | Report/case remains committed; admin queue remains authoritative |
| Worker restart/redelivery | Dedupe/version guards prevent duplicate harmful effects |

### Rule

```text
Safe non-progress or temporarily stale enrichment is preferable
to unsafe state application or cross-module truth corruption.
```

---

## 22) Flow T — Deferred Reply Compatibility

### Goal

Reserve schema compatibility for future reply-comment support without enabling reply behavior in V1.

### V1 behavior

```text
Comment.ParentCommentId exists as nullable reserved field.
All V1-created comments have ParentCommentId = NULL.
```

### V1 does not support

```text
Create reply
Read nested thread
Reply moderation rules
Reply report rules
Reply count rules
Parent-hidden/deleted reply behavior
```

### Future phase requirement

A later reply-comment phase must define:

- maximum depth;
- parent status requirements;
- whether hidden/deleted parents retain visible replies;
- report and moderation-case behavior for replies;
- counter semantics;
- public query and pagination structure.

---

## 23) Summary of V1 Runtime Rules

Interaction V1 runtime is governed by the following rules:

1. Interaction owns like, comment, report and moderation workflow state.
2. Interaction uses local Content-derived eligibility projection and fails closed when eligibility is uncertain.
3. Unpublish, archive or soft-delete disables new interaction but preserves existing counters and history.
4. View tracking is non-blocking and stored as `ArticleViewCount`, not individual raw view events.
5. Like/unlike truth is per user/article and remains safe under duplicate requests and concurrency.
6. Comments are top-level only in V1; `ParentCommentId` is reserved but always `NULL` for new V1 comments.
7. Comments begin as `Pending`; only `Visible` comments are publicly readable.
8. Author may delete their own comment without moderator approval.
9. Users may report visible comments of other users; reports never auto-hide content.
10. Reports are grouped into `CommentModerationCase`; only one case may be open for a comment.
11. One open report case may trigger at most one asynchronous administrator-alert intent.
12. `CommentModerationActionHistory` is committed locally for required admin moderation operations.
13. `ArticleInteractionStats` is a derived public counter snapshot and may lag.
14. Reading consumes only `interaction.article_counters_projection_published`.
15. Interaction mutations with async consequences commit state/history/outbox atomically.
16. Consumers dedupe by stable `MessageId` and reject stale updates by version.
17. Counters and projections support approved reconciliation/recovery posture.
18. Audit and Notifications may lag without invalidating committed Interaction workflow truth.
