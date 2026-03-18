# Interaction (Views / Likes / Comments) — Characteristic Profile (V1)

This profile captures the **top architecture characteristics** for the Interaction module,
derived from its explicit/inferred requirements and domain concerns.

---

## Top characteristics (3–5)

1) **Scalability (Hot Path / Burst Handling)** (Operational)  
2) **Performance (Non-blocking Read Path)** (Operational)  
3) **Robustness (Abuse & Partial Failures)** (Operational)  
4) **Correctness (Idempotency & Counter Integrity)** (Cross-cutting)  
5) **Observability (Hot Feature Telemetry)** (Cross-cutting)

> Notes:
> - Interaction traffic is bursty and abuse-prone.
> - This module must never become a bottleneck for the public reading experience.

---

## Why it matters (domain-driven)

- Views and likes are **high-volume** and spike during “hot article” events.
- If interaction processing blocks reads, the primary user experience degrades immediately.
- Like/unlike must be idempotent; counters must stay consistent under retries and concurrency.
- Comments are governance-sensitive: the system must be able to remove/hide abusive content (V2 moderation).

---

## Design implications (what this forces in design)

### Scalability (Hot Path / Burst Handling)
- Assume spikes and short bursts; design for high event throughput.
- Prefer patterns that absorb burst without collapsing under contention.

### Performance (Non-blocking Read Path)
- View tracking must be **non-blocking** for read endpoints (policy expectation).
- Avoid designs where reads depend on synchronous counter writes.

### Robustness (Abuse & Partial Failures)
- Abuse controls must exist or be pluggable (rate limiting hooks, especially for comments).
- The module must tolerate partial failures and retries without corrupting state.
- Fail safely: if interaction subsystems are delayed, reading still works.

### Correctness (Idempotency & Counter Integrity)
- Like/unlike operations must be idempotent (retries must not double-apply).
- Totals must remain consistent even under concurrent activity.
- Define “view semantics” explicitly (V1 simple counter; V2 unique-view policy with privacy considerations).

### Observability (Hot Feature Telemetry)
- Track interaction rates and failure patterns to detect spikes and abuse.
- Maintain visibility into backlog/lag (if async) and error rates.

---

## Suggested measures (policy-level)

- **Read path impact:** interaction processing does not increase read endpoint latency beyond acceptable bounds.
- **Idempotency violations:** duplicate likes/views causing inconsistent totals (target: near-zero).
- **Abuse signals:** rate-limited requests / spam attempts (tracked).
- **Throughput/backlog:** interaction event rate and backlog/lag (if asynchronous processing is used).

---

## Key trade-offs

- Making interaction non-blocking improves read performance but can introduce eventual consistency (stale counters).
- Strong anti-abuse controls reduce risk but add friction and operational overhead.
- Strict correctness under high concurrency may require additional state management and careful design.

---

## ADR candidates (from this profile)

- Unique view strategy (V2) and privacy implications.
- Comment moderation model (visibility states) and enforcement boundaries.
- Consistency model for counters (synchronous accuracy vs eventual consistency).