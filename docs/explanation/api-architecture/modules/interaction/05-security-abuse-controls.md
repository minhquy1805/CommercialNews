# Interaction — Security & Abuse Controls (V1)

This document defines Interaction security and abuse-control expectations for V1.
It focuses on public/user interaction endpoints, administrator moderation endpoints, report escalation, privacy, safe logging, and async-security boundaries.

Related:

- `01-api-surface.md`
- `02-domain-contracts.md`
- `03-runtime-flows.md`
- `04-errors-status-codes.md`
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
- ADR-0027 — Stream Processing and Derived State Policy (V1)
- ADR-0028 — Consumer Idempotency, Replay, and Rebuild Policy (V1)

---

## 1) Security Posture

Interaction is exposed to public and authenticated traffic and must assume that callers may be:

- anonymous visitors;
- authenticated legitimate users;
- automated bots;
- abusive users;
- clients retrying after network failure;
- callers intentionally probing hidden resources;
- moderators operating from stale UI state.

Interaction security is based on five principles:

```text
1. Public input is untrusted.
2. Actor identity and permissions come from trusted server-side context.
3. Article interaction eligibility is enforced locally and fails closed when uncertain.
4. Reports create review workflow, not automatic censorship.
5. Downstream async systems must not expand authority beyond Interaction decisions.
```

---

## 2) Non-Blocking Reading Rule

### 2.1 Article reading must not depend on Interaction availability

Reading owns public article response composition.

Interaction must not be placed in the critical path such that:

```text
Interaction unavailable
    -> public article cannot be rendered
```

Recommended runtime relationship:

```text
Reading serves public article successfully
    -> client separately sends view-tracking request to Interaction
```

### 2.2 Safe failure behavior

If view tracking fails because of:

- Interaction outage;
- timeout;
- rate limiting;
- anti-abuse rejection;
- temporary eligibility-projection uncertainty;
- counter persistence failure;

then:

```text
Public article reading remains successful.
The view may not be counted or may be reflected later.
```

### 2.3 Counter enrichment is non-authoritative

Stale or missing counters must not:

- expose unpublished articles;
- hide publicly valid articles;
- change comment visibility truth;
- prove whether a user command committed.

---

## 3) Trust Boundary and Actor Identity

### 3.1 Trusted actor source

For authenticated actions, Interaction must obtain actor identity from trusted server-side authentication context.

This applies to:

```text
Like / Unlike
Create Comment
Delete Own Comment
Report Comment
Admin Moderation
Moderation Case Resolution
```

### 3.2 Untrusted client fields

Interaction must not trust client-supplied fields as authority for:

```text
UserId
AuthorUserId
ReporterUserId
ModeratorUserId
Role
Permission
Article publication state
Comment ownership
Comment status
Case status
Counter values
Event timestamp ordering
Alert-trigger state
```

### 3.3 Object-level authorization

Object ownership must be validated from Interaction truth.

| Operation | Required object-level validation |
|---|---|
| Unlike | Active like belongs to current user |
| Delete own comment | `Comment.AuthorUserId = CurrentUserId` |
| Report comment | `Comment.AuthorUserId != CurrentUserId` |
| Admin comment moderation | Actor has moderation permission |
| Admin case resolution | Actor has case-resolution permission; hide additionally requires comment moderation permission |

---

## 4) Article Eligibility Protection

### 4.1 Local eligibility enforcement

New public interactions require a safe local projection:

```text
ArticleInteractionTargetProjection.IsInteractionEnabled = true
AND ArticleInteractionTargetProjection.RequiresResync = false
```

This rule applies to:

```text
New accepted view contribution
New like
New comment
New comment report
```

### 4.2 Fail-closed behavior

If eligibility is unknown, missing or unsafe:

```text
Do not accept new public interaction.
```

The API should distinguish:

| Condition | Security meaning | Response posture |
|---|---|---|
| Article confirmed non-public/ineligible | Stable business unavailability | `404 Not Found` |
| Eligibility projection unavailable or requires resync | Temporary safe-decision failure | `503 Service Unavailable` |

### 4.3 Why fail closed is required

Accepting interaction against an article whose public status is uncertain can cause:

- engagement on unpublished content;
- reports on content no longer public;
- inconsistent moderation queue data;
- policy bypass during consumer lag.

Temporary rejection is safer than accepting interaction under uncertain visibility.

### 4.4 Existing user-owned removals

When article becomes non-public, the security posture still permits users to remove their own prior engagement:

```text
Unlike own active like
Delete own existing comment
```

Authorized moderators may still resolve an already-open moderation case.

---

## 5) Endpoint Abuse-Control Matrix

| Endpoint / capability | Main abuse risk | Required protection |
|---|---|---|
| Track view | Bot refresh floods, view inflation | Rate limit, repeat-view suppression, atomic accepted-count update |
| Like article | Like spam, duplicate retry | Authentication, unique active-like invariant, per-user rate limit |
| Unlike article | Automated toggling / state churn | Authentication, ownership/state guard, rate limit |
| Create comment | Spam, malicious content, duplicate submissions | Authentication, content validation, rate limit, safe rendering, idempotency posture |
| Delete own comment | BOLA/IDOR attempt | Author ownership validation, concealed not-found response |
| Public list comments | Exposure of moderated content | Return `Visible` only, safe output rendering |
| Report comment | Coordinated false reporting, self-reporting, email amplification | Authentication, non-owner rule, uniqueness, rate limit, no auto-hide |
| Admin moderate comment | Privilege escalation, stale decisions | Explicit permission, state/version guard, history/outbox |
| Admin resolve case | Unauthorized removal/dismissal, race conditions | Explicit permission, case/comment version guards |
| Admin read report/case/history | Disclosure of sensitive moderation data | Admin-only authorization, bounded query and safe logs |
| Admin read stats | Disclosure/misinterpretation of derived data | Permission, document derived/stale posture |

---

## 6) View Counting Abuse Controls

### 6.1 V1 view model

Interaction V1 stores:

```text
ArticleViewCount
```

It does not persist one durable business row per individual page view.

### 6.2 View-count threat model

Attackers may attempt to inflate `ViewCount` through:

- repeated page refresh;
- scripted requests;
- high-frequency anonymous traffic;
- replayed requests;
- forged client metadata;
- distributed bot traffic.

### 6.3 Required controls

View tracking must support:

- endpoint rate limiting;
- repeat-view suppression within a configured time window;
- malformed-request rejection;
- bot/anomaly handling where operationally adopted;
- atomic increments only for accepted contributions;
- monitoring for unusual article-specific spikes.

### 6.4 Client-visible response posture

The endpoint may return a generic accepted response without revealing whether one request was counted after suppression policy:

```json
{
  "accepted": true
}
```

This reduces the risk that abusive clients tune behavior around internal deduplication thresholds.

### 6.5 Privacy-safe dedupe signals

If repeat-view suppression needs visitor-related signals, the implementation may use bounded and privacy-aware values such as:

```text
Authenticated UserId, where present
Anonymous session token/hash
IP-derived hash, if approved
User-agent-derived hash, if approved
Short-lived cache key/window
```

Rules:

- do not trust client-supplied identity as authoritative;
- do not store raw IP/user-agent unless explicitly justified and governed;
- prefer hashed or short-lived signals where feasible;
- retention must be bounded and documented;
- Redis/cache-based suppression must not become durable business truth.

### 6.6 No exact analytics claim

Because V1 does not retain raw per-view records:

```text
ViewCount is an accepted/materialized counter under policy,
not a forensic record of every individual reader visit.
```

---

## 7) Like and Unlike Abuse Controls

### 7.1 Authentication

Only authenticated users may like or unlike articles.

### 7.2 Integrity invariant

```text
At most one active ArticleLike per (ArticlePublicId, UserId).
```

