# Interaction — API Architecture Index (V1)

This document provides the Interaction module overview for V1.
It summarizes module purpose, ownership, runtime boundaries, dependencies, security posture, deferred capabilities, and links to detailed Interaction documents.

Interaction owns engagement and moderation workflow truth.
Reading, Audit and Notifications consume approved asynchronous outputs.

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

## 1) Purpose

The Interaction module provides user-engagement capabilities around publicly available articles while preserving clean module boundaries and non-blocking public reading.

Interaction V1 supports:

```text
Article view counting
Article like / unlike
Top-level comment submission
Public visible-comment querying
Author delete-own-comment
Admin comment moderation
User report-comment workflow
Comment moderation cases
Moderation action history
Async administrator-alert intent
Public counter snapshot publication for Reading
Consumer dedupe, version-aware apply and reconciliation posture
```

Interaction is intentionally designed as a hot and abuse-prone module:

- popular articles may generate high concurrent interaction traffic;
- comments and reports require moderation and abuse controls;
- views require non-blocking, anti-inflation-aware counting;
- public counters may lag without affecting truth correctness;
- downstream delivery is asynchronous and at-least-once.

---

## 2) Why This Module Is Critical

Interaction is critical because it combines:

- high write volume from public engagement;
- authenticated user relationship state;
- user-generated content;
- moderation-sensitive workflow;
- security and abuse risks;
- derived counters consumed by public reading;
- asynchronous side effects such as audit evidence and administrator notifications.

The design must preserve the following principles:

```text
Interaction truth commits locally and remains authoritative.

Public article reading must not fail because Interaction is degraded.

Derived counters may lag, but must never redefine truth.

Reports request moderator review; they do not automatically censor content.

All required cross-module effects leave Interaction through outbox-backed async contracts.
```

---

## 3) Module Ownership

### 3.1 Interaction-owned truth and workflow state

Interaction is the authoritative owner of:

```text
ArticleLike
Comment
CommentReport
CommentModerationCase
CommentModerationActionHistory
```

| Record | Purpose |
|---|---|
| `ArticleLike` | Current authenticated-user like relationship with an article |
| `Comment` | Submitted comment content and current visibility/moderation status |
| `CommentReport` | Valid user-submitted report against a visible comment |
| `CommentModerationCase` | One report-review cycle for one comment |
| `CommentModerationActionHistory` | Local operational moderation history for admin workflows |

### 3.2 Interaction-owned derived and processing state

Interaction also owns:

```text
ArticleInteractionTargetProjection
ArticleViewCount
ArticleInteractionStats
```

| Record | Purpose |
|---|---|
| `ArticleInteractionTargetProjection` | Local projection of whether an article may receive new interactions |
| `ArticleViewCount` | Durable materialized accepted-view counter |
| `ArticleInteractionStats` | Versioned public counter snapshot state published to Reading |

### 3.3 Interaction does not own

| Concern | Owning module |
|---|---|
| Article content and lifecycle truth | Content |
| Public article read-model composition | Reading |
| Public slug/routing and SEO metadata truth | SEO |
| Media assets and article-media relationship truth | Media |
| User account/authentication truth | Identity |
| Role/permission evaluation | Authorization |
| Email delivery and recipient policy | Notifications |
| Canonical cross-system audit evidence | Audit |

### 3.4 Projection ownership rule

```text
Copied or projected data does not transfer ownership.
```

Examples:

- `ArticleInteractionTargetProjection` does not make Interaction the owner of article publication state.
- Reading receiving counter snapshots does not make Reading the owner of likes/comments/views.
- Audit receiving moderation events does not replace Interaction operational moderation history.

---

## 4) Runtime Boundary Overview

Interaction participates in three runtime lanes.

### 4.1 Synchronous command lane

Used for authoritative Interaction mutations:

```text
Like / Unlike
Create Comment
Delete Own Comment
Report Comment
Approve / Reject / Hide / Restore Comment
Dismiss / Action Comment Moderation Case
```

Command success means:

```text
Interaction-owned mutation committed
+ required local moderation history committed where applicable
+ required OutboxMessage committed where applicable
```

Command success does not mean:

```text
Reading already applied new counters
Audit already stored evidence
Notifications already sent email
```

### 4.2 Async derived-state and propagation lane

Used for:

```text
Content -> Interaction article eligibility projection
Interaction -> Reading public counter snapshot
Interaction -> Notifications administrator-alert intent
Interaction -> Audit moderation-relevant facts
Counter materialization and publication
```

### 4.3 Recovery and reconciliation lane

Used for:

