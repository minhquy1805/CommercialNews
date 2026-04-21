# Identity — Security & Abuse Controls (V1)

## 1) Authentication baseline

- Access token: JWT (short TTL)
- Refresh token: opaque (long TTL), stored server-side

## 2) Token safety rules

- Never log tokens.
- Never send tokens via query string.
- Reset/verify tokens must be time-bound and single-use by policy.
- Raw verification/reset tokens must never appear in async event payloads or outbox payloads.
- Token-bearing data must be disclosed only at the minimum level required for the user-facing flow.

## 3) Abuse prevention (mandatory)

Rate limit at minimum:

- `/login`, `/refresh`
- `/register`
- `/resend-verification`, `/forgot-password`, `/reset-password`

Mitigations:

- progressive delays *(optional)*
- lockout policy *(optional; ADR hook)*
- idempotency support for retry-prone flows such as:
  - `/register`
  - `/resend-verification`
  - `/forgot-password`

## 4) Anti-enumeration and duplicate-safety

- `/forgot-password` must remain anti-enumeration-safe.
- `/resend-verification` must remain anti-enumeration-safe.
- Abuse-prone flows must remain safe under retries and duplicate client submissions.
- A valid resend/reset intent must not be blocked by stale delivery artifacts from downstream systems.

## 5) Safe logging and redaction

- No passwords, tokens, or unsafe PII in logs.
- Avoid logging raw email in high-cardinality logs if not needed; masking/redaction is preferred.
- Provider/downstream notification details must not leak secrets or weaken security posture.

## 6) Security truth vs downstream lag

- Identity security truth is authoritative.
- Notification delay/failure must not weaken verification, reset, refresh, or revocation truth.
- Timeout ambiguity must be reconciled from Identity truth, not from delivery visibility.

## 7) Security signals to monitor

- login failure spikes
- rate-limit trigger spikes
- resend/forgot bursts
- refresh reuse detection events
- verify/reset token invalid/expired/already-used spikes
- verification/reset delivery lag for auth-critical flows
- unusual refresh/token-rotation failure patterns