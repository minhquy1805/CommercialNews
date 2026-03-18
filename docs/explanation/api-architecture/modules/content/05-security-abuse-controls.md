# Content — Security & Abuse Controls (V1)

## 1) Mandatory authorization
- All endpoints under `/api/v1/admin/content/*` require:
  - Bearer auth
  - explicit policies (deny-by-default)

## 2) Governance boundaries
Publish/unpublish/archive/restore/delete actions:
- must be auditable
- must validate transitions
- must record required reason fields (unpublish)

## 3) Mass-assignment defense
Server owns:
- `Status`, `AuthorUserId`, `PublishedAt`, timestamps
Clients must not set these directly.

## 4) Safe logging
Never log:
- full article body content in high-cardinality logs
Log:
- actorUserId, action, articleId, correlationId

## 5) Abuse posture
Admin endpoints still require protection against:
- compromised admin tokens
- automation loops
Monitor:
- spikes in publish/unpublish attempts
- unusual error patterns (403/409)