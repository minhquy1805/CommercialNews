# Notifications — API Surface (V1)

Base path (Admin): `/api/v1/admin/notifications`

> Notifications V1 is primarily **event-driven** and executed through the Worker/runtime pipeline.  
> It does **not** expose public write APIs for sending emails directly.  
> Domain events from upstream truth-owning modules trigger delivery workflows asynchronously.  
> Admin APIs in this module exist for **operational visibility, troubleshooting, and controlled remediation** only.

---

## 1) API posture in V1

Notifications V1 is an **operations-facing module**, not a business-truth command module.

### Included in V1

- email delivery listing for operations
- email delivery detail inspection
- delivery attempt visibility
- controlled retry/remediation for failed deliveries
- correlation and investigation support

### Optional in V1

- delivery search by recipient/template/correlation/message identity
- suppression operations **only if** a first-class suppression capability is implemented
- additional operational filters or dashboards

### Not primary in V1

- public APIs that send system emails directly
- APIs that make Identity / Content / Authorization truth valid
- broad CRUD APIs for outbox internals
- direct business commands such as:
  - send verification email now
  - send reset email now
  - publish and notify now
  - confirm upstream truth by notification completion

**Rule:** Notifications APIs do not define business success for registration, verification, reset, publication, or governance changes.

---

## 2) Resource model

### Primary resource

- `EmailDelivery`

This represents the durable notification delivery workflow for a specific async message / intent boundary.

Recommended stable resource identity in V1:

- `messageId`

Rationale:

- aligns with async event and dedupe contracts
- supports investigation across outbox / consumer / provider logs
- avoids exposing outbox internals as the public center of the module API

### Related subordinate resource

- `EmailDeliveryAttempt`

Attempts are part of delivery history and are usually returned as embedded detail in V1.

---

## 3) Admin endpoints

### 3.1 GET `/email-deliveries`

Search and inspect email delivery workflows with paging.

#### Query parameters

- `page`
- `pageSize`
- `from` *(optional, ISO-8601 UTC lower bound)*
- `to` *(optional, ISO-8601 UTC upper bound)*
- `recipient` *(optional; must follow masking/search policy)*
- `templateKey` *(optional)*
- `status` *(optional)*
- `correlationId` *(optional)*
- `messageId` *(optional)*
- `userId` *(optional, if available in delivery context)*
- `sort` *(allowlist; default `-createdAt` or `-queuedAt` depending on implementation)*

#### Recommended status values

Delivery status should reflect the delivery workflow, not mixed investigation language.

Recommended V1 values:

- `Queued`
- `Sending`
- `Sent`
- `Failed`

Optional operational extension values, only if explicitly implemented and documented:

- `RetryScheduled`
- `TerminalFailed`
- `Suppressed`

Avoid introducing vague status values such as `Ambiguous` as a primary workflow status unless the module has a clearly defined operational state model for it.

#### Response (200)

```json id="bg8k1p"
{
  "items": [
    {
      "messageId": "string",
      "createdAt": "2026-03-02T10:30:00Z",
      "recipient": "masked@example.com",
      "templateKey": "VerifyEmail",
      "status": "Sent",
      "attemptCount": 1,
      "correlationId": "string",
      "userId": 12345
    }
  ],
  "pageInfo": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 1,
    "totalPages": 1
  }
}