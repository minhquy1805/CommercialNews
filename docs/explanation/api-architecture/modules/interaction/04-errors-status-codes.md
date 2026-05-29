# Interaction — Errors & Status Codes (V1)

This document defines Interaction error contracts and HTTP status-code posture for V1.
It focuses on public APIs, authenticated user commands, admin moderation/report-case APIs, async-boundary errors, and retry guidance.

Related:

- `../../02-contracts-and-standards.md`
- `01-api-surface.md`
- `02-domain-contracts.md`
- `03-runtime-flows.md`
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
- ADR-0027 — Stream Processing and Derived State Policy (V1)
- ADR-0028 — Consumer Idempotency, Replay, and Rebuild Policy (V1)

---

## 1) Error Contract Principles

### 1.1 Interaction command success boundary

For commands that produce asynchronous downstream effects:

```text
API success means:
    Interaction-owned state committed
    + required local moderation history committed where applicable
    + required Outbox intent committed where applicable
```

API success does not mean:

```text
Reading already applied counters
Audit already ingested evidence
Notifications already sent email
RabbitMQ consumers already finished processing
```

### 1.2 Timeout ambiguity

A timeout does not prove that an Interaction mutation failed.

Clients must not infer failure solely from:

- request timeout;
- stale Reading counter display;
- missing Audit record immediately after moderation;
- missing email alert immediately after report escalation.

### 1.3 Public safety and information disclosure

For public/user-facing endpoints, Interaction may return `404 Not Found` when revealing whether an article/comment exists or is non-public would expose protected state.

Examples:

- report target comment is not visible;
- article is confirmed not available for new public interaction;
- user attempts to delete a comment they do not own.

### 1.4 Transient projection unavailability

Interaction relies on local Content-derived eligibility projection.

There is a difference between:

| Condition | Meaning | Response posture |
|---|---|---|
| Projection confirms article is not interaction-enabled | Stable business unavailability | `404 Not Found` |
| Projection is missing, unsafe, lagging or requires resync | Transient inability to make a safe decision | `503 Service Unavailable` |

This distinction allows clients to retry temporary convergence failures without treating them as permanent resource absence.

### 1.5 Status-transition conflicts

A command that targets a resource which exists but is now in an incompatible or newer state returns `409 Conflict`.

Examples:

- approving a comment that another moderator already handled;
- resolving a moderation case that is no longer open;
- submitting a duplicate report;
- reusing an idempotency key with different comment content.

---

## 2) Standard Error Envelope

Errors follow the host-wide standard envelope defined in:

```text
../../02-contracts-and-standards.md
```

Conceptual response:

```json
{
  "code": "Interaction.Comment.VersionConflict",
  "message": "The comment has changed. Reload the current state and try again.",
  "correlationId": "01X..."
}
```

### Error envelope rules

- `code` must be stable and machine-readable.
- `message` must be safe for the intended caller.
- `correlationId` should be returned when available.
- Public responses must not expose internal stack traces, SQL errors, worker internals or moderation-sensitive information.
- Error messages must remain in English for API consistency.

---

## 3) Status Code Summary

| Status | Usage in Interaction V1 |
|---:|---|
| `200 OK` | Idempotent like/unlike result, successful admin mutation, successful detail/list query |
| `201 Created` | Comment created, comment report created |
| `202 Accepted` | View submission accepted by the endpoint contract |
| `204 No Content` | Author delete-own-comment success or idempotent already-deleted result |
| `400 Bad Request` | Validation error, malformed public id, unsupported input, invalid reason code |
| `401 Unauthorized` | Authentication required but missing or invalid |
| `403 Forbidden` | Authenticated actor is explicitly forbidden from the requested action |
| `404 Not Found` | Resource missing, safely concealed, not visible, or confirmed unavailable for public interaction |
| `409 Conflict` | Duplicate report, stale version, invalid current-state transition, idempotency-key conflict, case already resolved |
| `429 Too Many Requests` | Rate-limit or abuse-control limit exceeded |
| `500 Internal Server Error` | Unexpected internal failure before a successful commit can be confirmed |
| `503 Service Unavailable` | Temporary inability to safely execute due to unavailable/unsafe local projection or required local service dependency |

