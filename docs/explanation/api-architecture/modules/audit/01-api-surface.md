# Audit — API Surface (V1)

Audit is primarily an **async ingestion module** and typically runs in the Worker.
It may expose **Admin read APIs** for investigations, compliance support, and operational review.

Base path (Admin): `/api/v1/admin/audit`

> All audit read endpoints require Bearer auth + explicit admin policies.
> Audit ingestion itself is not exposed publicly.
> Audit read APIs expose **canonical evidence truth** and selected **derived operational views** only where explicitly documented.

---

## 1) Audit log read and search (admin)

### GET `/logs`

Search audit logs with paging.

**Query**

* `page`
* `pageSize`
* `from`, `to` (optional time range, ISO-8601)
* `actorUserId` (optional)
* `action` (optional)
* `resourceType` (optional)
* `resourceId` (optional)
* `correlationId` (optional)
* `eventId` (optional)
* `sort` allowlist (default `-occurredAt`)

**Response (200)**

```json
{
  "items": [
    {
      "auditId": "string",
      "occurredAt": "2026-03-02T10:30:00Z",
      "actorUserId": "string",
      "action": "ArticlePublished",
      "resource": {
        "type": "Article",
        "id": "string"
      },
      "correlationId": "string",
      "summary": "string"
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

* Results represent append-only canonical audit evidence.
* Search results may be affected by ingestion lag; admin UI should surface lag or backlog hints when relevant.
* Search must remain privacy-aware and must not expose unsafe raw payloads.
* Filtering, paging, and sorting must remain bounded and predictable for investigations.

### GET `/logs/{auditId}`

Get one audit record (detail view).

**Response (200)**

```json
{
  "auditId": "string",
  "occurredAt": "2026-03-02T10:30:00Z",
  "actorUserId": "string",
  "action": "ArticleUnpublished",
  "resource": {
    "type": "Article",
    "id": "string"
  },
  "correlationId": "string",
  "data": {
    "reason": "PolicyViolation",
    "note": "optional"
  }
}
```

**Rules**

* `data` must be minimal and redacted as needed.
* Never return tokens, secrets, or sensitive PII.
* Detail view returns evidence as recorded, not inferred current business truth.
* If correction-by-new-fact is introduced later, the detail view should make original versus corrective linkage explicit rather than silently merge records.

---

## 2) Investigation endpoints (optional but recommended)

### GET `/logs/by-correlation/{correlationId}`

Return bounded audit records for one correlation flow.

**Purpose**

* Help investigators trace one end-to-end admin or business action across modules.
* Reduce reliance on broad free-text search during incidents.

**Response (200)**

Standard list envelope.

**Rules**

* Results must remain bounded and paged.
* This endpoint groups evidence by shared correlation identifier; it does not claim perfect global chronology.
* It should support investigations without exposing unsafe internals.

### GET `/logs/by-event/{eventId}` (optional)

Return the canonical audit record associated with one upstream event identity.

**Purpose**

Help distinguish:

* true missing evidence
* delayed ingestion
* duplicate delivery already deduped

**Response (200)**

Canonical audit record if present.

**Rules**

* Useful for replay and remediation investigation paths.
* Should return canonical evidence if present; otherwise safe not-found.
* Must not expose replay internals or unsafe processing metadata unless policy explicitly allows it.

---

## 3) Derived operational endpoints (optional)

These endpoints are optional because many teams keep them in dashboards instead of the API surface.

### GET `/completeness` (optional)

Return bounded completeness or reconciliation summary for audit ingestion.

**Example uses**

* show ingest lag
* show mismatch count for governance-critical event scopes
* help admins distinguish missing evidence from lagging ingestion

**Response (200)**

```json
{
  "freshness": {
    "asOf": "2026-03-02T10:30:00Z",
    "lagSeconds": 12
  },
  "summary": {
    "missingCount": 0,
    "delayedCount": 2,
    "mismatchCount": 0
  }
}
```

**Rules**

* This endpoint returns a derived operational view, not canonical evidence truth.
* It must clearly signal freshness and lag.
* It must not be presented as stronger truth than `/logs`.

### GET `/archive-status` (optional)

Return archival and retention workflow status.

**Response (200)**

```json
{
  "primaryStoreAvailable": true,
  "archivedThrough": "2026-02-28T23:59:59Z",
  "archivalLag": "PT10M"
}
```

**Rules**

* Operational only.
* Must not imply archived evidence is absent unless archival policy explicitly says so.
* Should help operators distinguish:

  * evidence available in primary store
  * evidence archived by policy
  * archival lag

---

## 4) Versioning and conventions

* All endpoints use `/api/v1/admin/audit/*`.
* Standard list envelope and standard error envelope apply.
* Read APIs expose either:

  * canonical append-only evidence
  * or explicitly labeled derived operational views
* Query and filter semantics must remain predictable and allow investigation by:

  * actor
  * action
  * resource
  * time range
  * `correlationId`
  * `eventId`, where available

---

## 5) No write APIs (V1)

Audit writes happen through event ingestion only.
There are no admin insert, update, or delete APIs in V1.

**Rules**

* V1 avoids manual evidence mutation endpoints because they would weaken:

  * append-only integrity
  * canonical event identity guarantees
  * replay and remediation discipline
  * investigation trust
* If a future correction or backfill control endpoint is introduced, it must define:

  * append-only correction model
  * authorization and audit-of-audit policy
  * bounded scope
  * replay and idempotency safety
  * explicit distinction between canonical evidence and derived remediation output