This must be enforced authoritatively through database constraints or equivalent conditional-write semantics.

### 7.3 Required protections

- retrieve user identity from server-side context;
- validate article eligibility for new likes;
- rate limit repeated like/unlike toggling by user and article where needed;
- handle repeated like/unlike commands idempotently;
- do not derive current user like state from `LikeCount`;
- do not allow counter lag to create additional active likes.

### 7.4 Toggle abuse

A user may repeatedly toggle like/unlike to cause event and counter-materialization noise.

Operational protections may include:

- per-user/per-article mutation rate limits;
- coalesced counter publication;
- abuse metrics for high-frequency toggling.

### 7.5 Non-public article behavior

Once an article becomes non-public:

| Action | Allowed |
|---|---:|
| New like | No |
| Unlike an existing own active like | Yes |

---

## 8) Comment Submission Security

### 8.1 Authentication baseline

Only authenticated users may submit comments in V1.

Anonymous comments are not supported.

### 8.2 V1 top-level-only enforcement

The domain may reserve:

```text
Comment.ParentCommentId nullable
```

for future replies, but V1 must enforce:

```text
ParentCommentId = NULL for every newly created comment.
```

API requests that attempt reply behavior must be rejected.

### 8.3 Comment content is untrusted input

Comment content may contain:

- HTML/script injection;
- malicious links;
- spam;
- personal information;
- abusive material;
- oversized payloads;
- control characters or malformed Unicode.

### 8.4 Required controls

Comment creation must enforce:

- authentication;
- article eligibility;
- rate limiting;
- minimum/maximum length validation;
- empty/whitespace rejection;
- safe storage policy;
- safe output rendering/encoding;
- optional moderation-oriented content scanning only if explicitly adopted;
- request size limits.

### 8.5 Safe rendering rule

Interaction must never assume stored comment content is safe HTML.

At public/admin rendering boundary:

```text
Treat comment content as untrusted text unless an approved sanitization
and rich-content policy explicitly permits otherwise.
```

Preferred V1 posture:

- store text content;
- render with output encoding;
- do not accept executable HTML;
- do not embed raw comment HTML into admin or public pages.

### 8.6 Comment creation retry abuse

Repeated create requests after timeout can produce duplicate comments unless protected.

Production-oriented V1 should support:

```text
Idempotency-Key
```

for comment creation.

If adopted:

- same key + same semantic payload returns the original result;
- same key + different payload returns conflict;
- idempotency records must be scoped and retained according to policy.

### 8.7 Moderation-before-visibility

Every newly submitted comment begins as:

```text
Pending
```

It must not be exposed publicly until an authorized moderation action changes it to:

```text
Visible
```

This is the primary V1 control against public comment abuse.

---

## 9) Comment Public-Read Protection

### 9.1 Public-visible filter

Public comment APIs may return only comments satisfying:

```text
Status = Visible
AND ParentCommentId = NULL
AND associated article is public under the applicable contract
```

### 9.2 Never expose moderated/non-public state

Public API responses must not expose:

```text
Pending comments
Rejected comments
Hidden comments
Deleted comments
Report details
Reporter identities
Moderation case data
Moderation reasons
Moderator identity
Internal version or audit fields
```

### 9.3 Safe content output

Public comment output must be encoded/sanitized according to the approved content rendering policy.

Comment content must not create:

- stored XSS;
- script execution;
- unsafe link injection;
- HTML layout injection.

---

## 10) Author Delete-Own-Comment Protection

### 10.1 BOLA / IDOR protection

The delete-own-comment endpoint must validate ownership from authoritative Interaction data:

```text
Comment.AuthorUserId = CurrentAuthenticatedUserId
```

### 10.2 Resource concealment

For a user attempting to delete another user's comment, prefer:

```http
404 Not Found
```

rather than revealing that the comment exists but belongs to someone else.

### 10.3 Allowed author-delete behavior

