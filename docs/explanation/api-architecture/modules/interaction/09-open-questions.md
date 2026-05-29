# Interaction — Open Questions & ADR Hooks (V1)

This document tracks Interaction V1 decisions that are already closed, remaining implementation questions, and future ADR hooks.
It does not reopen V1 business rules already agreed in `10-business-rules.md`.

Related:

- `01-api-surface.md`
- `02-domain-contracts.md`
- `03-runtime-flows.md`
- `04-errors-status-codes.md`
- `05-security-abuse-controls.md`
- `06-idempotency-consistency.md`
- `07-observability-slos.md`
- `08-dependencies-and-ownership.md`
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

## 1) Decisions Already Closed for V1

The following items are no longer open questions for Interaction V1.

### 1.1 View-count model

Closed decision:

```text
Interaction V1 uses ArticleViewCount.
```

Rules already fixed:

- V1 does not retain one durable raw row for each page view.
- `ArticleViewCount` stores accepted/materialized view-count state.
- Accepted view mutations use atomic increment behavior.
- View tracking is non-blocking relative to Reading.
- Reading never consumes one message per individual view.
- V1 does not promise exact historical view reconstruction from raw events.

Remaining open details concern anti-abuse implementation and operational thresholds only.

---

### 1.2 Article eligibility dependency

Closed decision:

```text
Interaction consumes Content-derived article eligibility/public-state input asynchronously
and maintains ArticleInteractionTargetProjection locally.
```

Rules already fixed:

- Interaction does not synchronously query Content or Reading for ordinary public interaction commands.
- New view/like/comment/report operations require safe local interaction eligibility.
- Missing, unsafe or resync-required eligibility fails closed.
- Unpublish/archive/soft-delete disables new public interaction.
- Existing counters, likes, comments, reports, cases and history are preserved.
- Existing user-owned retractions remain allowed:
  - unlike own active like;
  - delete own comment.
- Authorized moderators may still resolve existing open cases.

Remaining open details concern final Content event contract alignment and resync implementation.

---

### 1.3 Comment moderation model

Closed decision:

```text
Comment moderation is part of Interaction V1.
```

Supported comment statuses:

```text
Pending
Visible
Rejected
Hidden
Deleted
```

Supported moderator transitions:

```text
Pending -> Visible   // Approve
Pending -> Rejected  // Reject
Visible -> Hidden    // Hide
Hidden  -> Visible   // Restore
```

Supported author behavior:

```text
Pending / Visible / Rejected / Hidden -> Deleted
```

Rules already fixed:

- Only `Visible` comments are public.
- Author deletion does not require moderator approval.
- Comment status transitions are version-protected.
- Required moderation history is stored locally through `CommentModerationActionHistory`.
- Audit is asynchronous and does not replace Interaction operational history.

---

### 1.4 Comment report and moderation-case workflow

Closed decision:

```text
Interaction V1 supports user reporting of visible comments and
CommentModerationCase workflow for administrator handling.
```

Rules already fixed:

- Only authenticated non-authors may report a `Visible` comment.
- One user may report a given comment at most once in V1.
- Report creation never automatically hides a comment.
- At most one `Open` moderation case may exist per comment.
- Valid case outcomes are:

```text
Dismissed
Actioned
ClosedByAuthorDeletion
```

- Hiding a reported comment resolves comment, case and reports atomically.
- Author deletion may close an open case atomically.

---

### 1.5 Administrator alert policy direction

Closed decision:

```text
Report escalation may trigger asynchronous administrator-alert intent,
but never automatic comment hiding.
```

Initial V1 default policy:

```text
3 distinct Normal-severity reporters -> trigger one admin-alert intent
1 High-severity report              -> trigger one admin-alert intent
1 Critical-severity report          -> trigger one admin-alert intent
```

Rules already fixed:

- Thresholds are configuration values.
- One open case may trigger at most one alert business intent in V1.
- Interaction publishes alert intent through outbox.
- Notifications owns recipient configuration, delivery, retries and provider ambiguity.
- Moderator queue remains authoritative even when email is delayed.

---

### 1.6 Public counter model and Reading integration

Closed decision:

```text
ArticleInteractionStats is Interaction-owned public counter snapshot state.

Interaction publishes:
    interaction.article_counters_projection_published

Reading consumes:
    versioned known-value counter snapshots only.
```

Counters included:

```text
ViewCount
LikeCount
VisibleCommentCount
```

Rules already fixed:

- Reading does not calculate counters from raw like/comment/view action events.
- `StatsVersion` determines freshness.
- Counter lag is acceptable.
- Counters do not decide article visibility or Interaction truth.
- Unpublish/archive/soft-delete does not reset counters.

---

### 1.7 Reply-comment compatibility

Closed decision:

```text
Comment.ParentCommentId may be nullable in schema for future compatibility,
but Interaction V1 supports top-level comments only.
```

Rules already fixed:

- Every V1-created comment has `ParentCommentId = NULL`.
- V1 create-comment APIs do not accept parent identifiers.
- V1 does not expose reply or nested-thread behavior.
- Reply rules require a later explicit design phase.

---

## 2) Remaining V1 Implementation Questions

The following questions remain valid for implementation planning. They do not change the closed V1 business model.

---

### OQ-01: Final Content Event Contract for Interaction Eligibility

#### Question

Which exact Content outbound event type and payload will Interaction consume to maintain:

```text
ArticleInteractionTargetProjection
```

Current conceptual direction:

```text
content.article_read_projection_published
```

#### Must be decided before implementation

- final event type spelling;
- source aggregate identity field;
- source version field;
- article public identifier field;
- public/interaction-enabled field;
- handling for publish, unpublish, archive and soft-delete;
- whether the same event consumed by Reading is sufficient for Interaction;
- whether Interaction needs any additional eligibility-specific field.

#### Fixed constraints

Whatever event is selected:

- Content remains source owner;
- Interaction applies messages idempotently;
- Interaction uses source-version ordering;
- uncertain projection state fails closed;
- no synchronous Content lookup is introduced for normal Interaction commands.

#### ADR hook

No new ADR is required if this is only finalization of an existing module event contract consistent with the current async projection decisions.

A new ADR would be needed only if the design changes toward synchronous cross-module eligibility checks or another ownership model.

---

### OQ-02: Eligibility Resync and Repair Trigger

#### Question

How is `ArticleInteractionTargetProjection` repaired when:

- Interaction detects a version gap;
- projection state is missing;
- an operator suspects projection drift;
- consumer backlog or failure causes prolonged uncertainty?

#### Options to decide later

```text
A. Operator-triggered bounded resync by ArticlePublicId
B. Scheduled reconciliation over a bounded page of article identities
C. Replay from retained source/outbox data where safe
D. Combination of A and B
```

#### Recommended V1 implementation direction

Start with:

```text
Operator-triggered bounded resync for a selected ArticlePublicId
plus observability for projection gap/resync-required state.
```

Scheduled reconciliation may be added after normal async flow is stable.

#### Fixed constraints

- new interactions fail closed while eligibility is unsafe;
- resync updates only Interaction local derived projection;
- resync must not mutate Content truth;
- repeated resync must be safe.

---

### OQ-03: View Acceptance and Anti-Inflation Policy

#### Question

Which concrete policy determines whether a page-view request contributes to:

```text
ArticleViewCount.ViewCount
```

#### Items to decide

- repeat-view suppression window;
- anonymous-session strategy;
- authenticated-user repeat counting strategy;
- whether Redis is used for temporary suppression;
- whether IP-derived or user-agent-derived hash signals are necessary;
- retention of any temporary suppression key;
- thresholds for rate limiting;
- whether view endpoint returns generic `accepted = true` for suppressed contributions.

#### Recommended V1 direction

Use a simple operational policy:

```text
- generic accepted response;
- rate limit the view endpoint;
- apply short-lived repeat-view suppression through temporary/cache state if adopted;
- persist only accepted aggregate count in ArticleViewCount;
- do not retain raw browsing-history rows.
```

#### Privacy hook

A dedicated privacy ADR or explicit privacy-policy amendment may be needed if future implementation stores durable visitor fingerprints, raw IP addresses or long-lived browsing-identification signals.

