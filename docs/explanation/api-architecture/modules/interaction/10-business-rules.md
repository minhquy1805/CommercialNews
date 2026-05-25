# Interaction — Business Rules (V1)

This document defines the core business rules for Interaction in V1.
It focuses on article views, likes, comments, moderation workflow, reporting, counters, async projection publication, and recovery/reconciliation rules.

Related:

- `01-api-surface.md`
- `02-domain-contracts.md`
- `03-runtime-flows.md`
- `04-errors-status-codes.md`
- `05-security-abuse-controls.md`
- `06-idempotency-consistency.md`
- `07-observability-slos.md`
- `08-dependencies-and-ownership.md`
- `09-open-questions.md`
- ADR-0013 — Outbox & Delivery Semantics (V1)
- ADR-0018 — Transaction Boundaries & Consistency Model (V1)
- ADR-0019 — System Model and Fault Assumptions (V1)
- ADR-0020 — Timeout, Retry, and Failure Detection Policy (V1)
- ADR-0022 — Versioning and Fencing Strategy (V1)
- ADR-0025 — Batch Processing and Derived State Policy (V1)
- ADR-0027 — Stream Processing and Derived State Policy (V1)
- ADR-0028 — Consumer Idempotency, Replay, and Rebuild Policy (V1)

---

## 1) Purpose

The Interaction module owns user engagement behavior around publicly available articles.

Interaction V1 supports:

- article view counting;
- article like and unlike;
- top-level comment submission;
- public reading of visible comments;
- author deletion of their own comments;
- administrative comment moderation;
- user reporting of visible comments;
- moderation cases for reported comments;
- asynchronous administrator alert triggering for high-priority reported comments;
- derived public counter publication for Reading.

Interaction must remain independent from public read composition, article lifecycle truth, email delivery, and canonical audit persistence.

---

## 2) Ownership Boundary

### 2.1 Interaction owns

Interaction is the authoritative owner of:

- active article-like relationships;
- comment content submitted through Interaction;
- current comment moderation/visibility status;
- comment reports submitted by authenticated users;
- moderation-case workflow state for reported comments;
- local moderation action history required for admin operations;
- accepted/materialized article view-count state under the configured anti-abuse policy;
- Interaction-derived public counter snapshot state;
- Interaction consumer dedupe/apply state.

### 2.2 Interaction does not own

| Concern | Owning module |
|---|---|
| Article content, lifecycle, publication, archival and soft deletion | Content |
| Public article read model and website article composition | Reading |
| Slug routing and SEO metadata truth | SEO |
| Media assets and article-media association truth | Media |
| User authentication/account truth | Identity |
| Roles, permissions and policy evaluation | Authorization |
| Email delivery and recipient configuration | Notifications |
| Canonical cross-system audit evidence | Audit |

### 2.3 Ownership does not transfer through projection

Copied or projected data inside Interaction does not transfer ownership from its source module.

In particular:

- `ArticleInteractionTargetProjection` does not make Interaction the owner of article publication state.
- `ArticleInteractionStats` does not make Reading the owner of interaction counters.
- Audit copies of moderation events do not replace Interaction operational workflow state.

---

## 3) Data Classification

Interaction V1 distinguishes between truth/workflow state and derived/processing state.

### 3.1 Truth and workflow state

The following records represent authoritative Interaction facts or workflow decisions:

```text
ArticleLike
Comment
CommentReport
CommentModerationCase
CommentModerationActionHistory
```

| Record | Meaning |
|---|---|
| `ArticleLike` | Whether an authenticated user currently likes an article |
| `Comment` | The submitted comment content and its current visibility/moderation status |
| `CommentReport` | A valid report submitted by an authenticated user against a visible comment |
| `CommentModerationCase` | One review cycle for unresolved reports against one comment |
| `CommentModerationActionHistory` | Local operational history of moderator decisions associated with comment moderation or a moderation case |

### 3.2 Derived and processing state

The following records are derived, materialized, projected or processing-related state:

```text
ArticleInteractionTargetProjection
ArticleViewCount
ArticleInteractionStats
InteractionConsumedMessage
```

| Record | Meaning |
|---|---|
| `ArticleInteractionTargetProjection` | Local eligibility projection consumed from Content-derived public article state |
| `ArticleViewCount` | Durable materialized count of accepted article views under V1 anti-abuse policy |
| `ArticleInteractionStats` | Derived public-facing counter snapshot state for publication to Reading |
| `InteractionConsumedMessage` | Durable consumer idempotency/apply tracking state |

### 3.3 Derived-state posture

Derived state:

- may lag behind truth;
- may be unavailable temporarily;
- must be observable;
- must not silently replace truth ownership;
- must have an approved reconciliation or recovery posture.

---

## 4) Article Interaction Eligibility

### 4.1 Eligibility source

Interaction must determine whether an article accepts new public interactions using its local `ArticleInteractionTargetProjection`.

That projection is maintained asynchronously from Content-owned public article lifecycle/projection messages.

Interaction must not synchronously query Content or Reading for each ordinary public interaction request in V1.

### 4.2 Eligibility states

An article accepts new public interactions only when the local projection confirms it is publicly interactable.