```text
Eligibility projection resync
LikeCount reconciliation
VisibleCommentCount reconciliation
ArticleInteractionStats repair
Counter snapshot republish
Consumer duplicate/stale/gap diagnostics
```

---

## 5) Upstream and Downstream Dependencies

### 5.1 Content → Interaction

Interaction consumes Content-derived article public/eligibility input asynchronously to maintain:

```text
ArticleInteractionTargetProjection
```

Conceptual inbound event:

```text
content.article_read_projection_published
```

The final event type and payload must align with the finalized Content outbound contract.

Interaction must:

- dedupe inbound messages;
- apply newer source versions only;
- fail closed when eligibility is missing or unsafe;
- support bounded resync/reconciliation.

### 5.2 Interaction → Reading

Interaction publishes versioned public counter snapshots:

```text
interaction.article_counters_projection_published
```

Snapshot values include:

```text
ViewCount
LikeCount
VisibleCommentCount
StatsVersion
```

Reading consumes known-value snapshots only.

Reading must not calculate counters from raw like/comment/view activity events.

### 5.3 Interaction → Notifications

Interaction publishes administrator-alert intent:

```text
interaction.comment_report_alert_triggered
```

Notifications owns:

- recipients;
- templates;
- email persistence;
- provider delivery;
- retry;
- provider ambiguity handling;
- notification business-intent dedupe.

### 5.4 Interaction → Audit

Interaction publishes moderation-relevant facts asynchronously, such as:

```text
interaction.comment_approved
interaction.comment_rejected
interaction.comment_hidden
interaction.comment_restored
interaction.comment_deleted_by_author
interaction.comment_reports_dismissed
```

Audit owns canonical evidence persistence after consumption.

---

## 6) Article Eligibility Rules

Interaction accepts new public engagement only when its local projection safely confirms that the article is interaction-enabled.

```text
ArticleInteractionTargetProjection.IsInteractionEnabled = true
AND ArticleInteractionTargetProjection.RequiresResync = false
```

| Article eligibility state | New view contribution | New like | New comment | New report |
|---|---:|---:|---:|---:|
| Confirmed public / interaction-enabled | Allowed | Allowed | Allowed | Allowed for visible comments |
| Draft / unpublished / archived / soft-deleted | Not allowed | Not allowed | Not allowed | Not allowed |
| Projection missing or unsafe | Fail closed | Fail closed | Fail closed | Fail closed |
| Projection requires resync | Fail closed | Fail closed | Fail closed | Fail closed |

### 6.1 Non-public article preservation rule

When an article becomes unpublished, archived or soft-deleted:

```text
Disable new public interactions.
Preserve existing Interaction state and counters.
```

Interaction preserves:

```text
ArticleLike
Comment
CommentReport
CommentModerationCase
CommentModerationActionHistory
ArticleViewCount
ArticleInteractionStats
```

Allowed operations after the article becomes non-public:

```text
Unlike own existing active like
Delete own existing comment
Resolve already-open moderation case when authorized
```

### 6.2 Counter lifetime rule

```text
ViewCount, LikeCount and VisibleCommentCount follow stable ArticlePublicId.
They are not reset merely because an article is unpublished or later republished.
```

---

## 7) Article View Counting

### 7.1 V1 model

Interaction V1 uses:

```text
ArticleViewCount
```

It does not store one durable raw row for every page view.

Conceptual state:

```text
ArticleViewCount
- ArticlePublicId
- ViewCount
- ViewVersion
- LastAcceptedViewAtUtc
- CreatedAtUtc
- UpdatedAtUtc
```

### 7.2 View behavior

- Anonymous and authenticated readers may submit view contributions.
- Public article rendering must remain independent from view-tracking success.
- A view contributes to `ArticleViewCount` only after eligibility and abuse-control evaluation.
- Accepted concurrent views must update the counter atomically.
- Reading does not receive one message per individual view.

### 7.3 View security posture

View acceptance may apply:

```text
Rate limiting
Repeat-view suppression
Temporary/session-based dedupe signals
Malformed traffic rejection
Bot/anomaly handling where adopted
```

### 7.4 V1 recovery limitation

```text
V1 does not retain raw per-view history.
ViewCount is durable materialized state and cannot be exactly rebuilt
from individual historical page views.
```

---

## 8) Article Likes

### 8.1 Truth model

```text
ArticleLike
```

represents the current authenticated-user relationship with an article.

### 8.2 Core invariant

```text
At most one active ArticleLike per (ArticlePublicId, UserId).
```

### 8.3 Rules

