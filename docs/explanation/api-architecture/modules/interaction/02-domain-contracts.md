# Interaction — Domain Contracts (V1)

This document defines the Interaction-owned domain contracts for V1.
It focuses on domain records, derived state, statuses, integration-event contracts, versioning, and ownership boundaries.

Related:

- `01-api-surface.md`
- `03-runtime-flows.md`
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

## 1) Module Role

Interaction owns user engagement and moderation workflow around public articles.

Interaction V1 is responsible for:

- accepted article view-count materialization;
- article like and unlike relationships;
- top-level comment submission;
- public visible-comment query semantics;
- author deletion of their own comment;
- administrative comment moderation;
- user reports against visible comments;
- reported-comment moderation cases;
- local moderation action history;
- administrator alert intent when report escalation policy is reached;
- derived public counter snapshot publication for Reading.

Interaction does not own article publication truth or public article-page composition.

---

## 2) Ownership Boundary

### 2.1 Interaction owns authoritative truth/workflow state

Interaction is the authoritative owner of:

```text
ArticleLike
Comment
CommentReport
CommentModerationCase
CommentModerationActionHistory
```

These records represent business facts or workflow decisions made within Interaction.

### 2.2 Interaction owns derived/processing state

Interaction also owns the following derived or processing records:

```text
ArticleInteractionTargetProjection
ArticleViewCount
ArticleInteractionStats
InteractionConsumedMessage
```

These records support eligibility checks, view counting, public counter publication and safe async processing.

### 2.3 Interaction does not own

| Concern | Owning module | Interaction usage |
|---|---|---|
| Article content and lifecycle truth | Content | Consumes public/eligibility projection input asynchronously |
| Public article read model | Reading | Publishes public counter snapshot input asynchronously |
| Slug and SEO metadata truth | SEO | No ownership |
| Media asset lifecycle and association truth | Media | No ownership |
| User authentication/account truth | Identity | Uses authenticated request actor context |
| Permissions and policies | Authorization | Requires permission checks for admin actions |
| Email delivery and recipient configuration | Notifications | Publishes admin-alert intent only |
| Canonical cross-system audit evidence | Audit | Publishes moderation facts asynchronously |

### 2.4 Projection ownership rule

Projected or copied data does not transfer source ownership.

In particular:

- `ArticleInteractionTargetProjection` does not make Interaction the owner of article publication lifecycle.
- `ArticleInteractionStats` does not make Reading the owner of interaction counters.
- Audit ingestion does not replace Interaction-owned moderation workflow state.

---

## 3) Truth, Derived State and Processing State

### 3.1 Truth/workflow classification

| Record | Classification | Authority |
|---|---|---|
| `ArticleLike` | Interaction truth | Active like relationship between user and article |
| `Comment` | Interaction truth | Comment content and current comment status |
| `CommentReport` | Interaction truth | A valid user-submitted report |
| `CommentModerationCase` | Interaction workflow truth | Current/resolved report-review cycle |
| `CommentModerationActionHistory` | Local operational history | Moderation decisions needed by admin operations |

### 3.2 Derived/processing classification

| Record | Classification | Authority posture |
|---|---|---|
| `ArticleInteractionTargetProjection` | Derived eligibility projection | Follows Content source state asynchronously |
| `ArticleViewCount` | Derived durable materialized view-count state | Represents accepted views under V1 policy |
| `ArticleInteractionStats` | Derived public counter snapshot state | Published to Reading |
| `InteractionConsumedMessage` | Durable processing state | Supports message dedupe and apply diagnostics |

### 3.3 Derived-state rule

Derived records may lag, require replay/resync/reconciliation or be temporarily unavailable.

Derived records must not be used as hidden replacement truth for:

- Content publication lifecycle;
- user like relationship existence;
- comment moderation decisions;
- report creation;
- moderation-case resolution.

---

## 4) Identifier and Versioning Conventions

### 4.1 Identifier direction

Conceptual records should distinguish:

