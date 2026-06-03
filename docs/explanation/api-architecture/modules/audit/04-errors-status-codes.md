# Audit — Errors & Status Codes (V1)

## 1. Purpose

This document defines status code usage and error codes for Audit Admin APIs in CommercialNews V1.

Audit APIs are admin-only read/diagnostic APIs over:

* SQL-backed append-only `AuditLog` evidence
* consumer-side `AuditIngestion` processing state
* lightweight derived dashboard views

Audit ingestion itself is not exposed publicly.

Audit V1 does not provide manual write APIs for creating, updating, deleting, correcting, replaying, archiving, or sending audit notifications.

---

## 2. Standard Error Envelope

Audit APIs use the standard API error envelope defined in:

```text
../../02-contracts-and-standards.md
```

Error responses should include the standard fields used by CommercialNews, such as:

* `code`
* `message`
* `traceId`
* `correlationId`
* `details` where safe

Error responses must not expose:

* raw tokens
* passwords
* password hashes
* refresh tokens
* verification tokens
* reset tokens
* raw authorization headers
* session cookies
* unsafe sensitive PII
* raw internal exception stacks
* unsafe event payloads

---

## 3. Status Code Mapping

## 3.1 Common Admin Read APIs

Applies to:

* `GET /api/v1/admin/audit/logs`
* `GET /api/v1/admin/audit/logs/{publicId}`
* `GET /api/v1/admin/audit/logs/by-message/{messageId}`
* `GET /api/v1/admin/audit/logs/by-correlation/{correlationId}`
* `GET /api/v1/admin/audit/modules/{sourceModule}/logs`
* `GET /api/v1/admin/audit/resources/{resourceType}/{resourceId}/timeline`
* `GET /api/v1/admin/audit/actors/{actorUserId}/timeline`
* `GET /api/v1/admin/audit/ingestions`
* `GET /api/v1/admin/audit/ingestions/failed`
* `GET /api/v1/admin/audit/ingestions/by-message/{messageId}`
* `GET /api/v1/admin/audit/dashboard/summary`
* `GET /api/v1/admin/audit/dashboard/recent-risk-events`

| Status | Meaning                                                                                                       |
| -----: | ------------------------------------------------------------------------------------------------------------- |
|  `200` | Request succeeded                                                                                             |
|  `400` | Invalid query, invalid filter, bad time range, invalid sort, invalid page/pageSize, invalid identifier format |
|  `401` | Missing or invalid authentication                                                                             |
|  `403` | Authenticated but policy denied                                                                               |
|  `404` | Requested AuditLog/AuditIngestion not found                                                                   |
|  `409` | Reserved for future write/control APIs; not expected for V1 read APIs                                         |
|  `422` | Valid JSON/request shape but semantically invalid query where policy distinguishes this from `400`            |
|  `429` | Rate limited by admin/API policy                                                                              |
|  `500` | Unexpected server failure                                                                                     |
|  `503` | Dependency unavailable or audit read path temporarily unavailable                                             |

---

## 3.2 `404` Semantics

A `404` from Audit APIs means the requested Audit-owned record or view item was not found.

Important rule:

A missing Audit record does not prove the source business action did not happen.

Examples:

* `GET /logs/by-message/{messageId}` returns `404`

  * the message may not have reached Audit yet
  * the Audit consumer may be lagging
  * the message may have failed before `AuditLog` insertion
  * the source business action may still have committed

* `GET /ingestions/by-message/{messageId}` returns `404`

  * Audit may not have received the message
  * Outbox may not have published it yet
  * RabbitMQ delivery may still be pending
  * the message may be outside retained/visible ingestion scope

Clients should use:

* Outbox operational metrics
* AuditIngestion APIs
* queue lag metrics
* correlation logs

to distinguish lag, failure, and true absence.

---

## 3.3 `503` Semantics

`503` means the Audit API cannot currently serve the request because a required dependency or local service is unavailable.

Examples:

* Audit SQL read path unavailable
* Audit repository timeout beyond policy
* operational dependency unavailable for derived dashboard view

