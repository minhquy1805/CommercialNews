# Interaction — Dependencies & Ownership (V1)

This document defines Interaction module ownership boundaries for V1.
It focuses on allowed dependencies, forbidden coupling, async contracts, derived-state responsibility, and recovery ownership.

Related:

- `01-api-surface.md`
- `02-domain-contracts.md`
- `03-runtime-flows.md`
- `04-errors-status-codes.md`
- `05-security-abuse-controls.md`
- `06-idempotency-consistency.md`
- `07-observability-slos.md`
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

## 1) Module Responsibility

Interaction owns user engagement and moderation workflow around publicly available articles.

Interaction V1 is responsible for:

- accepted article view-count materialization;
- article like and unlike relationships;
- top-level comment submission;
- public visible-comment query behavior;
- author deletion of their own comments;
- administrative comment moderation;
- user reports against visible comments;
- moderation-case workflow for reported comments;
- local moderation action history;
- asynchronous administrator-alert intent when a report case reaches escalation policy;
- derived public counter snapshot publication for Reading;
- consumer dedupe/apply behavior required by Interaction-owned async processing.

Interaction must remain independent from:

- article lifecycle truth;
- public article read-model ownership;
- email delivery implementation;
- canonical cross-system audit persistence.

---

## 2) Ownership Boundary

### 2.1 Interaction-owned truth and workflow state

Interaction is the authoritative owner of:

| State | Interaction authority |
|---|---|
| `ArticleLike` | Determines whether an authenticated user currently has an active like relationship with an article |
| `Comment` | Determines submitted content and current comment status |
| `CommentReport` | Determines whether a valid user report exists |
| `CommentModerationCase` | Determines current/resolved report-review workflow state |
| `CommentModerationActionHistory` | Stores local operational moderation history required for admin workflows |

### 2.2 Interaction-owned derived and processing state

Interaction owns the following derived or processing records:

| State | Role |
|---|---|
| `ArticleInteractionTargetProjection` | Local Content-derived eligibility projection for accepting new interactions |
| `ArticleViewCount` | Durable materialized accepted-view count state |
| `ArticleInteractionStats` | Derived public counter snapshot state published to Reading |

### 2.3 Interaction does not own

| Concern | Owning module | Interaction relationship |
|---|---|---|
| Article content and lifecycle truth | Content | Consumes public/eligibility projection input asynchronously |
| Public article read model and article-page composition | Reading | Publishes counter projection snapshots asynchronously |
| Public route and SEO metadata truth | SEO | No direct ownership |
| Media asset and article-media association truth | Media | No direct ownership |
| Account/authentication truth | Identity | Uses authenticated actor identity from trusted request context |
| Roles and permissions | Authorization | Requires authorization policies for admin operations |
| Email delivery and recipient configuration | Notifications | Publishes administrator-alert intent asynchronously |
| Canonical audit evidence | Audit | Publishes moderation-relevant facts asynchronously |
| Shared event delivery infrastructure | Building Blocks / Outbox / Worker host | Uses approved infrastructure contracts only |

---

## 3) Ownership Does Not Transfer Through Projection

Copied, projected, or materialized data does not transfer ownership between modules.

### 3.1 Content-derived eligibility projection

Content owns article publication truth.

Interaction owns only its local derived eligibility projection:

```text
ArticleInteractionTargetProjection
```

Interaction may use the projection to accept or reject new interaction commands, but it must not claim ownership of article lifecycle state.

### 3.2 Reading-facing counter snapshot

Interaction owns interaction counter derivation and publication.

Reading owns only its local public-serving projection after consuming Interaction counter snapshots.

Reading must not become the source of truth for:

- whether a user liked an article;
- whether a comment is visible;
- whether a report/case exists;
- whether Interaction counters require repair.

### 3.3 Audit replication

Interaction owns operational moderation workflow state.

Audit owns canonical cross-system evidence copied asynchronously from committed Interaction facts.

Audit lag does not invalidate an already committed Interaction moderation operation.

### 3.4 Notifications replication

Interaction owns whether an administrator-alert intent was triggered.

Notifications owns delivery records, recipients, provider calls, retry, and email-send dedupe.

---

## 4) Dependency Direction Overview

