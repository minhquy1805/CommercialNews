# Authorization — Security & Abuse Controls (V1)

## 1) Mandatory rules
- All endpoints require Bearer auth.
- All endpoints require explicit admin policies (deny-by-default).
- 100% policy coverage is enforced as a fitness function.

## 2) Least privilege
- Use roles/permissions to grant minimum necessary access.
- Avoid “super endpoints” that do many actions at once without clear policy boundaries.

## 3) Object-level authorization (BOLA)
Where relevant, enforce:
- only authorized admins can modify roles/permissions
- protected system roles cannot be deleted/renamed without elevated policy

## 4) Safe logging
- Do not log sensitive PII (email, tokens).
- Log actorUserId, action, target IDs, correlationId.

## 5) Abuse signals
- spikes in 403 (possible attack or misconfigured policies)
- repeated role/permission mutations (admin misuse)