| Identifier type | Usage |
|---|---|
| Internal database id | Persistence relation and efficient internal joins |
| Public id | External API/resource reference where applicable |
| `ArticlePublicId` | Cross-module stable article identity used by Interaction contracts |
| `MessageId` | Stable async message identity |
| `CorrelationId` | Cross-flow trace identity |

Interaction APIs and outbound events should use public identifiers rather than expose internal persistence ids.

### 4.2 Version-sensitive records

| Record | Version field | Purpose |
|---|---|---|
| `ArticleLike` | `Version` or equivalent conditional state guard | Prevent stale active/inactive relationship mutation where required |
| `Comment` | `Version` | Prevent stale moderation or author-delete transitions |
| `CommentModerationCase` | `Version` | Prevent duplicate/stale resolution and alert triggering |
| `ArticleInteractionTargetProjection` | `LastSourceVersion` | Prevent stale Content-derived eligibility apply |
| `ArticleViewCount` | `ViewVersion` | Track accepted-view materialized state progress |
| `ArticleInteractionStats` | `StatsVersion` | Order outbound public counter snapshots |
| `InteractionConsumedMessage` | Source `AggregateVersion` metadata where provided | Apply diagnostics and replay handling |

### 4.3 Freshness authority

```text
Versions and authoritative state transitions determine freshness.
Timestamps do not determine correctness ordering.
```

Timestamps remain useful for:

- display;
- investigation;
- operational lag measurements;
- retention;
- reporting.

---

## 5) ArticleInteractionTargetProjection

### 5.1 Purpose

`ArticleInteractionTargetProjection` is Interaction-owned derived eligibility state.

It allows Interaction to decide whether an article may accept new public interactions without synchronous request-path calls to Content or Reading.

### 5.2 Conceptual fields

```text
ArticleInteractionTargetProjection
- ArticleInteractionTargetProjectionId
- ArticlePublicId
- SourceArticleId nullable
- SourceStatus
- IsInteractionEnabled
- LastSourceVersion
- LastSourceMessageId
- LastSourceOccurredAtUtc
- LastSyncedAtUtc
- RequiresResync
```

### 5.3 Source ownership

The projection follows Content-owned public article lifecycle/projection input.

Conceptual inbound source event:

```text
content.article_read_projection_published
```

The exact event-type spelling must remain aligned with the finalized Content contract.

### 5.4 Eligibility contract

New public interaction is allowed only when:

```text
IsInteractionEnabled = true
AND RequiresResync = false
```

### 5.5 Apply contract

```text
If (ConsumerName, MessageId) was already processed:
    treat delivery as duplicate and do not reapply.

If IncomingSourceVersion <= LastSourceVersion:
    ignore as duplicate or stale.

If IncomingSourceVersion is safe forward progress:
    apply projection state.

If eligibility becomes uncertain or a correctness-significant gap is detected:
    mark projection as unsafe/requires resync;
    fail closed for new public interactions;
    execute approved reconciliation/resync posture.
```

### 5.6 Preservation contract

An article becoming unavailable for public interaction must disable new interactions but must not erase historical Interaction state or reset counters.

---

## 6) ArticleViewCount

### 6.1 Purpose

`ArticleViewCount` stores durable materialized count state for accepted article views.

Interaction V1 does not retain one durable record per individual page view.

### 6.2 Conceptual fields

```text
ArticleViewCount
- ArticleViewCountId
- ArticlePublicId
- ViewCount
- ViewVersion
- LastAcceptedViewAtUtc nullable
- CreatedAtUtc
- UpdatedAtUtc
```

### 6.3 Classification

`ArticleViewCount` is derived durable materialized state, not raw per-view truth history and not an integration event.

### 6.4 View eligibility contract

A page-view request contributes to `ViewCount` only when:

- article eligibility is confirmed locally;
- view request passes validation;
- request passes configured anti-abuse/repeat-view policy.

### 6.5 Mutation contract

Accepted concurrent views are valid independent contributions.

