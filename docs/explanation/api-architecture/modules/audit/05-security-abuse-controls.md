# Audit — Security & Abuse Controls (V1)

## 1) Access control (admin read)
- All `/api/v1/admin/audit/*` endpoints require explicit policies.
- Least privilege: only specific roles should read audit logs.

## 2) Privacy and redaction (non-negotiable)
- Never store or return:
  - passwords
  - tokens (access/refresh/reset/verify)
  - secrets
- Minimize PII in `Data` (prefer UserId over email).
- Apply redaction consistently per event type.

## 3) Abuse posture
- Audit search can be expensive:
  - enforce paging limits
  - allowlist sort fields
  - require time range for high-volume searches (policy option)
  - rate-limit audit search endpoints

## 4) Safe logging
- Audit system logs must not echo raw event payloads blindly.
- Log eventId/eventType/correlationId and mapping outcomes safely.