---

## 4) Article Eligibility Error Posture

New views, likes, comments and reports depend on local `ArticleInteractionTargetProjection`.

### 4.1 Confirmed non-interactable article

When local projection confirms that an article is not eligible for new public interaction because it is unpublished, archived or soft-deleted:

```http
404 Not Found
```

Error code:

```text
Interaction.Article.NotAvailableForInteraction
```

Safe message:

```text
The requested article is not available for interaction.
```

### 4.2 Missing or unsafe eligibility projection

When Interaction cannot safely determine eligibility because the projection is missing, stale in an unsafe direction, or marked for resync:

```http
503 Service Unavailable
```

Error code:

```text
Interaction.Article.EligibilityTemporarilyUnavailable
```

Safe message:

```text
Interaction is temporarily unavailable for this article. Please try again later.
```

### 4.3 Existing user-owned removal actions

Article non-public status must not prevent:

- unlike of the current user's existing active like;
- author deletion of their existing comment;
- authorized moderator resolution of an already-open case.

These commands must not return `Interaction.Article.NotAvailableForInteraction` solely because the article has become non-public.

---

## 5) View Endpoint Errors

### Endpoint

```text
POST /api/v1/articles/{articlePublicId}/views
```

### Success response

```http
202 Accepted
```

```json
{
  "accepted": true
}
```

### Important response meaning

For view tracking:

```text
accepted = true
```

means the request was accepted by the endpoint contract. It does not promise:

- that the request increased `ViewCount`;
- that repeat-view suppression did not apply;
- that Reading already displays an updated counter.

Interaction may intentionally avoid revealing detailed anti-abuse decisions.

### Error mapping

| Situation | Status | Error code |
|---|---:|---|
| Malformed article public id / malformed request | `400` | `Interaction.ValidationFailed` |
| Article confirmed unavailable for interaction | `404` | `Interaction.Article.NotAvailableForInteraction` |
| Eligibility projection temporarily unsafe/unavailable | `503` | `Interaction.Article.EligibilityTemporarilyUnavailable` |
| Request rejected by explicit rate limit | `429` | `Interaction.RateLimitExceeded` |
| Unexpected mutation failure | `500` | `Interaction.UnexpectedFailure` |

### Rules

- Public article reading must remain successful even when this endpoint fails.
- No error from view tracking may retroactively invalidate the Reading article response.
- Repeated view submissions are governed by view acceptance and anti-abuse policy rather than strict user-visible per-view idempotency.

---

## 6) Like / Unlike Endpoint Errors

### Endpoints

```text
POST   /api/v1/articles/{articlePublicId}/likes
DELETE /api/v1/articles/{articlePublicId}/likes
GET    /api/v1/articles/{articlePublicId}/my-like
```

### Like success

```http
200 OK
```

```json
{
  "articlePublicId": "01J...",
  "liked": true
}
```

Repeated like under idempotent semantics also returns `200 OK` with `liked = true`.

### Unlike success

```http
200 OK
```

```json
{
  "articlePublicId": "01J...",
  "liked": false
}
```

Repeated unlike under idempotent semantics also returns `200 OK` with `liked = false`.

### Error mapping

| Situation | Endpoint | Status | Error code |
|---|---|---:|---|
| Authentication missing/invalid | All like endpoints | `401` | `Interaction.AuthenticationRequired` |
| Malformed article public id | All like endpoints | `400` | `Interaction.ValidationFailed` |
| New like attempted for confirmed unavailable article | `POST /likes` | `404` | `Interaction.Article.NotAvailableForInteraction` |
| New like attempted while eligibility projection unsafe | `POST /likes` | `503` | `Interaction.Article.EligibilityTemporarilyUnavailable` |
| Unlike existing relationship after article became non-public | `DELETE /likes` | `200` | — |
| Rate limited | Mutation endpoints | `429` | `Interaction.RateLimitExceeded` |
| Unexpected failure before result is known | Mutation endpoints | `500` | `Interaction.UnexpectedFailure` |