`503` must not imply source module truth failure.

---

## 3.4 Dashboard API Status Codes

Dashboard APIs are derived operational views.

Applies to:

* `GET /dashboard/summary`
* `GET /dashboard/recent-risk-events`

| Status | Meaning                                                  |
| -----: | -------------------------------------------------------- |
|  `200` | Dashboard view generated                                 |
|  `400` | Invalid time range, module filter, risk filter, or limit |
|  `401` | Unauthenticated                                          |
|  `403` | Policy denied                                            |
|  `429` | Rate limited                                             |
|  `500` | Unexpected failure                                       |
|  `503` | Audit SQL/read dependency unavailable                    |

Rules:

* Dashboard failure does not mean audit evidence is lost.
* Dashboard views must not be treated as stronger truth than `AuditLog`.
* Dashboard APIs should fail clearly rather than return misleading partial results as complete summaries.

---

## 3.5 Ingestion API Status Codes

Applies to:

* `GET /ingestions`
* `GET /ingestions/failed`
* `GET /ingestions/by-message/{messageId}`

| Status | Meaning                                                                |
| -----: | ---------------------------------------------------------------------- |
|  `200` | Ingestion status query succeeded                                       |
|  `400` | Invalid filter, status, time range, page/pageSize, or messageId format |
|  `401` | Unauthenticated                                                        |
|  `403` | Policy denied                                                          |
|  `404` | Ingestion record not found                                             |
|  `429` | Rate limited                                                           |
|  `500` | Unexpected failure                                                     |
|  `503` | Audit ingestion store/read dependency unavailable                      |

Rules:

* Ingestion APIs expose consumer-side Audit state.
* Ingestion APIs must not be confused with producer-side Outbox state.
* Ingestion failure does not imply producer command failure.
* Ingestion not-found does not prove the source event was never created.

---

## 4. Validation Rules

Audit read APIs should reject invalid inputs.

Common invalid input cases:

* `page < 1`
* `pageSize <= 0`
* `pageSize` exceeds policy maximum
* `fromUtc > toUtc`
* time range exceeds maximum allowed query window where configured
* invalid `sort` field
* invalid sort direction
* invalid `PublicId` format
* invalid `MessageId` format
* invalid `riskLevel`
* invalid `severity`
* invalid `outcome`
* invalid `status`
* unsupported `sourceModule`
* unsupported `action`
* missing required path values
* malformed date/time values
* attempting to query raw payload without required permission

Recommended status:

* `400` for malformed or invalid query parameters
* `403` for missing permission
* `422` only if the platform uses semantic validation separately from syntactic validation

---

## 5. Error Codes

## 5.1 Common Audit API Error Codes