---

### OQ-04: Counter Materialization Trigger and Coalescing Strategy

#### Question

What implementation triggers updates from Interaction state into:

```text
ArticleInteractionStats
```

and publication of:

```text
interaction.article_counters_projection_published
```

#### Candidate approaches

##### Option A — Materialize after each count-affecting operation

```text
Accepted view / Like / Unlike / Visible-status transition
    -> materialize new stats snapshot
    -> publish snapshot if changed
```

Advantages:

- simplest reasoning;
- near-real-time convergence.

Concerns:

- view traffic can produce excessive stats/outbox publication.

##### Option B — Immediate for like/comment, coalesced for views

```text
Like/comment visibility changes
    -> promptly materialize/publicize snapshot

Accepted views
    -> increment ArticleViewCount
    -> coalesce snapshot publication by window or threshold
```

Advantages:

- keeps like/comment responsive in public counters;
- avoids one-publication-per-view amplification.

Concerns:

- requires separate view publication trigger policy.

##### Option C — Fully coalesced materialization

All counter changes are periodically/materially coalesced before publication.

Advantages:

- lowest event/publication amplification.

Concerns:

- counters appear slower during low traffic and require more worker orchestration.

#### Recommended V1 direction

```text
Option B:
Immediate or prompt publication for like/comment-visible changes,
coalesced publication for high-volume view changes.
```

#### Fixed constraints

- Reading receives only known-value snapshots;
- `StatsVersion` increments only when outward snapshot changes;
- Reading applies only newer `StatsVersion`;
- no per-view event is sent to Reading.

---

### OQ-05: Comment-Create Idempotency-Key Policy

#### Question

Will V1 implement a durable `Idempotency-Key` contract for:

```text
POST /api/v1/articles/{articlePublicId}/comments
```

#### Why this remains open

Comment creation is not naturally idempotent like a like relationship:

```text
Retry after timeout may accidentally create two Pending comments.
```

#### Options

##### Option A — Implement idempotency key in V1

Rules:

```text
Same key + same payload -> same logical comment result
Same key + different payload -> 409 Conflict
```

Advantages:

- correct retry posture;
- more production-ready.

##### Option B — Document duplicate risk and implement later

Advantages:

- less code initially.

Concerns:

- weaker client retry behavior;
- less aligned with production-grade command handling.

#### Recommended V1 direction

Implement durable idempotency-key support for comment creation before exposing the public website comment form.

#### ADR hook

No new ADR is required if implemented using an existing shared idempotency pattern. If the system introduces a general cross-module idempotency framework, document it centrally.

---

### OQ-06: Moderation Reason Taxonomy

#### Question

Which final reason-code sets should be used for:

```text
Comment report reasons
Moderator reject/hide/dismiss reasons
```

#### Current recommended sets

##### Report reason codes

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

##### Moderation reason codes

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

#### Items to finalize

- whether `Misinformation` is acceptable as a user-report reason in V1;
- whether report and moderation reason enums should be shared or separated;
- whether moderator `DismissReportedCase` needs distinct dismissal reason codes;
- maximum note/description length;
- whether `Other` always requires note/description.

#### Fixed constraints

- reason fields are allowlisted;
- `Other` requires explanatory text;
- descriptions/notes are untrusted input;
- report reason does not automatically prove a violation.

---

### OQ-07: Report Severity and Alert Configuration Storage

#### Question

Where does configuration live for:

```text
ReasonCode -> Severity mapping
Report threshold count
Administrator alert enabling/disabling
```

#### Candidate approaches

```text
A. Application configuration for V1
B. Interaction-owned database policy table
C. Admin-configurable settings UI
```

#### Recommended V1 direction

```text
Application configuration for V1,
with documented defaults and metrics.
```

This avoids introducing configuration-admin workflows before moderation behavior is stable.

#### Fixed default posture

```text
Normal threshold = 3 distinct reporters
High threshold   = 1 report
Critical threshold = 1 report
```

#### ADR hook

A later policy-management ADR may be needed if thresholds become tenant-specific, admin-editable or governed by audit/approval workflow.

---

### OQ-08: Notifications Recipient and Email Payload Contract