```text
Content
    |
    | content article public/eligibility projection event
    v
Interaction
    |
    | interaction.article_counters_projection_published
    v
Reading

Interaction
    |
    | interaction.comment_report_alert_triggered
    v
Notifications

Interaction
    |
    | moderation-relevant interaction events
    v
Audit

Identity / Authorization
    |
    | authenticated actor context / policy decision
    v
Interaction APIs
```

Dependency rule:

```text
Interaction may consume required input contracts and publish required output contracts,
but it must not synchronously write or query another module's truth to complete ordinary Interaction commands.
```

---

## 5) Allowed Dependencies

### 5.1 Content to Interaction: required async eligibility input

Interaction is allowed and required to consume Content-derived public article state needed to maintain:

```text
ArticleInteractionTargetProjection
```

Conceptual inbound event:

```text
content.article_read_projection_published
```

The finalized event name and payload must match Content outbound contracts.

Interaction uses this projection to determine whether new interactions are accepted:

- view contribution;
- like;
- comment submission;
- comment report.

Apply requirements:

- dedupe inbound messages through the adopted consumer dedupe policy;
- use source version checks through `LastSourceVersion`;
- ignore stale/duplicate messages safely;
- fail closed when eligibility is missing or uncertain;
- support bounded resync/reconciliation from Content where required.

### 5.2 Identity to Interaction: actor identity context

Interaction may depend on trusted authenticated request context for:

- like/unlike actor identity;
- comment author identity;
- comment-delete ownership enforcement;
- comment reporter identity.

Interaction must not trust user identifiers supplied by public client payload as the authority for acting user identity.

### 5.3 Authorization to Interaction: permission decisions

Interaction admin APIs may depend on Authorization policies for operations such as:

```text
Interaction.Comments.Read
Interaction.Comments.Moderate
Interaction.CommentReports.Read
Interaction.CommentReports.Resolve
Interaction.Counters.Read
Interaction.Analytics.Read
```

Authorization determines whether an actor may execute a command.

Interaction remains responsible for:

- current-state validation;
- status-transition legality;
- version/concurrency checks;
- local state mutation;
- outbox publication.

### 5.4 Interaction to Reading: required async public counter output

Interaction publishes public counter snapshots for Reading:

```text
interaction.article_counters_projection_published
```

The snapshot contains known values such as:

```text
ArticlePublicId
ViewCount
LikeCount
VisibleCommentCount
StatsVersion
ProjectedAtUtc
```

Reading consumes this snapshot to update its public-serving projection.

Reading must not consume raw Interaction action events to calculate public counters.

### 5.5 Interaction to Notifications: required async admin-alert intent

Interaction publishes:

```text
interaction.comment_report_alert_triggered
```

when one open moderation case satisfies configured escalation policy and has not already triggered an alert intent.

Notifications owns:

- moderation-alert recipient configuration;
- email-delivery records;
- email templates;
- provider send;
- retry behavior;
- provider-timeout ambiguity handling;
- business-intent dedupe.

### 5.6 Interaction to Audit: async moderation evidence

Interaction may publish moderation-relevant facts to Audit, including:

```text
interaction.comment_approved
interaction.comment_rejected
interaction.comment_hidden
interaction.comment_restored
interaction.comment_deleted_by_author
interaction.comment_reports_dismissed
```

For reported-comment removal, `interaction.comment_hidden` may contain report-resolution metadata so a separate report-actioned event is not required in V1.

Audit owns canonical evidence persistence after consuming those events.

### 5.7 Shared Outbox and Worker infrastructure

Interaction may use approved shared infrastructure for:

- writing outbox messages in the local Interaction transaction;
- worker-based publication to RabbitMQ;
- consumer hosting;
- correlation and message envelope conventions.

This infrastructure supports transport and reliability. It does not own Interaction business truth.

---

## 6) Public API and Reading Relationship

### 6.1 Public article page composition

Reading owns public article page/read-model composition.

Interaction does not query or compose:

- article title;
- article body;
- SEO route;
- media projection;
- article public visibility truth.

### 6.2 Public comments query

Interaction owns public comment listing behavior in V1.

Public clients may query Interaction for visible comments associated with a public article, subject to the public API design.

```text
Public article content/counters:
    Reading projection

Public visible comment list:
    Interaction query
```

This does not transfer public article-read-model ownership to Interaction.

### 6.3 View tracking call

View tracking must remain non-blocking relative to public article reading.

