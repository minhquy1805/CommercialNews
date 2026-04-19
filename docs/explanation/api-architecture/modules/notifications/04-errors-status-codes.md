# Notifications — Errors & Status Codes (V1)

## 1) Standard error envelope

Notifications uses the standard platform error envelope.

See:

- `../../02-contracts-and-standards.md`

Example:

```json
{
  "traceId": "string",
  "error": {
    "code": "NOTIFICATIONS.DELIVERY_NOT_FOUND",
    "message": "Email delivery was not found.",
    "details": []
  }
}
```

## 2) Error posture in V1

Notifications is an operations-facing async side-effect module.

Therefore, its API errors describe:

- delivery workflow state
- operator action validity
- policy denial
- operational visibility failures

They do **not** define:

- whether registration truth succeeded
- whether password reset truth is valid
- whether article publication truth committed
- whether governance truth is effective

**Rule:** a Notifications API failure must not be misrepresented as upstream business-truth failure.

## 3) Status codes — admin read APIs

These apply to endpoints such as:

- `GET /api/v1/admin/notifications/email-deliveries`
- `GET /api/v1/admin/notifications/email-deliveries/{messageId}`
- `GET /api/v1/admin/notifications/email-deliveries/{messageId}/attempts` (if implemented)

### 200 OK

Used when the read succeeds.

Examples:

- list returned successfully
- delivery detail returned successfully
- attempt history returned successfully

### 400 Bad Request

Used when the request is structurally invalid or uses unsupported filter/sort/query semantics.

Examples:

- invalid paging values
- malformed datetime range
- unsupported status filter
- unsupported sort field
- invalid `messageId` format

### 401 Unauthorized

Used when the caller is unauthenticated.

### 403 Forbidden

Used when the caller is authenticated but not allowed by policy.

### 404 Not Found

Used when the requested delivery workflow or subordinate resource does not exist.

Examples:

- `messageId` does not map to a known delivery workflow
- attempts requested for a non-existent workflow

### 429 Too Many Requests

Used only if operator-facing throttling/rate-limit policy is applied.

### 500 Internal Server Error

Used for unexpected application-level failures.

### 503 Service Unavailable

Used when the service or a critical dependency is temporarily unavailable and the read cannot be safely completed.

## 4) Status codes — admin operational APIs

These apply to endpoints such as:

- `POST /api/v1/admin/notifications/email-deliveries/{messageId}:retry`
- `POST /api/v1/admin/notifications/email-deliveries/{messageId}:cancel` (if implemented)
- `POST /api/v1/admin/notifications/suppressions/{recipientHash}:remove` (only if implemented in V1)

### 202 Accepted

Preferred when the operational action is accepted for asynchronous handling.

Examples:

- retry request accepted
- cancellation request accepted
- suppression removal accepted

**Important:** `202 Accepted` means operational intake succeeded.  
It does **not** mean final delivery success or final remediation success.

### 400 Bad Request

Used when the request body, path value, or action input is invalid.

Examples:

- malformed `messageId`
- invalid operator reason payload
- invalid suppression identifier format

### 401 Unauthorized

Used when the caller is unauthenticated.

### 403 Forbidden

Used when the caller lacks the required operational/admin policy.

### 404 Not Found

Used when the target delivery workflow or operational target does not exist.

Examples:

- delivery workflow not found
- suppression entry not found (if such capability exists)

### 409 Conflict

Used when the requested action is not valid for the current workflow state or current policy state.

Examples:

- retry requested for already-sent delivery
- retry requested for a non-retryable delivery
- cancel requested for a terminal workflow
- suppression removal requested when current state disallows removal

### 429 Too Many Requests

Used when operator actions are rate-limited or throttled by policy.

### 500 Internal Server Error

Used for unexpected application-level failures.

### 503 Service Unavailable

Used when the service or a critical dependency is temporarily unavailable and the action cannot even be safely accepted.

## 5) Canonical error codes (V1)

The following codes are recommended canonical examples for Notifications V1.

### Validation / request errors

- `NOTIFICATIONS.VALIDATION_FAILED`
- `NOTIFICATIONS.INVALID_QUERY`
- `NOTIFICATIONS.INVALID_MESSAGE_ID`
- `NOTIFICATIONS.INVALID_REQUEST`

### Auth / policy errors

- `NOTIFICATIONS.UNAUTHENTICATED`
- `NOTIFICATIONS.POLICY_DENIED`

### Read / lookup errors

- `NOTIFICATIONS.DELIVERY_NOT_FOUND`
- `NOTIFICATIONS.ATTEMPT_HISTORY_NOT_FOUND`  
  Optional; use only if the implementation distinguishes this from delivery not found.