### Not returned as errors

The following must not be exposed as an error under the recommended idempotent API posture:

| Situation | Response |
|---|---|
| Already liked, repeat like | `200`, `liked = true` |
| Already unliked / no active like, repeat unlike | `200`, `liked = false` |
| Derived `LikeCount` has not updated yet | No command error |

### Ambiguous timeout reconciliation

After a timeout, client may call:

```text
GET /api/v1/articles/{articlePublicId}/my-like
```

This endpoint reads authoritative `ArticleLike` state, not public counters.

---

## 7) Public Comment Query Errors

### Endpoint

```text
GET /api/v1/articles/{articlePublicId}/comments
```

### Success

```http
200 OK
```

### Error mapping

| Situation | Status | Error code |
|---|---:|---|
| Invalid paging/sort/public id input | `400` | `Interaction.ValidationFailed` |
| Article not publicly available or deliberately concealed | `404` | `Interaction.Article.NotFound` |
| Unexpected query failure | `500` | `Interaction.UnexpectedFailure` |

### Rules

The public comment endpoint must not expose the existence of:

- pending comments;
- rejected comments;
- hidden comments;
- deleted comments;
- moderation cases;
- reports.

A public query returning no visible comments is normally:

```http
200 OK
```

with an empty `items` list, not `404`.

---

## 8) Create Comment Errors

### Endpoint

```text
POST /api/v1/articles/{articlePublicId}/comments
```

### Success

```http
201 Created
```

```json
{
  "commentPublicId": "01J...",
  "articlePublicId": "01J...",
  "status": "Pending",
  "createdAtUtc": "2026-05-25T10:30:00Z",
  "version": 1
}
```

### Error mapping

| Situation | Status | Error code |
|---|---:|---|
| Authentication missing/invalid | `401` | `Interaction.AuthenticationRequired` |
| Empty/invalid/over-length comment content | `400` | `Interaction.Comment.InvalidContent` |
| Request includes parent comment identifier in V1 | `400` | `Interaction.Comment.RepliesNotSupported` |
| Article confirmed unavailable for interaction | `404` | `Interaction.Article.NotAvailableForInteraction` |
| Eligibility projection temporarily unsafe/unavailable | `503` | `Interaction.Article.EligibilityTemporarilyUnavailable` |
| Idempotency key reused with conflicting payload | `409` | `Interaction.IdempotencyKey.PayloadConflict` |
| Rate limited | `429` | `Interaction.RateLimitExceeded` |
| Unexpected create failure | `500` | `Interaction.UnexpectedFailure` |

### Retry behavior

If create-comment idempotency is implemented:

| Situation | Status posture |
|---|---|
| Same idempotency key and same request already committed | Return original logical success result |
| Same idempotency key with different payload | `409 Conflict` |

A timeout without idempotency support may require user/client reconciliation; it must not be inferred from comment counters.

---

## 9) Delete Own Comment Errors

### Endpoint

```text
DELETE /api/v1/comments/{commentPublicId}
```

### Success

```http
204 No Content
```

This applies both when:

- the author successfully deletes the comment;
- the author's repeated delete request finds the comment already deleted under idempotent behavior.

### Error mapping

| Situation | Status | Error code |
|---|---:|---|
| Authentication missing/invalid | `401` | `Interaction.AuthenticationRequired` |
| Malformed comment public id | `400` | `Interaction.ValidationFailed` |
| Comment does not exist | `404` | `Interaction.Comment.NotFound` |
| Current actor is not the author | `404` preferred | `Interaction.Comment.NotFound` |
| Comment changed concurrently in a way requiring refresh | `409` | `Interaction.Comment.VersionConflict` |
| Unexpected delete failure | `500` | `Interaction.UnexpectedFailure` |

### Ownership concealment rule

For user-facing delete-own-comment flow, do not reveal that a comment exists but belongs to another user.

Prefer:

```http
404 Not Found
```

instead of:

```http
403 Forbidden
```

for non-owner attempts.

### Open-case behavior

If author deletion closes an open moderation case, this remains part of the successful delete transaction. It does not require a special success status.

