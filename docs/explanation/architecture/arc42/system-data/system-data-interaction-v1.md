# System Data Model — Interaction (V1)

> **Recommended file:** `explanation/architecture/arc42/system-data/system-data-interaction-v1.md`
> **Module:** Interaction
> **Purpose:** Store engagement and moderation workflow data for public articles, while publishing derived public counters asynchronously to Reading.

---

## 1) Ownership and Boundaries

Interaction owns:

```text
ArticleLike
Comment
CommentReport
CommentModerationCase
CommentModerationActionHistory

ArticleInteractionTargetProjection
ArticleViewCount
ArticleInteractionStats
```

Interaction does not own:

| Concern | Owner |
|---|---|
| Article content and publication lifecycle | Content |
| Public article read model | Reading |
| User/account truth | Identity |
| Permissions | Authorization |
| Email delivery | Notifications |
| Canonical audit evidence | Audit |

Cross-module references use stable logical identifiers:

```text
ArticlePublicId
UserId
```

Interaction must not directly modify Content, Reading, Notifications or Audit tables.

---

## 2) Data Classification

### 2.1 Truth / Workflow State

| Entity | Purpose |
|---|---|
| `ArticleLike` | Current user-like relationship for an article |
| `Comment` | Comment content and current moderation status |
| `CommentReport` | Valid report submitted against a visible comment |
| `CommentModerationCase` | One report-review cycle for a comment |
| `CommentModerationActionHistory` | Local moderation history for admin flow |

### 2.2 Derived / Processing State

| Entity | Purpose |
|---|---|
| `ArticleInteractionTargetProjection` | Local article interaction eligibility derived from Content |
| `ArticleViewCount` | Durable accepted-view materialized counter |
| `ArticleInteractionStats` | Public counter snapshot published to Reading |

Derived state may lag, but it must never replace Interaction truth.

---

## 3) Main Dataflows

### 3.1 Content → Interaction Eligibility

```text
Content public-state event
    -> Interaction consumer
    -> ArticleInteractionTargetProjection
```

Interaction accepts new view/like/comment/report operations only when:

```text
IsInteractionEnabled = true
AND RequiresResync = false
```

If eligibility is missing or unsafe, Interaction fails closed for new public interactions.

---

### 3.2 View Tracking

```text
Public article rendered by Reading
    -> Client sends separate view request
    -> Interaction validates eligibility and anti-abuse policy
    -> Atomic increment ArticleViewCount
```

Rules:

- view tracking must not block public reading;
- V1 does not store raw `ArticleViewEvent` rows;
- V1 does not publish one Reading event per view.

---

### 3.3 Likes

```text
Like / Unlike
    -> ArticleLike truth mutation
    -> Outbox event where required
    -> ArticleInteractionStats updated asynchronously
```

Invariant:

```text
At most one active ArticleLike per (ArticlePublicId, UserId).
```

---

### 3.4 Comments and Moderation

```text
Create comment
    -> Comment(Status = Pending, ParentCommentId = NULL)

Admin approve
    -> Pending -> Visible

Admin reject
    -> Pending -> Rejected

Admin hide
    -> Visible -> Hidden

Admin restore
    -> Hidden -> Visible

Author delete own comment
    -> Pending / Visible / Rejected / Hidden -> Deleted
```

Required moderation operations also append:

```text
CommentModerationActionHistory
```

---

### 3.5 Reports and Cases

```text
User reports Visible comment
    -> CommentReport
    -> Create or join CommentModerationCase(Open)
```

Rules:

```text
One report per (CommentId, ReporterUserId).
At most one Open CommentModerationCase per Comment.
Report never automatically hides a comment.
```

Case resolution:

| Operation | Comment | Case | Pending Reports |
|---|---|---|---|
| Dismiss reports | Remains `Visible` | `Dismissed` | `Dismissed` |
| Hide reported comment | `Hidden` | `Actioned` | `Actioned` |
| Author deletes comment | `Deleted` | `ClosedByAuthorDeletion` | `ClosedByAuthorDeletion` |

---