- Only authenticated users may like or unlike.
- New likes require an interaction-enabled article.
- Unlike remains allowed after an article becomes non-public.
- Repeated like requests converge to `liked = true`.
- Repeated unlike requests converge to `liked = false`.
- Concurrent likes by different users are independent valid operations.
- `LikeCount` is derived and may lag behind relationship truth.

---

## 9) Comments

### 9.1 V1 scope

Interaction V1 supports top-level comments only.

```text
Comment.ParentCommentId nullable
```

may exist for future compatibility, but:

```text
Every V1-created comment must have ParentCommentId = NULL.
V1 APIs do not expose reply behavior.
```

### 9.2 Comment statuses

```text
Pending
Visible
Rejected
Hidden
Deleted
```

| Status | Meaning | Publicly visible |
|---|---|---:|
| `Pending` | Submitted and awaiting moderation | No |
| `Visible` | Approved and eligible for public display while article is public | Yes |
| `Rejected` | Rejected before public visibility | No |
| `Hidden` | Removed from public visibility by moderator/admin | No |
| `Deleted` | Deleted by comment author | No |

### 9.3 Comment creation

A new comment:

```text
Requires authenticated user
Requires interaction-enabled article
Starts with Status = Pending
Uses ParentCommentId = NULL
Does not immediately increase VisibleCommentCount
```

### 9.4 Public comments

Public comment APIs return only:

```text
Status = Visible
AND ParentCommentId = NULL
AND article remains public
```

---

## 10) Admin Comment Moderation

### 10.1 Moderation is part of V1

Admin/moderator workflow is not deferred to V2.

Recommended permissions:

```text
Interaction.Comments.Read
Interaction.Comments.Moderate
```

### 10.2 Supported moderation transitions

| Current status | Action | Next status |
|---|---|---|
| `Pending` | Approve | `Visible` |
| `Pending` | Reject | `Rejected` |
| `Visible` | Hide | `Hidden` |
| `Hidden` | Restore | `Visible` |

### 10.3 Version protection

Comment moderation is stale-write-sensitive.

Each moderation transition must enforce:

```text
Legal current status
+ expected/current Comment.Version
```

### 10.4 Local history

Required moderation actions append:

```text
CommentModerationActionHistory
```

in the same Interaction transaction as the comment/case transition and required outbox intent.

---

## 11) Author Delete-Own-Comment

A comment author may delete their own comment directly.

```text
Author deletion does not require moderator approval.
```

Allowed transitions:

| Current status | Next status |
|---|---|
| `Pending` | `Deleted` |
| `Visible` | `Deleted` |
| `Rejected` | `Deleted` |
| `Hidden` | `Deleted` |
| `Deleted` | Idempotent no-op |

If an author deletes a visible comment with an open moderation case:

```text
Comment: Visible -> Deleted
Case: Open -> ClosedByAuthorDeletion
Pending Reports -> ClosedByAuthorDeletion
History: CloseCaseByAuthorDeletion
```

These changes commit atomically.

---

## 12) Comment Reports and Moderation Cases

### 12.1 Report behavior

Authenticated users may report visible comments written by other users.

A report:

```text
Requests moderator review.
Does not prove policy violation.
Does not automatically hide content.
Does not immediately change public counters.
```

### 12.2 Report invariant

```text
At most one CommentReport per (CommentId, ReporterUserId).
```

### 12.3 Moderation case model

Reports are grouped into:

```text
CommentModerationCase
```

A case represents one report-review cycle for one comment.

Invariant:

```text
At most one Open CommentModerationCase per Comment.
```

### 12.4 Case outcomes

```text
Open
Dismissed
Actioned
ClosedByAuthorDeletion
```

| Case operation | Comment result | Case result | Pending report result |
|---|---|---|---|
| Admin dismiss reports | Remains `Visible` | `Dismissed` | `Dismissed` |
| Admin hide reported comment | `Visible -> Hidden` | `Actioned` | `Actioned` |
| Author deletes comment | `Visible -> Deleted` | `ClosedByAuthorDeletion` | `ClosedByAuthorDeletion` |

---

## 13) Administrator Alert Escalation

### 13.1 Alert purpose

Email alerts prioritize urgent moderation work.

They do not replace the moderation queue and do not automatically modify comment visibility.

### 13.2 Initial V1 policy

| Open-case condition | Result |
|---|---|
| Three distinct normal-severity reporters | Trigger one async admin-alert intent |
| One high-severity report | Trigger one async admin-alert intent |
| One critical-severity report | Trigger one async admin-alert intent |

Thresholds remain configurable.

### 13.3 One-alert invariant