Author deletion may transition:

```text
Pending  -> Deleted
Visible  -> Deleted
Rejected -> Deleted
Hidden   -> Deleted
```

Retry when already deleted must be handled idempotently.

### 10.4 Open-case protection

If author deletion races with an open moderation case:

- case/report closure must be committed atomically where the author deletion wins;
- stale moderator attempts must conflict deterministically;
- case/report/history data must remain preserved for investigation.

### 10.5 No physical destructive delete in normal flow

Author deletion must not physically remove all moderation/report evidence required for:

- operational history;
- investigation;
- Audit correlation;
- case resolution traceability;
- counter reconciliation.

---

## 11) Report Comment Abuse Controls

### 11.1 Report threat model

Report functionality may be abused through:

- self-reporting;
- repeated report spam;
- coordinated false reporting;
- reporting non-public comments;
- report payload abuse;
- attempting to trigger admin email repeatedly;
- using report reasons as a way to expose sensitive material in admin notifications.

### 11.2 Required eligibility checks

A report may be accepted only when:

```text
Reporter is authenticated
Comment.Status = Visible
Article is interaction-enabled
ReporterUserId != Comment.AuthorUserId
No existing report for (CommentId, ReporterUserId)
ReasonCode is valid
Description satisfies validation rules
Rate limit permits request
```

### 11.3 Unique report invariant

```text
At most one CommentReport per (CommentId, ReporterUserId) in V1.
```

This protects against one account artificially inflating report count through retry or repeated submissions.

### 11.4 No automatic censorship

```text
A report is an allegation requiring moderator review.
It does not automatically hide a comment.
```

Even when alert threshold is reached:

- comment remains visible until a moderator hides it or author deletes it;
- severity affects prioritization, not automatic verdict.

### 11.5 Report reason validation

Allowlisted reason codes:

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

Rules:

| Field | Control |
|---|---|
| `ReasonCode` | Required and allowlisted |
| `Description` | Bounded length |
| `Description` when `Other` | Required |
| Content in `Description` | Treat as untrusted text |
| Description in email/logs | Avoid or sanitize/minimize |

### 11.6 Coordinated-report abuse posture

V1 does not automatically detect coordinated brigading or reporter reputation.

Therefore, V1 deliberately avoids:

```text
Auto-hide comment after report threshold
Auto-ban author based on reports
Treat report count as confirmed violation
```

Such automation requires future evidence-based abuse policy.

---

## 12) Moderation Case and Admin Alert Protection

### 12.1 Single-open-case invariant

```text
At most one Open CommentModerationCase per Comment.
```

This prevents:

- duplicate admin queue items for one unresolved report cycle;
- duplicated alert workflows;
- inconsistent moderator decisions.

### 12.2 Alert amplification threat

Concurrent or malicious reports may attempt to trigger multiple emails for the same comment/case.

### 12.3 One-alert invariant

```text
One Open CommentModerationCase may trigger at most one
administrator-alert business intent in V1.
```

Interaction must persist durable trigger metadata:

```text
AlertTriggeredAtUtc
AlertLevel
AlertMessageId or equivalent correlation metadata
```

### 12.4 Atomic trigger rule

When report escalation policy is reached:

```text
Create valid report
+ atomically verify open case has no prior alert
+ persist alert-trigger metadata
+ write interaction.comment_report_alert_triggered to Outbox
+ commit
```

Concurrent reports must not result in duplicate alert intents.

### 12.5 Notification email content minimization

Email alert payloads and templates should minimize sensitive content.

Prefer including:

```text
Case public id
Comment public id
Article public id
Priority / alert level
Report count
Admin dashboard link/reference where supported
```

Avoid placing full raw comment body or unbounded report descriptions directly into email unless explicitly required and safely handled.

### 12.6 Notifications boundary

Interaction owns:

```text
Whether alert intent should be created.
```

Notifications owns:

```text
Recipients
Email template
Delivery persistence
Provider calls
Retry
Provider ambiguity
Business-intent send dedupe
```

Suggested Notifications dedupe key:

```text
InteractionCommentReportAlert:{CommentModerationCasePublicId}
```

---

## 13) Admin Moderation Authorization and Safety

### 13.1 Permissions

Recommended V1 administrative permissions:

```text
Interaction.Comments.Read
Interaction.Comments.Moderate
Interaction.CommentReports.Read
Interaction.CommentReports.Resolve
Interaction.Counters.Read
Interaction.Analytics.Read
```

### 13.2 Suggested role posture

| Role | V1 moderation capability |
|---|---|
| `Admin` | Full Interaction admin capability |
| `Moderator` | Comment moderation and report-case handling |
| `Author` | No implicit comment moderation right |
| `User` | User-facing interaction only |

### 13.3 No implicit article-author moderation

The author of an article must not automatically receive rights to:

- approve comments;
- reject comments;
- hide comments;
- restore comments;
- resolve reports.

That requires a future explicit authorization design.

### 13.4 Permission does not bypass state legality

Even when actor has valid permission, Interaction must validate:

- current `Comment.Status`;
- current `Comment.Version`;
- current `CommentModerationCase.Status`;
- current `CommentModerationCase.Version`;
- required reason/note;
- whether a report case requires atomic resolution.

### 13.5 Direct hide versus reported-case hide

If a visible comment has an open moderation case, admin must resolve through the case flow rather than hiding independently.

```text
Open moderation case exists
    -> use case hide-comment resolution flow
    -> update Comment + Case + Reports + History atomically
```

This prevents orphaned open cases and unresolved reports after the comment is hidden.

### 13.6 Stale admin UI protection

Moderator actions must be version-protected.

A stale moderator view must not:

- overwrite a newer decision;
- hide an already deleted comment;
- dismiss a case another moderator already actioned;
- restore a comment from an incompatible state.

Conflicting actions must return deterministic conflict behavior.

---

## 14) Moderation History and Audit Protection

### 14.1 Local operational history

`CommentModerationActionHistory` is Interaction-owned local history used by admin workflows.

Required moderation operations write history in the same transaction as the state mutation.

### 14.2 Audit is asynchronous

Audit receives moderation facts asynchronously after Interaction commits.

```text
Audit lag does not invalidate Interaction moderation success.
```

### 14.3 History read protection

Moderation history endpoints are admin-only and must not expose data publicly.

History may contain:

- moderator identity;
- resolution notes;
- reason codes;
- case linkage;
- correlation identifiers.

All such fields require authorization and safe logging treatment.

### 14.4 Notes are untrusted operational text

Moderator notes and report descriptions must still be treated as untrusted input for display and logging.

Even privileged admin-authored text should be safely rendered.

---

## 15) Counter and Derived-State Security

### 15.1 Counters are not authorization truth

Interaction-derived counters must never determine:

- whether an article is public;
- whether a user has an active like;
- whether a comment is visible;
- whether moderation is required;
- whether a report exists.

### 15.2 Counter manipulation protection

`ArticleViewCount` requires atomic increments.

`ArticleInteractionStats` must be materialized and published using known-value snapshots and monotonic `StatsVersion`.

Reading must not calculate counters from raw deltas.

### 15.3 Public exposure

Website/public article counters are served through Reading projection, not a public Interaction diagnostics endpoint.

Admin diagnostic stats endpoints must require permission:

```text
Interaction.Counters.Read
```

### 15.4 Counter lag disclosure

Counter lag is acceptable and does not constitute a security failure unless used incorrectly as authority.

The system must not expose detailed operational internals unnecessarily to public clients, such as:

- consumer backlog;
- resync markers;
- moderation queue status;
- suppressed view decisions;
- outbox delivery metadata.

---

## 16) Async Events and Payload Protection

### 16.1 Outbox boundary

Any async event required by an Interaction mutation must be written in the same local transaction as the relevant state change.