| Code                              | Meaning                                                                                 |
| --------------------------------- | --------------------------------------------------------------------------------------- |
| `AUDIT.VALIDATION_FAILED`         | Request/query validation failed                                                         |
| `AUDIT.POLICY_DENIED`             | Caller lacks required permission                                                        |
| `AUDIT.LOG_NOT_FOUND`             | AuditLog not found                                                                      |
| `AUDIT.INGESTION_NOT_FOUND`       | AuditIngestion not found                                                                |
| `AUDIT.MESSAGE_NOT_FOUND`         | No AuditLog or AuditIngestion found for the provided `MessageId`, depending on endpoint |
| `AUDIT.CORRELATION_NOT_FOUND`     | No records found for the provided correlation id, where treated as not-found            |
| `AUDIT.INVALID_TIME_RANGE`        | Invalid `fromUtc` / `toUtc` range                                                       |
| `AUDIT.TIME_RANGE_TOO_LARGE`      | Query time range exceeds configured limit                                               |
| `AUDIT.INVALID_PAGE`              | Invalid page value                                                                      |
| `AUDIT.INVALID_PAGE_SIZE`         | Invalid pageSize value                                                                  |
| `AUDIT.PAGE_SIZE_TOO_LARGE`       | pageSize exceeds maximum allowed value                                                  |
| `AUDIT.INVALID_SORT`              | Sort field or direction is not allowlisted                                              |
| `AUDIT.INVALID_FILTER`            | One or more filters are invalid                                                         |
| `AUDIT.INVALID_PUBLIC_ID`         | Invalid Audit-owned `PublicId` format                                                   |
| `AUDIT.INVALID_MESSAGE_ID`        | Invalid `MessageId` format                                                              |
| `AUDIT.INVALID_CORRELATION_ID`    | Invalid correlation id format                                                           |
| `AUDIT.UNSUPPORTED_SOURCE_MODULE` | Source module is not supported or not recognized                                        |
| `AUDIT.UNSUPPORTED_ACTION`        | Action is not supported or not recognized                                               |
| `AUDIT.UNSUPPORTED_STATUS`        | Ingestion status filter is not supported                                                |
| `AUDIT.UNSUPPORTED_RISK_LEVEL`    | RiskLevel filter is not supported                                                       |
| `AUDIT.UNSUPPORTED_SEVERITY`      | Severity filter is not supported                                                        |
| `AUDIT.UNSUPPORTED_OUTCOME`       | Outcome filter is not supported                                                         |
| `AUDIT.READ_FAILED`               | Audit read failed unexpectedly                                                          |
| `AUDIT.DASHBOARD_QUERY_FAILED`    | Dashboard query failed                                                                  |
| `AUDIT.INGESTION_QUERY_FAILED`    | Ingestion query failed                                                                  |
| `AUDIT.DEPENDENCY_UNAVAILABLE`    | Required dependency unavailable                                                         |
| `AUDIT.RATE_LIMITED`              | Request was rate limited                                                                |

---

## 5.2 Sensitive Data and Redaction Error Codes

| Code                              | Meaning                                                   |
| --------------------------------- | --------------------------------------------------------- |
| `AUDIT.REDACTION_VIOLATION`       | Internal redaction policy violation detected              |
| `AUDIT.RAW_PAYLOAD_ACCESS_DENIED` | Caller lacks permission to view sensitive payload details |
| `AUDIT.SENSITIVE_FIELD_BLOCKED`   | Requested field is blocked by audit privacy policy        |
| `AUDIT.PAYLOAD_NOT_AVAILABLE`     | Sanitized payload is not available for this record        |
| `AUDIT.PAYLOAD_REDACTED`          | Payload exists but has been redacted by policy            |

Rules:

* `AUDIT.REDACTION_VIOLATION` is primarily internal/operational.
* Public/admin response should not leak unsafe field names or raw values.
* If raw payload access is not supported in V1, return either:

  * `403 AUDIT.RAW_PAYLOAD_ACCESS_DENIED`, or
  * omit payload fields by design.

---

## 5.3 Ingestion Status Error Codes

These apply to ingestion read APIs and operational views.

| Code                                       | Meaning                                                                                                        |
| ------------------------------------------ | -------------------------------------------------------------------------------------------------------------- |
| `AUDIT.INGESTION_FAILED`                   | Audit consumer processing failed                                                                               |
| `AUDIT.INGESTION_DEADLETTERED`             | Message reached terminal/dead-letter handling                                                                  |
| `AUDIT.INGESTION_DUPLICATE`                | Message was duplicate-safe                                                                                     |
| `AUDIT.INGESTION_IGNORED`                  | Message was intentionally ignored by policy                                                                    |
| `AUDIT.INGESTION_PROCESSING`               | Message is still processing                                                                                    |
| `AUDIT.INGESTION_STATUS_UNKNOWN`           | Ingestion state cannot be determined                                                                           |
| `AUDIT.MESSAGE_SEEN_BUT_LOG_MISSING`       | Audit has seen the message but no AuditLog exists yet                                                          |
| `AUDIT.MESSAGE_PUBLISHED_BUT_NOT_INGESTED` | Operational condition indicating producer-side publish completed but Audit has not ingested yet, if detectable |

These codes are normally used in operational responses, not as generic failure responses for successful read queries.

---

