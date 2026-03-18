# Media — Security & Abuse Controls (V1)

## 1) Admin-only enforcement
All endpoints require Bearer auth + explicit policies.

## 2) Abuse surface controls (mandatory)
- Enforce allowed file types and size limits (policy-level).
- Sanitize metadata fields; do not trust client-supplied metadata blindly.
- Apply request size limits at edge/gateway.
- Consider malware scanning pipeline (V2+ if required).

## 3) Safe logging
- Do not log raw binary payloads.
- Avoid logging full media URLs if they contain sensitive tokens (prefer paths/ids).
- Log actorUserId, mediaId, articleId, action, correlationId.

## 4) Governance actions auditing
- Attach/detach/set-primary/reorder/delete/restore are governance-sensitive; emit audit events.