| Source-derived article condition | New view | New like | New comment | New report |
|---|---:|---:|---:|---:|
| Publicly available / interaction-enabled | Allowed | Allowed | Allowed | Allowed only for visible comments |
| Draft / not yet public | Rejected or safely ignored | Rejected | Rejected | Rejected |
| Unpublished / not public | Rejected or safely ignored | Rejected | Rejected | Rejected |
| Archived | Rejected or safely ignored | Rejected | Rejected | Rejected |
| Soft deleted | Rejected or safely ignored | Rejected | Rejected | Rejected |
| Projection missing or uncertain | Rejected or safely ignored | Rejected | Rejected | Rejected |
| Projection requires resync | Rejected or safely ignored | Rejected | Rejected | Rejected |

### 4.3 Fail-closed rule

```text
Unknown or uncertain interaction eligibility must not be treated as public eligibility.
```

Temporary inability to like, comment, report or count a new view is preferred over accepting interactions for an article that may no longer be public.

### 4.4 Existing user-owned state after article becomes unavailable

When an article is no longer publicly interactable:

- existing likes are preserved;
- existing comments are preserved;
- existing reports and moderation history are preserved;
- derived counters are preserved unless later reconciled;
- no new public interactions are accepted.

An authenticated user may still remove their own existing content or relationship where permitted by the relevant rule below:

- unlike may retract an existing active like;
- comment author may delete their own existing comment.

---

## 5) Article View Count Rules

### 5.1 V1 view model

Interaction V1 uses:

```text
ArticleViewCount
```

Interaction V1 does not retain one permanent row for each individual page view.

`ArticleViewCount` represents durable materialized count state for views accepted under the configured anti-abuse policy.

### 5.2 View eligibility

A view may contribute to `ArticleViewCount` only when:

- the target article is interaction-enabled according to local projection;
- the request satisfies public input validation requirements;
- the request passes configured anti-abuse and repeat-view controls.

Both anonymous and authenticated readers may produce eligible accepted views.

### 5.3 View acceptance is policy-controlled

A raw page-load request is not automatically a countable view.

The implementation must support operational controls such as:

- request rate limiting;
- repeat-view suppression within a configured window;
- anonymous-session or equivalent bounded dedupe signals;
- malformed-request rejection;
- bot or abusive-traffic filtering where adopted.

Threshold values and detailed detection mechanisms are operational/configuration concerns and are not hard-coded as immutable domain constants in V1 business rules.

### 5.4 View count mutation

Accepted concurrent views are valid independent contributions.

`ArticleViewCount` must therefore support server-side atomic increments.

The implementation must not use unsafe read-modify-write behavior such as:

```text
read current count
increment in application memory
overwrite count without concurrency-safe mutation
```

### 5.5 View versioning

`ArticleViewCount` may maintain a monotonic `ViewVersion` or equivalent counter-state version.

The version is used for:

- freshness diagnostics;
- safe materialization into public counter snapshot state;
- reconciliation and publication tracking.

It must not be used to reject otherwise valid concurrent accepted views merely because other users viewed the same article concurrently.

### 5.6 No per-view outbound integration event

Interaction must not publish an outbound Reading integration message for every accepted view.

```text
Individual accepted views are not published to Reading.
Reading consumes only aggregated public counter snapshot messages.
```

This avoids:

- outbox amplification;
- RabbitMQ amplification;
- duplicate-driven view inflation in downstream projections;
- unnecessary coupling between Reading and raw view-intake behavior.

### 5.7 View recovery posture

Because V1 does not retain raw per-view records:

- `ViewCount` is not rebuilt from a full durable history of individual views;
- it is treated as a durable materialized accepted-view count;
- it must be protected by atomic mutation, operational monitoring and snapshot publication discipline;
- a future analytics or bucketed-view pipeline may be introduced without changing Interaction ownership.

---

## 6) Article Like and Unlike Rules

### 6.1 Authentication requirement

Only authenticated users may like or unlike an article.

The acting user identity must come from trusted server-side request context, never from a client-supplied `UserId`.

### 6.2 Like eligibility

A new like may be created only when:

- the target article is interaction-enabled;
- the authenticated user does not already have an active like for that article.

### 6.3 Unique active-like invariant

```text
At most one active ArticleLike may exist per (ArticlePublicId, UserId).
```

This invariant must be protected by database-level enforcement or an equivalent authoritative conditional mutation strategy.

Application-level check-then-insert logic alone is not sufficient.

### 6.4 Concurrent likes from different users

Concurrent likes from different authenticated users for the same article are independent valid mutations.

```text
User A liking Article X must not conflict merely because User B likes Article X concurrently.
```

The domain model must not force every like request to compete on one article-wide truth lock.

### 6.5 Like success

A successful like operation commits:

- the active `ArticleLike` truth mutation;
- required outbox intent for asynchronous downstream processing.

A successful like does not require:

- Reading counter projection completion;
- Audit ingestion completion;
- any email or external effect.

### 6.6 Unlike behavior

An authenticated user may retract their own existing active like.

Unlike may be permitted even if the article has since become non-public, because it removes the user's own relationship rather than creating a new public interaction.

An unlike operation must:

- affect only the acting user's active like;
- produce no negative aggregate count;
- remain safe under retry and concurrent repeated requests.

### 6.7 Timeout and retry behavior

A timeout does not prove a like or unlike operation failed.

Therefore:

- duplicate like requests must not create duplicate active-like truth;
- duplicate unlike requests must not remove more than one active-like truth;
- clients may read their current like state after an ambiguous outcome;
- counter projection may converge asynchronously after truth has committed.

---

## 7) Comment Status and Visibility Rules

### 7.1 Supported comment statuses

Interaction V1 uses the following statuses:

```text
Pending
Visible
Rejected
Hidden
Deleted
```