`ViewCount` must be incremented atomically by the authoritative store.

Disallowed behavior:

```text
Read ViewCount
Increment in application memory
Overwrite without atomic/concurrency-safe mutation
```

### 6.6 Recovery limitation

Because V1 does not store durable raw per-view records:

- `ViewCount` cannot be fully recomputed from individual-view history;
- `ArticleViewCount` must be protected through durable storage, atomic increment, monitoring and publication reconciliation;
- a future bucketed/raw analytics input model may be added without changing Interaction ownership.

---

## 7) ArticleLike

### 7.1 Purpose

`ArticleLike` represents an authenticated user's current like relationship with one article.

### 7.2 Conceptual fields

```text
ArticleLike
- ArticleLikeId
- PublicId
- ArticlePublicId
- UserId
- IsActive
- CreatedAtUtc
- RemovedAtUtc nullable
- UpdatedAtUtc nullable
- Version
```

### 7.3 Invariants

```text
At most one active ArticleLike exists per (ArticlePublicId, UserId).
```

Required behavior:

- concurrent likes from different users for one article are independent valid operations;
- duplicate/concurrent likes from the same user must produce at most one active relationship;
- retry after timeout must not create a second active like;
- unlike affects only the acting user's existing active relationship;
- unlike retry must not remove or decrement more than once.

### 7.4 Article lifecycle behavior

Creating a new like requires interaction-enabled article eligibility.

Removing an existing active like may remain allowed after the article is no longer public, because it retracts the user's own relationship.

### 7.5 Counter relation

`ArticleLike` is truth.

`LikeCount` is derived through `ArticleInteractionStats` and may converge asynchronously.

---

## 8) Comment

### 8.1 Purpose

`Comment` stores user-submitted comment content and current comment visibility/moderation status.

### 8.2 Conceptual fields

```text
Comment
- CommentId
- PublicId
- ArticlePublicId
- AuthorUserId
- ParentCommentId nullable
- Content
- Status
- CreatedAtUtc
- UpdatedAtUtc nullable
- DeletedAtUtc nullable
- Version
```

### 8.3 ParentCommentId reservation

`ParentCommentId` is reserved for future reply-comment capability.

In V1:

```text
ParentCommentId must be NULL for every newly created comment.
```

V1 does not support:

- creating replies;
- nested comment threads;
- reply query composition;
- reply-specific moderation;
- reply-specific counters.

Create-comment request contracts must not accept a parent-comment identifier in V1.

### 8.4 Comment statuses

Interaction V1 uses:

```text
Pending
Visible
Rejected
Hidden
Deleted
```

The existing domain enum must be extended with `Rejected` during implementation alignment.

| Status | Meaning | Publicly visible |
|---|---|---:|
| `Pending` | Submitted and awaiting moderation | No |
| `Visible` | Approved and eligible for display when article is public | Yes |
| `Rejected` | Rejected before being public | No |
| `Hidden` | Removed from public display by moderator/admin | No |
| `Deleted` | Deleted by comment author | No |

### 8.5 Submission contract

New comment creation requires:

- authenticated actor;
- interaction-enabled article;
- valid and safe comment content;
- `ParentCommentId = NULL`;
- initial `Status = Pending`.

### 8.6 Valid status transitions

| Current status | Action | Actor | Next status |
|---|---|---|---|
| `Pending` | Approve | Authorized moderator/admin | `Visible` |
| `Pending` | Reject | Authorized moderator/admin | `Rejected` |
| `Visible` | Hide | Authorized moderator/admin | `Hidden` |
| `Hidden` | Restore | Authorized moderator/admin | `Visible` |
| `Pending` | Delete own comment | Comment author | `Deleted` |
| `Visible` | Delete own comment | Comment author | `Deleted` |
| `Rejected` | Delete own comment | Comment author | `Deleted` |
| `Hidden` | Delete own comment | Comment author | `Deleted` |

### 8.7 Invalid transitions

