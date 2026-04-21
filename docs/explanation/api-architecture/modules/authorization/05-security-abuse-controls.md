# Authorization — Security & Abuse Controls (V1)

## 1) Mandatory rules

- All endpoints require Bearer authentication.
- All endpoints require explicit admin authorization policies.
- Authorization posture is deny-by-default.
- 100% policy coverage over `/api/v1/admin/*` is enforced as a fitness function.
- Client identity alone is never sufficient; permission/policy enforcement is mandatory.
- Security-sensitive authorization uncertainty must fail closed or fall back to authoritative truth.

## 2) Least privilege

- Roles and permissions must grant the minimum necessary access.
- Avoid overly broad “super endpoints” that combine multiple governance actions without clear policy boundaries.
- Protected system roles and permissions must be guarded by stronger policy where required.
- Optional downstream convenience layers must not widen effective authority beyond committed governance truth.

## 3) Object-level authorization and protected governance state

Where relevant, enforce:

- only authorized admins can create, update, activate, deactivate, assign, grant, or revoke governance state
- protected system roles cannot be renamed, deactivated, or deleted without elevated policy
- protected system permissions cannot be renamed, deactivated, or deleted without elevated policy
- object-level checks must compose with centralized authorization, not replace it

Rules:

- stale cache/materialized state must not authorize governance mutation
- replayed or delayed derived state must not resurrect revoked authority
- governance truth must remain authoritative on critical/admin paths

## 4) Safe logging and disclosure

- Do not log raw sensitive data unnecessarily.
- Do not log tokens or unsafe secret-bearing payloads.
- Avoid logging raw PII unless operationally justified and policy-approved.
- Prefer logging:
  - `actorUserId`
  - action name
  - target identifiers
  - `correlationId`
  - stable outcome classification
- Error responses and logs must not leak protected governance internals unnecessarily.

## 5) Abuse and misuse protection

Authorization admin endpoints must be protected against:

- automation loops
- retry storms
- accidental repeated governance mutations
- admin token misuse
- bulk mutation mistakes

Recommended controls:

- rate limiting or throttling by policy
- anomaly detection for repeated equivalent writes
- alerting on unusual protected-object mutation attempts
- explicit review/audit for high-risk governance actions where policy requires it

## 6) Truth-safe evaluation under derived lag

- Authorization truth is authoritative for governance decisions.
- Cache/materialized views are acceleration only.
- If derived state is stale, missing, or uncertain on a security-sensitive path:
  - fall back to authoritative truth
  - or fail closed
- Downstream audit, cache invalidation, reporting, or optional notification lag must not weaken committed governance truth.

## 7) Abuse signals to monitor

- spikes in `403` *(possible attack, stale permissions, or misconfigured policy)*
- spikes in `409` *(governance conflicts, protected invariant violations, or automation mistakes)*
- repeated role/permission mutations against the same targets
- unusual no-op/idempotent-hit rates
- repeated protected system role/permission mutation attempts
- fail-closed events caused by stale or unavailable derived evaluation inputs
- retry storms or bursty governance writes from automation