# Notifications — Runtime Flows (V1)

Supports arc42 scenarios:
- Scenario 4: register + verification email (async)
- Scenario 5: forgot + reset email (async)
- Optional: new article notification after publish

Related:
- `../../../../architecture/arc42/04-runtime-view-v1.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## Runtime posture in V1

Notifications primarily participates in two runtime lanes:

### A) Async side-effect lane
Used for:
- verification email delivery
- reset-password email delivery
- optional publish notification delivery
- retry / DLQ / provider error handling
- durable delivery-state transitions
- provider-throttle-aware sending

### B) Batch / replay / cleanup / summarization lane
Used for:
- replaying failed or stuck deliveries
- bounded resend/remediation workflows
- cleanup of expired delivery artifacts by policy
- delivery summaries and reporting views
- controlled batch sending for bursty notification flows

**Rule:** Notifications never decides whether the originating domain action is valid.  
It executes the delivery side effect after another module has already committed truth.

**Rule:** Notifications success is defined by correct **delivery-state handling**, not by pretending provider ambiguity does not exist.

**Rule:** Notification delivery is assumed **at-least-once** at the event-processing boundary.  
Duplicates, retries, replay, worker restarts, and ambiguous provider outcomes must be tolerated safely.

---

## Flow A — Verification email (non-blocking)

### Goal
Send verification email eventually without blocking registration success.

### Async flow
1. Identity registers user (sync) and emits `UserRegistered` (or `VerificationEmailRequested`).
2. Notifications worker consumes the event.
3. Build outbound message:
   - determine recipient
   - render template with safe variables
   - compute dedupe key
4. Send email via provider.
5. Mark as `Sent` or `Failed`; on failure retry with backoff.
6. After retries exhausted, move to DLQ / `Dead` and alert.

### Runtime stream semantics
- The event is a **delivery intent**, not proof that email already sent.
- Duplicate event delivery must converge to one canonical notification intent.
- Provider timeout is ambiguous:
  - it may have sent
  - it may not have sent
  - Notifications must not assume either without state discipline

### Failure modes
- Provider down: message retries; core register flow already succeeded.
- Duplicate event delivery: dedupe prevents multiple emails.
- Provider timeout ambiguity: do not assume “not sent”; state must remain safe for retry/review.
- Worker crash before final state persistence: replay must remain safe.
- Template/render error: classify deterministically as retryable or terminal by policy.

### Observability notes
- Track:
  - send success/failure by template
  - retry volume
  - dedupe hits
  - provider timeout/error class
  - queue backlog / age
  - ambiguous-provider-outcome count

---

## Flow B — Password reset email (non-blocking)

### Goal
Send reset email eventually while preserving anti-enumeration and duplicate protection.

### Async flow
1. Identity emits `PasswordResetRequested` (sync flow already responded accepted).
2. Notifications consumes the event.
3. Build outbound reset message using safe template variables.
4. Compute message-level and business-intent dedupe identity.
5. Send via provider.
6. Persist resulting delivery state and retry/dead handling as needed.

### Runtime stream semantics
- Reset delivery must preserve business-intent semantics:
  - same canonical reset intent should not fan out duplicate emails accidentally
  - a genuinely new reset intent must still be sendable
- Duplicate delivery and replay are normal operational cases.

### Non-negotiable
- `/forgot-password` remains anti-enumeration and does not reveal existence.
- Duplicate event processing must not create duplicate reset emails for the same canonical reset intent.
- Legitimate new reset intent must remain sendable.

### Failure modes
- Provider down: retries allowed; originating flow remains successful.
- Ambiguous timeout: must not create unsafe blind duplicate resend.
- Invalid template or invalid recipient state: terminal failure / dead classification by policy.
- Replay after later reset intent exists: stale resend must not override newer intent/state incorrectly.

---

## Flow C — New-article notifications (optional, bursty)

### Goal
Send optional publish notifications without overloading provider or blocking Content publish.

### Async flow
1. Content publishes an article (sync).
2. Content emits `ArticlePublished`.
3. Notifications worker determines recipients (policy-driven; V2 likely adds subscriptions).
4. Build delivery intents per recipient.
5. Send emails in controlled batches; monitor backlog and provider limits.
6. Record per-intent delivery state.

### Runtime stream semantics
- `ArticlePublished` is an upstream truth-following event, not delivery truth itself.
- Delivery intent fan-out may be large and bursty.
- Business dedupe must be per canonical send intent:
  - e.g. article + recipient + template/purpose

### Failure modes
- Burst causes backlog growth: must be observable; do not overload provider; throttle.
- Duplicate publish event delivery: business dedupe prevents duplicate sends for the same recipient + article intent.
- Partial batch send failure: successful sends remain recorded; failed sends remain retryable or dead by policy.
- Provider throttling: must be handled explicitly, not as silent queue growth.
- Replay after later campaign state exists: stale resend must not resurrect already-finalized intents incorrectly.

### Rules
- Content publish success never waits for notification completion.
- Bulk/bursty flow must remain bounded and observable.
- Provider throttling must be handled explicitly, not as silent queue growth.
- Safe backlog growth is preferable to unsafe duplicate send storms.

---

## Flow D — Delivery retry / DLQ remediation workflow

### Goal
Recover failed or ambiguous deliveries safely without duplicating already-sent intents.

### Typical workflow shape
1. Select bounded failed/ambiguous/dead delivery set.
2. Re-check current delivery state and canonical business intent key.
3. Rebuild candidate resend/remediation set.
4. Apply retry only where still valid by policy.
5. Persist updated state.
6. Record remediation outcome.

### Runtime rules
- Replay/remediation must be safe on already-sent or deduped intents.
- Retry/remediation decisions must respect current delivery state, not stale operator assumptions.
- A newer terminal state must not be moved backward by stale replay/remediation logic.

### Rules
- Replay/remediation must be safe on already-sent or deduped intents.
- Replay must not override a newer terminal state incorrectly.
- Bounded remediation is preferred over unbounded bulk resend.

### Failure modes
- Ambiguous provider outcome remains unresolved: surface to operators if policy requires.
- Overlapping replay attempts: must remain safe via delivery-state checks and dedupe.
- Stale retry attempt: must not move a newer terminal state backward.
- Duplicate remediation run: must converge safely under dedupe and state checks.

---

## Flow E — Delivery cleanup / retention workflow

### Goal
Clean expired or terminal operational delivery artifacts without weakening investigation capability.

### Typical workflow shape
1. Select bounded delivery records by retention policy.
2. Filter by eligible state/time window.
3. Archive or purge according to policy.
4. Record cleanup outcome.

### Rules
- Cleanup must be bounded.
- Cleanup must not delete artifacts still required for investigation or dedupe policy.
- Cleanup is operational maintenance, not business truth mutation.
- Retention must distinguish:
  - live delivery-state truth
  - replay/remediation-needed artifacts
  - derived summaries/reports
  - expired operational residue

### Failure modes
- Cleanup too aggressive: replay/remediation or investigation capability is weakened.
- Cleanup lag: storage grows, but correctness remains intact.
- Overlapping cleanup and remediation: must remain safe under bounded-input and state checks.

---

## Flow F — Delivery summary / reporting workflow

### Goal
Generate derived summaries for operations or admin reporting.

### Typical workflow shape
1. Select bounded delivery-state input window.
2. Aggregate by template / status / recipient group / time window.
3. Generate candidate summary output.
4. Validate candidate summary.
5. Publish derived summary if policy requires.
6. Cleanup temporary workflow state.

### Typical outputs
- delivery success/failure summaries
- retry/dead counts by template
- provider error-class reports
- operational backlog summaries

### Rules
- These outputs are derived and may lag.
- They must not be mistaken for canonical delivery-state truth.
- Partial candidate summary must not be treated as complete active report state.
- If a full recompute is simpler than fragile incremental summary repair, recompute is preferred.

### Failure modes
- Summary lag: ops/reporting may be behind, but delivery-state truth remains intact.
- Candidate publication failure: previous active summary remains if one exists.
- Stale summary candidate: must not replace fresher published reporting output blindly.

---

## Flow G — Truth-safe delivery-state handling under provider ambiguity

### Goal
Ensure provider ambiguity never creates unsafe duplicate-send or false-success behavior.

### Typical runtime shape
1. Worker attempts provider send.
2. Provider returns:
   - success
   - clear failure
   - timeout / ambiguous outcome
3. Notifications transitions delivery state according to provider result class and policy.
4. Retry/remediation later only if still valid under current state.

### Rules
- Provider timeout is not proof of non-send.
- Provider acknowledgment is not the same as business truth from upstream modules.
- Delivery-state truth belongs to Notifications and must track ambiguity explicitly where needed.
- Safe non-progress or operator review is preferable to unsafe blind resend.

### Examples
- provider timed out after possibly accepting message
- worker crashed after send attempt but before final state persistence
- duplicate event reappears after an ambiguous previous send attempt

---

## Summary

Notifications runtime in V1 is governed by ten rules:

1. Notifications is downstream of originating truth and never blocks core business success.  
2. Duplicate processing must not cause duplicate sends for the same canonical intent.  
3. Provider timeout is ambiguous and must be handled with safe state transitions.  
4. Delivery-state truth is local to Notifications; business truth remains with the originating module.  
5. Async processing is at-least-once; replay, retries, and worker restarts are normal.  
6. Batch workflows support replay, cleanup, burst control, and summaries — not domain truth ownership.  
7. Important replay/remediation workflows must be bounded and rerun-safe.  
8. Partial derived summaries or remediation outputs must not masquerade as complete final state.  
9. Safe backlog growth and safe non-progress are preferable to unsafe duplicate-send behavior.  
10. Provider ambiguity must never silently become false certainty.