# Identity — Errors & Status Codes (V1)

## 1) Standard error envelope
See: `../../02-contracts-and-standards.md`

---

## 2) Status code mapping

- `201 Created` — register success
- `200 OK` — all other successful operations
- `400 Bad Request` — validation errors, invalid token input, expired token, already-used token, malformed request
- `401 Unauthorized` — invalid credentials, invalid/expired access token, unauthenticated caller
- `403 Forbidden` — authenticated caller denied by policy or gating rules
- `409 Conflict` — optional, if used for deterministic conflict cases such as `EMAIL_EXISTS` under chosen policy
- `429 Too Many Requests` — rate-limited
- `500 Internal Server Error` / `503 Service Unavailable` — unexpected failures or dependency-level service failure

**Rule:** downstream email delivery failure must not be surfaced as Identity API failure for register, resend-verification, or forgot-password acceptance flows.

---

## 3) Canonical error codes (examples)

- `IDENTITY.VALIDATION_FAILED`
- `IDENTITY.UNAUTHENTICATED`
- `IDENTITY.POLICY_DENIED`
- `IDENTITY.INVALID_CREDENTIALS`
- `IDENTITY.ACCOUNT_LOCKED` *(if applicable)*
- `IDENTITY.TOKEN_INVALID`
- `IDENTITY.TOKEN_EXPIRED`
- `IDENTITY.TOKEN_ALREADY_USED`
- `IDENTITY.REFRESH_REUSE_DETECTED`
- `IDENTITY.PASSWORD_POLICY_VIOLATION`
- `IDENTITY.RATE_LIMITED`
- `IDENTITY.EMAIL_EXISTS` *(only if chosen by policy for that surface)*

---

## 4) Anti-enumeration rules (mandatory)

- `/forgot-password` → always `200 { "accepted": true }`
- `/resend-verification` → always `200 { "accepted": true }`

These endpoints must not reveal whether the account exists.

---

## 5) Email already exists on register (policy decision)

Two acceptable approaches:

- **A (privacy-first):** return a safe response that does not confirm existence
- **B (explicit conflict):** return `409 IDENTITY.EMAIL_EXISTS`

Whichever approach is chosen must be recorded in an ADR and implemented consistently across:
- controller behavior
- service logic
- logs/telemetry
- documentation

---

## 6) Rules summary

- `401` is for unauthenticated/invalid-auth situations.
- `403` is for authenticated callers denied by policy/gating.
- Token failures should be deterministic where possible:
  - invalid
  - expired
  - already used
- Rate-limited endpoints must return stable rate-limit behavior.
- Identity error results must be based on Identity truth, not on downstream notification or audit completion.