The existing Interaction domain enum must be extended with `Rejected` when implementation is updated.

### 7.2 Status meaning

| Status | Meaning | Publicly visible |
|---|---|---:|
| `Pending` | Submitted and awaiting moderation | No |
| `Visible` | Approved for public display and not administratively hidden or author-deleted | Yes, only while the article remains public |
| `Rejected` | Reviewed and rejected before becoming public | No |
| `Hidden` | Removed from public visibility by admin/moderator action | No |
| `Deleted` | Removed by the comment author | No |

### 7.3 Public visibility condition

A comment is publicly returned only when:

- `Comment.Status = Visible`; and
- the associated article remains publicly eligible according to the relevant public-read boundary.

A visible comment attached to an unavailable article must not be publicly exposed merely because the comment row still remains `Visible`.

### 7.4 Status semantics

```text
Visible  = the comment has been allowed for public display.
Rejected = the comment was rejected before public visibility.
Hidden   = the comment was administratively removed from public visibility.
Deleted  = the author removed their own comment.
```

`Hidden` and `Deleted` must not be used interchangeably.

---

## 8) Comment Submission Rules

### 8.1 Authentication requirement

Only authenticated users may submit comments in V1.

Anonymous comment submission is not supported.

### 8.2 Supported comment shape and future reply compatibility

Interaction V1 supports top-level comments only.

The `Comment` model may reserve a nullable self-reference:

```text
ParentCommentId nullable
```

This field exists only to preserve future compatibility with a later reply-comment capability.

In V1:

- every newly created comment must have `ParentCommentId = NULL`;
- create-comment APIs must not accept a parent-comment identifier;
- public comment APIs must not expose reply or nested-thread behavior;
- admin moderation APIs must treat all V1 comments as top-level comments;
- reporting and moderation-case behavior applies only to top-level comments;
- `VisibleCommentCount` counts only supported V1 top-level visible comments.

The following capabilities are deferred:

- creating replies;
- nested discussion threads;
- loading comment threads;
- replying to a reply;
- comment reactions;
- comment editing.

Future reply support requires explicit business rules for:

- maximum reply depth;
- whether only one-level replies are supported;
- parent comment visibility requirements;
- behavior when a parent comment is hidden or deleted;
- report and moderation-case handling for replies;
- whether replies contribute to public comment counters;
- public query and paging composition for comment threads.

### 8.3 Submission eligibility

A new comment may be submitted only when the target article is interaction-enabled.

### 8.4 Initial comment status

Every newly submitted comment begins as:

```text
Pending
```

A newly submitted comment:

- must not be publicly exposed;
- must not contribute to visible/public comment counters;
- may appear in authorized admin moderation queries.

### 8.5 Comment content handling

Comment input is untrusted external input.

The implementation must support:

- empty-content rejection;
- maximum-length validation;
- safe rendering/sanitization policy;
- rate limiting;
- safe logging that does not unnecessarily expose sensitive content.

### 8.6 Duplicate submission posture

A client timeout may occur after comment truth has already committed.

V1 API contracts must either:

- support an idempotency key for comment creation; or
- explicitly document the accepted duplicate-submission behavior and mitigation.

Production-oriented implementation should prefer request idempotency protection for comment creation.

---

## 9) Admin Comment Moderation Rules

### 9.1 Moderation authorization

Only actors authorized through Interaction moderation permissions may moderate comments.

Recommended permission boundary:

```text
Interaction.Comments.Read
Interaction.Comments.Moderate
```

Authorization determines whether an actor may perform a command. Interaction remains the owner of the comment state transition.

### 9.2 Valid moderator transitions

| Current status | Moderator action | Next status | Public visibility effect |
|---|---|---|---|
| `Pending` | Approve | `Visible` | Comment becomes publicly eligible |
| `Pending` | Reject | `Rejected` | Comment remains non-public |
| `Visible` | Hide | `Hidden` | Comment is removed from public display |
| `Hidden` | Restore | `Visible` | Comment becomes publicly eligible again, subject to article visibility |

### 9.3 Invalid moderator transitions

| Current status | Invalid action | Reason |
|---|---|---|
| `Pending` | Hide | Comment has not been public |
| `Visible` | Approve | Comment is already visible |
| `Visible` | Reject | A previously public comment must be hidden, not retroactively rejected |
| `Rejected` | Hide | Rejected comment is already non-public |
| `Rejected` | Approve / Restore | Re-opening rejected comments is deferred in V1 |
| `Hidden` | Reject | Hidden status preserves that the comment was previously public |
| `Deleted` | Approve / Reject / Hide / Restore | Author deletion is terminal for public visibility in V1 |

### 9.4 Moderation reasons

The following actions require moderation reason handling:

| Action | Reason requirement |
|---|---|
| Approve | Optional |
| Reject | Required |
| Hide | Required |
| Restore | Optional but should support a note |
| Dismiss reported-comment case | Required or strongly validated according to admin contract |

Recommended moderation reason codes:

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

When `Other` is selected, a moderation note must be required.

### 9.5 Version-sensitive moderation

Comment moderation is a stale-write-sensitive workflow.

`Comment` must support a version/concurrency mechanism so that:

- two moderators cannot silently overwrite each other's decision;
- a stale admin screen cannot apply a transition based on outdated state;
- repeated command delivery after timeout cannot duplicate a completed transition.

A moderator command must succeed only if the current authoritative comment status and expected version permit the requested transition.

---