Recommended request relationship:

```text
Reading/public page is served successfully
    -> client performs separate track-view request to Interaction
```

Interaction failure to count a view must not cause article rendering to fail.

---

## 7) Forbidden Dependencies and Coupling

Interaction must not perform the following coupling patterns.

### 7.1 Perform synchronous Content truth lookup for ordinary interaction commands

Forbidden request-path behavior:

```text
Like / Comment / Report request
    -> synchronous call/query to Content truth
    -> Interaction mutation
```

V1 uses `ArticleInteractionTargetProjection` and fails closed when uncertain.

### 7.2 Write into Reading projections directly

Forbidden:

```text
Interaction command transaction
    -> update Reading.ArticleReadModel counters directly
```

Required:

```text
Interaction counter snapshot outbox event
    -> Reading consumer applies projection asynchronously
```

### 7.3 Send email directly

Forbidden:

```text
Report command transaction
    -> call SMTP/MailKit/provider directly
```

Required:

```text
Report/case transaction
    -> outbox interaction.comment_report_alert_triggered
    -> Notifications sends asynchronously
```

### 7.4 Write into Audit state directly

Forbidden:

```text
Interaction moderation transaction
    -> insert Audit evidence row directly
```

Required:

```text
Interaction moderation state/history/outbox commit
    -> Audit consumes event asynchronously
```

### 7.5 Treat derived counters as business truth

Interaction must not use `ArticleInteractionStats` to decide:

- whether one user has liked an article;
- whether one comment is visible;
- whether one report exists;
- whether one moderation case is resolved.

### 7.6 Reset counters because article becomes non-public

Forbidden:

```text
Article unpublish/archive/soft-delete
    -> ViewCount = 0
    -> LikeCount = 0
    -> VisibleCommentCount = 0
```

Required:

- disable new public interactions;
- preserve existing Interaction state and counters.

### 7.7 Publish per-view messages to Reading

Forbidden:

```text
Each accepted view
    -> outbound event
    -> Reading increments ViewCount
```

Required:

```text
ArticleViewCount materialization
    -> ArticleInteractionStats snapshot
    -> versioned counter projection event for Reading
```

### 7.8 Trust client-owned identity or time fields as authority

Interaction must not rely on client-supplied:

- `UserId`;
- ownership claims;
- timestamps for state ordering;
- report reason as confirmed violation;
- retry behavior as proof of failure.

### 7.9 Use physical database reachability as ownership permission

Even if all modules share one SQL Server database, Interaction must not mutate another module's truth or serving projection merely because its tables are reachable.

---

## 8) Truth and Workflow Ownership Rules

### 8.1 ArticleLike ownership

Interaction owns active like state.

```text
At most one active ArticleLike per (ArticlePublicId, UserId).
```

`LikeCount` is derived and may lag.

### 8.2 Comment ownership

Interaction owns comment content and current status.

Supported V1 statuses:

```text
Visible
Hidden
Deleted
Pending
Rejected
```

Default V1 comment creation produces `Visible` comments immediately.

`Pending` and `Rejected` are reserved for a future selective-moderation workflow.

`VisibleCommentCount` is derived and may lag.

### 8.3 ParentCommentId compatibility

Interaction may reserve:

```text
Comment.ParentCommentId nullable
```

for future reply support.

In V1:

```text
All newly created comments must have ParentCommentId = NULL.
```

Reply behavior is not owned or implemented in V1 beyond this reserved schema compatibility.

### 8.4 CommentReport ownership

Interaction owns valid report creation and report status.

A report:

- is an allegation requiring moderator review;
- does not automatically hide a comment;
- does not change public counters.

### 8.5 CommentModerationCase ownership

Interaction owns the workflow state of one report-review cycle.

```text
At most one Open CommentModerationCase per Comment.
```

The case owns:

- current case status;
- resolution metadata;
- alert-trigger state;
- version used for concurrent resolution/alert protection.

### 8.6 CommentModerationActionHistory ownership

Interaction owns local operational moderation history needed for immediate admin workflows.

It is not a substitute for Audit, and Audit is not a substitute for this local workflow history.

---

## 9) Derived-State Ownership Rules

### 9.1 ArticleInteractionTargetProjection

Interaction owns this local projection as a derived copy of Content eligibility/public-state knowledge.

It is:

- version-applied;
- dedupe-protected;
- allowed to lag;
- fail-closed when unsafe;
- resyncable from Content source state.

### 9.2 ArticleViewCount

Interaction owns:

```text
ArticleViewCount
```

as durable materialized accepted-view count state.

It is not:

- raw per-view history;
- outbound event state;
- Reading-owned data.

V1 accepts that exact view reconstruction from historical individual views is unavailable because raw view records are not retained.

### 9.3 ArticleInteractionStats

Interaction owns:

```text
ArticleInteractionStats
```

as the public counter snapshot publication source.

It contains:

```text
ViewCount
LikeCount
VisibleCommentCount
StatsVersion
```

It is derived from:

| Counter | Source |
|---|---|
| `ViewCount` | `ArticleViewCount` |
| `LikeCount` | Active `ArticleLike` truth |
| `VisibleCommentCount` | `Comment.Status = Visible` truth |

### 9.4 Consumer Dedupe and Apply Policy

Interaction owns the behavior and operational correctness of consumer apply/dedupe for Interaction consumers.

```text
Unique(ConsumerName, MessageId)
```

The adopted implementation supports processing correctness but does not become business truth for likes, comments, reports, or moderation decisions.

---

## 10) Transaction Ownership Rules

### 10.1 Local mutation boundary

Any Interaction command requiring asynchronous propagation must commit atomically:

```text
Interaction-owned truth/workflow mutation
+ required local operational history
+ required outbox message
+ local idempotency record where adopted
```

### 10.2 Transaction examples

| Operation | Interaction-owned atomic commit set |
|---|---|
| Like article | `ArticleLike` mutation + outbox |
| Unlike article | `ArticleLike` mutation + outbox |
| Create visible comment | `Comment(Visible)` + optional idempotency record + outbox |
| Hide/restore comment | `Comment` transition + history + outbox |
| Future approve/reject selective-moderation command | Reserved; applies only if selective moderation is explicitly adopted |
| Report comment | `CommentReport` + case create/join + possible alert state + outbox |
| Dismiss report case | Case + reports + history + outbox |
| Hide reported comment | Comment + case + reports + history + outbox |
| Delete own comment with open case | Comment + case + reports + history + outbox |
| Publish counter snapshot | `ArticleInteractionStats` update + projection outbox |

### 10.3 Outside the local transaction

The following are not part of Interaction command success:

- RabbitMQ downstream processing completion;
- Reading projection apply;
- Audit ingestion;
- Notifications email delivery;
- external email provider acknowledgement;
- counter rebuild completion;
- future analytics/trending computation.

---

## 11) Async Contract Ownership

### 11.1 Inbound event contract

| Producer | Consumer | Event purpose | Interaction-owned result |
|---|---|---|---|
| Content | Interaction | Public article eligibility/lifecycle projection | `ArticleInteractionTargetProjection` |

Conceptual event:

```text
content.article_read_projection_published
```

Interaction must align the final event name and payload with Content contracts.

### 11.2 Outbound Reading contract

| Producer | Consumer | Event | Purpose |
|---|---|---|---|
| Interaction | Reading | `interaction.article_counters_projection_published` | Publish known-value public counters |

Reading must apply the snapshot by `StatsVersion`, not raw delta or timestamp.

### 11.3 Outbound Notifications contract

| Producer | Consumer | Event | Purpose |
|---|---|---|---|
| Interaction | Notifications | `interaction.comment_report_alert_triggered` | Request async admin/moderator alert delivery |

Notifications owns recipient/delivery/provider behavior.

### 11.4 Outbound Audit contract

| Producer | Consumer | Example events | Purpose |
|---|---|---|---|
| Interaction | Audit | Comment moderation and case-resolution facts | Store canonical evidence asynchronously |

---

## 12) Admin Moderation and Authorization Ownership

### 12.1 Authorization owns permission decision

Recommended permissions:

```text
Interaction.Comments.Read
Interaction.Comments.Moderate
Interaction.CommentReports.Read
Interaction.CommentReports.Resolve
Interaction.Counters.Read
Interaction.Analytics.Read
```

Authorization decides whether the actor is permitted.

### 12.2 Interaction owns state legality

Even when authorization succeeds, Interaction must still validate:

- current comment status;
- current moderation-case status;
- version/expected-version match;
- required reason/note;
- report relationship;
- author ownership where applicable.

