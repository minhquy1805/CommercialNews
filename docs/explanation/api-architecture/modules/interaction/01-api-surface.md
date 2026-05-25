# Interaction — API Surface (V1)

This document defines the public, authenticated, and administrator API surface for Interaction V1.
It focuses on request/response contracts, status semantics, moderation APIs, and async response boundaries.

Related:

- `02-domain-contracts.md`
- `03-runtime-flows.md`
- `04-errors-status-codes.md`
- `05-security-abuse-controls.md`
- `06-idempotency-consistency.md`
- `07-observability-slos.md`
- `08-dependencies-and-ownership.md`
- `09-open-questions.md`
- `10-business-rules.md`

---

## Base Paths

### Public and authenticated user-facing endpoints

```text
/api/v1
```

### Administrator/moderator endpoints

```text
/api/v1/admin/interaction
```

## API Posture

Interaction endpoints must be:

- fast;
- retry-safe where commands may be repeated;
- explicit about authoritative state versus derived counters;
- non-blocking relative to downstream Reading, Audit and Notifications processing.

For commands with asynchronous consequences:

```text
API success means Interaction-owned state and required Outbox intent committed.

API success does not mean:
- Reading already applied new counters;
- Audit already stored canonical evidence;
- Notifications already sent administrator email.
```

## Identifier Convention

All API routes use public identifiers:

```text
articlePublicId
commentPublicId
commentReportPublicId
commentModerationCasePublicId
```

Internal persistence identifiers must not be exposed in public/admin API contracts.

---

## 1) Article Views

### POST `/api/v1/articles/{articlePublicId}/views`

Submit a page-view contribution for an article.

This endpoint is called independently after or alongside successful public article rendering. Article rendering must not depend on this request succeeding.

### Authentication

Optional.

- Anonymous readers may submit view contributions.
- Authenticated readers may submit view contributions using trusted server-side actor context.
- Client-provided user identity must not be accepted as authority.

### Headers

| Header | Required | Purpose |
|---|---:|---|
| `X-Correlation-Id` | No | Request tracing |
| `Idempotency-Key` | No | Not a strict per-view idempotency contract in V1 |

### Request

No request body is required in V1.

The client must not provide authoritative values such as:

```text
UserId
ViewCount
OccurredAtUtc used for ordering
Article visibility state
```

### Response

#### `202 Accepted`

```json
{
  "accepted": true
}
```

`accepted = true` means the request was accepted by the endpoint contract. It does not promise immediate counter visibility in Reading.

### Rules

- A view contributes to `ArticleViewCount` only when the article is locally confirmed as interaction-enabled.
- The request is subject to anti-abuse and repeat-view suppression policy.
- Interaction may avoid revealing detailed suppression decisions to clients.
- Accepted count mutation must use atomic increment semantics.
- Interaction does not store one permanent raw row for each page view in V1.
- Interaction does not publish one Reading event for each view.
- Reading eventually receives public counter snapshots through:

```text
interaction.article_counters_projection_published
```

### Relevant outcomes

| Situation | Response posture |
|---|---|
| Valid request for eligible article | `202 Accepted` |
| Article is not publicly interaction-enabled | `404 Not Found` or standard unavailable-resource response according to API policy |
| Malformed request | `400 Bad Request` |
| Rate limited | `429 Too Many Requests` |
| Unexpected server failure | `500 Internal Server Error` |

---

## 2) Article Likes

### POST `/api/v1/articles/{articlePublicId}/likes`

Like an article.

### Authentication

Required.

### Headers

| Header | Required | Purpose |
|---|---:|---|
| `X-Correlation-Id` | No | Request tracing |

A separate idempotency key is not required for normal like semantics because the resource relationship itself is idempotent.

### Response

#### `200 OK`

```json
{
  "articlePublicId": "01J...",
  "liked": true
}
```

### Rules

- New likes require an interaction-enabled article.
- The current authenticated actor comes from trusted request context.
- A user may have at most one active like for the same article.
- Repeated like requests converge to `liked = true`.
- Two different users may like the same article concurrently without conflicting merely because the article is the same.
- `LikeCount` is derived and may update asynchronously after this response.

### Authoritative invariant

```text
At most one active ArticleLike per (ArticlePublicId, UserId).
```

### Relevant outcomes