| Current status | Invalid command |
|---|---|
| `Pending` | Hide |
| `Visible` | Approve again |
| `Visible` | Reject |
| `Rejected` | Approve, Restore or Hide |
| `Hidden` | Reject |
| `Deleted` | Approve, Reject, Hide or Restore |

### 8.8 Public query contract

Only comments satisfying both conditions may be returned publicly:

```text
Comment.Status = Visible
AND associated article is publicly exposable
```

### 8.9 Counter relation

`Comment` is truth.

`VisibleCommentCount` is derived and reflects comments currently in `Visible` status for public counter publication.

---

## 9) CommentReport

### 9.1 Purpose

`CommentReport` represents a valid report submitted by an authenticated non-author user against a visible comment.

### 9.2 Conceptual fields

```text
CommentReport
- CommentReportId
- PublicId
- CommentId
- CommentModerationCaseId
- ReporterUserId
- ReasonCode
- Description nullable
- Status
- CreatedAtUtc
- ResolvedAtUtc nullable
```

### 9.3 Report statuses

```text
Pending
Dismissed
Actioned
ClosedByAuthorDeletion
```

| Status | Meaning |
|---|---|
| `Pending` | Awaiting resolution in an open moderation case |
| `Dismissed` | Moderator reviewed and kept the comment visible |
| `Actioned` | Moderator resolved the case by hiding the comment |
| `ClosedByAuthorDeletion` | Comment author deleted the comment before case resolution |

### 9.4 Report eligibility

A report may be created only when:

- reporter is authenticated;
- target comment currently has `Status = Visible`;
- target article is interaction-enabled;
- reporter is not the comment author;
- reporter has not previously reported the same comment;
- input satisfies rate-limit and validation policy.

### 9.5 Uniqueness invariant

```text
At most one CommentReport exists per (CommentId, ReporterUserId).
```

This V1 invariant remains effective even if a previous moderation case for the same comment has already been resolved.

### 9.6 Report reason codes

Recommended V1 reason codes:

```text
Spam
Harassment
HateSpeech
Violence
SexualContent
PersonalInformation
Misinformation
OffTopic
Other
```

Validation contract:

| Field | Rule |
|---|---|
| `ReasonCode` | Required; must be allowlisted |
| `Description` | Optional except for `Other` |
| `Description` when `ReasonCode = Other` | Required and bounded |

### 9.7 No automatic moderation verdict

A report is an allegation requiring review.

Creating a report must not directly change:

- `Comment.Status`;
- public visible-comment count;
- public Reading projection counters.

---

## 10) CommentModerationCase

### 10.1 Purpose

`CommentModerationCase` represents one report-review cycle for a comment.

It groups related unresolved reports and records how that review cycle was resolved.

### 10.2 Conceptual fields

```text
CommentModerationCase
- CommentModerationCaseId
- PublicId
- CommentId
- Status
- Priority
- HighestSeverity
- AlertTriggeredAtUtc nullable
- AlertLevel nullable
- AlertMessageId nullable
- OpenedAtUtc
- ResolvedAtUtc nullable
- ResolvedByUserId nullable
- ResolutionType nullable
- ResolutionReasonCode nullable
- ResolutionNote nullable
- Version
```

### 10.3 Case statuses

```text
Open
Dismissed
Actioned
ClosedByAuthorDeletion
```

| Status | Meaning |
|---|---|
| `Open` | Reports are awaiting moderator decision |
| `Dismissed` | Moderator resolved the case without hiding the comment |
| `Actioned` | Moderator hid the comment in response to the case |
| `ClosedByAuthorDeletion` | Comment author deleted the comment before moderator resolution |

### 10.4 Case creation and joining

When a valid report is created:

```text
If no Open case exists for the comment:
    create one Open case;
    attach the report to that case.

If an Open case already exists:
    attach the report to the existing case;
    evaluate escalation policy.
```

### 10.5 Single-open-case invariant

```text
At most one Open CommentModerationCase exists per Comment.
```

