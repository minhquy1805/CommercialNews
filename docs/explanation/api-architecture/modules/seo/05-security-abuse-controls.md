# SEO — Security & Abuse Controls (V1)

## 1) Public endpoint safety
- `/resolve` and `/metadata` are public read endpoints.
- Must return safe not-found and never expose draft/unpublished content.

## 2) Admin endpoint protection
- All `/api/v1/admin/seo/*` endpoints require:
  - Bearer auth
  - explicit policies (deny-by-default)

## 3) Abuse posture
- `/resolve` can be scraped or hammered:
  - rely on caching and edge protections
  - enforce bounded query patterns
  - monitor abnormal traffic

## 4) Safe logging
- Do not log full URLs with sensitive query strings (avoid token-like patterns).
- Log scope/slug carefully (slugs are public identifiers; ok to log at low risk).