### 3.6 Counters → Reading

```text
ArticleViewCount + ArticleLike + Comment
    -> ArticleInteractionStats
    -> interaction.article_counters_projection_published
    -> Reading
```

Published counters:

```text
ViewCount
LikeCount
VisibleCommentCount
StatsVersion
```

Reading applies newer snapshots by `StatsVersion`.

---

## 4) Entity Summary

### 4.1 `ArticleInteractionTargetProjection`

| Field | Purpose |
|---|---|
| `ArticleInteractionTargetProjectionId` | Internal PK |
| `ArticlePublicId` | Article logical identity |
| `SourceStatus` | Latest Content-derived state |
| `IsInteractionEnabled` | Accept new interactions or not |
| `LastSourceVersion` | Latest applied source version |
| `LastSourceMessageId` | Latest applied message |
| `LastSourceOccurredAtUtc` | Source diagnostic timestamp |
| `LastSyncedAtUtc` | Local apply timestamp |
| `RequiresResync` | Unsafe projection marker |
| `CreatedAtUtc`, `UpdatedAtUtc` | Tracking fields |

Invariant:

```text
One row per ArticlePublicId.
```

---

### 4.2 `ArticleViewCount`

| Field | Purpose |
|---|---|
| `ArticleViewCountId` | Internal PK |
| `ArticlePublicId` | Article logical identity |
| `ViewCount` | Accepted materialized view total |
| `ViewVersion` | Monotonic counter version |
| `LastAcceptedViewAtUtc` | Latest accepted view timestamp |
| `CreatedAtUtc`, `UpdatedAtUtc` | Tracking fields |

Rules:

```text
One row per ArticlePublicId.
ViewCount >= 0.
View updates are atomic.
No raw per-view history is stored in V1.
```

---

### 4.3 `ArticleLike`

| Field | Purpose |
|---|---|
| `ArticleLikeId` | Internal PK |
| `PublicId` | Public identity |
| `ArticlePublicId` | Article logical identity |
| `UserId` | Authenticated user identity |
| `IsActive` | Current like state |
| `LikedAtUtc` | Like timestamp |
| `UnlikedAtUtc` | Unlike timestamp, nullable |
| `Version` | Mutation version |
| `CreatedAtUtc`, `UpdatedAtUtc` | Tracking fields |

Invariant:

```text
At most one active like per (ArticlePublicId, UserId).
```

---

### 4.4 `Comment`

| Field | Purpose |
|---|---|
| `CommentId` | Internal PK |
| `PublicId` | Public identity |
| `ArticlePublicId` | Article logical identity |
| `AuthorUserId` | Comment author |
| `ParentCommentId` | Nullable self-reference reserved for future replies |
| `Content` | Comment text |
| `Status` | Current visibility/moderation status |
| `Version` | Concurrency version |
| `CreatedAtUtc`, `UpdatedAtUtc` | Tracking fields |
| `DeletedAtUtc` | Author deletion timestamp, nullable |

Statuses:

```text
Pending
Visible
Rejected
Hidden
Deleted
```

V1 rule:

```text
Every V1-created comment has ParentCommentId = NULL.
```

---

### 4.5 `CommentReport`

| Field | Purpose |
|---|---|
| `CommentReportId` | Internal PK |
| `PublicId` | Admin-facing identity |
| `CommentId` | Reported comment |
| `CommentModerationCaseId` | Owning review case |
| `ReporterUserId` | Reporting user |
| `ReasonCode` | Allowlisted report reason |
| `Description` | Optional bounded text |
| `Status` | Report status |
| `CreatedAtUtc` | Creation timestamp |
| `ResolvedAtUtc` | Resolution timestamp, nullable |

Statuses:

```text
Pending
Dismissed
Actioned
ClosedByAuthorDeletion
```

Invariant:

```text
One report per (CommentId, ReporterUserId).
```

---

### 4.6 `CommentModerationCase`