#### Question

What exact Notifications contract and recipient policy will be used for:

```text
interaction.comment_report_alert_triggered
```

#### Items to decide

- final event payload fields;
- whether recipients are users with an admin/moderator permission, configured mailbox addresses, or both;
- email template name;
- whether email includes direct admin frontend URL;
- whether raw comment/report content is omitted or minimized;
- notification priority and retry policy;
- exact business-intent dedupe key implementation.

#### Fixed constraints

- Interaction owns only alert-intent trigger;
- Notifications owns recipient selection and delivery truth;
- one open moderation case triggers at most one Interaction alert intent;
- email failure does not revert Interaction case/report truth;
- email payload must minimize sensitive user-generated text.

#### Recommended business-intent identity

```text
InteractionCommentReportAlert:{CommentModerationCasePublicId}
```

---

### OQ-09: Audit Event Inclusion for Report Submission

#### Question

Should Audit ingest:

```text
interaction.comment_reported
```

in addition to moderation decision events?

#### Current fixed Audit candidates

```text
interaction.comment_approved
interaction.comment_rejected
interaction.comment_hidden
interaction.comment_restored
interaction.comment_deleted_by_author
interaction.comment_reports_dismissed
```

#### Considerations

Including report submission in Audit may help with:

- abuse investigations;
- report-brigading review;
- moderator accountability context.

However, it also increases:

- stored allegation data;
- privacy/retention concerns;
- audit event volume.

#### Recommended V1 direction

Audit moderator decisions and case outcomes definitely.

Decide separately whether individual user reports are canonical audit evidence or only Interaction operational truth retained under Interaction policy.

---

### OQ-10: Moderation History Retention

#### Question

How long should Interaction retain:

```text
CommentModerationActionHistory
Resolved CommentReport records
Resolved CommentModerationCase records
```

#### Considerations

These records support:

- admin operational review;
- incident investigation;
- abuse-pattern analysis;
- Audit correlation.

They may also contain:

- user-generated allegation text;
- moderator notes;
- actor references.

#### Items to decide

- retention period per record type;
- soft-delete/archival policy;
- whether resolved report descriptions are minimized after retention window;
- whether audit retention differs from operational Interaction retention.

#### Fixed constraints

- cleanup must not break current workflow integrity;
- open cases must not be removed;
- deletion of operational data must follow approved privacy/audit policy;
- normal comment author deletion must not immediately erase active investigation context.

---

### OQ-11: Public Comment Query and Display Identity

#### Question

What author information may public comment responses expose?

Current conceptual response used:

```text
authorDisplayName
```

#### Items to decide

- display name source and projection strategy;
- whether avatar is exposed;
- whether deleted/disabled users affect visible historical comments;
- whether Interaction stores display snapshot or resolves user-public display elsewhere;
- privacy rules for user identity exposure.

#### Fixed constraints

- public APIs do not expose internal user ids;
- moderator/report information is never public;
- comment content remains Interaction truth;
- Identity remains owner of account truth.

#### ADR hook

A dedicated public user-profile/display projection decision may be required if multiple public modules need user display snapshots without synchronous Identity calls.

---

### OQ-12: Admin Queue Query Shape and Prioritization

#### Question

How should the admin frontend retrieve moderation work efficiently?

Current V1 APIs include:

```text
GET /api/v1/admin/interaction/comments
GET /api/v1/admin/interaction/comment-moderation-cases
```

#### Items to decide

- whether Pending comments and reported cases appear in separate tabs or one unified queue;
- allowed sort fields;
- whether priority is persisted or derived;
- whether alert-triggered cases are pinned;
- whether count summaries are queried live or materialized;
- paging size and filtering requirements.

#### Fixed constraints

- pending-comment approval queue and reported-comment case queue represent different workflows;
- only authorized admins/moderators may read either;
- admin queue projections/summaries must remain subordinate to Interaction truth.

---

### OQ-13: Direct Hide When an Open Report Case Exists

#### Question

Should the direct comment hide endpoint reject when a comment currently has an open moderation case, requiring admin to use the case-resolution endpoint instead?

Current recommended rule:

```text
If Comment has an Open CommentModerationCase:
    reject direct hide with conflict;
    require hide-comment case resolution flow.
```

#### Why this is recommended

It guarantees atomic update of:

```text
Comment
Case
Pending Reports
Moderation History
Outbox Event
```

without leaving an unresolved open case attached to an already hidden comment.

#### Decision needed before API implementation

Either:

```text
A. Enforce case-resolution-only hide when Open case exists. // recommended
B. Allow direct hide and automatically resolve open case internally.
```

Whichever option is selected must preserve one atomic workflow and must not orphan unresolved reports.

---

### OQ-14: Counter Inspection and Manual Repair Admin Surface

#### Question

Should V1 expose an admin/operator endpoint to trigger bounded stats reconciliation for one article?

Current admin read endpoint direction:

```text
GET /api/v1/admin/interaction/articles/{articlePublicId}/stats
```

Potential future command:

```text
POST /api/v1/admin/interaction/articles/{articlePublicId}/stats/reconcile
```

#### Considerations

Advantages:

- useful during testing and operational repair;
- demonstrates recovery posture clearly.

Concerns:

- requires authorization;
- must avoid becoming an uncontrolled expensive operation;
- must produce audit/operational evidence;
- must not claim to rebuild `ViewCount` from raw history.

#### Recommended sequencing

```text
Implement stats read diagnostics in V1.
Add manual bounded reconcile command when stats aggregation and Reading consumer testing begins.
Do not implement full scheduled rebuild infrastructure yet.
```

---

### OQ-15: Counter Snapshot Publication Frequency for Views

#### Question

Because views may have higher traffic than likes/comments, how frequently should view-driven counter snapshots be published to Reading?

#### Candidate policies

```text
Time window:
    publish no more often than once every N seconds per article

Count threshold:
    publish after N additional accepted views

Hybrid:
    publish after threshold or maximum time window, whichever occurs first
```

#### Recommended V1 direction

Use configurable coalescing for view-driven publication while keeping the behavior simple enough to test.

Example posture:

```text
Likes/comment visibility changes:
    materialize/publish promptly.

Views:
    increment ArticleViewCount immediately when accepted;
    publish ArticleInteractionStats snapshot through configured coalescing.
```

#### Fixed constraints

- public counter changes may lag;
- no individual view event is sent to Reading;
- published snapshot always contains known values;
- `StatsVersion` must be monotonic when snapshot output changes.

---

## 3) Deferred Future-Phase Questions

The following questions are intentionally outside Interaction V1 implementation scope.

---

### FUTURE-01: Reply Comment Behavior

Schema compatibility may exist through:

```text
Comment.ParentCommentId nullable
```

but V1 does not support replies.

Future design must decide:

- one-level replies or unlimited nesting;
- whether a reply may target only a `Visible` parent;
- what happens to replies when parent is hidden;
- what happens to replies when parent is deleted by author;
- whether reports apply independently to replies;
- whether a moderation case may target a reply;
- whether replies count toward `VisibleCommentCount`;
- public thread paging and sorting behavior.

Likely direction:

```text
Support one-level replies before considering unlimited nesting.
```

---

### FUTURE-02: Comment Editing and Re-Moderation

V1 does not support comment editing.

Future design must decide:

- whether a visible edited comment becomes `Pending` again;
- whether the old visible version remains public until approval;
- whether comment revisions are stored;
- how existing reports/cases relate to edited content;
- how Audit records before/after content safely;
- idempotency and optimistic-concurrency rules for edit.

---

### FUTURE-03: Raw View Analytics and Unique Visitor Measurement

V1 uses `ArticleViewCount` and does not retain raw per-view records.

Future analytics design may decide:

- event/bucket storage model;
- unique visitor strategy;
- event-time/windowing policy;
- bot detection sophistication;
- privacy and retention policy;
- trending/ranking inputs;
- rebuildability of view-based aggregates.

A dedicated privacy/analytics ADR is likely required before storing durable visitor-level data.

---

### FUTURE-04: Trending and Engagement Ranking

V1 publishes counters only and does not compute trending/ranking scores.

Future design must decide:

- scoring formula;
- time windows;
- bot/report manipulation resistance;
- source counters;
- materialization and rebuild flow;
- Reading exposure;
- candidate/cutover behavior where correctness matters.

---

### FUTURE-05: Automated Moderation Based on Reports

V1 never auto-hides comments based only on reports.

Any future automated moderation must decide:

- false-report/brigading protection;
- confidence/evidence thresholds;
- moderator appeal/review flow;
- reporter reputation;
- auditability;
- user notification;
- rollback/restore behavior.

This would require an explicit future ADR because it changes content-visibility decision authority.

---

### FUTURE-06: Bulk Moderation

V1 supports individual moderation commands only.

Future bulk moderation must define:

- per-item versus whole-batch transaction boundary;
- partial success response model;
- history and outbox per affected comment/case;
- idempotency and retry;
- authorization;
- operational/audit controls.

---

### FUTURE-07: Article-Author Moderation Capability

V1 does not automatically allow an article author to moderate comments on their article.

Future design must decide:

- whether authors may moderate comments only on owned articles;
- whether hide/approve/report-resolve capabilities differ;
- Authorization resource attributes and ABAC rules;
- conflict with moderator decisions;
- audit requirements.

---

### FUTURE-08: Full Scheduled Rebuild / Reconciliation Infrastructure

V1 documents bounded repair and does not require full scheduled orchestration.

Future scale may require:

- scheduled reconciliation;
- partitioned rebuild scope;
- worker ownership/fencing;
- candidate publication/cutover;
- large-volume stats drift monitoring;
- rebuild history tables;
- operational controls.

This must align with the system-wide batch and ownership/fencing policies.

---

## 4) Implementation Decision Checklist Before DB and Code

Before finalizing Interaction tables, indexes, stored procedures and application use cases, resolve or explicitly defer the following implementation questions:

| Question | Must resolve before DB? | Must resolve before API/code? | Current direction |
|---|---:|---:|---|
| Final Content eligibility event payload | No for broad entity model; yes for consumer implementation | Yes | Reuse Content public projection event if sufficient |
| Eligibility resync command/job shape | No | Before recovery tooling | Operator-triggered bounded resync first |
| View anti-abuse implementation | Partly | Yes before public endpoint exposure | Rate limit + optional short-lived suppression |
| Counter materialization/coalescing | Affects stats/outbox procedures | Yes | Prompt likes/comments; coalesced views |
| Comment idempotency-key persistence | Affects table/proc if durable | Yes | Recommended in V1 |
| Reason-code enums | Yes for DB constraints/enums | Yes | Finalize recommended lists |
| Severity mapping/config location | May be config-only | Yes for alerts | Application configuration first |
| Notifications alert payload | No for core truth tables | Yes for handler/event contract | Minimal payload + case dedupe key |
| Audit ingestion of individual reports | No | Before Audit handler implementation | Decide separately |
| Moderation/history retention | May affect cleanup/index policy | Before production | Define after workflow stabilizes |
| Public comment author display source | May affect read DTO/projection | Yes for frontend | Requires explicit public identity approach |
| Direct hide with open case behavior | Yes for stored procedure/use case | Yes | Require case-resolution endpoint |
| Manual stats reconcile endpoint | No | Optional after aggregation test | Add later if useful |
| View snapshot publication interval | No for entity tables; affects worker/config | Yes for materializer | Configurable coalescing |

---

## 5) Final Open-Question Posture

Interaction V1 no longer has architectural uncertainty about:

```text
Use of ArticleViewCount
Async article eligibility projection
Comment moderation being in V1
Report and moderation-case workflow
No automatic hide from reports
Async administrator alert intent
ArticleInteractionStats snapshot publication to Reading
ParentCommentId being reserved only for future replies
Counter preservation across unpublish/archive/soft-delete
```

The remaining work is implementation finalization around:

```text
Content event contract alignment
Eligibility resync execution
View anti-abuse thresholds
Counter materialization/coalescing
Comment-create idempotency storage
Reason/severity configuration
Notifications/Audit payload details
Retention and admin operational tooling
```

Future capability questions remain explicitly deferred and must not silently expand V1 scope.
