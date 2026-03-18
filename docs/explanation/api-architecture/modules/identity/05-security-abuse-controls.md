# Identity — Security & Abuse Controls (V1)

## 1) Authentication baseline
- Access token: JWT (short TTL)
- Refresh token: opaque (long TTL), stored server-side

## 2) Token safety rules
- Never log tokens.
- Never send tokens via query string.
- Reset/verify tokens must be time-bound and single-use by policy.

## 3) Abuse prevention (mandatory)
Rate limit at minimum:
- `/login`, `/refresh`
- `/register`
- `/resend-verification`, `/forgot-password`, `/reset-password`

Mitigations:
- progressive delays (optional)
- lockout policy (optional; ADR hook)

## 4) Safe logging and redaction
- No passwords/tokens/PII in logs.
- Avoid logging raw email in high-cardinality logs if not needed (masking acceptable).

## 5) Security signals to monitor
- login failure spikes
- rate-limit trigger spikes
- resend/forgot bursts
- refresh reuse detection events