- `NOTIFICATIONS.SUPPRESSION_NOT_FOUND`  
  Only if suppression capability exists.

### State / workflow errors

- `NOTIFICATIONS.INVALID_DELIVERY_STATE`
- `NOTIFICATIONS.DELIVERY_NOT_RETRYABLE`
- `NOTIFICATIONS.DELIVERY_ALREADY_SENT`
- `NOTIFICATIONS.RETRY_NOT_ALLOWED`
- `NOTIFICATIONS.CANCEL_NOT_ALLOWED`
- `NOTIFICATIONS.OPERATION_NOT_SUPPORTED`

### Provider / integration / runtime errors

- `NOTIFICATIONS.PROVIDER_FAILURE`
- `NOTIFICATIONS.PROVIDER_TIMEOUT`
- `NOTIFICATIONS.DEPENDENCY_UNAVAILABLE`
- `NOTIFICATIONS.REMEDIATION_ACCEPTANCE_FAILED`

### Rate limiting / abuse / operational protection

- `NOTIFICATIONS.RATE_LIMITED`

## 6) Error code usage guidance

### 6.1 Stable module-level codes

Error codes must be stable at the module contract level.

Do:

- return `NOTIFICATIONS.PROVIDER_FAILURE`

Do not:

- leak raw provider-specific codes/messages as the main API contract

Provider-specific details may be logged or attached to internal diagnostics only if policy allows and sanitization is applied.

### 6.2 Delivery-truth errors only

Notifications API errors should describe delivery workflow truth and operator action validity.

Examples of correct Notifications errors:

- delivery not found
- retry not allowed
- invalid delivery state
- provider failure during operational processing

Examples of incorrect Notifications errors:

- user not verified
- password reset token invalid
- article not published
- role assignment invalid

Those belong to upstream truth-owning modules.

### 6.3 Conflict vs validation

Use `400 Bad Request` when:

- the request itself is malformed or invalid

Use `409 Conflict` when:

- the request shape is valid, but the current delivery workflow state or current policy state makes the action invalid

Examples:

- invalid `messageId` format → `400`
- valid `messageId`, but retry not allowed because status is `Sent` → `409`

### 6.4 Accepted does not mean completed

For operational async actions:

- `202 Accepted` means the request has been accepted for operational processing
- it does **not** guarantee:
  - final delivery success
  - retry success
  - suppression removal completion in downstream systems
  - provider acceptance

## 7) Privacy and sanitization rules

Notifications errors must follow strict privacy and safety rules.

### Must not expose

- raw verification/reset secrets
- unsafe provider payloads
- stack traces with sensitive values
- raw recipient addresses where masking/redaction is required
- internal transport or infrastructure details that create security or privacy risk

### Must sanitize

- provider error messages
- provider error codes when surfaced indirectly
- delivery failure reasons shown to operators
- recipient search/display values where policy requires masking

**Rule:** operational usefulness is required, but privacy and safe disclosure take priority over raw diagnostic detail.

## 8) Suggested response examples

### 8.1 Delivery not found

**Status:** `404 Not Found`

```json
{
  "traceId": "string",
  "error": {
    "code": "NOTIFICATIONS.DELIVERY_NOT_FOUND",
    "message": "Email delivery was not found.",
    "details": []
  }
}
```

### 8.2 Retry not allowed

**Status:** `409 Conflict`

```json
{
  "traceId": "string",
  "error": {
    "code": "NOTIFICATIONS.DELIVERY_NOT_RETRYABLE",
    "message": "The delivery is not eligible for retry in its current state.",
    "details": []
  }
}
```

### 8.3 Policy denied

**Status:** `403 Forbidden`

```json
{
  "traceId": "string",
  "error": {
    "code": "NOTIFICATIONS.POLICY_DENIED",
    "message": "You do not have permission to perform this action.",
    "details": []
  }
}
```

### 8.4 Provider failure (sanitized)

**Status:** `503 Service Unavailable` or `500 Internal Server Error` depending on policy and failure class

```json
{
  "traceId": "string",
  "error": {
    "code": "NOTIFICATIONS.PROVIDER_FAILURE",
    "message": "The notification provider could not complete the operation.",
    "details": []
  }
}
```

## 9) Rules summary

- Notifications errors describe delivery workflow truth and operator action validity only.
- Notifications errors must not redefine or impersonate upstream business truth.
- Read APIs use standard read-oriented status codes; operational APIs additionally use `202` and `409` where appropriate.
- `202 Accepted` means accepted for asynchronous operational handling, not completed successfully.
- Provider/runtime details must be sanitized and must not leak secrets or unsafe payloads.
- `400` is for malformed/invalid requests; `409` is for valid requests that conflict with current delivery/policy state.
- Error codes must remain stable even if provider implementation changes.