## 5.4 Future Extension Error Codes

The following are reserved for future APIs and should not be required for V1 core delivery.

### Alerting

| Code                               | Meaning                        |
| ---------------------------------- | ------------------------------ |
| `AUDIT.ALERT_NOT_FOUND`            | AuditAlert not found           |
| `AUDIT.ALERT_ALREADY_ACKNOWLEDGED` | Alert already acknowledged     |
| `AUDIT.ALERT_ALREADY_RESOLVED`     | Alert already resolved         |
| `AUDIT.ALERT_INVALID_STATUS`       | Invalid alert status           |
| `AUDIT.ALERT_DEDUPE_CONFLICT`      | Alert business dedupe conflict |

### Replay / Remediation

| Code                               | Meaning                                      |
| ---------------------------------- | -------------------------------------------- |
| `AUDIT.REPLAY_NOT_SUPPORTED`       | Replay API not enabled in V1                 |
| `AUDIT.REPLAY_SCOPE_INVALID`       | Replay scope is invalid                      |
| `AUDIT.REPLAY_SCOPE_TOO_LARGE`     | Replay scope exceeds policy limit            |
| `AUDIT.REPLAY_MESSAGE_ID_REQUIRED` | Replay request requires original `MessageId` |
| `AUDIT.REPLAY_UNSAFE`              | Replay cannot be safely performed            |
| `AUDIT.REPLAY_ALREADY_PROCESSED`   | Replay target already processed              |
| `AUDIT.REPLAY_FAILED`              | Replay attempt failed                        |

### Reconciliation

| Code                                   | Meaning                              |
| -------------------------------------- | ------------------------------------ |
| `AUDIT.RECONCILIATION_NOT_SUPPORTED`   | Reconciliation API not enabled in V1 |
| `AUDIT.RECONCILIATION_SCOPE_INVALID`   | Reconciliation scope is invalid      |
| `AUDIT.RECONCILIATION_SCOPE_TOO_LARGE` | Scope exceeds policy limit           |
| `AUDIT.RECONCILIATION_RUN_NOT_FOUND`   | Reconciliation run not found         |
| `AUDIT.RECONCILIATION_OUTPUT_STALE`    | Reconciliation output is stale       |

### Archive / Retention

| Code                                 | Meaning                               |
| ------------------------------------ | ------------------------------------- |
| `AUDIT.ARCHIVE_NOT_SUPPORTED`        | Archive API not enabled in V1         |
| `AUDIT.ARCHIVE_STATUS_UNAVAILABLE`   | Archive status unavailable            |
| `AUDIT.RETENTION_POLICY_NOT_DEFINED` | Retention policy has not been defined |
| `AUDIT.PURGE_NOT_ALLOWED`            | Purge is not allowed by policy        |
| `AUDIT.PURGE_REQUIRES_APPROVAL`      | Purge requires explicit approval      |

### Cache / Partition

| Code                                      | Meaning                              |
| ----------------------------------------- | ------------------------------------ |
| `AUDIT.CACHE_OPERATION_NOT_SUPPORTED`     | Cache control API not enabled        |
| `AUDIT.PARTITION_OPERATION_NOT_SUPPORTED` | Partition management API not enabled |
| `AUDIT.PARTITION_STATE_UNAVAILABLE`       | Partition state unavailable          |

---

## 6. Status Values

## 6.1 AuditIngestion Status Values

AuditIngestion status values:

* `Processing`
* `Succeeded`
* `Duplicate`
* `Ignored`
* `Failed`
* `DeadLettered`

Meanings:

| Status         | Meaning                                                                       |
| -------------- | ----------------------------------------------------------------------------- |
| `Processing`   | Audit has received and is processing the message                              |
| `Succeeded`    | Audit processed the message and persisted evidence or completed intentionally |
| `Duplicate`    | Message was already represented by existing evidence                          |
| `Ignored`      | Message was intentionally ignored by policy                                   |
| `Failed`       | Audit consumer processing failed and may retry                                |
| `DeadLettered` | Message reached terminal consumer-side failure handling                       |

