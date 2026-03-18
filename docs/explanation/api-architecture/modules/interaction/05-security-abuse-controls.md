# Interaction — Security & Abuse Controls (V1)

## 1) Non-blocking read path rule
Interaction must never block Reading. If Interaction fails, Reading still works.

## 2) Abuse prevention (mandatory hooks)
- Rate limit:
  - view endpoint (to reduce bot floods)
  - comment create/edit
  - like/unlike (per user)
- Consider CAPTCHA or additional friction in V2 for spam bursts.

## 3) Object-level authorization (BOLA)
- Only comment author can edit/delete by default.
- Admin/moderator overrides are V2 governance.

## 4) Privacy considerations
- IP/UserAgent collection is optional and must follow privacy rules.
- Unique view strategy (V2) requires privacy ADR.

## 5) Safe logging
- Avoid logging raw comment content in request logs.
- Log ids + actorUserId + correlationId.