### 12.3 No implicit author moderation

Article authors do not automatically gain moderation rights over comments on their article in V1.

That capability is deferred until an explicit scoped authorization policy is designed.

---

## 13) Report Alert Ownership

### 13.1 Interaction-owned alert trigger decision

Interaction determines whether an open moderation case satisfies configured alert policy.

Initial V1 default:

| Case condition | Alert behavior |
|---|---|
| Three distinct normal-severity reporters | Trigger one alert intent |
| One high-severity report | Trigger one alert intent |
| One critical-severity report | Trigger one alert intent |

### 13.2 Interaction-owned dedupe invariant

```text
One Open CommentModerationCase may trigger at most one administrator-alert business intent in V1.
```

Interaction stores durable case alert metadata.

### 13.3 Notifications-owned delivery behavior

Notifications determines:

- email recipient addresses;
- templates;
- provider integrations;
- delivery status;
- retry;
- provider-timeout ambiguity;
- email business-intent dedupe.

Suggested business intent identity:

```text
InteractionCommentReportAlert:{CommentModerationCasePublicId}
```

### 13.4 Visibility does not belong to alerting

Alert generation does not grant Notifications or alert policy the right to change comment visibility.

Only authorized Interaction moderation commands may transition a visible comment to hidden.

---

## 14) Counter Publication and Reconciliation Ownership

### 14.1 Interaction owns counter derivation

Interaction is responsible for composing public counters from its state:

```text
ViewCount
LikeCount
VisibleCommentCount
```

### 14.2 Interaction owns counter repair

Interaction owns bounded repair/reconciliation of:

```text
ArticleInteractionStats
```

Repair inputs:

| Counter | Repair input |
|---|---|
| `ViewCount` | Durable `ArticleViewCount` state |
| `LikeCount` | Active `ArticleLike` truth |
| `VisibleCommentCount` | Visible `Comment` truth |

### 14.3 Reading does not rebuild Interaction counters

Reading may apply or expose counter snapshots but must not:

- reconstruct Interaction truth;
- independently count Interaction records;
- repair Interaction-owned statistics;
- decide that a newer counter is correct using timestamps only.

### 14.4 V1 view recovery limitation

Because raw per-view records are not retained, Interaction can republish or preserve `ViewCount` from `ArticleViewCount`, but cannot exactly rebuild historical `ViewCount` from individual view facts in V1.

### 14.5 Batch posture

Interaction V1 documents bounded reconciliation and counter republish behavior.

V1 does not require initial implementation of:

- full scheduled rebuild orchestration;
- candidate/cutover datasets for large analytics projections;
- trending/popularity pipelines;
- exclusive counter-rebuild worker ownership.

---

## 15) Replay, Rebuild and Retention Ownership

### 15.1 Replay ownership

Interaction consumers own safe replay behavior for Interaction-managed derived/projection state.

This includes:

- inbound Content eligibility projection apply;
- public counter publication recovery where applicable;
- consumer dedupe and stale apply diagnostics.

### 15.2 Rebuild ownership

Interaction may rebuild or reconcile:

- `ArticleInteractionTargetProjection`;
- `ArticleInteractionStats`;
- consumer apply state diagnostics where operationally appropriate.

Interaction must not rebuild:

- Content article lifecycle truth;
- Reading-owned public article read model;
- Notifications delivery truth;
- Audit canonical evidence truth.

### 15.3 Retention ownership

Interaction owns retention policy for its own operational/derived records, subject to system policy:

- `CommentModerationActionHistory`;
- `CommentReport` / resolved case retention;
- `ArticleInteractionStats` history/state;
- `ArticleViewCount` operational retention.

Current truth/workflow rows required for correctness or investigation must not be removed by cleanup without an explicit policy.

---

## 16) Coordination and Ownership-Sensitive Workflows

### 16.1 Default posture

Ordinary Interaction correctness must rely on:

- authoritative Interaction state;
- database invariants;
- conditional/version-protected transitions;
- durable consumer dedupe;
- version-aware projection apply;
- bounded reconciliation.

It must not rely on assumptions such as:

- only one request will arrive;
- only one worker exists;
- startup order defines ownership;
- no redelivery will occur;
- one process is implicitly the current leader.

### 16.2 Future exclusive workflow rule

If future Interaction workflows require exclusive ownership, such as:

- one-current-owner hot-article counter rebuild;
- one-current-owner trending calculation;
- exclusive publication/cutover for large derived datasets;

then the workflow must define:

- authoritative ownership source;
- monotonic generation/fencing token;
- resource-side stale-owner rejection;
- lease/recovery behavior;
- observability for ownership conflict.

Naive singleton or lock-only assumptions are not sufficient.

---

## 17) Dependency Matrix

| Dependency direction | Sync / Async | Required in V1 | Purpose | Ownership result |
|---|---|---:|---|---|
| Client to Interaction view endpoint | Sync request, non-blocking relative to Reading | Yes | Track accepted view contribution | Interaction updates `ArticleViewCount` |
| Authenticated client to Interaction like/comment/report endpoints | Sync | Yes | Create Interaction truth/workflow | Interaction owns committed result |
| Admin client to Interaction moderation endpoints | Sync | Yes | Moderate comment/resolve case | Interaction owns committed result/history |
| Content to Interaction | Async | Yes | Maintain article interaction eligibility | Interaction owns local projection only |
| Identity context to Interaction | Sync request context | Yes | Determine authenticated actor | Identity remains user authority |
| Authorization to Interaction API | Sync policy evaluation | Yes for admin flow | Decide permitted action | Interaction still validates mutation legality |
| Interaction to Reading | Async | Yes | Publish public counter snapshot | Reading owns its consumed serving copy |
| Interaction to Notifications | Async | Yes for alert flow | Deliver admin alert | Notifications owns delivery truth |
| Interaction to Audit | Async | Yes for moderation evidence posture | Canonical audit ingestion | Audit owns evidence copy |
| Interaction to Redis/cache | Optional implementation detail | No as correctness boundary | Performance/temporary suppression | Must not become truth authority |

---

## 18) V1 Deferred Dependencies

Interaction V1 must not introduce dependencies solely for capabilities that are deferred:

| Deferred capability | Dependency not required in V1 |
|---|---|
| Reply comments/nested threads | No reply-thread projection dependency |
| Comment editing/revision | No comment-revision workflow dependency |
| Auto-hide by report threshold | No automated moderation decision service |
| Reporter reputation | No reputation/scoring service |
| Trending/popularity ranking | No analytics/ranking consumer dependency |
| Raw view analytics | No per-view event analytics pipeline |
| Bulk moderation | No batch moderation orchestration |
| Article-author moderation | No scoped author-moderation authorization contract |

`Comment.ParentCommentId` may exist for future compatibility, but does not create a V1 dependency on reply behavior.

---

## 19) Ownership Summary

```text
Interaction owns:
    ArticleLike
    Comment
    CommentReport
    CommentModerationCase
    CommentModerationActionHistory
    ArticleInteractionTargetProjection
    ArticleViewCount
    ArticleInteractionStats

Content owns:
    Article lifecycle and public eligibility source truth

Reading owns:
    Public article serving projection

Notifications owns:
    Administrator-alert email delivery

Audit owns:
    Canonical cross-system evidence

Authorization owns:
    Permission decisions

Identity owns:
    Authenticated user identity
```

---

## 20) Final Dependency Posture

Interaction V1 follows these dependency rules:

- Interaction accepts new engagement only for articles locally confirmed as interaction-enabled through Content-derived projection state.
- Interaction preserves counters and workflow history when an article becomes non-public; it does not reset engagement because of unpublish/archive/soft-delete.
- Interaction owns like, comment, report and moderation-case truth.
- Interaction owns view materialization through `ArticleViewCount`, not raw per-view history.
- Interaction owns public counter derivation through `ArticleInteractionStats`.
- Reading consumes only versioned public counter snapshots from Interaction.
- Notifications receives alert intent asynchronously and owns email delivery.
- Audit receives moderation facts asynchronously and owns canonical evidence.
- Authorization grants permission; Interaction enforces business-state legality and concurrency.
- No Interaction command may directly mutate another module's truth or serving projection.
- All required cross-module effects leave Interaction through outbox-backed async contracts.
- Derived state may lag and must remain observable and recoverable according to its approved posture.
- Future reply behavior, analytics, trending, auto-hide, and bulk moderation do not create V1 dependencies.
- Exclusive worker ownership is not assumed; any future ownership-sensitive workflow must use fencing/generation protection.