| Situation | Status |
|---|---:|
| Like created or already active under idempotent semantics | `200 OK` |
| Unauthenticated | `401 Unauthorized` |
| Article not interaction-enabled | `404 Not Found` or standard unavailable-resource response |
| Rate limited | `429 Too Many Requests` |
| Unexpected failure | `500 Internal Server Error` |

---

### DELETE `/api/v1/articles/{articlePublicId}/likes`

Remove the authenticated user's active like from an article.

### Authentication

Required.

### Response

#### `200 OK`

```json
{
  "articlePublicId": "01J...",
  "liked": false
}
```

### Rules

- Unlike affects only the current user's active like.
- Repeated unlike requests converge to `liked = false`.
- Unlike remains allowed after the article becomes unpublished, archived or soft-deleted because the user is retracting their own relationship.
- `LikeCount` may update asynchronously after the unlike truth mutation commits.
- Like count must never become negative.

---

### GET `/api/v1/articles/{articlePublicId}/my-like`

Return the authenticated user's authoritative like state for an article.

### Authentication

Required.

### Response

#### `200 OK`

```json
{
  "articlePublicId": "01J...",
  "liked": true
}
```

### Rules

- This endpoint reads `ArticleLike` truth, not derived public counters.
- It is the recommended reconciliation endpoint after ambiguous like/unlike timeout.

---

## 3) Public Comments

### GET `/api/v1/articles/{articlePublicId}/comments`

List publicly visible top-level comments for an article.

### Authentication

Not required.

### Query Parameters

| Parameter | Required | Default | Rules |
|---|---:|---:|---|
| `page` | No | `1` | Must be positive |
| `pageSize` | No | Configured default | Must remain within configured maximum |
| `sort` | No | `-createdAtUtc` | Allowlisted values only |

Recommended sort allowlist:

```text
-createdAtUtc
createdAtUtc
```

### Response

#### `200 OK`

