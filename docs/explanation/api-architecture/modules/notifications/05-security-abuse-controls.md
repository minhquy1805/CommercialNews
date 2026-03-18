# Notifications — Security & Abuse Controls (V1)

## 1) Non-blocking rule
Notifications must never be a synchronous dependency for:
- register/resend/forgot/reset
- publish/unpublish

## 2) Dedupe and retry safety (mandatory)
- Use a deterministic dedupe key (ADR hook) to prevent duplicates.
- Handle at-least-once delivery from broker safely.

## 3) Template safety (mandatory)
- Templates must use an allowlist of variables.
- Never log raw tokens or secrets.
- Avoid storing full rendered email body unless required; prefer storing metadata only.

## 4) Abuse prevention hooks
- Identity endpoints triggering emails are rate-limited (handled in Identity).
- Notifications must still enforce provider-safe throttling to avoid:
  - account suspension
  - cost spikes
  - queue runaway

## 5) Safe logging and privacy
- Mask or hash email addresses in logs and admin read APIs.
- Never expose reset/verify tokens in admin endpoints.