---

## 10) Report Comment Errors

### Endpoint

```text
POST /api/v1/comments/{commentPublicId}/reports
```

### Success

```http
201 Created
```

```json
{
  "commentReportPublicId": "01J...",
  "commentPublicId": "01J...",
  "status": "Pending",
  "createdAtUtc": "2026-05-25T10:35:00Z"
}
```

### Error mapping

| Situation | Status | Error code |
|---|---:|---|
| Authentication missing/invalid | `401` | `Interaction.AuthenticationRequired` |
| Invalid reason code | `400` | `Interaction.CommentReport.InvalidReasonCode` |
| Missing description when reason is `Other` | `400` | `Interaction.CommentReport.DescriptionRequired` |
| Description exceeds limit | `400` | `Interaction.CommentReport.InvalidDescription` |
| Reporter is comment author | `403` | `Interaction.CommentReport.CannotReportOwnComment` |
| Comment missing, hidden, pending, rejected, deleted or otherwise not reportable | `404` | `Interaction.Comment.NotReportable` |
| Related article confirmed unavailable for interaction | `404` | `Interaction.Article.NotAvailableForInteraction` |
| Eligibility projection temporarily unsafe/unavailable | `503` | `Interaction.Article.EligibilityTemporarilyUnavailable` |
| Reporter already reported this comment | `409` | `Interaction.CommentReport.AlreadySubmitted` |
| Rate limited | `429` | `Interaction.RateLimitExceeded` |
| Unexpected failure | `500` | `Interaction.UnexpectedFailure` |

### Rules

- Report creation never returns success for an automatic hide action, because report does not auto-hide content.
- Alert triggering may occur in the same committed report transaction, but email delivery remains asynchronous.
- A report response does not expose whether the case triggered an administrator email alert.

---

## 11) Admin Comment Query Errors

### Endpoints

```text
GET /api/v1/admin/interaction/comments
GET /api/v1/admin/interaction/comments/{commentPublicId}
GET /api/v1/admin/interaction/comments/{commentPublicId}/moderation-history
```

### Error mapping

| Situation | Status | Error code |
|---|---:|---|
| Authentication missing/invalid | `401` | `Interaction.AuthenticationRequired` |
| Missing admin read permission | `403` | `Interaction.Forbidden` |
| Invalid filters/paging/sort | `400` | `Interaction.ValidationFailed` |
| Comment detail target not found | `404` | `Interaction.Comment.NotFound` |
| Unexpected query failure | `500` | `Interaction.UnexpectedFailure` |

---

## 12) Admin Comment Moderation Errors

### Endpoints

```text
POST /api/v1/admin/interaction/comments/{commentPublicId}/approve
POST /api/v1/admin/interaction/comments/{commentPublicId}/reject
POST /api/v1/admin/interaction/comments/{commentPublicId}/hide
POST /api/v1/admin/interaction/comments/{commentPublicId}/restore
```

### Success

```http
200 OK
```

### Error mapping

| Situation | Status | Error code |
|---|---:|---|
| Authentication missing/invalid | `401` | `Interaction.AuthenticationRequired` |
| Missing moderation permission | `403` | `Interaction.Forbidden` |
| Invalid reason/note payload | `400` | `Interaction.Moderation.InvalidReason` |
| Comment not found | `404` | `Interaction.Comment.NotFound` |
| Expected version does not match current comment version | `409` | `Interaction.Comment.VersionConflict` |
| Current status does not permit requested transition | `409` | `Interaction.Comment.InvalidStatusTransition` |
| Hide endpoint used while an open report case must be resolved atomically | `409` | `Interaction.Comment.OpenModerationCaseRequiresResolution` |
| Unexpected mutation failure | `500` | `Interaction.UnexpectedFailure` |

### Transition conflicts

| Requested action | Required current status | Conflict code if state incompatible |
|---|---|---|
| Approve | `Pending` | `Interaction.Comment.InvalidStatusTransition` |
| Reject | `Pending` | `Interaction.Comment.InvalidStatusTransition` |
| Hide | `Visible` and no unresolved case requiring case flow | `Interaction.Comment.InvalidStatusTransition` or `OpenModerationCaseRequiresResolution` |
| Restore | `Hidden` | `Interaction.Comment.InvalidStatusTransition` |

