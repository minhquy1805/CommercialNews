# Reading Experience (Public) — Characteristic Profile (V1)

This profile captures the **top architecture characteristics** for the public reading experience
(listing, detail, related, and basic search), derived from requirements and domain concerns.

---

## Top characteristics (3–5)

1) **Performance (Read Path)** (Operational)  
2) **Availability (Read Path)** (Operational)  
3) **Scalability (Bursty Traffic)** (Operational)  
4) **Correctness (Visibility & Semantics)** (Cross-cutting)  
5) **Robustness (Graceful Degradation)** (Operational)

> Notes:
> - Reading is the primary product experience and traffic driver.
> - This module is where “hot article spikes” will stress the system first.

---

## Why it matters (domain-driven)

- Public reads must be **fast**; slow pages immediately reduce retention and trust.
- Read results must be **correct**: draft/unpublished content must never be publicly visible.
- Sorting/filter semantics must be consistent; “popularity” must have a clear definition.
- The system must remain readable even when non-critical subsystems are delayed or failing.

---

## Design implications (what this forces in design)

### Performance (Read Path)
- Keep listing/detail queries bounded in complexity (avoid chatty or multi-join patterns by default).
- Optimize for percentiles (P95/P99), not averages.
- Apply caching where it provides clear value (policy-driven).

### Availability (Read Path)
- Reading endpoints should remain online even if async subsystems (audit, notifications, interaction aggregation) are degraded.
- Avoid hard dependencies from read endpoints into slow/non-critical paths.

### Scalability (Bursty Traffic)
- Expect short spikes with high concurrency; the architecture must absorb bursts without collapse.
- Plan an evolution path from V1 query facade to V2 read models/projections when needed.

### Correctness (Visibility & Semantics)
- Enforce publication state filtering everywhere (listing/detail/search must only show public content).
- Define canonical rules for filters/sorts and keep behavior stable over time.
- Related articles logic must be deterministic, with explicit fallback behavior.

### Robustness (Graceful Degradation)
- Degrade gracefully if non-critical data is missing or delayed:
  - interaction counters may be stale or temporarily unavailable
  - related articles may fall back to simpler logic
- Fail safely: never expose draft/unpublished content in degraded modes.

---

## Suggested measures (policy-level)

- **Latency (read endpoints):** P95/P99 for listing and detail.
- **Availability/SLO:** uptime and error rate (timeouts/5xx) for read endpoints.
- **Degradation success:** ability to keep reading functional when non-critical subsystems fail.
- **Correctness checks:** zero tolerance for public exposure of draft/unpublished content.

---

## Key trade-offs

- Strong performance and burst absorption may require caching and (later) projections, increasing complexity.
- Strict correctness filters can add query constraints but prevent severe governance incidents.
- Graceful degradation improves availability but requires careful fallback design and testing.

---

## ADR candidates (from this profile)

- Definition of “popularity” (views vs likes vs blended) and when it becomes authoritative.
- Search capability level (basic keyword search vs full-text) and upgrade path.
- Read model strategy (when to introduce projections and what triggers the migration).