| Field | Purpose |
|---|---|
| `CommentModerationCaseId` | Internal PK |
| `PublicId` | Admin-facing identity |
| `CommentId` | Target comment |
| `Status` | Case status |
| `Priority` | Moderation priority |
| `HighestSeverity` | Highest evaluated severity in case |
| `AlertTriggeredAtUtc` | Alert-trigger timestamp, nullable |
| `AlertLevel` | Triggered alert level, nullable |
| `AlertMessageId` | Alert correlation/message identity, nullable |
| `OpenedAtUtc` | Open timestamp |
| `ResolvedAtUtc` | Resolution timestamp, nullable |
| `ResolvedByUserId` | Moderator identity, nullable |
| `ResolutionType` | Resolution result, nullable |
| `ResolutionReasonCode` | Resolution reason, nullable |
| `ResolutionNote` | Optional bounded note |
| `Version` | Concurrency version |

Statuses:

```text
Open
Dismissed
Actioned
ClosedByAuthorDeletion
```

Invariants:

```text
At most one Open case per Comment.
One Open case triggers at most one admin-alert intent in V1.
```

---

### 4.7 `CommentModerationActionHistory`

| Field | Purpose |
|---|---|
| `CommentModerationActionHistoryId` | Internal PK |
| `PublicId` | Admin-facing identity |
| `CommentId` | Affected comment |
| `CommentModerationCaseId` | Related case, nullable |
| `ActionType` | Moderation action |
| `FromStatus`, `ToStatus` | Transition state |
| `ActorUserId` | Acting user/admin, nullable |
| `ActorType` | Actor classification |
| `ReasonCode` | Action reason, nullable |
| `Note` | Optional bounded note |
| `OccurredAtUtc` | Action timestamp |
| `CorrelationId` | Flow correlation identity |

Action types:

```text
Approve
Reject
Hide
Restore
DismissReportedCase
HideReportedComment
CloseCaseByAuthorDeletion
```

---

### 4.8 `ArticleInteractionStats`

| Field | Purpose |
|---|---|
| `ArticleInteractionStatsId` | Internal PK |
| `ArticlePublicId` | Article logical identity |
| `ViewCount` | Published view count |
| `LikeCount` | Published active-like count |
| `VisibleCommentCount` | Published visible-comment count |
| `StatsVersion` | Monotonic snapshot version |
| `LastMaterializedAtUtc` | Materialization time |
| `LastPublishedMessageId` | Last publication message, nullable |
| `LastPublishedAtUtc` | Last publication time, nullable |
| `CreatedAtUtc`, `UpdatedAtUtc` | Tracking fields |

Rules:

```text
One row per ArticlePublicId.
Counters are non-negative.
StatsVersion is monotonic.
```

## 5) Relationships

### 5.1 Interaction-local relationships

```text
Comment.ParentCommentId
    -> Comment.CommentId                         // reserved for future replies

CommentReport.CommentId
    -> Comment.CommentId

CommentReport.CommentModerationCaseId
    -> CommentModerationCase.CommentModerationCaseId

CommentModerationCase.CommentId
    -> Comment.CommentId

CommentModerationActionHistory.CommentId
    -> Comment.CommentId

CommentModerationActionHistory.CommentModerationCaseId
    -> CommentModerationCase.CommentModerationCaseId
```

### 5.2 Logical external references

```text
ArticlePublicId -> Content-owned article identity
UserId / AuthorUserId / ReporterUserId / ResolvedByUserId / ActorUserId
    -> Identity-owned user identity
```

---

## 6) Database Invariants and Index Direction

| Table | Required constraint / index direction |
|---|---|
| `ArticleInteractionTargetProjection` | Unique `ArticlePublicId`; index for `RequiresResync` |
| `ArticleViewCount` | Unique `ArticlePublicId`; counter checks |
| `ArticleLike` | Unique active `(ArticlePublicId, UserId)` |
| `Comment` | Unique `PublicId`; index by `(ArticlePublicId, Status, ParentCommentId, CreatedAtUtc)`; index for admin status queue |
| `CommentReport` | Unique `(CommentId, ReporterUserId)`; index by case/status |
| `CommentModerationCase` | Unique open case per `CommentId`; index by status/priority/opened time |
| `CommentModerationActionHistory` | Index by comment/time and case/time |
| `ArticleInteractionStats` | Unique `ArticlePublicId`; counter checks |

