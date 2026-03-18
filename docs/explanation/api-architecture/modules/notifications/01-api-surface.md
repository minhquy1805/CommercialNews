# Notifications — API Surface (V1)

Notifications is primarily **event-driven** and runs in the Worker.
It may expose minimal **Admin read APIs** for operations and troubleshooting.

Base path (Admin): `/api/v1/admin/notifications`

> There are no public write APIs for sending emails.
> Emails are triggered by domain events (Identity/Content) and processed asynchronously.
> Admin APIs in this module are for **inspection and operations**, not for making upstream business truth valid.

---

## 1) Admin read endpoints (optional but recommended)

### GET `/emails`

Search email delivery logs and status with paging.

**Query**

* `page`
* `pageSize`
* `from`, `to` (optional ISO-8601 time range)
* `recipient` (optional; consider hashing or masking policy)
* `template` (optional)
* `status` (optional): `Pending | Sending | Sent | Failed | Dead | Suppressed | Ambiguous`
* `correlationId` (optional)
* `eventId` (optional)
* `messageId` (optional)
* `intentKey` (optional; hashed or redacted policy)
* `sort` allowlist (default `-queuedAt`)

**Response (200)**

```json
{
  "items": [
    {
      "messageId": "string",
      "queuedAt": "2026-03-02T10:30:00Z",
      "recipient": "masked@example.com",
      "template": "VerifyEmail",
      "status": "Sent",
      "attempts": 1,
      "correlationId": "string",
      "eventId": "string"
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

**Rules**

* This endpoint is for operational visibility only.
* Returned delivery state is operational truth for Notifications, not upstream business truth.
* Sensitive fields such as recipient and intent keys should follow masking, hashing, or redaction policy.
* Pagination, filtering, and sorting must be bounded and predictable.

### GET `/emails/{messageId}`

Get detailed delivery history for a specific email message.

**Response (200)**

```json
{
  "messageId": "string",
  "template": "VerifyEmail",
  "recipient": "masked@example.com",
  "status": "Sent",
  "attempts": [
    {
      "attempt": 1,
      "startedAt": "2026-03-02T10:30:01Z",
      "finishedAt": "2026-03-02T10:30:03Z",
      "outcome": "Sent",
      "providerMessageId": "string"
    }
  ],
  "correlationId": "string",
  "eventId": "string",
  "queuedAt": "2026-03-02T10:30:00Z"
}
```

**Rules**

* This endpoint is read-only and intended for debugging or operations.
* Provider-specific identifiers should be exposed only if allowed by policy.
* PII must remain masked or redacted according to policy.

---

## 2) Admin operational endpoints (optional, carefully governed)

### POST `/emails/{messageId}/retry` (optional)

Request a controlled retry for a failed or dead message.

**Auth**

* Required
* Must be restricted to privileged operators or admins

**Headers**

* `Idempotency-Key` (recommended)
* `X-Correlation-Id` (optional)

**Response (202 preferred)**

```json
{
  "accepted": true
}
```

**Rules**

* Retry is an operational action only; it does not repair upstream business truth.
* Retry eligibility must follow policy (for example: failed, dead, not suppressed, within retention window).
* Repeated equivalent retry requests must be safe and converge deterministically.
* Acceptance does not guarantee successful final delivery.

### POST `/emails/suppressions/{recipient}/remove` (optional)

Remove a suppression entry if policy allows.

**Auth**

* Required
* Must be restricted to privileged operators or admins

**Headers**

* `X-Correlation-Id` (optional)

**Response (202 preferred)**

```json
{
  "accepted": true
}
```

**Rules**

* This action must be auditable.
* Removal of a suppression entry does not resend prior messages automatically unless a separate retry action is issued.
* Recipient handling must follow privacy and masking policy.

---

## 3) Versioning and conventions

* All admin endpoints are under `/api/v1/admin/notifications`.
* Errors follow the standard envelope.
* List responses follow `{ items[], pageInfo{} }`.
* Operational actions should prefer `202 Accepted` when work continues asynchronously.
* Notifications correctness is about durable intake and observable delivery state, not synchronous completion.

---

## 4) Notes on coupling and truth boundaries

* Notifications is downstream from business modules such as Identity and Content.
* Domain events trigger notification workflows; Notifications does not make upstream state valid.
* A successful email send does not define whether registration, verification, publish, or other business truth committed.
* Failed or delayed delivery must not roll back upstream truth unless an explicit saga or compensation policy is introduced.
