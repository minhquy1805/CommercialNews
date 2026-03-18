# Reading — Security & Abuse Controls (V1)

## 1) Data exposure rules
- Public endpoints must not expose non-public content.
- Do not expose internal admin metadata or audit fields.

## 2) Abuse posture
- Read endpoints are scrape-prone:
  - enforce paging limits (max pageSize)
  - bounded queries
  - edge protections (rate limiting / caching)
- Monitor spikes in traffic and unusual query patterns.

## 3) Safe logging
- Log correlationId, route, latency, status codes.
- Avoid logging full bodies; avoid high-cardinality data.