This invariant must be enforced authoritatively, not by an application-only pre-check.

### 10.6 Case resolution contract

| Operation | Comment transition | Case transition | Pending reports transition |
|---|---|---|---|
| Dismiss reported case | No change; remains `Visible` | `Open -> Dismissed` | `Pending -> Dismissed` |
| Hide reported comment | `Visible -> Hidden` | `Open -> Actioned` | `Pending -> Actioned` |
| Author deletes reported comment | `Visible -> Deleted` | `Open -> ClosedByAuthorDeletion` | `Pending -> ClosedByAuthorDeletion` |

### 10.7 Alert escalation contract

An open case may trigger one asynchronous administrator-alert intent according to configured policy.

Initial V1 default:

| Condition in open case | Alert result |
|---|---|
| Three distinct authenticated normal-severity reporters | Trigger one admin-alert intent |
| One high-severity report | Trigger one admin-alert intent |
| One critical-severity report | Trigger one admin-alert intent |

```text
One Open moderation case may trigger at most one administrator-alert business intent in V1.
```

### 10.8 Versioning contract

`CommentModerationCase.Version` protects:

- concurrent resolution;
- concurrent threshold-reaching report submissions;
- duplicate alert triggering;
- stale admin commands;
- retry after ambiguous timeout.

---

## 11) CommentModerationActionHistory

### 11.1 Purpose

`CommentModerationActionHistory` is Interaction-owned local operational history required for admin moderation workflows.

It allows admin UI to inspect Interaction moderation decisions immediately after Interaction commits, without waiting for Audit async ingestion.

### 11.2 Conceptual fields

```text
CommentModerationActionHistory
- CommentModerationActionHistoryId
- PublicId
- CommentId
- CommentModerationCaseId nullable
- ActionType
- FromStatus nullable
- ToStatus nullable
- ActorUserId nullable
- ActorType
- ReasonCode nullable
- Note nullable
- OccurredAtUtc
- CorrelationId
```

### 11.3 Action types

Recommended V1 action types:

```text
Approve
Reject
Hide
Restore
DismissReportedCase
HideReportedComment
CloseCaseByAuthorDeletion
```

### 11.4 History scope

Required history includes:

- moderator approval/rejection/hide/restore decisions;
- moderator report-case resolutions;
- author deletion only where it closes an open reported-comment case.

Ordinary author deletion with no active moderation case does not have to produce a moderation-history row in V1.

### 11.5 Audit distinction

| Interaction moderation history | Audit |
|---|---|
| Operational state owned by Interaction | Canonical cross-system evidence owned by Audit |
| Written in Interaction workflow transaction where required | Receives events asynchronously |
| Supports admin comment/case views | Supports investigation/governance traceability |

---

## 12) ArticleInteractionStats

### 12.1 Purpose

`ArticleInteractionStats` stores Interaction-owned derived public counter snapshot state intended for publication to Reading.

### 12.2 Conceptual fields

```text
ArticleInteractionStats
- ArticleInteractionStatsId
- ArticlePublicId
- ViewCount
- LikeCount
- VisibleCommentCount
- StatsVersion
- LastMaterializedAtUtc
- LastPublishedMessageId nullable
- LastPublishedAtUtc nullable
- CreatedAtUtc
- UpdatedAtUtc
```

### 12.3 Counter derivation contract

| Counter | Source posture |
|---|---|
| `ViewCount` | Materialized accepted-view count from `ArticleViewCount` |
| `LikeCount` | Derived from active `ArticleLike` relationships |
| `VisibleCommentCount` | Derived from `Comment` records in `Visible` status |

### 12.4 Counter classification

`ArticleInteractionStats` is derived state.

It may lag behind:

- committed like/unlike truth;
- committed comment moderation truth;
- accepted view count mutation.

It must not be used to decide:

- whether a user has liked an article;
- whether a comment is visible;
- whether a report exists;
- whether moderation has been resolved.

### 12.5 StatsVersion contract

`StatsVersion` is monotonic per article counter snapshot.