```json
{
  "items": [
    {
      "commentPublicId": "01J...",
      "articlePublicId": "01J...",
      "authorDisplayName": "Reader",
      "content": "Comment content",
      "createdAtUtc": "2026-05-25T10:30:00Z"
    }
  ],
  "pageInfo": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

### Rules

Public comments query returns only comments satisfying:

```text
Comment.Status = Visible
AND Comment.ParentCommentId = NULL
AND article is eligible for public exposure according to Interaction/public contract
```

Public response must never expose:

```text
Pending comments
Rejected comments
Hidden comments
Deleted comments
Reporter information
CommentModerationCase metadata
Moderation reasons or notes
Moderator identity
Internal version fields
```

### V1 reply limitation

```text
V1 returns top-level comments only.
Reply creation and nested-thread rendering are not supported.
```

---

## 4) Authenticated Comment Commands

### POST `/api/v1/articles/{articlePublicId}/comments`

Submit a new top-level comment for moderation.

### Authentication

Required.

### Headers

| Header | Required | Purpose |
|---|---:|---|
| `Idempotency-Key` | Recommended | Prevent duplicate comment creation after retry ambiguity |
| `X-Correlation-Id` | No | Request tracing |

### Request

```json
{
  "content": "This is my comment."
}
```

### V1 Request Constraint

The request must not accept:

```text
parentCommentPublicId
parentCommentId
```

`ParentCommentId` is reserved in the domain model for future reply support only.

### Response

#### `201 Created`

```json
{
  "commentPublicId": "01J...",
  "articlePublicId": "01J...",
  "status": "Pending",
  "createdAtUtc": "2026-05-25T10:30:00Z",
  "version": 1
}
```

### Rules

- Article must be locally confirmed as interaction-enabled.
- Comment content must pass validation, length and safety rules.
- Every V1-created comment is created with:

```text
Status = Pending
ParentCommentId = NULL
```

- A new comment is not immediately public.
- A new pending comment does not increase `VisibleCommentCount`.
- If `Idempotency-Key` is supported:
  - retry with the same key and same semantic payload returns the same logical created result;
  - reuse with conflicting payload returns deterministic conflict.

### Relevant outcomes

| Situation | Status |
|---|---:|
| Comment created | `201 Created` |
| Same idempotency key and same request already committed | `200 OK` or `201 Created` with original result, according to API convention |
| Conflicting reuse of idempotency key | `409 Conflict` |
| Unauthenticated | `401 Unauthorized` |
| Article not interaction-enabled | `404 Not Found` or standard unavailable-resource response |
| Invalid content | `400 Bad Request` |
| Rate limited | `429 Too Many Requests` |

---

### DELETE `/api/v1/comments/{commentPublicId}`

Delete a comment owned by the authenticated user.

### Authentication

Required.

### Headers

| Header | Required | Purpose |
|---|---:|---|
| `X-Correlation-Id` | No | Request tracing |

### Response

#### `204 No Content`

No response body.

### Rules

- The authenticated user must be the authoritative author of the comment.
- Admin approval is not required for author deletion.
- Supported deletion transitions:

| Current status | Next status |
|---|---|
| `Pending` | `Deleted` |
| `Visible` | `Deleted` |
| `Rejected` | `Deleted` |
| `Hidden` | `Deleted` |
| `Deleted` | Idempotent no-op |

- If deleting a `Visible` comment, `VisibleCommentCount` eventually decreases.
- If the comment belongs to an open moderation case:
  - comment becomes `Deleted`;
  - case becomes `ClosedByAuthorDeletion`;
  - pending reports become `ClosedByAuthorDeletion`;
  - required moderation history is written atomically.

### Relevant outcomes

| Situation | Status |
|---|---:|
| Deleted successfully | `204 No Content` |
| Already deleted by author, retry/no-op | `204 No Content` |
| Unauthenticated | `401 Unauthorized` |
| Not comment owner | `404 Not Found` preferred to avoid exposing ownership/resource information |
| Comment not found | `404 Not Found` |
| Concurrent stale conflict requiring client refresh | `409 Conflict` |

---

## 5) Comment Reports

### POST `/api/v1/comments/{commentPublicId}/reports`

Report a publicly visible comment for moderator review.

### Authentication

Required.

### Headers

| Header | Required | Purpose |
|---|---:|---|
| `X-Correlation-Id` | No | Request tracing |

### Request

```json
{
  "reasonCode": "Spam",
  "description": "Optional bounded details."
}
```

### Allowed Reason Codes

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

### Request Rules

| Field | Rule |
|---|---|
| `reasonCode` | Required and allowlisted |
| `description` | Optional for standard reasons |
| `description` with `Other` | Required |
| `description` length | Bounded by validation policy |

### Response

#### `201 Created`

```json
{
  "commentReportPublicId": "01J...",
  "commentPublicId": "01J...",
  "status": "Pending",
  "createdAtUtc": "2026-05-25T10:35:00Z"
}
```

### Rules

A report is allowed only when:

- comment currently has `Status = Visible`;
- related article is interaction-enabled;
- reporter is not the comment author;
- reporter has not already reported that comment;
- request passes rate-limit and validation policy.

A report:

- creates or joins a moderation case;
- does not hide the comment automatically;
- does not change public comment counters;
- may asynchronously trigger one admin alert intent if the open case reaches configured escalation policy.

### Authoritative invariant

```text
At most one CommentReport per (CommentId, ReporterUserId).
```

### Relevant outcomes

| Situation | Status |
|---|---:|
| Report created | `201 Created` |
| User already reported comment | `409 Conflict` |
| User reports own comment | `403 Forbidden` or standard domain-forbidden response |
| Comment not visible / article not eligible / not found | `404 Not Found` preferred for public-facing safety |
| Invalid reason/description | `400 Bad Request` |
| Unauthenticated | `401 Unauthorized` |
| Rate limited | `429 Too Many Requests` |

---

## 6) Public Counter Read Surface

### No dedicated public counter endpoint in Interaction V1

Interaction does not expose a required public endpoint such as:

```text
GET /api/v1/articles/{articlePublicId}/counters
```

for website article composition in V1.

Instead:

```text
Interaction
    -> interaction.article_counters_projection_published
    -> Reading
    -> public article response contains projected counters