## 10) Comment Author Deletion Rules

### 10.1 Direct author deletion

A comment author may delete their own comment directly.

```text
Author deletion does not require moderator approval.
```

The acting user must be validated against authoritative comment ownership.

### 10.2 Author deletion transitions

| Current status | Author action | Next status | Visibility effect |
|---|---|---|---|
| `Pending` | Delete own comment | `Deleted` | No public change |
| `Visible` | Delete own comment | `Deleted` | Removed from public display |
| `Rejected` | Delete own comment | `Deleted` | No public change |
| `Hidden` | Delete own comment | `Deleted` | No public change |
| `Deleted` | Retry delete | `Deleted` | Idempotent no-op |

### 10.3 Soft-delete retention

Comment author deletion must not physically erase all stored moderation/report context.

Deleted comments may still be needed for:

- moderation-case closure;
- report investigation;
- audit correlation;
- operational diagnostics;
- counter reconciliation.

### 10.4 Author deletion with an open moderation case

If the author deletes a visible comment while an open moderation case exists:

- `Comment.Status` becomes `Deleted`;
- the open moderation case becomes `ClosedByAuthorDeletion`;
- unresolved reports in that case become `ClosedByAuthorDeletion`;
- relevant moderation history is appended;
- downstream audit/event intent is recorded when required.

This preserves historical accountability while respecting the author's ability to remove their own visible content.

### 10.5 Idempotent delete behavior

Delete-own-comment should be idempotent from the author's perspective.

If a delete request is retried after an ambiguous timeout and the comment is already `Deleted` by its author, the operation must not:

- append duplicate history;
- close a case twice;
- emit duplicate business effects;
- decrement any public-facing derived counter twice.

---

## 11) Public Comment Reading Rules

### 11.1 Query ownership

Interaction owns public comment listing queries in V1.

Reading does not need to own or project complete comment bodies in V1.

### 11.2 Public result filtering

Public comment queries must return only:

```text
Comment.Status = Visible
```

and only when the related article is publicly eligible for exposure.

### 11.3 Non-public data protection

Public APIs must never expose:

- `Pending` comments;
- `Rejected` comments;
- `Hidden` comments;
- `Deleted` comments;
- report details;
- reporter identity;
- moderation case metadata;
- moderation notes;
- admin actor metadata.

---

## 12) Comment Report Rules

### 12.1 Report eligibility

Only an authenticated user may report a comment.

A report is allowed only when:

- the comment currently has status `Visible`;
- the related article is publicly interaction-enabled;
- the reporter is not the comment author;
- the reporter has not already reported that comment under the V1 uniqueness rule;
- report submission passes validation and abuse-control policy.

### 12.2 Report is an allegation, not an automatic moderation verdict

A user-supplied report reason is untrusted external input.

```text
A report indicates that moderator review is requested.
It does not prove the comment violates policy.
```

### 12.3 No automatic hide

Creating a report must not automatically change comment visibility.

```text
Reported Visible comment remains Visible
until a moderator performs a valid Hide action
or the author deletes the comment.
```

Automatic hiding based only on report count is deferred from V1.

### 12.4 Report uniqueness

V1 uses the following simple business rule:

```text
An authenticated user may report a given comment at most once.
```

Required authoritative uniqueness:

```text
(CommentId, ReporterUserId)
```

This uniqueness applies even when an earlier moderation case has already been resolved.

### 12.5 Report reason codes

Recommended report reason codes:

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

Request rules:

| Field | Rule |
|---|---|
| `ReasonCode` | Required and allowlisted |
| `Description` | Optional for known reason codes |
| `Description` when `ReasonCode = Other` | Required |
| `Description` length | Bounded by validation policy |

### 12.6 Report statuses

Interaction V1 supports:

```text
Pending
Dismissed
Actioned
ClosedByAuthorDeletion
```

| Status | Meaning |
|---|---|
| `Pending` | Awaiting moderator resolution within an open moderation case |
| `Dismissed` | Moderator reviewed and did not remove the comment |
| `Actioned` | Moderator accepted the case and hid the comment |
| `ClosedByAuthorDeletion` | Author deleted the comment before moderator resolution |

---

## 13) Comment Moderation Case Rules

### 13.1 Purpose

`CommentModerationCase` represents one unresolved or resolved review cycle for reports against one comment.

It exists to support:

- grouping multiple reports for the same visible comment;
- one admin decision for the current report cycle;
- one alert lifecycle for that report cycle;
- historical separation between an earlier resolved cycle and later reports.

### 13.2 Open-case creation

When the first valid report is created for a visible comment that has no open moderation case:

- create a new `CommentModerationCase`;
- set the case status to `Open`;
- associate the report with that case.

### 13.3 Joining an open case

When a valid report is created for a comment that already has an open moderation case:

- associate the new report with the existing open case;
- do not create a second open case;
- evaluate whether the case now satisfies alert escalation rules.

### 13.4 Single-open-case invariant

```text
At most one Open CommentModerationCase may exist for a Comment.
```

This rule must be protected authoritatively, not only by application-level pre-checks.

### 13.5 Case statuses

Interaction V1 supports:

```text
Open
Dismissed
Actioned
ClosedByAuthorDeletion
```

| Status | Meaning |
|---|---|
| `Open` | The case has unresolved reports awaiting review |
| `Dismissed` | Moderator reviewed the case and left the comment visible |
| `Actioned` | Moderator resolved the case by hiding the comment |
| `ClosedByAuthorDeletion` | Author deleted the comment before moderation resolution |