Each outward public counter snapshot carries `StatsVersion`.

```text
StatsVersion, not timestamp, determines newer public counter state.
```

### 12.6 Article lifecycle preservation

When an article becomes unpublished, archived or soft-deleted:

- Interaction stops accepting new public interactions;
- `ArticleInteractionStats` is not reset;
- `ViewCount`, `LikeCount` and `VisibleCommentCount` remain preserved;
- if the same article becomes public again with the same `ArticlePublicId`, preserved interaction state continues to apply.

---

## 13) InteractionConsumedMessage

### 13.1 Purpose

`InteractionConsumedMessage` is durable consumer-processing state for Interaction-owned async consumers.

It supports:

- message-level idempotency;
- replay diagnostics;
- stale/version apply decisions;
- safe consumer restart behavior.

### 13.2 Conceptual fields

```text
InteractionConsumedMessage
- InteractionConsumedMessageId
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
- FailureCode nullable
- FailureDetail nullable
```

### 13.3 Required uniqueness

```text
Unique(ConsumerName, MessageId)
```

The consumer name is required because independent Interaction handlers may process different consequences of the same source message.

### 13.4 Apply decisions

Recommended diagnostic values:

```text
Applied
DuplicateIgnored
StaleIgnored
GapDetected
ResyncRequired
Failed
```

### 13.5 Processing rule

Message-level dedupe does not replace aggregate-version checks.

A different older message may arrive after a newer state has already been applied.

---

## 14) Status and Classification Constants

The Interaction domain should define allowlisted constants for the following contract values.

### 14.1 CommentStatus

```text
Pending
Visible
Rejected
Hidden
Deleted
```

### 14.2 CommentReportStatus

```text
Pending
Dismissed
Actioned
ClosedByAuthorDeletion
```

### 14.3 CommentModerationCaseStatus

```text
Open
Dismissed
Actioned
ClosedByAuthorDeletion
```

### 14.4 CommentModerationActionType

```text
Approve
Reject
Hide
Restore
DismissReportedCase
HideReportedComment
CloseCaseByAuthorDeletion
```

### 14.5 CommentReportReasonCode

```text
Spam
Harassment
HateSpeech
Violence
SexualContent
PersonalInformation
Misinformation
OffTopic
Other
```

### 14.6 ModerationReasonCode

```text
Spam
Harassment
HateSpeech
Violence
SexualContent
PersonalInformation
Misinformation
OffTopic
PolicyViolation
Other
```

### 14.7 ReportSeverity

```text
Normal
High
Critical
```

### 14.8 ReportAlertLevel

```text
High
Critical
```

---

## 15) Public Counter Contract

### 15.1 Public counter names

Interaction's public counter snapshot exposes:

```text
ViewCount
LikeCount
VisibleCommentCount
```

`VisibleCommentCount` means comments eligible for public display according to Interaction status, not all submitted comments.

### 15.2 Counter effects by state transition

| Operation | ViewCount | LikeCount | VisibleCommentCount |
|---|---:|---:|---:|
| Accepted view count contribution | Eventually increases | No effect | No effect |
| Like | No effect | Eventually increases | No effect |
| Unlike | No effect | Eventually decreases | No effect |
| Create pending comment | No effect | No effect | No effect |
| Approve comment: `Pending -> Visible` | No effect | No effect | Eventually increases |
| Reject comment: `Pending -> Rejected` | No effect | No effect | No effect |
| Hide comment: `Visible -> Hidden` | No effect | No effect | Eventually decreases |
| Restore comment: `Hidden -> Visible` | No effect | No effect | Eventually increases |
| Delete visible comment: `Visible -> Deleted` | No effect | No effect | Eventually decreases |
| Create/dismiss report | No effect | No effect | No effect |

### 15.3 Lag contract

Public counters may lag behind truth/workflow mutations.

Counter lag must not:

- change article visibility;
- expose unavailable articles;
- change comment status truth;
- cause the same user action to be accepted twice.