### 16.2 Events must not leak unnecessary sensitive data

Outbound events should include only fields required by the consumer.

#### Reading counter event

```text
interaction.article_counters_projection_published
```

May include:

```text
ArticlePublicId
ViewCount
LikeCount
VisibleCommentCount
StatsVersion
ProjectedAtUtc
```

Must not include:

```text
Reporter identities
Moderator notes
Comment bodies
User private data
```

#### Notifications alert event

```text
interaction.comment_report_alert_triggered
```

Should include minimal case/priority metadata required for email orchestration.

Avoid embedding:

```text
Full raw report descriptions
Unbounded comment content
Sensitive personal-information excerpts
```

#### Audit-facing moderation events

May require actor, reason and correlation metadata, but payload must remain bounded and follow Audit data-governance rules.

### 16.3 Replay and duplicate delivery

Consumers must handle duplicate delivery without causing:

- duplicate counter application;
- duplicate email send intent;
- duplicate Audit operational meaning;
- repeated visibility transitions.

---

## 17) Rate Limiting and Abuse Policy

### 17.1 Required rate-limit areas

| Capability | Recommended rate-limit scope |
|---|---|
| Track view | IP/session/user/article/window where approved |
| Like/unlike | Authenticated user and article/action window |
| Create comment | Authenticated user and global/article window |
| Delete own comment | Authenticated user window |
| Report comment | Authenticated user/comment and global window |
| Admin moderation actions | Actor/admin operational limit to reduce accidental automation abuse |
| Admin case queries | Authenticated permissioned actor/query limit |

### 17.2 Rate-limit response

Use:

```http
429 Too Many Requests
```

with stable error code:

```text
Interaction.RateLimitExceeded
```

### 17.3 Future escalation controls

The following may be added later if real abuse evidence justifies them:

- CAPTCHA or challenge flow for suspected automated behavior;
- temporary account-level commenting/reporting restrictions;
- reporter reputation signals;
- automated anomaly detection;
- moderation workload prioritization.

They are not required as V1 business truth.

---

## 18) Privacy and Data Minimization

### 18.1 Personal data minimization

Interaction must collect only data required for:

- engagement behavior;
- abuse prevention;
- moderation operation;
- audit correlation;
- operational diagnostics.

### 18.2 View-related privacy

Since V1 uses `ArticleViewCount` instead of raw per-view history, long-term retention of individual browsing traces is not required for the core feature.

If short-lived anti-abuse signals are used:

- prefer hash/derived values over raw data where feasible;
- bound retention;
- document the purpose;
- do not expose these values in API responses;
- do not propagate them to Reading.

### 18.3 Comment/report data

Comment bodies and report descriptions may contain personal or sensitive content.

Controls must include:

- authorization on admin access;
- bounded field length;
- safe rendering;
- limited logging;
- retention policy for resolved moderation records;
- controlled inclusion in async payloads/email.

### 18.4 Moderator data

Moderator actor identity, notes and decision history are restricted operational data.

They must appear only in authorized admin/audit contexts.

---

## 19) Safe Logging and Observability

### 19.1 Log allowed identifiers

Logs may include where necessary:

```text
CorrelationId
MessageId
ConsumerName
ArticlePublicId
CommentPublicId
CommentModerationCasePublicId
ActorUserId where authorized/appropriate
ActionType
ReasonCode
CurrentStatus
ExpectedVersion
CurrentVersion
ApplyDecision
IdempotencyOutcome
```

### 19.2 Do not log by default

Avoid logging:

```text
Raw comment content
Raw report description
Raw moderator notes
Raw IP address
Raw user-agent string
Authentication tokens
Refresh tokens
Email content
Sensitive personal-information excerpts
Full outbox payloads containing user-generated text
```

### 19.3 Metrics required for abuse/security posture

Recommended metrics:

```text
interaction_view_accepted_total
interaction_view_rejected_total
interaction_view_rate_limited_total

interaction_like_rate_limited_total
interaction_like_unique_conflicts_total

interaction_comment_created_total
interaction_comment_rejected_by_validation_total
interaction_comment_rate_limited_total
interaction_comment_version_conflicts_total

interaction_report_created_total
interaction_report_duplicate_rejected_total
interaction_report_rate_limited_total
interaction_report_self_report_rejected_total

interaction_moderation_case_open_total
interaction_moderation_case_resolution_conflicts_total

interaction_report_alert_triggered_total
interaction_report_alert_suppressed_total

interaction_admin_forbidden_total
interaction_consumer_duplicate_ignored_total
interaction_consumer_stale_ignored_total
interaction_eligibility_resync_required_total
```

### 19.4 Alerting signals

Operational/security alerts may be appropriate for:

- sudden view-count spikes by article;
- comment spam bursts;
- report submission bursts;
- repeated report-alert suppression due to concurrency;
- repeated admin permission failures;
- repeated version conflicts in moderation;
- eligibility projection resync spikes;
- unusual consumer replay or dedupe-hit rates.

---

## 20) Security Behavior by Article Lifecycle

### 20.1 Article unpublished, archived or soft-deleted

When Interaction receives confirmed non-public state from Content:

| Operation | Allowed |
|---|---:|
| New view contribution | No |
| New like | No |
| New comment | No |
| New report | No |
| Unlike own existing like | Yes |
| Delete own existing comment | Yes |
| Admin resolve existing open case | Yes, when authorized |
| Preserve counters and history | Yes |

### 20.2 No counter reset

Security and data-integrity rule:

```text
Unpublish, archive and soft-delete must not reset
ViewCount, LikeCount or VisibleCommentCount.
```

Resetting counters would destroy historical Interaction data and create misleading admin/republish behavior.

---

## 21) Explicitly Deferred Security Concerns

The following concerns are deferred because the associated capabilities are outside V1:

| Deferred capability | Security work deferred with it |
|---|---|
| Reply comments | Parent/reply visibility, moderation and report abuse rules |
| Comment editing | Re-moderation, revision history and edited-content abuse handling |
| Auto-hide after reports | Coordinated-report/false-positive automated moderation safeguards |
| Reporter reputation | Reputation manipulation and appeal rules |
| Raw per-view analytics | Detailed browsing-data retention/privacy policy |
| Trending/ranking | Manipulation resistance for engagement-derived ranking |
| Bulk moderation | Mass-action authorization and rollback/partial-failure handling |
| Article-author moderation | Scoped ownership-based moderation authorization |

`Comment.ParentCommentId` may exist for future compatibility, but does not enable reply-related attack surface in V1 APIs.

---

## 22) Security Summary

Interaction V1 follows these security and abuse-control rules:

1. Public article reading must work independently of Interaction view tracking.
2. New interactions require safe local article eligibility; uncertain eligibility fails closed.
3. User identity, permissions, ownership and status authority come from trusted server-side state.
4. Views use accepted `ArticleViewCount` contributions with rate-limit and repeat-view protection.
5. Likes are authenticated and protected by one-active-like-per-user/article invariant.
6. Comments are authenticated, top-level-only, moderated before public visibility and safely rendered.
7. Comment authors may delete their own comments, with BOLA protection and idempotent behavior.
8. Reports require authentication, visible non-owned comments, uniqueness and rate limiting.
9. Reports and alert thresholds never automatically hide content.
10. A comment has at most one open moderation case; one open case creates at most one admin-alert intent.
11. Admin moderation requires explicit permissions plus legal state/version transitions.
12. Moderation history is admin-restricted local operational data; Audit is asynchronous canonical evidence.
13. Counter snapshots are derived and must never act as authorization or visibility truth.
14. Async payloads, logs and emails must minimize exposure of user-generated and sensitive data.
15. Unpublish disables new interaction but preserves counters, history and existing workflow data.