### 13.6 Case resolution

| Resolution operation | Comment outcome | Case outcome | Associated unresolved report outcome |
|---|---|---|---|
| Dismiss reports | Remains `Visible` | `Dismissed` | `Dismissed` |
| Hide reported comment | `Visible -> Hidden` | `Actioned` | `Actioned` |
| Author deletes reported comment | `Visible -> Deleted` | `ClosedByAuthorDeletion` | `ClosedByAuthorDeletion` |

### 13.7 Case concurrency protection

Moderation-case resolution and alert triggering are stale-write-sensitive.

`CommentModerationCase` must support a version/concurrency mechanism so that:

- two moderators cannot resolve the same open case inconsistently;
- two concurrent threshold-reaching reports do not trigger duplicate alert intent;
- stale retry does not re-open or re-resolve an already resolved case.

---

## 14) Admin Alert Escalation Rules

### 14.1 Queue-first rule

Every valid report becomes available to the authorized moderation queue after Interaction truth commits.

Email is an escalation mechanism only.

```text
Email alerting does not replace the moderation queue.
```

### 14.2 Default V1 alert policy

V1 adopts configurable escalation policy with the following initial defaults:

| Case condition | Alert behavior |
|---|---|
| Three distinct authenticated reporters with normal-severity reports in one open case | Trigger one asynchronous admin alert |
| One valid high-severity report in one open case | Trigger one asynchronous admin alert |
| One valid critical-severity report in one open case | Trigger one asynchronous admin alert |

### 14.3 Severity signal

Suggested default report severity classification:

| Report reason | Default severity |
|---|---|
| `Spam` | Normal |
| `OffTopic` | Normal |
| `Misinformation` | Normal |
| `Harassment` | High |
| `HateSpeech` | High |
| `Violence` | High |
| `SexualContent` | High |
| `PersonalInformation` | Critical |
| `Other` | Normal unless later reviewed otherwise |

Severity is a prioritization signal for moderator attention. It is not automatic proof of a policy violation.

### 14.4 No alert-driven hide

Reaching an alert threshold:

- may raise case priority;
- may trigger asynchronous admin email intent;
- must not automatically hide the comment.

Moderator action remains required for a visibility change.

### 14.5 One alert per open case in V1

```text
An open moderation case may trigger at most one admin-alert business intent in V1.
```

The case must persist durable alert-trigger metadata, such as:

```text
AlertTriggeredAtUtc
AlertLevel
AlertMessageId or equivalent correlation metadata
```

This prevents concurrent reports, retry or replay from creating duplicate alert intent.

### 14.6 Notification boundary

Interaction must not send administrator email directly.

When alert conditions are satisfied, Interaction commits:

- case alert-trigger state;
- the required outbox message representing admin-alert intent;

in the same local transaction.

Notifications is responsible asynchronously for:

- recipient configuration;
- email delivery persistence;
- provider send/retry handling;
- durable business-intent dedupe.

### 14.7 Suggested notification business intent key

Notifications should protect duplicate admin-alert email delivery with a stable business-intent key equivalent to:

```text
InteractionCommentReportAlert:{CommentModerationCasePublicId}
```

---

## 15) Comment Moderation Action History Rules

### 15.1 Purpose

`CommentModerationActionHistory` is Interaction-owned local operational history.

It exists so authorized admin flows can immediately inspect moderation decisions without depending on asynchronous Audit ingestion.

### 15.2 History versus Audit

| `CommentModerationActionHistory` | Audit |
|---|---|
| Owned by Interaction | Owned by Audit |
| Written as part of Interaction workflow mutation where required | Ingested asynchronously through integration events |
| Supports comment/case operational admin views | Supports canonical cross-system evidence and investigation |
| Immediately available after Interaction transaction commits | May lag behind the originating command |

### 15.3 Supported action types

Recommended V1 history action types:

```text
Approve
Reject
Hide
Restore
DismissReportedCase
HideReportedComment
CloseCaseByAuthorDeletion
```

### 15.4 Author deletion scope

Normal author deletion without an active moderation case does not have to be represented as a moderation-history row in V1.

If author deletion closes an open reported-comment case, Interaction must record the relevant moderation history action:

```text
CloseCaseByAuthorDeletion
```

### 15.5 Minimum history metadata

A moderation-history record should capture:

```text
CommentId
CommentModerationCaseId nullable
ActionType
FromStatus
ToStatus
ActorUserId nullable
ActorType
ReasonCode nullable
Note nullable
OccurredAtUtc
CorrelationId
```

### 15.6 History atomicity

Where a moderator action changes comment or case truth, its required local history row must commit in the same Interaction transaction as the corresponding state transition.

---

## 16) Interaction Counter and Public Projection Rules

### 16.1 Public counter fields

Interaction V1 publishes public interaction counters including:

```text
ViewCount
LikeCount
VisibleCommentCount
```

### 16.2 ArticleViewCount role

`ArticleViewCount` stores durable materialized accepted-view count state.

It is specialized for view workload and may evolve independently for high-volume optimization.

### 16.3 ArticleInteractionStats role

`ArticleInteractionStats` is the derived public counter snapshot state published outward for public serving.

Recommended conceptual fields:

```text
ArticlePublicId
ViewCount
LikeCount
VisibleCommentCount
StatsVersion
LastMaterializedAtUtc
UpdatedAtUtc
```

### 16.4 Counter source rules