---

## 16) Inbound Integration Contract

### 16.1 Content to Interaction

Interaction consumes Content-derived article public/eligibility projection input to maintain:

```text
ArticleInteractionTargetProjection
```

Expected information required from the source message:

```text
MessageId
EventType
AggregateId / ArticlePublicId
Version
Status
IsPublic or equivalent interaction-eligibility information
OccurredAtUtc
CorrelationId
```

Conceptual event type:

```text
content.article_read_projection_published
```

The final event type and payload field names must match the Content module's finalized outbound contract.

### 16.2 Inbound apply semantics

Interaction must:

- dedupe by `(ConsumerName, MessageId)`;
- apply only valid forward source-version progress;
- ignore stale or duplicate inputs safely;
- fail closed for new interactions when eligibility is uncertain or requires resync.

---

## 17) Outbound Integration Contracts

### 17.1 Interaction business/workflow events

Recommended Interaction-emitted events:

```text
interaction.article_liked
interaction.article_unliked

interaction.comment_created
interaction.comment_approved
interaction.comment_rejected
interaction.comment_hidden
interaction.comment_restored
interaction.comment_deleted_by_author

interaction.comment_reported
interaction.comment_reports_dismissed
interaction.comment_report_alert_triggered
```

### 17.2 Reading counter projection event

Interaction publishes a known-value public counter snapshot for Reading:

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

Required contract behavior:

- Reading consumes a known-value snapshot, not raw interaction increments;
- `StatsVersion` is the freshness authority;
- duplicate/stale snapshots must be safely ignored downstream;
- counter lag is acceptable and does not govern public article visibility.

### 17.3 Notifications alert-intent event

Interaction publishes:

```text
interaction.comment_report_alert_triggered
```

Conceptual payload:

```json
{
  "commentModerationCasePublicId": "01J...",
  "commentPublicId": "01J...",
  "articlePublicId": "01J...",
  "alertLevel": "High",
  "alertReason": "ReportThresholdReached",
  "distinctReporterCount": 3,
  "highestSeverity": "Normal",
  "triggeredAtUtc": "2026-05-25T10:35:00Z"
}
```

Notifications must apply durable business-intent dedupe equivalent to:

```text
InteractionCommentReportAlert:{CommentModerationCasePublicId}
```

### 17.4 Audit-facing moderation events

Audit may consume moderation-relevant events including:

```text
interaction.comment_approved
interaction.comment_rejected
interaction.comment_hidden
interaction.comment_restored
interaction.comment_deleted_by_author
interaction.comment_reports_dismissed
```

For a reported-comment hide, `interaction.comment_hidden` should be sufficient when its payload contains:

```text
ResolutionSource = Report
CommentModerationCasePublicId
ResolvedReportCount
ReasonCode
ModeratorUserId
```

A separate `interaction.comment_reports_actioned` event is not required in V1.

---

## 18) Event Envelope Contract

Important outbound Interaction messages must include the shared async envelope identity fields:

```text
MessageId
EventType
AggregateType
AggregateId
AggregatePublicId where applicable
Version where ordered apply matters
Payload
CorrelationId
InitiatorUserId where applicable
OccurredAtUtc
```

### 18.1 Aggregate/version usage examples

| Event family | Aggregate identity | Version |
|---|---|---|
| Comment moderation event | Comment identity | `Comment.Version` |
| Moderation-case alert/dismiss event | Moderation case identity | `CommentModerationCase.Version` |
| Counter projection event | Article interaction stats/article identity | `ArticleInteractionStats.StatsVersion` |
| Like/unlike event | Like relationship identity or approved contract identity | Relationship version/state marker where emitted |

### 18.2 Outbox rule

Any emitted event required by an Interaction mutation must be written to the shared outbox in the same local transaction as the relevant Interaction state mutation and required local history.

---

## 19) Idempotency and Concurrency Contract

### 19.1 Required business invariants

