# Notifications — Observability & SLO Signals (V1)

Related:
- `../../../../architecture/arc42/04-runtime-view-v1.md`
- `../../../../architecture/arc42/11-replication-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 1) Core SLIs (must measure)

### Delivery health
- send success rate (by template)
- send failure rate (by template and error class)
- delivery latency distribution:
  - `queuedAt → sentAt` (P50/P95/P99)
- retry volume (count and rate)
- backlog/lag in queue:
  - broker queue depth (ready/unacked)
  - consumer processing latency (P95/P99)
- DLQ size and growth
- **DLQ oldest age** (time since oldest message in DLQ)

### Replication freshness (Outbox → broker → Notifications consumer)
- outbox pending count for notification-triggering events
- outbox oldest pending age (seconds)
- publish failure rate (outbox → broker), if tracked

Operational expectation:
- emails are eventually consistent, but backlog/age must remain bounded under normal load.

### Interpretation rule
Observability must distinguish:
- upstream intent creation is healthy but delivery is lagging
- queueing is healthy but provider is failing
- provider is healthy but dedupe/state transitions are blocking progress
- delivery-state truth is healthy but summaries/reports are behind

---

## 2) Provider health signals

- provider error codes (sanitized, no PII/tokens)
- throttling events / rate-limit responses
- timeouts / transient network failures
- provider latency (request → response) (P95/P99)

Classification (must track):
- transient errors (retryable)
- permanent errors (non-retryable: invalid recipient, template invalid, etc.)
- ambiguous outcomes (if modeled explicitly)

### Provider interpretation rule
Operators should be able to tell whether:
- failures are mostly retryable
- failures are mostly permanent/domain-invalid
- provider ambiguity is increasing
- latency regression is coming from provider call time or from queue backlog before send attempt

---

## 3) Duplicate prevention signals

### Message-level dedupe
- dedupe hits count (same `MessageId/EventId` redelivered)
- unique-key conflicts (if delivery log enforces unique MessageKey)

### Intent-level dedupe (must measure for real duplicates)
Track duplicates by business intent key (policy-defined per template), e.g.:
- verify email: `(TemplateKey, RecipientUserId, TokenHash/TokenId)`
- reset password: `(TemplateKey, RecipientUserId, TokenHash/TokenId)`
- publish notification: `(TemplateKey, RecipientUserId, ArticleId, PublishedVersion/PublishedAt)`

Signals:
- unexpected duplicate sends per intent (should trend to zero)
- ratio of `sent` vs `deduped/no-op` outcomes
- spikes in dedupe hits (can indicate retry storms or broker redelivery loops)

### Stale-attempt protection signals
- stale-attempt/state-transition reject count
- retries ignored because newer terminal state already exists
- replay candidate rejected because intent already converged
- ambiguous-provider-outcome recheck count before resend

### Policy
Duplicate-prevention signals are not just “nice to have.”
They are core correctness signals for Notifications.

---

## 4) Identity / content workflow impact signals

### Upstream demand signals
- spikes in resend verification triggers
- spikes in forgot password triggers
- spikes in optional publish notification demand
- correlation between rate-limit triggers and email backlog growth

### User experience proxies (optional)
- verification completion rate over time window (send → verify)
- reset completion rate over time window (send → reset)

These help distinguish:
- provider outage vs abuse spikes vs product UX issues

### Cross-module interpretation rule
Operators should be able to tell whether:
- upstream demand genuinely increased
- upstream is retrying too aggressively
- the problem is in Notifications execution
- the issue is product-side (users not completing flows) rather than delivery-side

---

## 5) Replay / remediation / cleanup workflow signals

These apply when Notifications runs bounded workflows for:
- replay
- remediation
- cleanup
- burst-controlled sending
- delivery summaries / reporting

### Workflow health SLIs
- run success/failure count
- run duration
- records selected / processed / retried / skipped / dead
- replay candidate count
- stale-delivery / ambiguous-delivery remediation count
- cleanup count
- summary freshness age
- candidate output generation success/failure
- publication/cutover success/failure for important derived reports

### Replay / recovery indicators
- replay count
- rerun count
- repeated stuck-intent count across runs
- candidate-built-but-not-applied count
- already-converged intent skip count
- full remediation run chosen vs bounded selective retry count

### Ownership-sensitive workflow signals (if introduced later)
- duplicate-run detection count
- stale-owner rejection count
- safe no-owner / degraded intervals

### Policy
Canonical delivery-state truth remains authoritative even when these workflows lag or fail.
Derived summaries and remediation outputs may lag, but must not be mistaken for canonical delivery truth.

### Recovery interpretation rule
Observability should make it clear whether:
- bounded replay is helping
- remediation is stuck on the same ambiguous intents repeatedly
- cleanup is safe and policy-compliant
- summary/report lag is separate from actual delivery-state convergence

---

## 6) Release gates (rollout policy)

During rollout, watch and gate on:
- failure rate spike (overall and per template)
- sustained P99 delivery latency regression (`queuedAt → sentAt`)
- backlog growth trend:
  - queue depth sustained increase
  - outbox oldest pending age sustained increase
- DLQ non-zero and increasing, or DLQ oldest age breach
- spike in unexpected duplicate sends (intent-level) (release blocker)
- remediation/replay failure spikes for stuck delivery flows
- candidate publication/cutover failure for important derived delivery reports
- spike in ambiguous provider outcomes without safe state convergence

### Strong stop conditions
Immediate pause/rollback is recommended if:
- unexpected duplicate sends occur at meaningful volume
- stale retry/remediation behavior is overriding newer delivery truth
- backlog grows without recovery and threatens major user-facing flows
- provider ambiguity spikes and resend logic is no longer clearly safe

---

## 7) Operator questions this module must answer

Notifications observability should help answer:

1. Is the issue in upstream event flow, queue backlog, worker processing, or provider delivery?  
2. Are duplicates coming from redelivery, retry storms, or incorrect business-intent modeling?  
3. Are failures transient, permanent, or ambiguous?  
4. Is remediation/replay helping convergence, or are we repeatedly reprocessing the same stuck intents?  
5. Is the problem in canonical delivery state, or only in lagging summaries/reports?  
6. Are stale retries being rejected correctly, or are older attempts still mutating state unsafely?  
7. Is the system degraded-but-safe, or has it crossed into duplicate-send / unsafe-remediation risk?