| Counter | Source / derivation posture |
|---|---|
| `ViewCount` | Materialized accepted-view state from `ArticleViewCount` |
| `LikeCount` | Derived from active `ArticleLike` truth |
| `VisibleCommentCount` | Derived from `Comment` truth where `Status = Visible` |

### 16.5 Counter effects by operation

| Operation | ViewCount effect | LikeCount effect | VisibleCommentCount effect |
|---|---:|---:|---:|
| Accepted article view | Increase through `ArticleViewCount` | None | None |
| Like article | None | Eventually increases by 1 | None |
| Unlike article | None | Eventually decreases by 1 | None |
| Submit pending comment | None | None | None |
| Approve comment (`Pending -> Visible`) | None | None | Eventually increases by 1 |
| Reject pending comment | None | None | None |
| Hide visible comment (`Visible -> Hidden`) | None | None | Eventually decreases by 1 |
| Restore hidden comment (`Hidden -> Visible`) | None | None | Eventually increases by 1 |
| Author deletes visible comment (`Visible -> Deleted`) | None | None | Eventually decreases by 1 |
| Create report | None | None | None |
| Dismiss report case | None | None | None |

### 16.6 Counter lag is acceptable

Counter changes are derived-state changes.

Therefore:

- counter values may lag behind committed like/comment/moderation truth;
- public display may temporarily show older counts;
- counter lag must not affect article visibility correctness;
- counter lag must be observable.

### 16.7 Counter snapshot publication for Reading

Interaction publishes a single known-value public counter snapshot event for Reading:

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

### 16.8 No raw engagement consumption by Reading

Reading must not be expected to consume and interpret raw Interaction facts such as:

```text
interaction.article_liked
interaction.article_unliked
interaction.comment_approved
interaction.comment_hidden
individual accepted views
```

Reading receives only public counter snapshot projection messages from Interaction.

### 16.9 Counter versioning

`ArticleInteractionStats` must expose a monotonic `StatsVersion`.

Every published public counter snapshot must carry that version.

Downstream consumers must use version-aware set semantics:

```text
Apply incoming snapshot only when IncomingStatsVersion is newer
than the currently applied Interaction stats version.
```

Timestamps are diagnostic metadata only; they are not the freshness authority.

---

## 17) Integration Event Rules

### 17.1 Event identity requirements

Important Interaction integration events must carry:

```text
MessageId
EventType
AggregateId
Version where ordered application matters
OccurredAtUtc
CorrelationId
```

### 17.2 Interaction-emitted business events

Recommended Interaction V1 truth/workflow events:

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

### 17.3 Reading projection event

Interaction publishes:

```text
interaction.article_counters_projection_published
```

for Reading-owned public counter projection updates.

### 17.4 Report action event simplification

V1 does not require a separate:

```text
interaction.comment_reports_actioned
```

event when a reported comment is hidden.

Instead, `interaction.comment_hidden` may contain report-resolution metadata such as:

```text
ResolutionSource = Report
CommentModerationCasePublicId
ResolvedReportCount
ReasonCode
ModeratorUserId
```

### 17.5 Event publication boundary

Any Interaction mutation that requires asynchronous propagation must write its outbox message in the same local transaction as the state mutation and required local operational history.

---

## 18) Async Dependency Rules

### 18.1 Content to Interaction

Interaction receives Content-derived article eligibility/public-state messages asynchronously in order to maintain:

```text
ArticleInteractionTargetProjection
```

Interaction must apply Content inputs with message dedupe and source-version checks.

### 18.2 Interaction to Reading

Interaction publishes only public counter projection snapshots required by Reading:

```text
interaction.article_counters_projection_published
```

Reading integration implementation is documented separately in the Reading module.

### 18.3 Interaction to Audit

Audit may consume moderation-relevant events, including:

```text
interaction.comment_approved
interaction.comment_rejected
interaction.comment_hidden
interaction.comment_restored
interaction.comment_deleted_by_author
interaction.comment_reports_dismissed
```

Whether `interaction.comment_reported` is included in canonical Audit ingestion may be finalized in the relevant contract documentation.

### 18.4 Interaction to Notifications

Notifications consumes:

```text
interaction.comment_report_alert_triggered
```

Interaction must not directly send email or resolve notification recipients.

---

## 19) Transaction Boundary Rules

### 19.1 Local transaction boundary

Interaction command success is based on committing Interaction-owned state.

When asynchronous consequences are required, the same transaction must include the required outbox intent.

### 19.2 Truth/workflow mutation examples

The following must be atomic within Interaction when applicable:

| Operation | Atomic state |
|---|---|
| Like article | `ArticleLike` truth mutation + required outbox intent |
| Unlike article | `ArticleLike` truth mutation + required outbox intent |
| Submit comment | `Comment` creation + required outbox intent |
| Approve/reject/hide/restore | `Comment` transition + required moderation history + required outbox intent |
| Report comment | `CommentReport` + open-case creation/join + possible alert-trigger metadata + required outbox intent |
| Resolve report case | `CommentModerationCase` transition + report transitions + possible comment transition + required history + required outbox intent |
| Delete own comment with open case | `Comment` transition + case/report closure + required history + required outbox intent |

### 19.3 Not part of Interaction transaction success

Interaction must not wait inside its truth transaction for:

- RabbitMQ publication completion;
- Reading counter projection apply;
- Audit ingestion;
- Notifications email creation or sending;
- external provider calls;
- long-running reconciliation or rebuild.

### 19.4 Shared database boundary