```text
One active ArticleLike per (ArticlePublicId, UserId)
One CommentReport per (CommentId, ReporterUserId)
At most one Open CommentModerationCase per Comment
V1-created Comment.ParentCommentId must be NULL
One administrator-alert business intent per Open CommentModerationCase
```

### 19.2 State-transition protection

| Workflow | Required protection |
|---|---|
| Comment approve/reject/hide/restore | Current status + `Comment.Version` |
| Author delete comment | Ownership + current status/version guard; idempotent deleted result |
| Resolve moderation case | Current case status + `CommentModerationCase.Version` |
| Trigger case alert | Open case + alert-not-already-triggered + version/atomic conditional mutation |
| Consume Content projection | Message dedupe + source-version apply |

### 19.3 Timeout ambiguity contract

A timeout does not prove a write did not commit.

Therefore:

- like/unlike must tolerate same-intent retry safely;
- report creation must not create duplicate reports or duplicate alert effects;
- author-delete retry must not repeat side effects;
- moderation retry must not repeat transitions/history/events;
- Notifications must own email delivery ambiguity handling.

---

## 20) Recovery and Reconciliation Contract

### 20.1 Eligibility projection

`ArticleInteractionTargetProjection` recovery posture:

- dedupe repeated source messages;
- reject stale source versions;
- fail closed under uncertain eligibility;
- support bounded resync/reconciliation from Content source state.

### 20.2 Like and comment counter reconciliation

The following public counters are reconcilable from Interaction truth:

```text
LikeCount = COUNT(active ArticleLike)
VisibleCommentCount = COUNT(Comment WHERE Status = Visible)
```

### 20.3 View-count posture

`ArticleViewCount` is durable materialized count state.

Because no raw per-view history is retained in V1:

- exact historical recomputation from individual views is not supported;
- recovery relies on durable state, operational backup/recovery, atomic update correctness and snapshot republication;
- future view bucket/raw analytics support may be introduced separately.

### 20.4 Counter snapshot recovery

After repair/reconciliation of Interaction public counters:

- materialize the corrected `ArticleInteractionStats`;
- increment `StatsVersion` if the public snapshot changes;
- publish a new `interaction.article_counters_projection_published` message;
- allow Reading to apply the newer snapshot by version.

---

## 21) V1 Deferred Contracts

Interaction V1 reserves no behavioral contract for the following features beyond explicitly noted schema compatibility:

| Deferred capability | Current contract posture |
|---|---|
| Reply comments | `ParentCommentId` may exist but must remain `NULL` for V1-created comments |
| Nested discussion threads | Not supported |
| Comment editing | Not supported |
| Comment reactions | Not supported |
| Article reactions beyond like | Not supported |
| Auto-hide by report threshold | Not supported |
| Bulk moderation | Not supported |
| Article-author moderation | Not supported |
| Reporter reputation scoring | Not supported |
| Raw per-view analytics history | Not supported |
| Trending/engagement ranking pipeline | Not supported |
| Full scheduled batch infrastructure | Not required in initial V1 implementation |

---

## 22) Domain Contract Summary

```text
Interaction truth/workflow:
    ArticleLike
    Comment
    CommentReport
    CommentModerationCase
    CommentModerationActionHistory

Interaction derived/processing state:
    ArticleInteractionTargetProjection
    ArticleViewCount
    ArticleInteractionStats
    InteractionConsumedMessage

Comment statuses:
    Pending
    Visible
    Rejected
    Hidden
    Deleted

Reply compatibility:
    Comment.ParentCommentId nullable is reserved for future use.
    V1-created comments always use ParentCommentId = NULL.

Reading output:
    interaction.article_counters_projection_published

Notifications output:
    interaction.comment_report_alert_triggered

Correctness posture:
    Local Interaction mutation + outbox commit atomically.
    Consumers dedupe by stable message identity.
    Ordered state applies by version, not timestamp.
    Counters and eligibility projections may lag and require recovery posture.
    Report threshold may alert administrators but never automatically hides content.
```