Rule:

Do not use `Published` as an AuditIngestion status.

`Published` belongs to producer-side `OutboxMessage.Status`.

---

## 6.2 Outcome Values

AuditLog outcome values:

* `Success`
* `Failure`
* `Denied`
* `Ignored`

Meanings:

| Outcome   | Meaning                                                        |
| --------- | -------------------------------------------------------------- |
| `Success` | Source action completed successfully                           |
| `Failure` | Source action failed                                           |
| `Denied`  | Source action was rejected by authorization or business policy |
| `Ignored` | Source event was intentionally ignored or classified as no-op  |

Rule:

`Outcome` describes the source business action.

It is not the same as `AuditIngestion.Status`.

---

## 6.3 Severity Values

AuditLog severity values:

* `Info`
* `Warning`
* `Error`
* `Critical`

Meanings:

| Severity   | Meaning                               |
| ---------- | ------------------------------------- |
| `Info`     | Normal informational evidence         |
| `Warning`  | Action deserves operational attention |
| `Error`    | Failure or error condition occurred   |
| `Critical` | Severe operational or security event  |

---

## 6.4 RiskLevel Values

AuditLog risk level values:

* `Low`
* `Medium`
* `High`
* `Critical`

Meanings:

| RiskLevel  | Meaning                                  |
| ---------- | ---------------------------------------- |
| `Low`      | Low security/business sensitivity        |
| `Medium`   | Moderate sensitivity                     |
| `High`     | High sensitivity                         |
| `Critical` | Critical security/governance sensitivity |

---

## 7. Safe Error Response Rules

### 7.1 Do not leak sensitive payload details

Error messages must not include:

* raw payload
* raw headers
* token values
* password hashes
* stack traces
* SQL text with sensitive values
* internal secrets
* full PII values

### 7.2 Prefer stable, actionable messages

Good:

```json
{
  "code": "AUDIT.INVALID_TIME_RANGE",
  "message": "The audit query time range is invalid.",
  "correlationId": "corr-20260302-001"
}
```

Bad:

```json
{
  "code": "AUDIT.READ_FAILED",
  "message": "SQL failed while reading payload { ... raw data ... }"
}
```

### 7.3 Operational details belong in logs and metrics

Detailed internal diagnostics should go to:

* structured logs
* metrics
* traces
* secure operational dashboards

They should not be exposed in API error responses unless explicitly safe.

---

## 8. No Write API Status Codes in V1

Audit V1 has no admin APIs for:

* inserting audit logs
* updating audit logs
* deleting audit logs
* manually correcting evidence
* manually replaying failed messages
* executing reconciliation
* executing archive/purge
* acknowledging or resolving alerts

Therefore, V1 does not normally use write-specific statuses such as:

* `201 Created`
* `202 Accepted`
* `204 No Content`
* `409 Conflict`

These may be introduced later for future extension APIs.

Future write/control APIs must define:

* authorization policy
* idempotency behavior
* bounded scope
* audit-of-audit behavior
* append-only correction model where evidence is involved
* safe status code mapping

---

## 9. Summary

Audit V1 error/status behavior is governed by these rules:

1. Admin read APIs return `200` on success.
2. Invalid query/filter/sort/paging input returns `400`.
3. Unauthenticated requests return `401`.
4. Policy denial returns `403`.
5. Missing Audit-owned records return `404`.
6. Audit API rate limiting returns `429`.
7. Unexpected server failures return `500`.
8. Temporary dependency/read-path unavailability returns `503`.
9. `MessageId` is the public API name for Outbox/event identity, not `eventId`.
10. `PublicId` is the public API identifier for Audit-owned records.
11. AuditIngestion status is consumer-side state, separate from producer-side Outbox status.
12. Dashboard responses are derived operational views, not canonical evidence truth.
13. Error responses must not expose secrets, unsafe payloads, or internal stack traces.
14. Future replay, alert, archive, cache, and partition APIs are deferred and must define their own write/control status behavior when introduced.