```

### Rules

- Reading owns public article response composition.
- Interaction owns counter derivation and publication.
- Public counters are eventually consistent.
- Clients must not use displayed counters to determine whether a like/comment command committed.

### Optional administrative diagnostics

An authorized admin/operational endpoint may be introduced for counter inspection, as defined later in the admin surface below.

---

## 7) Admin Comment Moderation API

### Authorization

Admin/moderator comment APIs require appropriate permissions:

```text
Interaction.Comments.Read
Interaction.Comments.Moderate
```

---

### GET `/api/v1/admin/interaction/comments`

List comments for administrative moderation.

### Query Parameters

| Parameter | Required | Example | Purpose |
|---|---:|---|---|
| `status` | No | `Pending` | Filter by comment status |
| `articlePublicId` | No | `01J...` | Filter by article |
| `authorUserId` | No | `01J...` | Filter by author where authorized |
| `page` | No | `1` | Paging |
| `pageSize` | No | `20` | Paging |
| `sort` | No | `-createdAtUtc` | Allowlisted sort |

### Response

#### `200 OK`

```json
{
  "items": [
    {
      "commentPublicId": "01J...",
      "articlePublicId": "01J...",
      "authorUserId": "01U...",
      "content": "Submitted comment",
      "status": "Pending",
      "parentCommentPublicId": null,
      "createdAtUtc": "2026-05-25T10:30:00Z",
      "version": 1
    }
  ],
  "pageInfo": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

### Rules

- Authorized admin query may include non-public comment states.
- V1 comments remain top-level only; `parentCommentPublicId` remains `null`.

---

### GET `/api/v1/admin/interaction/comments/{commentPublicId}`

Return administrative detail for one comment.

### Authorization

Requires:

```text
Interaction.Comments.Read
```

### Response

#### `200 OK`

```json
{
  "commentPublicId": "01J...",
  "articlePublicId": "01J...",
  "authorUserId": "01U...",
  "content": "Submitted comment",
  "status": "Visible",
  "parentCommentPublicId": null,
  "createdAtUtc": "2026-05-25T10:30:00Z",
  "updatedAtUtc": "2026-05-25T10:40:00Z",
  "deletedAtUtc": null,
  "version": 2
}
```

---

### POST `/api/v1/admin/interaction/comments/{commentPublicId}/approve`

Approve a pending comment for public visibility.

### Authorization

Requires:

```text
Interaction.Comments.Moderate
```

### Request

```json
{
  "expectedVersion": 1,
  "note": "Optional note."
}
```

### Response

#### `200 OK`

```json
{
  "commentPublicId": "01J...",
  "status": "Visible",
  "version": 2
}
```

### Rules

```text
Valid transition: Pending -> Visible
```

Success commits:

```text
Comment transition
+ CommentModerationActionHistory: Approve
+ interaction.comment_approved outbox intent
```

`VisibleCommentCount` changes asynchronously.

---

### POST `/api/v1/admin/interaction/comments/{commentPublicId}/reject`

Reject a pending comment before it becomes public.

### Authorization

Requires:

```text
Interaction.Comments.Moderate
```

### Request

```json
{
  "expectedVersion": 1,
  "reasonCode": "Spam",
  "note": "Promotional content."
}
```

### Response

#### `200 OK`

```json
{
  "commentPublicId": "01J...",
  "status": "Rejected",
  "version": 2
}
```

### Rules

```text
Valid transition: Pending -> Rejected
```

- `reasonCode` is required.
- When `reasonCode = Other`, `note` is required.
- No visible-comment counter change occurs.

---

### POST `/api/v1/admin/interaction/comments/{commentPublicId}/hide`

Hide a visible comment outside an open report-case resolution flow.

### Authorization

Requires:

```text
Interaction.Comments.Moderate
```

### Request

```json
{
  "expectedVersion": 2,
  "reasonCode": "PolicyViolation",
  "note": "Comment violates moderation policy."
}
```

### Response

#### `200 OK`

```json
{
  "commentPublicId": "01J...",
  "status": "Hidden",
  "version": 3
}
```

### Rules

```text
Valid transition: Visible -> Hidden
```

If an open moderation case exists for this comment, admin should use the reported-comment resolution endpoint instead:

```text
POST /api/v1/admin/interaction/comment-moderation-cases/{casePublicId}/hide-comment
```

This ensures case and associated reports are resolved atomically.

---

### POST `/api/v1/admin/interaction/comments/{commentPublicId}/restore`

Restore a hidden comment to visible state.

### Authorization

Requires:

```text
Interaction.Comments.Moderate
```

### Request

```json
{
  "expectedVersion": 3,
  "note": "Restored after moderator review."
}
```

### Response

#### `200 OK`

```json
{
  "commentPublicId": "01J...",
  "status": "Visible",
  "version": 4
}
```

### Rules

```text
Valid transition: Hidden -> Visible
```

If the related article is currently non-public, the comment state may become `Visible` in Interaction but must not appear publicly until the article is public again.

---

## 8) Admin Comment Report / Moderation Case API

### Authorization

Reading and resolving reported-comment cases uses:

```text
Interaction.CommentReports.Read
Interaction.CommentReports.Resolve
```

Resolving a case by hiding its comment may additionally require:

```text
Interaction.Comments.Moderate
```

---

### GET `/api/v1/admin/interaction/comment-moderation-cases`

List moderation cases.

### Query Parameters

| Parameter | Required | Example | Purpose |
|---|---:|---|---|
| `status` | No | `Open` | Filter case lifecycle |
| `priority` | No | `High` | Filter urgency |
| `articlePublicId` | No | `01J...` | Filter article |
| `commentPublicId` | No | `01J...` | Filter comment |
| `alertTriggered` | No | `true` | Filter alert state |
| `page` | No | `1` | Paging |
| `pageSize` | No | `20` | Paging |
| `sort` | No | `-openedAtUtc` | Allowlisted sort |

### Response

#### `200 OK`

```json
{
  "items": [
    {
      "commentModerationCasePublicId": "01C...",
      "commentPublicId": "01J...",
      "articlePublicId": "01A...",
      "status": "Open",
      "priority": "High",
      "highestSeverity": "Normal",
      "pendingReportCount": 3,
      "distinctReporterCount": 3,
      "alertTriggeredAtUtc": "2026-05-25T10:35:00Z",
      "alertLevel": "High",
      "openedAtUtc": "2026-05-25T10:30:00Z",
      "version": 3
    }
  ],
  "pageInfo": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

### Rules

- This is an authorized admin-only view.
- Report counts and severity are moderation data and must not appear in public APIs.
- Derived case-summary fields, where materialized, remain subordinate to report/case truth.

---

### GET `/api/v1/admin/interaction/comment-moderation-cases/{casePublicId}`

Return case detail, associated comment and submitted reports.

### Authorization

Requires:

```text
Interaction.CommentReports.Read
```

### Response

#### `200 OK`

```json
{
  "commentModerationCasePublicId": "01C...",
  "status": "Open",
  "priority": "High",
  "highestSeverity": "Normal",
  "alertTriggeredAtUtc": "2026-05-25T10:35:00Z",
  "alertLevel": "High",
  "openedAtUtc": "2026-05-25T10:30:00Z",
  "resolvedAtUtc": null,
  "resolutionType": null,
  "resolutionReasonCode": null,
  "resolutionNote": null,
  "version": 3,
  "comment": {
    "commentPublicId": "01J...",
    "articlePublicId": "01A...",
    "authorUserId": "01U...",
    "content": "Reported comment content.",
    "status": "Visible",
    "version": 2
  },
  "reports": [
    {
      "commentReportPublicId": "01R...",
      "reporterUserId": "01U...",
      "reasonCode": "Spam",
      "description": "Promotional comment.",
      "status": "Pending",
      "createdAtUtc": "2026-05-25T10:30:00Z"
    }
  ]
}
```

---

### POST `/api/v1/admin/interaction/comment-moderation-cases/{casePublicId}/dismiss`

Dismiss reports and keep the comment visible.

### Authorization

Requires:

```text
Interaction.CommentReports.Resolve
```

### Request

```json
{
  "expectedCaseVersion": 3,
  "reasonCode": "PolicyViolation",
  "note": "Reports reviewed; comment does not require removal."
}
```

### Response

#### `200 OK`

```json
{
  "commentModerationCasePublicId": "01C...",
  "caseStatus": "Dismissed",
  "commentStatus": "Visible",
  "version": 4
}
```

### Rules

Valid state change:

```text
Case: Open -> Dismissed
Pending Reports -> Dismissed
Comment remains Visible
```

Success commits:

```text
Case/report transitions
+ CommentModerationActionHistory: DismissReportedCase
+ interaction.comment_reports_dismissed outbox intent
```

No public counter change occurs.

---

### POST `/api/v1/admin/interaction/comment-moderation-cases/{casePublicId}/hide-comment`

Resolve an open case by hiding the reported visible comment.

### Authorization

Requires:

```text
Interaction.CommentReports.Resolve
Interaction.Comments.Moderate
```

### Request

```json
{
  "expectedCaseVersion": 3,
  "expectedCommentVersion": 2,
  "reasonCode": "Harassment",
  "note": "Confirmed violation after review."
}
```

### Response

#### `200 OK`

```json
{
  "commentModerationCasePublicId": "01C...",
  "caseStatus": "Actioned",
  "commentPublicId": "01J...",
  "commentStatus": "Hidden",
  "caseVersion": 4,
  "commentVersion": 3
}
```

### Rules

Valid atomic state change:

```text
Comment: Visible -> Hidden
Case: Open -> Actioned
Pending Reports -> Actioned
```

Success commits:

```text
Comment/case/report transitions
+ CommentModerationActionHistory: HideReportedComment
+ interaction.comment_hidden outbox intent with case-resolution metadata
```

`VisibleCommentCount` decreases asynchronously.

---

## 9) Admin Moderation History API

### GET `/api/v1/admin/interaction/comments/{commentPublicId}/moderation-history`

Return Interaction-owned moderation operational history for one comment.

### Authorization

Requires:

```text
Interaction.Comments.Read
```

or a more specific future history-read permission.

### Query Parameters

| Parameter | Required | Default |
|---|---:|---:|
| `page` | No | `1` |
| `pageSize` | No | Configured default |
| `sort` | No | `-occurredAtUtc` |

### Response

#### `200 OK`

```json
{
  "items": [
    {
      "historyPublicId": "01H...",
      "commentPublicId": "01J...",
      "commentModerationCasePublicId": "01C...",
      "actionType": "HideReportedComment",
      "fromStatus": "Visible",
      "toStatus": "Hidden",
      "actorUserId": "01U...",
      "actorType": "Moderator",
      "reasonCode": "Harassment",
      "note": "Confirmed violation after review.",
      "occurredAtUtc": "2026-05-25T11:00:00Z",
      "correlationId": "01X..."
    }
  ],
  "pageInfo": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

### Rules

- This reads Interaction local operational history.
- It is not an Audit query endpoint.
- Audit may contain canonical cross-system evidence asynchronously.

---

## 10) Admin Counter Inspection API

### GET `/api/v1/admin/interaction/articles/{articlePublicId}/stats`

Inspect Interaction-owned derived counter state for administration or diagnostics.

### Authorization

Requires:

```text
Interaction.Counters.Read
```

### Response

#### `200 OK`

```json
{
  "articlePublicId": "01A...",
  "viewCount": 1250,
  "likeCount": 48,
  "visibleCommentCount": 12,
  "viewVersion": 1250,
  "statsVersion": 29,
  "lastMaterializedAtUtc": "2026-05-25T10:35:00Z",
  "lastPublishedAtUtc": "2026-05-25T10:35:02Z"
}
```

### Rules

- This endpoint exposes derived state for authorized operations only.
- It must not be treated as authority for one user's like truth or one comment's status.
- Counter values may lag committed truth.
- Public website counters should be served through Reading projection, not this endpoint.

---

## 11) Endpoints Not Supported in V1

### Comment editing

Not supported:

```text
PUT /api/v1/comments/{commentPublicId}
PATCH /api/v1/comments/{commentPublicId}
```

Reason:

- requires re-moderation policy;
- requires edit history/revision rules;
- increases report and visibility complexity.

### Reply comments

Not supported:

```text
POST /api/v1/comments/{commentPublicId}/replies
POST /api/v1/articles/{articlePublicId}/comments with parentCommentPublicId
```

`Comment.ParentCommentId` may be reserved in persistence, but V1 API behavior remains top-level only.

### Auto-hide through report endpoint

Not supported.

Submitting a report never directly changes comment visibility.

### Public counter query directly from Interaction

Not required for V1 public website serving. Reading owns public composition using projected counter snapshots.

---

## 12) Status and Error Summary

| Situation | Status posture |
|---|---:|
| Valid create command | `201 Created` |
| Valid idempotent relationship state command | `200 OK` |
| Valid idempotent delete-own-comment | `204 No Content` |
| View submission accepted | `202 Accepted` |
| Valid query | `200 OK` |
| Request validation failure | `400 Bad Request` |
| Authentication missing/invalid | `401 Unauthorized` |
| Authenticated but lacks admin permission | `403 Forbidden` |
| Public/user resource unavailable or deliberately concealed | `404 Not Found` |
| Duplicate report / idempotency conflict / stale state conflict | `409 Conflict` |
| Rate limit exceeded | `429 Too Many Requests` |
| Unexpected error | `500 Internal Server Error` |

### Standard error envelope

Errors follow the host-wide standard envelope. Conceptual example:

```json
{
  "code": "Interaction.Comment.VersionConflict",
  "message": "The comment has changed. Reload the current state and try again.",
  "correlationId": "01X..."
}
```

---

## 13) Endpoint and Async Effect Matrix

| Endpoint / command | Interaction authoritative mutation | Local history | Async event/outbox consequence |
|---|---|---|---|
| `POST /articles/{id}/views` | `ArticleViewCount` atomic increment if accepted | No | Counter snapshot publication later/coalesced |
| `POST /articles/{id}/likes` | Activate/create `ArticleLike` | No | `interaction.article_liked`; counter snapshot later |
| `DELETE /articles/{id}/likes` | Deactivate/remove own `ArticleLike` | No | `interaction.article_unliked`; counter snapshot later |
| `POST /articles/{id}/comments` | Create `Comment(Pending)` | No | `interaction.comment_created` |
| `DELETE /comments/{id}` | `Comment -> Deleted`; optionally close case | Only if closes open case | `interaction.comment_deleted_by_author`; counter snapshot later if previously visible |
| `POST /comments/{id}/reports` | Create report; create/join open case; optional alert state | No | `interaction.comment_reported`; optional `interaction.comment_report_alert_triggered` |
| Admin approve comment | `Pending -> Visible` | `Approve` | `interaction.comment_approved`; counter snapshot later |
| Admin reject comment | `Pending -> Rejected` | `Reject` | `interaction.comment_rejected` |
| Admin hide comment | `Visible -> Hidden` | `Hide` | `interaction.comment_hidden`; counter snapshot later |
| Admin restore comment | `Hidden -> Visible` | `Restore` | `interaction.comment_restored`; counter snapshot later |
| Admin dismiss case | Case/reports dismissed; comment unchanged | `DismissReportedCase` | `interaction.comment_reports_dismissed` |
| Admin hide reported comment | Comment hidden; case/reports actioned | `HideReportedComment` | `interaction.comment_hidden`; counter snapshot later |

---

## 14) Async Consistency Conventions

### Interaction → Reading

```text
interaction.article_counters_projection_published
```

Reading receives:

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

Rules:

- Reading applies known values, not increments.
- Reading accepts only a newer `statsVersion`.
- Public counters may lag.

### Interaction → Notifications

```text
interaction.comment_report_alert_triggered
```

Rules:

- One open moderation case triggers at most one alert intent in V1.
- Notifications owns email delivery and recipient policy.
- Email failure does not roll back report/case truth.

### Interaction → Audit

Moderation facts are sent asynchronously.

Rules:

- Interaction local moderation history is available immediately after commit.
- Audit ingestion may lag.
- Audit lag does not make moderation command fail.

---

## 15) V1 Surface Summary

### Public / authenticated endpoints

```text
POST   /api/v1/articles/{articlePublicId}/views
POST   /api/v1/articles/{articlePublicId}/likes
DELETE /api/v1/articles/{articlePublicId}/likes
GET    /api/v1/articles/{articlePublicId}/my-like

GET    /api/v1/articles/{articlePublicId}/comments
POST   /api/v1/articles/{articlePublicId}/comments
DELETE /api/v1/comments/{commentPublicId}

POST   /api/v1/comments/{commentPublicId}/reports
```

### Administrator endpoints

```text
GET    /api/v1/admin/interaction/comments
GET    /api/v1/admin/interaction/comments/{commentPublicId}
POST   /api/v1/admin/interaction/comments/{commentPublicId}/approve
POST   /api/v1/admin/interaction/comments/{commentPublicId}/reject
POST   /api/v1/admin/interaction/comments/{commentPublicId}/hide
POST   /api/v1/admin/interaction/comments/{commentPublicId}/restore

GET    /api/v1/admin/interaction/comment-moderation-cases
GET    /api/v1/admin/interaction/comment-moderation-cases/{casePublicId}
POST   /api/v1/admin/interaction/comment-moderation-cases/{casePublicId}/dismiss
POST   /api/v1/admin/interaction/comment-moderation-cases/{casePublicId}/hide-comment

GET    /api/v1/admin/interaction/comments/{commentPublicId}/moderation-history
GET    /api/v1/admin/interaction/articles/{articlePublicId}/stats
```

### Explicitly deferred endpoints

```text
PUT/PATCH /api/v1/comments/{commentPublicId}
POST      /api/v1/comments/{commentPublicId}/replies
POST      /api/v1/articles/{articlePublicId}/comments with parent identifier
Public GET /api/v1/articles/{articlePublicId}/counters from Interaction
```