```text
One Open CommentModerationCase may trigger at most one
administrator-alert business intent in V1.
```

Interaction persists alert-trigger metadata and publishes:

```text
interaction.comment_report_alert_triggered
```

Notifications performs actual email delivery asynchronously.

---

## 14) Public Counters and Reading Projection

### 14.1 Counter state

Interaction exposes public engagement summary through:

```text
ArticleInteractionStats
```

Conceptual fields:

```text
ArticlePublicId
ViewCount
LikeCount
VisibleCommentCount
StatsVersion
LastMaterializedAtUtc
LastPublishedAtUtc
```

### 14.2 Counter source

| Counter | Source |
|---|---|
| `ViewCount` | `ArticleViewCount` |
| `LikeCount` | Active `ArticleLike` truth |
| `VisibleCommentCount` | `Comment.Status = Visible` truth |

### 14.3 Reading event

Interaction publishes:

```text
interaction.article_counters_projection_published
```

Reading applies only versioned known-value snapshots:

```text
Apply only when IncomingStatsVersion > CurrentStatsVersion.
```

### 14.4 Counter correctness posture

```text
Counters may lag.
Counters never control article visibility.
Counters never replace relationship/comment/report truth.
Reading never rebuilds Interaction truth from counter snapshots.
```

---

## 15) Async, Idempotency and Versioning Posture

### 15.1 Outbox rule

For any mutation requiring downstream propagation:

```text
Interaction mutation
+ required local history
+ required OutboxMessage
```

must commit atomically in the same local transaction.

### 15.2 Delivery assumptions

Interaction assumes:

```text
At-least-once delivery
Duplicate delivery
Retry after timeout
Out-of-order/stale messages
Consumer restart
Replay/reconciliation activity
```

### 15.3 Required guards

| Concern | Guard |
|---|---|
| Duplicate active like | Unique active like per `(ArticlePublicId, UserId)` |
| Duplicate report | Unique report per `(CommentId, ReporterUserId)` |
| Duplicate open case | At most one `Open` case per comment |
| Duplicate alert | At most one alert intent per open case |
| Stale moderation action | `Comment.Version` |
| Stale case action | `CommentModerationCase.Version` |
| Stale eligibility input | `LastSourceVersion` |
| Stale counter snapshot | `StatsVersion` |
| Duplicate consumed message | `Unique(ConsumerName, MessageId)` |
| Concurrent accepted views | Atomic `ArticleViewCount` increment |

### 15.4 Freshness rule

```text
Version determines ordered freshness.
Timestamp is diagnostic metadata only.
```

---

## 16) Recovery and Reconciliation Posture

### 16.1 Eligibility projection recovery

Interaction must support safe repair/resync of:

```text
ArticleInteractionTargetProjection
```

from Content-owned source state.

While eligibility is unsafe:

```text
New public interactions fail closed.
```

### 16.2 Counter reconciliation

Interaction may reconcile:

```text
LikeCount = count active ArticleLike
VisibleCommentCount = count Comment where Status = Visible
```

`ViewCount` uses durable `ArticleViewCount` state in V1 and is not rebuilt from raw historical view events.

### 16.3 Counter repair publication

When repaired outward stats differ:

```text
Update ArticleInteractionStats
Increment StatsVersion
Publish interaction.article_counters_projection_published
```

Reading then converges by version.

---

## 17) Security and Abuse-Control Posture

Required protection areas:

| Capability | Mandatory posture |
|---|---|
| View tracking | Rate limiting, anti-inflation/repeat-view controls, non-blocking reading |
| Like/unlike | Authentication, relationship uniqueness, toggle-abuse monitoring |
| Comment submission | Authentication, validation, safe rendering, moderation before visibility |
| Delete-own-comment | Object-level ownership validation and safe concealment |
| Report comment | Authentication, non-owner restriction, uniqueness, no auto-hide |
| Moderation | Explicit permissions, state/version validation, history |
| Admin alert | One-intent invariant and minimal sensitive payload |
| Async events | Dedupe, version safety and payload minimization |

Safe logging must avoid raw comment content, raw report descriptions, raw moderator notes, authentication tokens and unnecessary visitor-identification data.

---

## 18) Primary API Consumers

| Consumer | Interaction usage |
|---|---|
| Public website/mobile clients | Track views; read visible comments |
| Authenticated users | Like/unlike; submit/delete own comments; report comments |
| Admin frontend | Moderate comments; resolve report cases; view moderation history/stats |
| Content worker/event flow | Provides eligibility projection input |
| Reading | Consumes versioned public counter snapshots |
| Notifications | Consumes report-alert intent |
| Audit | Consumes moderation-relevant committed facts |
| Operations/monitoring | Observes lag, reconciliation, abuse and invariant health |

