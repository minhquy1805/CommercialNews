# Identity — Open Questions & ADR Hooks (V1)

## ADR-01: Verification gating rules
- What actions require verified email in V1?
- Do we allow login before verification? If yes, what is restricted?

## ADR-02: Refresh token reuse detection response
- Revoke token family vs revoke all tokens for user?
- Do we notify user/admin on suspected theft?

## ADR-03: Logout semantics
- Support `mode=Current` only or also `mode=All`?

## ADR-04: Register conflict behavior
- Anti-enumeration response vs explicit `409 EMAIL_EXISTS`?

## ADR-05: Lockout policy
- Do we lock accounts on repeated failures? For how long?