### Retry rule

A retry of a completed moderation action must not:

- perform the transition again;
- append duplicate moderation history;
- emit duplicate business event;
- double-apply counter consequences.

The API may return deterministic `409 Conflict` for stale/repeated admin commands unless a specific idempotent-command contract is later introduced.

---

## 13) Admin Moderation Case Query Errors

### Endpoints

```text
GET /api/v1/admin/interaction/comment-moderation-cases
GET /api/v1/admin/interaction/comment-moderation-cases/{casePublicId}
```

### Error mapping

| Situation | Status | Error code |
|---|---:|---|
| Authentication missing/invalid | `401` | `Interaction.AuthenticationRequired` |
| Missing report-read permission | `403` | `Interaction.Forbidden` |
| Invalid filters/paging/sort | `400` | `Interaction.ValidationFailed` |
| Case not found | `404` | `Interaction.CommentModerationCase.NotFound` |
| Unexpected query failure | `500` | `Interaction.UnexpectedFailure` |

---

## 14) Admin Moderation Case Resolution Errors

### Endpoints

```text
POST /api/v1/admin/interaction/comment-moderation-cases/{casePublicId}/dismiss
POST /api/v1/admin/interaction/comment-moderation-cases/{casePublicId}/hide-comment
```

### Success

```http
200 OK
```

### Error mapping

| Situation | Status | Error code |
|---|---:|---|
| Authentication missing/invalid | `401` | `Interaction.AuthenticationRequired` |
| Missing case-resolution permission | `403` | `Interaction.Forbidden` |
| Hide-comment action lacks comment-moderation permission | `403` | `Interaction.Forbidden` |
| Missing/invalid required resolution reason | `400` | `Interaction.Moderation.InvalidReason` |
| Case not found | `404` | `Interaction.CommentModerationCase.NotFound` |
| Expected case version mismatch | `409` | `Interaction.CommentModerationCase.VersionConflict` |
| Case is no longer open | `409` | `Interaction.CommentModerationCase.AlreadyResolved` |
| Hide-comment command finds comment no longer `Visible` | `409` | `Interaction.Comment.InvalidStatusTransition` |
| Expected comment version mismatch during hide-comment | `409` | `Interaction.Comment.VersionConflict` |
| Author deletion won race against admin resolution | `409` | `Interaction.CommentModerationCase.AlreadyClosedByAuthorDeletion` |
| Unexpected mutation failure | `500` | `Interaction.UnexpectedFailure` |

### Rules

- A resolved case is never resolved again as a new business effect.
- Case-resolution conflict must not emit new moderation history or outbox intent.
- If the author deleted the comment before admin resolution, admin must refresh case state rather than force the previous action.

---

## 15) Admin Counter Inspection Errors

### Endpoint

```text
GET /api/v1/admin/interaction/articles/{articlePublicId}/stats
```

### Error mapping

| Situation | Status | Error code |
|---|---:|---|
| Authentication missing/invalid | `401` | `Interaction.AuthenticationRequired` |
| Missing counter-read permission | `403` | `Interaction.Forbidden` |
| Invalid article public id | `400` | `Interaction.ValidationFailed` |
| No Interaction stats state exists for target | `404` | `Interaction.ArticleInteractionStats.NotFound` |
| Unexpected query failure | `500` | `Interaction.UnexpectedFailure` |

### Rules

- This endpoint exposes derived state for authorized diagnostics.
- Counter lag is not an error.
- Missing/rebuilding counters must not change Content or Reading article visibility decisions.

---

## 16) Async and Downstream Failure Posture

### 16.1 After local command commit

Once Interaction truth/workflow mutation and required outbox intent commit successfully, downstream lag or failure must not change the API result into a failed command.

Examples:

| Downstream issue | Originating command posture |
|---|---|
| Reading has not yet applied new counters | Interaction command remains successful |
| Audit consumer is delayed | Moderation command remains successful |
| Notifications email delivery is delayed | Report/case alert-trigger transaction remains successful |
| RabbitMQ redelivery occurs later | Consumer idempotency handles duplicate effect |

### 16.2 Outbox persistence failure inside command transaction

If a required outbox intent cannot be written and the local transaction cannot commit atomically:

```http
500 Internal Server Error
```

The command must not return success for a mutation whose required propagation intent was not committed.

Error code:

```text
Interaction.Outbox.CommitFailed
```

This code is primarily diagnostic; the safe public message should remain generic.

### 16.3 Counter materialization errors

Counter materialization/publication failures are not returned to the original like/comment/view command once that command has completed.

They must be handled through:

- worker retry;
- metrics/logging;
- reconciliation;
- counter snapshot republish.

---

## 17) Error Code Catalogue

### 17.1 General errors

| Error code | Suggested HTTP status | Meaning |
|---|---:|---|
| `Interaction.ValidationFailed` | `400` | Request fields or parameters are invalid |
| `Interaction.AuthenticationRequired` | `401` | Authenticated actor is required |
| `Interaction.Forbidden` | `403` | Authenticated actor lacks permission |
| `Interaction.RateLimitExceeded` | `429` | Request exceeded allowed rate/abuse threshold |
| `Interaction.UnexpectedFailure` | `500` | Unexpected internal failure |
| `Interaction.Outbox.CommitFailed` | `500` | Required outbox intent could not commit with mutation |
| `Interaction.IdempotencyKey.PayloadConflict` | `409` | Same idempotency key reused with a different request |

### 17.2 Article / eligibility errors

| Error code | Suggested HTTP status | Meaning |
|---|---:|---|
| `Interaction.Article.NotFound` | `404` | Article resource is not publicly available or safely concealed |
| `Interaction.Article.NotAvailableForInteraction` | `404` | Article is confirmed ineligible for new interaction |
| `Interaction.Article.EligibilityTemporarilyUnavailable` | `503` | Eligibility projection is missing, unsafe or requires resync |
| `Interaction.ArticleInteractionStats.NotFound` | `404` | No admin-visible counter state exists for requested article |

### 17.3 Comment errors

| Error code | Suggested HTTP status | Meaning |
|---|---:|---|
| `Interaction.Comment.NotFound` | `404` | Comment missing or deliberately concealed |
| `Interaction.Comment.NotReportable` | `404` | Comment is not currently eligible for public reporting |
| `Interaction.Comment.InvalidContent` | `400` | Comment content fails validation |
| `Interaction.Comment.RepliesNotSupported` | `400` | Parent/reply input is unsupported in V1 |
| `Interaction.Comment.VersionConflict` | `409` | Expected version is stale |
| `Interaction.Comment.InvalidStatusTransition` | `409` | Current status cannot execute requested action |
| `Interaction.Comment.OpenModerationCaseRequiresResolution` | `409` | Use case-resolution flow instead of direct hide |

### 17.4 Comment report errors

| Error code | Suggested HTTP status | Meaning |
|---|---:|---|
| `Interaction.CommentReport.InvalidReasonCode` | `400` | Report reason is not supported |
| `Interaction.CommentReport.DescriptionRequired` | `400` | Description is required for chosen reason |
| `Interaction.CommentReport.InvalidDescription` | `400` | Report description fails validation |
| `Interaction.CommentReport.CannotReportOwnComment` | `403` | Author cannot report their own comment |
| `Interaction.CommentReport.AlreadySubmitted` | `409` | Reporter previously submitted a report for this comment |

### 17.5 Moderation errors

| Error code | Suggested HTTP status | Meaning |
|---|---:|---|
| `Interaction.Moderation.InvalidReason` | `400` | Required moderation reason/note is missing or invalid |
| `Interaction.CommentModerationCase.NotFound` | `404` | Moderation case does not exist |
| `Interaction.CommentModerationCase.VersionConflict` | `409` | Case expected version is stale |
| `Interaction.CommentModerationCase.AlreadyResolved` | `409` | Case is no longer open |
| `Interaction.CommentModerationCase.AlreadyClosedByAuthorDeletion` | `409` | Author deletion closed case before admin resolution |