---

## 19) V1 Non-Goals and Deferred Capabilities

Interaction V1 does not support:

```text
Reply-comment behavior
Nested comment threads
Comment editing
Comment reactions
Article reactions beyond like
Automatic hiding based on report count
Bulk moderation
Article-author moderation
Reporter reputation scoring
Raw per-view analytics history
Unique visitor analytics
Trending/popularity ranking pipeline
Full scheduled rebuild orchestration
```

### 19.1 Reply compatibility note

`Comment.ParentCommentId nullable` may exist in persistence for future compatibility, but it does not enable any reply API or reply behavior in V1.

---

## 20) Document Map

| File | Purpose |
|---|---|
| `00-index.md` | Module overview, ownership, runtime and V1 direction |
| `01-api-surface.md` | Public/authenticated/admin HTTP endpoints and async response boundaries |
| `02-domain-contracts.md` | Entities, statuses, invariants and inbound/outbound event contracts |
| `03-runtime-flows.md` | Synchronous commands, async propagation, moderation/report and counter flows |
| `04-errors-status-codes.md` | HTTP/error contracts, conflict and temporary-projection behavior |
| `05-security-abuse-controls.md` | Abuse prevention, permissions, privacy and safe logging |
| `06-idempotency-consistency.md` | Retry, transaction, concurrency, versioning and reconciliation rules |
| `07-observability-slos.md` | Metrics, dashboards, release gates and operator questions |
| `08-dependencies-and-ownership.md` | Allowed/forbidden dependencies and cross-module ownership |
| `09-open-questions.md` | Remaining implementation decisions and deferred ADR hooks |
| `10-business-rules.md` | Final V1 business rules used to drive DB/API/code design |

---

## 21) Key Architecture Links

### 21.1 System-wide API architecture

```text
../../01-api-architecture-charter-v1.md
../../02-contracts-and-standards.md
../../07-security-threat-modeling.md
../../09-observability-and-slos.md
```

### 21.2 Arc42 architecture

```text
../../../architecture/arc42/03-building-blocks-modularity.md
../../../architecture/arc42/04-runtime-view-v1.md
../../../architecture/arc42/05-quality-requirements.md
../../../architecture/arc42/13-transactions-and-consistency-v1.md
../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md
../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md
../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md
../../../architecture/arc42/19-stream-processing-runtime-v1.md
```

### 21.3 ADRs

```text
../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md
../../../decisions/adr-0018-transaction-boundaries-and-consistency-model-v1.md
../../../decisions/adr-0019-system-model-and-fault-assumptions-v1.md
../../../decisions/adr-0020-timeout-retry-and-failure-detection-policy-v1.md
../../../decisions/adr-0022-versioning-and-fencing-strategy-v1.md
../../../decisions/adr-0025-batch-processing-and-derived-state-policy-v1.md
../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md
../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md
```

---

## 22) Remaining Implementation Questions

The architecture and V1 business scope are now fixed.

Remaining implementation questions include:

```text
Final Content eligibility event payload/name alignment
Eligibility resync command/job shape
View anti-abuse and repeat-view suppression thresholds
Counter materialization/coalescing strategy for high-volume views
Durable Idempotency-Key support for comment creation
Final reason/severity configuration
Notifications alert payload and recipient policy
Audit inclusion policy for individual report submissions
Moderation/report retention policy
Public comment author-display source
Admin queue query/prioritization shape
Optional bounded stats-reconcile admin command
```

These questions refine implementation details; they do not reopen Interaction V1 ownership or business rules.

---

## 23) Final V1 Architecture Posture

```text
Interaction owns engagement and moderation workflow truth.

Content asynchronously tells Interaction whether an article accepts new interaction.

Views use durable ArticleViewCount materialization, not raw per-view history.

Likes are per-user/article truth with idempotent relationship semantics.

Comments are top-level only, begin Pending and become public only when Visible.

Comment authors may delete their own comments without admin approval.

Users may report visible comments, but reports never auto-hide content.

Reports are grouped into CommentModerationCase and may trigger one async admin-alert intent.

Admin moderation writes local CommentModerationActionHistory.

ArticleInteractionStats publishes versioned public counter snapshots to Reading.

Unpublish disables new interaction but preserves counters and history.

All cross-module effects use outbox-backed asynchronous contracts,
durable dedupe, version-aware apply and reconciliation-safe derived state.
```