Even if modules are hosted in the same physical SQL Server database, Interaction must not write directly into:

- Reading business/projection tables as part of Interaction command success;
- Audit truth tables;
- Notifications delivery tables;
- Content truth tables.

Cross-module effects must follow approved asynchronous contracts.

---

## 20) Idempotency and Concurrency Rules

### 20.1 Delivery assumptions

Interaction must assume:

- outbox publication can retry;
- RabbitMQ may redeliver;
- consumers may restart after partial progress;
- old messages may arrive after newer state;
- recovery/replay may intentionally reprocess messages.

### 20.2 Required business guards

| Concern | Required protection |
|---|---|
| Duplicate like by same user | Unique active-like invariant |
| Duplicate unlike | Conditional active-like transition / idempotent removal |
| Duplicate comment report | Unique `(CommentId, ReporterUserId)` |
| Two open cases for one comment | At most one `Open` case invariant |
| Duplicate admin alert | One alert intent per open case |
| Duplicate moderator transition | Status and version guard |
| Duplicate consumer delivery | Durable message-level dedupe |
| Stale projection application | Version-aware apply rule |

### 20.3 Consumer dedupe state

Interaction consumers must use durable processing/apply state equivalent to:

```text
InteractionConsumedMessage
```

Because multiple independent consumers may process different effects from the same source message, dedupe identity should be scoped by consumer purpose:

```text
Unique(ConsumerName, MessageId)
```

### 20.4 Projection freshness

`ArticleInteractionTargetProjection` must track the latest applied source version.

Approved behavior:

```text
If MessageId was already processed:
    treat as duplicate and do not reapply.

If IncomingSourceVersion <= LastSourceVersion:
    ignore as duplicate or stale.

If IncomingSourceVersion is valid forward progress:
    apply projection update.

If a gap or unsafe uncertainty is detected:
    mark or treat eligibility as unsafe;
    fail closed for new public interactions;
    trigger approved reconciliation/resync posture.
```

### 20.5 Comment and case versioning

The following are version-sensitive state:

```text
Comment
CommentModerationCase
```

They require explicit optimistic-concurrency or equivalent compare-and-set protection for:

- comment moderation;
- case resolution;
- alert triggering;
- author deletion while case processing may race.

### 20.6 Counter publication versioning

`ArticleInteractionStats.StatsVersion` provides ordered freshness for outbound counter snapshots.

`StatsVersion`, not wall-clock timestamp, is the authoritative ordering marker for counter projection publication.

### 20.7 No article-wide like contention requirement

The existence of concurrent likes on the same article must not require every user request to participate in a single article-wide optimistic-concurrency workflow.

Like truth is protected at the `(Article, User)` relationship boundary. Counter materialization may converge asynchronously.

---

## 21) Timeout and Retry Rules

### 21.1 Timeout ambiguity

A timeout means the caller stopped waiting.

It does not prove:

- the Interaction transaction did not commit;
- the report was not created;
- the case alert was not triggered;
- an outbox message was not published;
- an administrator email was not sent.

### 21.2 Retry-safe command posture

| Command | Retry-safe requirement |
|---|---|
| Like | Must not create duplicate active like |
| Unlike | Must not remove/decrement twice |
| Submit comment | Prefer idempotency-key protection |
| Delete own comment | Already-deleted-by-author result is an idempotent success/no-op |
| Report comment | Duplicate report must be rejected or represented as already submitted without new case effect |
| Approve/reject/hide/restore | Status/version guard must prevent duplicate state transition |
| Resolve moderation case | Case status/version guard must prevent duplicate resolution |
| Trigger alert | Existing alert-trigger metadata must suppress repeated intent |

### 21.3 External email ambiguity

If Notifications experiences ambiguous email-provider outcome, Interaction must not independently generate another alert intent for the same open case.

Email retry/resend policy belongs to Notifications and must be protected by durable business-intent dedupe.

---

## 22) Recovery and Reconciliation Rules

### 22.1 Eligibility projection recovery

`ArticleInteractionTargetProjection` is derived from Content.

Its approved recovery posture includes:

- idempotent message reprocessing;
- source-version stale rejection;
- bounded resync/reconciliation from Content-owned source data;
- fail-closed behavior while eligibility is uncertain.

### 22.2 Like and visible-comment counter recovery

`ArticleInteractionStats` counter components derived from durable Interaction truth must be reconcilable:

```text
LikeCount           = count of active ArticleLike relations
VisibleCommentCount = count of Comment records with Status = Visible
```

The implementation may use:

- normal event-driven materialization;
- bounded recomputation for a selected article;
- reconciliation jobs or operator-triggered repair later.

### 22.3 View-count recovery limitation

In V1, `ArticleViewCount` is durable materialized count state and individual raw view history is not retained.

Therefore:

- view count is not fully recomputable from one-row-per-view truth;
- view count recovery relies on durable counter state, atomic mutation, backup/operational recovery and publication reconciliation;
- future view buckets or analytics ingestion may improve recomputability without changing Interaction ownership.

### 22.4 Public counter snapshot repair

After reconciliation or repair of Interaction counter state:

- update/materialize `ArticleInteractionStats`;
- increment `StatsVersion` when the outward public snapshot changes;
- publish a fresh `interaction.article_counters_projection_published` message;
- allow Reading to replace older projected counters by version.

### 22.5 Batch posture

Interaction V1 documents bounded recomputation and reconciliation as the repair lane.

V1 does not require initial implementation of:

- full scheduled counter rebuild infrastructure;
- long-running batch orchestration tables;
- exclusive singleton aggregation ownership;
- trending analytics pipelines.

If future batch or reconciliation work requires exclusive ownership transfer, it must adopt the system fencing/ownership policy.

---

## 23) Security and Abuse-Control Rules

### 23.1 Public input is untrusted

Interaction must not trust client-supplied:

- actor identity;
- ownership claims;
- timestamps for ordering or correctness;
- article/comment eligibility claims;
- report allegations as confirmed policy violations;
- retry behavior as evidence of earlier failure.

### 23.2 View abuse controls

View counting must support protections against:

- automated refresh inflation;
- malformed traffic;
- obvious bot traffic where identifiable;
- unbounded repeated count contribution.

### 23.3 Comment abuse controls

Comment submission must support:

- authentication;
- rate limiting;
- bounded input;
- rendering safety/sanitization;
- moderation-before-public-visibility.

### 23.4 Report abuse controls

Report submission must support:

- authentication;
- non-owner restriction;
- unique report restriction;
- rate limiting;
- bounded reason description;
- no automatic content removal based only on user allegations.

### 23.5 Moderation protection

Admin moderation and report-resolution endpoints must:

- require explicit permissions;
- use authoritative current state;
- enforce state-machine legality;
- enforce version/concurrency safety;
- emit required downstream evidence asynchronously.

---

## 24) Authorization Rules

### 24.1 User/public operations

| Operation | Required actor |
|---|---|
| Track eligible article view | Public or authenticated, subject to abuse controls |
| Read visible public comments | Public |
| Like article | Authenticated |
| Unlike own active like | Authenticated owner |
| Submit comment | Authenticated |
| Delete own comment | Authenticated comment author |
| Report visible comment | Authenticated non-author |

### 24.2 Admin permissions

Recommended explicit permissions:

```text
Interaction.Comments.Read
Interaction.Comments.Moderate
Interaction.CommentReports.Read
Interaction.CommentReports.Resolve
Interaction.Counters.Read
Interaction.Analytics.Read
```

### 24.3 Suggested built-in role posture

| Role | Interaction administration posture |
|---|---|
| Admin | All Interaction administrative permissions |
| Moderator | Comment read/moderate and report read/resolve |
| Author | No comment moderation permission in V1 |
| User | No admin permission; user-facing interaction operations only |

### 24.4 No implicit article-author moderation

An article author must not automatically receive permission to approve, reject, hide, restore or resolve reports for comments on their article in V1.

That capability requires a later explicit scoped-authorization decision.

---

## 25) Deferred Capabilities

The following capabilities are outside Interaction V1 scope:

| Capability | Reason deferred |
|---|---|
| User editing of comments | Requires re-moderation, revision and visibility rules |
| Reply-comment behavior and nested comment threads | `ParentCommentId` may be reserved in schema, but V1 APIs and workflows support top-level comments only |
| Comment likes/reactions | Not required for initial news engagement model |
| Article reactions beyond like | Increases public aggregate complexity |
| Automatic hiding after report threshold | Vulnerable to coordinated report abuse |
| Bulk moderation | Increases transaction, audit and failure-handling complexity |
| Article-author comment moderation | Requires scoped authorization model |
| Reporter reputation and automatic penalties | Requires real operational evidence |
| Detailed raw per-view analytics retention | High-volume concern not required for V1 |
| Trending score pipelines | Separate future analytics/derived-state concern |

---

## 26) V1 Entity Direction Summary

### 26.1 Truth and workflow entities

```text
ArticleLike
Comment
  - ParentCommentId nullable, reserved for future reply support
CommentReport
CommentModerationCase
CommentModerationActionHistory
```

### 26.2 Derived and processing entities

```text
ArticleInteractionTargetProjection
ArticleViewCount
ArticleInteractionStats
InteractionConsumedMessage
```

### 26.3 Core invariants to reflect in DB design

```text
One active like per (Article, User)
One report per (Comment, ReporterUser)
At most one Open moderation case per Comment
V1-created comments must have ParentCommentId = NULL
ParentCommentId is reserved for future reply support and must not enable reply behavior in V1 APIs
Future reply support must introduce explicit validation before non-null ParentCommentId is accepted
Version-protected Comment moderation transitions
Version-protected CommentModerationCase resolution and alert triggering
Source-version-protected ArticleInteractionTargetProjection apply
Monotonic ArticleInteractionStats.StatsVersion
Durable consumer dedupe by (ConsumerName, MessageId)
Atomic accepted-view count mutation
```

### 26.4 Core outbound event direction

```text
Interaction -> Reading:
    interaction.article_counters_projection_published

Interaction -> Notifications:
    interaction.comment_report_alert_triggered

Interaction -> Audit:
    moderation-relevant Interaction facts
```

---

## 27) Final V1 Business Posture

Interaction V1 follows this business posture:

```text
Interaction owns user engagement and moderation workflow truth.

New interactions are accepted only for locally confirmed publicly eligible articles.

Likes, comments, reports and moderation cases are durable workflow facts.

Views are stored as durable accepted count state rather than raw per-view history.

Public counters are derived and may lag.

Reading consumes only Interaction counter projection snapshots.

Reports prioritize moderation work but never automatically censor content.

Administrator email is an asynchronous escalation effect, not part of report transaction success.

Comment and moderation-case transitions are state-machine and version protected.

All async processing is retry-safe, dedupe-aware and designed for reconciliation where derived state matters.
```
