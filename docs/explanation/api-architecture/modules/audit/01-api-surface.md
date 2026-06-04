# Audit — API Surface (V1)

Audit is primarily an asynchronous ingestion module and typically runs in the Worker.

Audit exposes Admin read APIs for investigation, governance review, operational diagnostics, and lightweight dashboard visibility.

Base path:

```http
/api/v1/admin/audit
```

All Audit APIs require:

* Bearer authentication
* explicit admin authorization policy
* privacy-aware response shaping
* bounded query behavior

Audit ingestion itself is not exposed publicly.

Audit write path happens through:

```text
Outbox → RabbitMQ → CommercialNews.Worker → IngestAuditEventCommand → Audit.Application → AuditLog / AuditIngestion
```

The Worker owns queue consumption and message-to-command mapping. Audit.Application
does not read broker queues directly; it receives commands through MediatR.

There are no public or admin APIs for manually inserting, updating, or deleting audit evidence in V1.

---

## 1. API Surface Principles

### 1.1 Evidence APIs vs derived operational APIs

Audit APIs expose two kinds of data.

#### Canonical evidence APIs

These read from SQL-backed `AuditLog`.

Examples:

* `/logs`
* `/logs/{publicId}`
* `/logs/by-message/{messageId}`
* `/logs/by-correlation/{correlationId}`
* `/resources/{resourceType}/{resourceId}/timeline`
* `/actors/{actorUserId}/timeline`

These represent append-only audit evidence as recorded by Audit.

#### Operational / derived APIs

These read from `AuditLog`, `AuditIngestion`, or future derived outputs.

Examples:

* `/dashboard/summary`
* `/dashboard/recent-risk-events`
* `/ingestions`
* `/ingestions/failed`

These support operations and dashboard visibility. They do not replace `AuditLog` evidence truth.

### 1.2 V1 API scope

V1 should support:

* audit log search
* audit log detail
* lookup by `MessageId`
* lookup by `CorrelationId`
* module-specific logs
* resource timeline
* actor timeline
* ingestion status queries
* failed ingestion queries
* lightweight dashboard summary
* recent high-risk events

V1 does not require:

* manual audit write APIs
* correction APIs
* alert APIs
* digest APIs
* archive APIs
* replay control APIs
* reconciliation control APIs
* batch job control APIs
* Redis cache control APIs
* partition management APIs

These are future extension candidates.

---

## 2. Audit Log APIs

## 2.1 GET `/logs`

Search audit logs with paging.

### Query parameters

| Parameter         |    Required | Description                                           |
| ----------------- | ----------: | ----------------------------------------------------- |
| `page`            |          No | Page number. Default: `1`                             |
| `pageSize`        |          No | Page size. Default policy-defined                     |
| `fromUtc`         | Recommended | Start of time range                                   |
| `toUtc`           | Recommended | End of time range                                     |
| `sourceModule`    |          No | Filter by source module                               |
| `action`          |          No | Filter by normalized audit action                     |
| `actionCategory`  |          No | Filter by action category                             |
| `actorUserId`     |          No | Filter by stable actor identifier                     |
| `actorInternalId` |          No | Filter by internal producer-side actor id when needed |
| `resourceType`    |          No | Filter by resource type                               |
| `resourceId`      |          No | Filter by stable resource identifier                  |
| `outcome`         |          No | `Success`, `Failure`, `Denied`, `Ignored`             |
| `severity`        |          No | `Info`, `Warning`, `Error`, `Critical`                |
| `riskLevel`       |          No | `Low`, `Medium`, `High`, `Critical`                   |
| `correlationId`   |          No | Filter by correlation id                              |
| `messageId`       |          No | Filter by Outbox message identity                     |
| `eventType`       |          No | Filter by original event type                         |
| `sort`            |          No | Allowlisted sort. Default: `-occurredAtUtc`           |

`messageId` remains part of the `/logs` search contract. Application/Search
contracts are expected to support this filter so callers can narrow list results
without switching to `/logs/by-message/{messageId}`.

### Response `200`

