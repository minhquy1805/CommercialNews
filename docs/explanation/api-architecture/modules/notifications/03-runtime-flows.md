# Notifications — Runtime Flows (V1)

Supports arc42 scenarios:

- Scenario 4: register + verification email (async)
- Scenario 5: forgot + reset email (async)
- optional: new-article notification after publish

Related:

- `../../../../architecture/arc42/04-runtime-view-v1.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0020-timeout-retry-and-failure-detection-policy-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## Runtime posture in V1

Notifications primarily participates in two runtime lanes:

### A) Async side-effect lane

Used for:

- verification email delivery
- password reset email delivery
- optional publish notification delivery
- durable delivery-state transitions
- provider-aware sending
- retry/remediation intake
- bounded failure handling under at-least-once delivery

### B) Batch / replay / cleanup / summarization lane

Used for:

- replaying failed or stuck delivery workflows safely
- bounded remediation workflows
- cleanup of expired operational artifacts by policy
- delivery summaries and reporting views
- controlled batch fan-out for bursty notification flows

### Core runtime rules

**Rule:** Notifications never decides whether the originating domain action is valid.  
It executes delivery work only after another module has already committed truth.

**Rule:** Notifications success is defined by correct **delivery-state handling**, not by pretending provider ambiguity does not exist.

**Rule:** Notification processing is assumed **at-least-once** at the event-processing boundary.  
Duplicates, retries, replay, worker restarts, and ambiguous provider outcomes must be tolerated safely.

**Rule:** Notifications owns **delivery workflow truth**, not upstream business truth.

**Rule:** retry/remediation must remain **bounded, backoff-based, and observable**.  
Hidden infinite looping is not allowed.

---

## Flow A — Verification email (non-blocking)

### Goal

Send verification email eventually without blocking registration success.

### Upstream truth boundary

1. Identity commits registration truth.
2. Identity emits an async event such as:
   - `Auth.EmailVerificationRequested`

Notifications does not participate in the registration truth transaction.

### Async flow

3. Notifications worker consumes the event.
4. Resolve or validate delivery-safe recipient context.
5. Create or load the canonical `EmailDelivery` workflow by `MessageId` and applicable business-intent protection.
6. Evaluate whether delivery is still safe and eligible.
7. Render the verification template using safe variables only.
8. Create an `EmailDeliveryAttempt`.
9. Attempt provider send.
10. Classify provider result:
    - success
    - clear failure
    - ambiguous timeout/outcome
11. Transition delivery state safely:
    - `Sent`
    - retryable failure / retry scheduled
    - terminal-failed / operator-remediation state
12. Persist attempt outcome and updated delivery state.

### Runtime semantics

- The consumed event is a **delivery intent**, not proof that email has already been sent.
- Duplicate event delivery must converge to one canonical delivery workflow.
- Provider timeout is ambiguous:
  - the provider may have sent
  - the provider may not have sent
  - Notifications must not assume either outcome without safe state discipline
- Message-level dedupe is required.
- Business-level duplicate protection is also required where the same outward effect could otherwise happen twice.

### Failure modes

- provider unavailable:
  - retryable failure
  - registration truth already succeeded
- duplicate event delivery:
  - must converge safely through workflow state + dedupe
- provider timeout ambiguity:
  - must not trigger unsafe blind resend
- worker crash after provider attempt but before final persistence:
  - replay must remain safe
- template/render failure:
  - classify deterministically as retryable or terminal according to policy
- stale remediation:
  - must not move a newer terminal state backward

### Observability notes

Track at minimum:

- delivery success/failure by template
- retry volume
- dedupe hits
- provider timeout/error class
- outbox pending count / oldest pending age
- broker queue depth
- auth-critical delivery lag
- ambiguous-provider-outcome count

---

## Flow B — Password reset email (non-blocking)

### Goal

Send reset email eventually while preserving anti-enumeration and duplicate protection.

### Upstream truth boundary

1. Identity accepts the reset request according to anti-enumeration policy.
2. Identity emits an async event such as:
   - `Auth.PasswordResetRequested`

Notifications does not determine whether the endpoint response should reveal account existence.

### Async flow

3. Notifications consumes the event.
4. Resolve delivery-safe recipient context.
5. Create or load the canonical `EmailDelivery` workflow.
6. Evaluate both:
   - message-level identity (`MessageId`)
   - business-intent safety for the reset intent
7. Render the reset template using safe variables only.
8. Create an `EmailDeliveryAttempt`.
9. Attempt provider send.
10. Persist attempt outcome and transition delivery state safely.

### Runtime semantics

- Reset delivery must preserve business-intent semantics:
  - the same canonical reset intent must not accidentally fan out duplicate emails
  - a genuinely new reset intent must still remain sendable
- Duplicate delivery and replay are normal operational cases.
- Timeout ambiguity must not be treated as proof that no reset mail was sent.

### Non-negotiables

- `/forgot-password` remains anti-enumeration and does not reveal account existence.
- Duplicate processing must not create duplicate reset emails for the same protected reset intent.
- Legitimate new reset intent must still remain sendable.
- Replay of an older reset intent must not override or confuse a newer valid reset workflow.

### Failure modes

- provider unavailable:
  - retry allowed
  - originating flow already succeeded
- ambiguous provider timeout:
  - must not create unsafe same-intent replay
- invalid template or invalid recipient state:
  - terminal failure/remediation state by policy
- replay after later reset intent exists:
  - stale resend/retry must not override newer intent/state incorrectly

### Observability notes

Track at minimum:

- reset-delivery success/failure
- retry volume
- dedupe hits
- same-intent reject count where measurable
- provider timeout/error class
- auth-critical delivery lag
- terminal-failed/remediation-needed counts

---

## Flow C — New-article notifications (optional, bursty)

### Goal

Send optional publish notifications without overloading provider or blocking Content publish.

### Upstream truth boundary

1. Content commits publication truth.
2. Content emits:
   - `Content.ArticlePublished`

Notifications follows publication truth; it does not define it.

### Async flow

3. Notifications worker consumes the event.
4. Determine recipients according to policy.
   - V1 may use a limited/static/policy-driven recipient model
   - V2 may introduce subscriptions/preferences
5. For each canonical recipient intent:
   - create or load `EmailDelivery`
   - apply business-intent dedupe
6. Render the appropriate notification template.
7. Send in bounded, provider-aware batches.
8. Record per-intent attempt outcome and delivery state.
9. Continue draining backlog according to priority/throttle policy.

### Runtime semantics

- `Content.ArticlePublished` is upstream truth-following, not delivery truth.
- Recipient fan-out may be large and bursty.
- Business dedupe must be per canonical send intent, for example:
  - article + recipient + template/purpose
- Bulk delivery safety is more important than fast uncontrolled fan-out.

### Failure modes

- burst backlog growth:
  - must be observable
  - must not overload provider
- duplicate publish event delivery:
  - business dedupe prevents duplicate sends for the same recipient/article intent
- partial batch failure:
  - successful sends remain recorded
  - failed sends remain retryable or terminal by policy
- provider throttling:
  - must be handled explicitly
  - must not degrade into silent queue growth
- replay after later campaign/policy state exists:
  - stale replay must not resurrect already-finalized intents incorrectly

### Runtime rules

- Content publish success never waits for notification completion.
- Bulk/bursty flow must remain bounded and observable.
- Safe backlog growth is preferable to unsafe duplicate-send storms.
- Lower-priority content notifications must not starve auth-critical notification traffic.

### Observability notes

Track at minimum:

- batch throughput
- backlog growth and drain time
- provider throttle signals
- duplicate-intent preventions
- per-template success/failure rates
- queue depth and age by priority lane where implemented

---

## Flow D — Delivery retry / remediation workflow

### Goal

Recover failed or ambiguous delivery workflows safely without duplicating already-completed harmful outward effects.

### Typical workflow shape

1. Select a **bounded** set of retry-eligible or remediation-eligible deliveries.
2. Re-check current delivery workflow state.
3. Re-check canonical business-intent protection.
4. Rebuild a candidate retry/remediation set.
5. Apply retry only where still valid under current state and policy.
6. Persist updated state.
7. Record remediation outcome.

### Runtime rules

- Retry is not the same as resend.
- Replay is not the same as resend.
- Same-intent replay must remain policy-controlled.
- Remediation decisions must respect **current delivery state**, not stale operator assumptions.
- A newer terminal state must not be moved backward by stale retry/remediation logic.
- If safe forward progress cannot be established, remediation must:
  - defer
  - reject
  - or surface for operator review
  rather than force delivery unsafely

### Failure modes

- ambiguous provider outcome remains unresolved:
  - surface to operators if policy requires
- overlapping remediation attempts:
  - must remain safe via workflow state + dedupe checks
- stale retry attempt:
  - must not move a newer terminal state backward
- duplicate remediation run:
  - must converge safely under idempotent logic

### Observability notes

Track at minimum:

- retry/remediation accepted count
- retry/remediation rejected count
- operator-remediation-needed count
- stale-state reject count
- convergence failures or repeated dead-state resurfacing

---

## Flow E — Delivery cleanup / retention workflow

### Goal

Clean expired or terminal operational delivery artifacts without weakening investigation or dedupe capability prematurely.

### Typical workflow shape

1. Select a **bounded** delivery artifact set by retention policy.
2. Filter by eligible state and time window.
3. Archive, redact, or purge according to policy.
4. Record cleanup outcome.

### Runtime rules

- Cleanup must be bounded.
- Cleanup must not remove artifacts still needed for:
  - investigation
  - dedupe policy
  - replay/remediation policy
  - compliance/ops retention obligations
- Cleanup is operational maintenance, not business-truth mutation.
- Retention must distinguish between:
  - active delivery workflow truth
  - retry/remediation-needed artifacts
  - derived summaries/reports
  - expired operational residue

### Failure modes

- cleanup too aggressive:
  - replay/remediation or investigation capability is weakened
- cleanup lag:
  - storage grows, but correctness remains intact
- overlapping cleanup and remediation:
  - must remain safe under bounded-input and state checks

### Observability notes

Track at minimum:

- cleanup volume
- cleanup lag
- artifacts retained for remediation
- redaction/purge failures where applicable

---

## Flow F — Delivery summary / reporting workflow

### Goal

Generate derived summaries for operations or admin reporting.

### Typical workflow shape

1. Select a **bounded** delivery-state input window.
2. Aggregate by template, status, recipient group, provider class, or time window.
3. Generate a candidate summary output.
4. Validate candidate summary.
5. Publish derived summary if policy requires.
6. Clean up temporary workflow state.

### Typical outputs

- delivery success/failure summaries
- retry/dead counts by template
- provider error-class reports
- operational backlog summaries
- delivery lag summaries for auth-critical flows

### Runtime rules

- These outputs are **derived** and may lag.
- They must not be mistaken for canonical delivery-workflow truth.
- Partial candidate summary must not be treated as complete active report state.
- If a full recompute is simpler and safer than fragile incremental repair, recompute is preferred.

### Failure modes

- summary lag:
  - ops/reporting may be behind
  - delivery truth remains intact
- candidate publication failure:
  - previous active summary remains if one exists
- stale summary candidate:
  - must not replace fresher reporting output blindly

### Observability notes

Track at minimum:

- summary generation duration
- summary lag/freshness age
- candidate publish success/failure
- recompute frequency
- fallback to previous active summary where applicable

---

## Flow G — Truth-safe delivery-state handling under provider ambiguity

### Goal

Ensure provider ambiguity never creates unsafe duplicate-send or false-certainty behavior.

### Typical runtime shape

1. Worker attempts provider send for an `EmailDeliveryAttempt`.
2. Provider returns one of:
   - success
   - clear failure
   - timeout / ambiguous outcome
3. Notifications classifies the result according to provider and policy semantics.
4. Notifications updates delivery-state truth safely.
5. Retry/remediation may happen later only if still valid under current state.

### Runtime rules

- Provider timeout is **not** proof of non-send.
- Provider acknowledgment is **not** the same as upstream business truth.
- Delivery-state truth belongs to Notifications and must track ambiguity safely where needed.
- Safe non-progress or operator review is preferable to unsafe blind resend.
- The system must avoid manufacturing false certainty from ambiguous provider behavior.

### Typical ambiguity examples

- provider timed out after possibly accepting the message
- worker crashed after send attempt but before final state persistence
- duplicate event reappears after an ambiguous previous attempt
- provider returned an unclear or partially classified error

### Expected safe outcomes

Depending on policy:

- retain retryable but not blindly re-send
- mark as remediation-needed
- defer for operator review
- allow bounded later retry only if state still supports safe forward progress

### Observability notes

Track at minimum:

- ambiguous outcome count
- provider timeout rate
- operator review queue size where applicable
- post-ambiguity retry decisions
- eventual convergence of ambiguous workflows

---

## Summary

Notifications runtime in V1 is governed by the following rules:

1. Notifications is downstream of originating truth and never blocks core business success.  
2. Duplicate processing must not cause duplicate harmful outward effects for the same protected delivery intent.  
3. Provider timeout is ambiguous and must be handled with safe delivery-state transitions.  
4. Delivery-workflow truth is local to Notifications; business truth remains with the originating module.  
5. Async processing is at-least-once; replay, retries, and worker restarts are normal.  
6. Batch workflows support replay, cleanup, burst control, reporting, and remediation — not business-truth ownership.  
7. Important retry/remediation workflows must be bounded and rerun-safe.  
8. Partial derived summaries or remediation outputs must not masquerade as complete final state.  
9. Safe backlog growth and safe non-progress are preferable to unsafe duplicate-send behavior.  
10. Provider ambiguity must never silently become false certainty.  
11. Retry, replay, and resend are different concepts and must not be conflated.  
12. Auth-critical notification flows must remain protected from starvation under bursty lower-priority traffic.