Required check-constraint values:

```text
Comment.Status:
    Pending, Visible, Rejected, Hidden, Deleted

CommentReport.Status:
    Pending, Dismissed, Actioned, ClosedByAuthorDeletion

CommentModerationCase.Status:
    Open, Dismissed, Actioned, ClosedByAuthorDeletion

ArticleViewCount.ViewCount >= 0
ArticleInteractionStats.ViewCount >= 0
ArticleInteractionStats.LikeCount >= 0
ArticleInteractionStats.VisibleCommentCount >= 0
```

---

## 7) Redis / Cache Posture

Redis is optional in V1 and may support only temporary operational controls such as:

```text
Rate-limit keys
Short-lived repeat-view suppression keys
Temporary anti-abuse signals
```

Rules:

```text
Redis is not durable Interaction truth.
Accepted view state is stored durably in ArticleViewCount.
Like/comment/report/case truth remains in SQL Server.
```

---

## 8) Transaction and Async Rules

For operations requiring downstream propagation:

```text
Interaction state mutation
+ required CommentModerationActionHistory
+ required OutboxMessage
```

must commit atomically.

Outbound async direction:

| Consumer | Event |
|---|---|
| Reading | `interaction.article_counters_projection_published` |
| Notifications | `interaction.comment_report_alert_triggered` |
| Audit | Moderation-relevant Interaction events |

Inbound async direction:

| Producer | Result in Interaction |
|---|---|
| Content | Updates `ArticleInteractionTargetProjection` |

---

## 9) Article Lifecycle Behavior

When Content confirms that an article is unpublished, archived or soft-deleted:

```text
Stop accepting new public interactions.
Preserve existing Interaction truth, workflow history and counters.
```

| State / Operation | Behavior |
|---|---|
| New view contribution | Not accepted |
| New like | Not accepted |
| New comment | Not accepted |
| New report | Not accepted |
| Unlike own active like | Allowed |
| Delete own comment | Allowed |
| Resolve existing moderation case | Allowed when authorized |
| `ArticleViewCount` | Preserved |
| `ArticleInteractionStats` | Preserved |

If the same `ArticlePublicId` is later published again, preserved interaction state continues to apply.

---

## 10) Reconciliation Posture

Interaction may repair derived state as follows:

```text
ArticleInteractionTargetProjection
    -> resync from Content-owned source state

LikeCount
    -> recount active ArticleLike rows

VisibleCommentCount
    -> recount Comment rows where Status = Visible

ViewCount
    -> read preserved ArticleViewCount state
```

V1 does not reconstruct `ViewCount` from raw historical page-view records because no such raw history is stored.

If the outward public counter snapshot changes during repair:

```text
Update ArticleInteractionStats
Increment StatsVersion
Publish interaction.article_counters_projection_published
```

---

## 11) DBML Entity List

The Interaction ERD / DBML should contain:

```text
ArticleInteractionTargetProjection
ArticleViewCount
ArticleLike
Comment
CommentReport
CommentModerationCase
CommentModerationActionHistory
ArticleInteractionStats
```

Shared `OutboxMessage` may be referenced as infrastructure, but it is not an Interaction business entity.

---

## 12) Final Data Posture

```text
Interaction owns like, comment, report and moderation workflow truth.

Content asynchronously supplies article interaction eligibility.

Views are stored through durable ArticleViewCount, not ArticleViewEvent.

Comments are top-level only in V1; ParentCommentId is reserved for future use.

Reports never automatically hide comments.

One open moderation case may trigger at most one async admin-alert intent.

ArticleInteractionStats publishes versioned public counter snapshots to Reading.

Unpublish disables new interaction but preserves counters and history.

Redis is optional temporary support only, never durable truth.

Async correctness relies on local transaction + Outbox,
durable consumer dedupe, version-aware apply and bounded reconciliation.
```