```json
{
  "items": [
    {
      "publicId": "01HY8Q9Z6J9V9V6HE0M77XQK4A",
      "messageId": "01HY8Q9YTVM7W4M6VQVNQ4Q8CY",
      "occurredAtUtc": "2026-03-02T10:30:00Z",
      "ingestedAtUtc": "2026-03-02T10:30:03Z",
      "sourceModule": "Content",
      "eventType": "content.article_published",
      "action": "ArticlePublished",
      "actionCategory": "ContentLifecycle",
      "actor": {
        "actorUserId": "01HY8Q9X7JR5QQM2Z8KZ3X6V2M",
        "actorDisplayName": "Admin User",
        "actorType": "Admin"
      },
      "resource": {
        "type": "Article",
        "id": "01HY8Q9W8QYW6Z3EWDGGE6N7FP",
        "displayName": "Example article"
      },
      "outcome": "Success",
      "severity": "Info",
      "riskLevel": "Medium",
      "correlationId": "corr-20260302-001",
      "summary": "Article was published."
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

* Results represent append-only canonical audit evidence.
* Results may lag behind source module truth because Audit ingestion is asynchronous.
* Admin UI should surface lag/backlog hints when relevant.
* Search must remain privacy-aware.
* Search must not expose unsafe raw payloads.
* Queries must be bounded, paged, and predictable.
* Internal numeric primary keys must not be exposed as stable API identifiers.

---

## 2.2 GET `/logs/{publicId}`

Get one audit record by Audit-owned `PublicId`.

### Response `200`

```json
{
  "publicId": "01HY8Q9Z6J9V9V6HE0M77XQK4A",
  "messageId": "01HY8Q9YTVM7W4M6VQVNQ4Q8CY",
  "eventType": "content.article_unpublished",
  "sourceModule": "Content",
  "action": "ArticleUnpublished",
  "actionCategory": "ContentLifecycle",
  "aggregate": {
    "type": "Article",
    "id": "12345",
    "publicId": "01HY8Q9W8QYW6Z3EWDGGE6N7FP",
    "version": 7
  },
  "actor": {
    "actorInternalId": 42,
    "actorUserId": "01HY8Q9X7JR5QQM2Z8KZ3X6V2M",
    "actorEmail": "admin@example.com",
    "actorDisplayName": "Admin User",
    "actorType": "Admin"
  },
  "resource": {
    "type": "Article",
    "id": "01HY8Q9W8QYW6Z3EWDGGE6N7FP",
    "displayName": "Example article"
  },
  "outcome": "Success",
  "severity": "Warning",
  "riskLevel": "Medium",
  "correlationId": "corr-20260302-001",
  "occurredAtUtc": "2026-03-02T10:30:00Z",
  "ingestedAtUtc": "2026-03-02T10:30:03Z",
  "summary": "Article was unpublished.",
  "metadata": {
    "reason": "PolicyViolation",
    "slug": "example-article"
  }
}
```

### Rules

* Detail view returns evidence as recorded, not inferred current business truth.
* `metadata` must be minimal and redacted.
* Never return tokens, secrets, password hashes, refresh tokens, verification tokens, reset tokens, raw authorization headers, session cookies, or unsafe sensitive PII.
* Slug may appear as metadata, but resource identity should use stable identifiers.
* If future correction-by-new-fact is introduced, detail view should make original versus corrective linkage explicit.

---

## 2.3 GET `/logs/by-message/{messageId}`

Return the canonical audit record associated with one upstream Outbox `MessageId`.

### Purpose

Help distinguish:

* true missing evidence
* delayed ingestion
* duplicate delivery already deduped
* producer-side publish completed but consumer-side processing not completed

### Response `200`

Same shape as audit log detail.

### Response `404`

Returned when no `AuditLog` exists for the given `MessageId`.

### Rules

* `messageId` is the system-wide async message identity.
* This endpoint replaces the old `/logs/by-event/{eventId}` naming.
* If the message was received but failed before evidence insertion, `/ingestions/by-message/{messageId}` may provide consumer-side status.
* Safe not-found does not prove the source business action did not happen.

---

## 2.4 GET `/logs/by-correlation/{correlationId}`

Return bounded audit records for one correlation flow.

### Query parameters

| Parameter  | Required | Description                  |
| ---------- | -------: | ---------------------------- |
| `page`     |       No | Page number                  |
| `pageSize` |       No | Page size                    |
| `fromUtc`  |       No | Optional start of time range |
| `toUtc`    |       No | Optional end of time range   |
| `sort`     |       No | Allowlisted sort             |

### Purpose

Help investigators trace one admin or business operation across modules.

### Response `200`

Standard paged list envelope.

### Rules

* Results must be bounded and paged.
* This endpoint groups evidence by shared correlation identifier.
* It does not claim perfect global chronology.
* It must not expose unsafe internals.

---

## 3. Module Investigation APIs

## 3.1 GET `/modules`

Return supported source modules known to Audit.

### Response `200`

```json
{
  "items": [
    {
      "sourceModule": "Identity",
      "description": "Identity and account-security audit events"
    },
    {
      "sourceModule": "Authorization",
      "description": "Role and permission governance audit events"
    },
    {
      "sourceModule": "Content",
      "description": "Content lifecycle audit events"
    }
  ]
}
```

### Rules

* This may be static or derived from known normalizer registrations.
* This endpoint is metadata, not evidence truth.

---

## 3.2 GET `/modules/{sourceModule}/logs`

Return audit logs for a specific source module.

### Query parameters

Same as `/logs`, with `sourceModule` supplied by path.

### Example

```http
GET /api/v1/admin/audit/modules/Authorization/logs?action=RolePermissionGranted&fromUtc=2026-03-01T00:00:00Z&toUtc=2026-03-02T00:00:00Z
```

### Response `200`

Standard paged list envelope.

### Rules

* Results read canonical `AuditLog` evidence.
* Query must remain bounded and paged.

---

## 3.3 GET `/modules/{sourceModule}/actions`

Return known normalized actions for a module.

### Response `200`

```json
{
  "sourceModule": "Authorization",
  "items": [
    "UserRoleAssigned",
    "UserRoleRevoked",
    "RolePermissionGranted",
    "RolePermissionRevoked"
  ]
}
```

### Rules

* This endpoint supports UI filters.
* It may be static or derived from normalizer metadata.
* It does not replace audit evidence queries.

---

## 4. Timeline APIs

## 4.1 GET `/resources/{resourceType}/{resourceId}/timeline`

Return audit timeline for one resource.

### Query parameters

| Parameter  | Required | Description                                                          |
| ---------- | -------: | -------------------------------------------------------------------- |
| `page`     |       No | Page number                                                          |
| `pageSize` |       No | Page size                                                            |
| `fromUtc`  |       No | Start time                                                           |
| `toUtc`    |       No | End time                                                             |
| `sort`     |       No | `occurredAtUtc`, `-occurredAtUtc`, `ingestedAtUtc`, `-ingestedAtUtc` |

### Purpose

Answer investigation questions such as:

* Who changed this article?
* Who locked this user?
* Who hid this comment?
* Who changed this permission?
* Which event affected this resource?

### Response `200`

Standard paged list envelope.

### Rules

* `resourceId` should be a stable source resource identifier, preferably `PublicId`.
* If only internal IDs are available for some legacy events, API documentation must make that explicit.
* Timeline order is contextual and does not claim global total ordering.

---

## 4.2 GET `/actors/{actorUserId}/timeline`

Return audit timeline for one actor.

### Query parameters

| Parameter      | Required | Description                   |
| -------------- | -------: | ----------------------------- |
| `page`         |       No | Page number                   |
| `pageSize`     |       No | Page size                     |
| `fromUtc`      |       No | Start time                    |
| `toUtc`        |       No | End time                      |
| `sourceModule` |       No | Optional source module filter |
| `riskLevel`    |       No | Optional risk filter          |
| `sort`         |       No | Allowlisted sort              |

### Purpose

Answer investigation questions such as:

* What did this admin do today?
* Which moderator hid comments?
* Which user repeatedly triggered denied actions?
* Which actor performed high-risk actions?

### Response `200`

Standard paged list envelope.

### Rules

* `actorUserId` should be a stable actor public identifier when available.
* If filtering by internal producer-side actor id is required, use explicit query parameters rather than overloading this path.

---

## 5. Ingestion APIs

`AuditIngestion` APIs expose consumer-side processing status.

They help distinguish:

* Outbox not published yet
* message published but Audit not processed
* Audit processed successfully
* Audit deduped duplicate delivery
* Audit failed after broker handoff
* message dead-lettered or awaiting remediation

## 5.1 GET `/ingestions`

Search audit ingestion records with paging.

### Query parameters

| Parameter       | Required | Description                                                                 |
| --------------- | -------: | --------------------------------------------------------------------------- |
| `page`          |       No | Page number                                                                 |
| `pageSize`      |       No | Page size                                                                   |
| `fromUtc`       |       No | First received start time                                                   |
| `toUtc`         |       No | First received end time                                                     |
| `status`        |       No | `Processing`, `Succeeded`, `Duplicate`, `Ignored`, `Failed`, `DeadLettered` |
| `messageId`     |       No | Filter by message id                                                        |
| `eventType`     |       No | Filter by event type                                                        |
| `aggregateType` |       No | Filter by source aggregate type                                             |
| `aggregateId`   |       No | Filter by source aggregate id                                               |
| `aggregatePublicId` | No | Filter by source aggregate public id                                         |
| `correlationId` |       No | Filter by correlation id                                                    |
| `consumerName`  |       No | Filter by consumer name                                                     |
| `lastErrorClass` |      No | Filter by last error class                                                  |
| `sort`          |       No | Allowlisted sort                                                            |

### Response `200`

```json
{
  "items": [
    {
      "publicId": "01HY8QA1XHT1W07PF2FB3B9PZC",
      "messageId": "01HY8Q9YTVM7W4M6VQVNQ4Q8CY",
      "eventType": "authorization.role_permission_granted",
      "correlationId": "corr-20260302-001",
      "status": "Succeeded",
      "attemptCount": 1,
      "firstReceivedAtUtc": "2026-03-02T10:30:02Z",
      "lastAttemptAtUtc": "2026-03-02T10:30:02Z",
      "processedAtUtc": "2026-03-02T10:30:03Z",
      "lastErrorCode": null,
      "lastErrorClass": null
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

* This endpoint exposes consumer-side processing state, not source business truth.
* It must not expose raw unsafe payloads.
* It must not be confused with producer-side Outbox status.

---

## 5.2 GET `/ingestions/failed`

Return failed or dead-lettered ingestion records.

### Query parameters

| Parameter       | Required | Description                         |
| --------------- | -------: | ----------------------------------- |
| `page`          |       No | Page number                         |
| `pageSize`      |       No | Page size                           |
| `fromUtc`       |       No | First received start time           |
| `toUtc`         |       No | First received end time             |
| `eventType`     |       No | Filter by event type                |
| `aggregateType` |       No | Filter by source aggregate type     |
| `aggregateId`   |       No | Filter by source aggregate id       |
| `aggregatePublicId` | No | Filter by source aggregate public id |
| `correlationId` |       No | Filter by correlation id            |
| `consumerName`  |       No | Filter by consumer name             |
| `lastErrorClass` |      No | Filter by last error class          |
| `sort`          |       No | Allowlisted sort                    |

### Response `200`

Standard paged ingestion list.

### Rules

* Used for operational diagnostics.
* Helps operators identify Audit consumer failures.
* Does not imply source module truth failed.

---

## 5.3 GET `/ingestions/by-message/{messageId}`

Return Audit consumer-side processing status for one `MessageId`.

### Response `200`

```json
{
  "publicId": "01HY8QA1XHT1W07PF2FB3B9PZC",
  "messageId": "01HY8Q9YTVM7W4M6VQVNQ4Q8CY",
  "eventType": "authorization.role_permission_granted",
  "aggregateType": "RolePermission",
  "aggregateId": "role-1:permission-9",
  "aggregatePublicId": null,
  "aggregateVersion": 3,
  "correlationId": "corr-20260302-001",
  "status": "Succeeded",
  "attemptCount": 1,
  "sourceOccurredAtUtc": "2026-03-02T10:30:00Z",
  "sourcePublishedAtUtc": "2026-03-02T10:30:01Z",
  "firstReceivedAtUtc": "2026-03-02T10:30:02Z",
  "lastAttemptAtUtc": "2026-03-02T10:30:02Z",
  "processedAtUtc": "2026-03-02T10:30:03Z",
  "lastErrorCode": null,
  "lastErrorMessage": null,
  "lastErrorClass": null
}
```

### Response `404`

Returned when Audit has not seen the message.

### Rules

* Useful when `/logs/by-message/{messageId}` returns not found.
* If no ingestion exists, the message may not have reached Audit yet.
* Not-found does not prove source truth did not commit.
* This endpoint must not mutate ingestion state.

---

## 6. Dashboard APIs

Dashboard APIs are lightweight V1 operational views.

They are derived views computed from SQL-backed `AuditLog` and `AuditIngestion`.

They are not canonical evidence truth.

## 6.1 GET `/dashboard/summary`

Return high-level audit dashboard summary.

### Query parameters

| Parameter      |    Required | Description            |
| -------------- | ----------: | ---------------------- |
| `fromUtc`      | Recommended | Start time             |
| `toUtc`        | Recommended | End time               |
| `sourceModule` |          No | Optional module filter |

### Response `200`

```json
{
  "window": {
    "fromUtc": "2026-03-02T00:00:00Z",
    "toUtc": "2026-03-03T00:00:00Z"
  },
  "totals": {
    "auditEvents": 120,
    "highRiskEvents": 8,
    "criticalEvents": 2,
    "failedIngestion": 1,
    "duplicateIngestion": 4
  },
  "byModule": [
    {
      "sourceModule": "Identity",
      "count": 50
    },
    {
      "sourceModule": "Authorization",
      "count": 20
    }
  ],
  "bySeverity": [
    {
      "severity": "Warning",
      "count": 12
    }
  ],
  "byRiskLevel": [
    {
      "riskLevel": "High",
      "count": 8
    }
  ],
  "freshness": {
    "generatedAtUtc": "2026-03-03T00:00:05Z",
    "oldestFailedIngestionAgeSeconds": 120
  }
}
```

### Rules

* V1 computes summary directly from SQL using bounded queries.
* V1 does not require materialized summary tables.
* Summary is derived and may lag.
* Summary must not replace `/logs` as evidence truth.

---

## 6.2 GET `/dashboard/recent-risk-events`

Return recent high-risk or critical audit events.

### Query parameters

| Parameter   | Required | Description               |
| ----------- | -------: | ------------------------- |
| `limit`     |       No | Max items, policy bounded |
| `fromUtc`   |       No | Start time                |
| `toUtc`     |       No | End time                  |
| `riskLevel` |       No | Defaults to high/critical |

### Response `200`

```json
{
  "items": [
    {
      "publicId": "01HY8Q9Z6J9V9V6HE0M77XQK4A",
      "messageId": "01HY8Q9YTVM7W4M6VQVNQ4Q8CY",
      "occurredAtUtc": "2026-03-02T10:30:00Z",
      "sourceModule": "Authorization",
      "action": "RolePermissionGranted",
      "resource": {
        "type": "RolePermission",
        "id": "role-1:permission-9"
      },
      "riskLevel": "Critical",
      "severity": "Warning",
      "summary": "Sensitive permission was granted to role."
    }
  ]
}
```

### Rules

* This is a dashboard panel, not a replacement for detail view.
* Clicking an item should navigate to `/logs/{publicId}`.
* Result size must be bounded.

---

## 7. Deferred Extension APIs

The following APIs are valid future extensions but are not required for V1 core delivery.

They should not be implemented until the business need justifies them.

---

## 7.1 Deferred: Audit Alerts

Possible future endpoints:

```http
GET /api/v1/admin/audit/alerts
GET /api/v1/admin/audit/alerts/{publicId}
POST /api/v1/admin/audit/alerts/{publicId}/acknowledge
POST /api/v1/admin/audit/alerts/{publicId}/resolve
```

Rules:

* `AuditAlert` is derived from evidence or operational state.
* Alert creation must be idempotent.
* Alert dedupe must use durable business keys.
* Audit must not send emails directly.
* Notification delivery should go through Notifications module.

---

## 7.2 Deferred: Audit Digests

Possible future endpoints:

```http
GET /api/v1/admin/audit/digests
GET /api/v1/admin/audit/digests/{publicId}
```

Rules:

* Digests are derived outputs.
* Digest generation should be bounded by time window.
* Digest generation must be rerun-safe.
* Digests must not replace `AuditLog`.

---

## 7.3 Deferred: Completeness and Reconciliation

Possible future endpoints:

```http
GET /api/v1/admin/audit/completeness
GET /api/v1/admin/audit/reconciliation-runs
GET /api/v1/admin/audit/reconciliation-runs/{publicId}
```

Rules:

* Completeness and reconciliation views are derived.
* They must clearly signal freshness and lag.
* They must not be presented as stronger truth than `/logs`.
* They must not infer source business truth from Audit absence alone.

---

## 7.4 Deferred: Replay / Remediation Controls

Possible future endpoints:

```http
POST /api/v1/admin/audit/replay
POST /api/v1/admin/audit/failed-ingestions/{publicId}/replay
```

Rules:

* Not required in V1.
* Must use bounded scope.
* Must preserve original `MessageId`.
* Must be idempotent.
* Must not rewrite existing `AuditLog`.
* Must require strong admin authorization.
* Must be audited itself.

---

## 7.5 Deferred: Archive and Retention

Possible future endpoints:

```http
GET /api/v1/admin/audit/archive-status
GET /api/v1/admin/audit/retention-policy
```

Rules:

* Operational only.
* Must not imply archived evidence is absent unless archival policy explicitly says so.
* Must distinguish primary evidence store from archive outputs.
* Purge/delete behavior requires explicit retention policy.

---

## 7.6 Deferred: Cache or Partition Operations

V1 exposes no cache-control or partition-management APIs for Audit.

Future operational APIs, if introduced, must remain admin-only and must not change evidence truth.

---

## 8. No Evidence Write APIs in V1

Audit writes happen through event ingestion only.

V1 has no admin APIs for:

* inserting audit logs
* updating audit logs
* deleting audit logs
* manually correcting audit logs
* manually creating evidence
* marking evidence as true/false

### Rules

V1 avoids manual evidence mutation endpoints because they would weaken:

* append-only integrity
* `MessageId` identity guarantees
* replay discipline
* remediation discipline
* investigation trust

If a future correction or backfill control endpoint is introduced, it must define:

* append-only correction model
* authorization policy
* audit-of-audit policy
* bounded scope
* replay and idempotency safety
* explicit distinction between canonical evidence and derived remediation output

---

## 9. Authorization and Security

### 9.1 Required authorization

All Audit admin endpoints require explicit authorization.

Recommended permissions:

* `Audit.Read`
* `Audit.ReadSensitive`
* `Audit.ReadIngestion`
* `Audit.ReadDashboard`
* `Audit.ReadOperationalHealth`

Future permissions may include:

* `Audit.Alert.Read`
* `Audit.Alert.Manage`
* `Audit.Replay.Execute`
* `Audit.Retention.Read`
* `Audit.Retention.Manage`

### 9.2 Sanitized payload policy

V1 should avoid exposing raw input payload by default.

AuditLog detail may expose `sanitizedPayloadJson` only after redaction. The raw
input payload from Outbox is an ingestion input, not a default API response field.

If raw input payload exposure is ever introduced:

* require `Audit.ReadSensitive`
* return redacted/sanitized payload only
* never return tokens, secrets, passwords, password hashes, refresh tokens, verification tokens, reset tokens, session cookies, or raw authorization headers

### 9.3 Privacy

Audit APIs must minimize sensitive data.

Responses should prefer:

* summary
* normalized metadata
* actor/resource identifiers
* correlation identifiers
* safe operational fields

over raw event payloads.

---

## 10. Versioning and Conventions

### 10.1 Base path

All endpoints use:

```http
/api/v1/admin/audit/*
```

### 10.2 Naming conventions

Use:

* `publicId` for Audit-owned API identifiers
* `messageId` for Outbox/event identity
* `correlationId` for request/workflow tracing
* `sourceModule` for producer module
* `resourceType` and `resourceId` for affected source resource
* `actorUserId` for stable actor identity when available
* `actorInternalId` only when internal producer-side actor id is needed

Do not use:

* `eventId` as the public API name for Outbox message identity
* internal numeric `AuditLogId` as stable API identifier
* slug as primary resource identity

### 10.3 Standard envelopes

Standard list envelope and standard error envelope apply.

### 10.4 Bounded query rule

List, search, dashboard, and timeline endpoints must remain bounded.

Production endpoints should avoid unbounded full-table scans.

---

## 11. Summary

Audit API Surface V1 provides:

* evidence search
* evidence detail
* lookup by `MessageId`
* lookup by `CorrelationId`
* module-specific investigation
* resource timeline
* actor timeline
* ingestion status visibility
* failed ingestion visibility
* lightweight dashboard summary
* recent high-risk event panel

Audit API Surface V1 does not provide:

* public ingestion APIs
* manual evidence write APIs
* correction APIs
* replay execution APIs
* archive execution APIs
* alert management APIs
* digest APIs
* cache control APIs
* partition management APIs

Future APIs may extend Audit toward alerts, digests, replay, reconciliation, archival, retention, cache acceleration, and partitioning, but those extensions must not change the core rule:

```text
AuditLog is append-only evidence truth.
AuditIngestion is consumer-side processing state.
Everything else is derived or operational.
```