---

## 18) Endpoint Status Matrix

| Endpoint | Success | Common errors |
|---|---:|---|
| `POST /api/v1/articles/{articlePublicId}/views` | `202` | `400`, `404`, `429`, `500`, `503` |
| `POST /api/v1/articles/{articlePublicId}/likes` | `200` | `400`, `401`, `404`, `429`, `500`, `503` |
| `DELETE /api/v1/articles/{articlePublicId}/likes` | `200` | `400`, `401`, `429`, `500` |
| `GET /api/v1/articles/{articlePublicId}/my-like` | `200` | `400`, `401`, `404`, `500` |
| `GET /api/v1/articles/{articlePublicId}/comments` | `200` | `400`, `404`, `500` |
| `POST /api/v1/articles/{articlePublicId}/comments` | `201` | `400`, `401`, `404`, `409`, `429`, `500`, `503` |
| `DELETE /api/v1/comments/{commentPublicId}` | `204` | `400`, `401`, `404`, `409`, `500` |
| `POST /api/v1/comments/{commentPublicId}/reports` | `201` | `400`, `401`, `403`, `404`, `409`, `429`, `500`, `503` |
| `GET /api/v1/admin/interaction/comments` | `200` | `400`, `401`, `403`, `500` |
| `GET /api/v1/admin/interaction/comments/{commentPublicId}` | `200` | `400`, `401`, `403`, `404`, `500` |
| `POST /api/v1/admin/interaction/comments/{commentPublicId}/approve` | `200` | `400`, `401`, `403`, `404`, `409`, `500` |
| `POST /api/v1/admin/interaction/comments/{commentPublicId}/reject` | `200` | `400`, `401`, `403`, `404`, `409`, `500` |
| `POST /api/v1/admin/interaction/comments/{commentPublicId}/hide` | `200` | `400`, `401`, `403`, `404`, `409`, `500` |
| `POST /api/v1/admin/interaction/comments/{commentPublicId}/restore` | `200` | `400`, `401`, `403`, `404`, `409`, `500` |
| `GET /api/v1/admin/interaction/comment-moderation-cases` | `200` | `400`, `401`, `403`, `500` |
| `GET /api/v1/admin/interaction/comment-moderation-cases/{casePublicId}` | `200` | `400`, `401`, `403`, `404`, `500` |
| `POST /api/v1/admin/interaction/comment-moderation-cases/{casePublicId}/dismiss` | `200` | `400`, `401`, `403`, `404`, `409`, `500` |
| `POST /api/v1/admin/interaction/comment-moderation-cases/{casePublicId}/hide-comment` | `200` | `400`, `401`, `403`, `404`, `409`, `500` |
| `GET /api/v1/admin/interaction/comments/{commentPublicId}/moderation-history` | `200` | `400`, `401`, `403`, `404`, `500` |
| `GET /api/v1/admin/interaction/articles/{articlePublicId}/stats` | `200` | `400`, `401`, `403`, `404`, `500` |

---

## 19) Important V1 Error Rules

1. View tracking failure must not fail public article reading.
2. Confirmed non-interactable article returns `404`; temporarily unsafe eligibility projection returns `503`.
3. Like and unlike use idempotent success semantics rather than duplicate-operation conflicts.
4. Comment creation returns `201`; comments begin as `Pending`.
5. Reply input is invalid in V1 and returns `400`.
6. Author delete-own-comment returns `204` and is idempotent.
7. Non-owner delete attempts should return `404` to avoid ownership disclosure.
8. Reports return `201`, never auto-hide content, and duplicate reports return `409`.
9. Moderator stale version or illegal transitions return `409`.
10. An open report case requiring atomic resolution prevents direct standalone hide and returns `409`.
11. Case resolution races and author-delete races return deterministic `409` outcomes.
12. Counters may lag without producing command errors.
13. Audit or email delivery lag must not change a successful Interaction command result.
14. API errors must never use timestamps or downstream observations to infer whether truth committed.
