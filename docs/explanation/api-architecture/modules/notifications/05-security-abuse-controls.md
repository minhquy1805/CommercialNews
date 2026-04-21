# Notifications — Security & Abuse Controls (V1)

## 1) Purpose and security posture

Notifications is an async side-effect module.

Its security and abuse-control posture exists to ensure that notification delivery:

- does not become a blocking dependency for upstream truth-owning modules
- does not leak secrets, sensitive recipient data, or unsafe provider details
- does not create duplicate or runaway delivery behavior under retry, replay, or broker redelivery
- does not allow abuse patterns that cause account suspension, cost spikes, queue exhaustion, or operator confusion

**Rule:** Notifications protects the delivery pipeline and operational visibility surface.  
It does **not** own upstream business truth such as registration success, password reset validity, article publication truth, or authorization truth.

---

## 2) Non-blocking rule (mandatory)

Notifications must never be a synchronous success dependency for upstream truth-owning operations such as:

- register
- resend verification
- forgot password
- reset password
- publish
- unpublish

This means:

- upstream modules may emit notification work as a post-commit side effect
- upstream API success must not depend on provider acceptance or delivery completion
- notification delay, retry, degradation, or temporary outage must not retroactively redefine committed upstream truth

Examples:

- user registration truth may succeed even if email delivery is delayed
- password reset initiation may succeed even if provider submission is retried later
- article publication truth may succeed even if publication email notifications lag

**Rule:** notification outcome is not the authoritative source of upstream business success.

---

## 3) Module boundary ownership

### Upstream truth-owning modules own

- registration truth
- password reset truth
- article publication truth
- authorization/governance truth
- public endpoint rate limiting for user-triggered actions

### Notifications owns

- delivery workflow truth
- provider submission and retry behavior
- dedupe and replay safety for notification processing
- operational throttling and provider protection
- safe rendering and safe logging
- delivery-state visibility for operators/admins

**Rule:** Notifications failures must be expressed as delivery workflow or operational failures only.  
They must not impersonate upstream domain failures.

---

## 4) Dedupe and retry safety (mandatory)

Notifications must be safe under:

- broker at-least-once delivery
- producer retry after timeout ambiguity
- consumer retry after transient failure
- operator retry of remediation actions
- provider callback or submission ambiguity

### 4.1 Deterministic dedupe

A deterministic dedupe key must be used for each logical notification workflow according to the system ADR/contract.

Typical inputs may include:

- upstream event identity
- template or notification type
- recipient identity representation
- tenant or scope identifier
- business correlation key

The exact composition is implementation-defined, but the result must be stable for the same logical notification intent.

### 4.2 Duplicate prevention rule

The system must prevent duplicate visible sends for the same logical notification intent when the workflow has already been accepted, completed, or safely determined to be non-repeatable.

This applies across:

- enqueue/publish
- broker redelivery
- consumer execution
- provider submission retry where reconciliation is possible

### 4.3 Retry safety rule

Retry behavior must be bounded, policy-driven, and safe.

Requirements:

- transient failures may be retried
- terminal failures must not be retried indefinitely
- retry count and retry spacing must be bounded
- poison or repeatedly failing messages must be isolated from the main flow

### 4.4 Timeout ambiguity

When timeout ambiguity occurs, the system must reconcile from notification workflow truth where possible rather than assuming failure or blindly resending.

**Rule:** ambiguity must not default to duplicate sends.

---

## 5) Queue, DLQ, and poison-message safety

Notifications must protect the delivery pipeline from runaway retry and queue instability.

### Requirements

- repeated failures must not create unbounded retry storms
- poison messages must be isolated using bounded retry and dead-letter handling
- dead-lettered items must remain visible for operator remediation where supported
- queue backlog thresholds should trigger alerting and operational visibility
- retry policies must distinguish transient provider failure from invalid message state where possible

### Operational intent

The goal is to preserve:

- queue health
- provider safety
- cost control
- operator ability to diagnose and remediate failures

**Rule:** one bad message or one failing provider must not destabilize the full notification pipeline.

---

## 6) Provider protection and abuse prevention

Notifications must enforce downstream delivery protection even when upstream modules already perform end-user throttling.

### 6.1 Upstream throttling

Identity and other public-facing modules may already rate-limit user-triggered operations such as:

- register
- resend verification
- forgot password
- reset password

That protection remains upstream-owned.

### 6.2 Notifications-side protection

Notifications must still enforce provider-safe and system-safe controls to avoid:

- account suspension by the provider
- delivery bursts beyond provider policy
- cost spikes
- queue runaway
- retry storms
- operational flood conditions
- repeated duplicate sends caused by automation or replay

### 6.3 Allowed protection strategies

Implementation may use:

- per-template or per-channel throttling
- recipient-scoped suppression or cooldown
- provider-scoped rate controls
- circuit breaking or temporary intake degradation
- batch/backpressure strategies
- policy-based rejection or deferral of non-critical sends

### 6.4 Boundary rule

Notifications may delay, shed, or safely reject notification intake according to policy.  
However, this must not redefine upstream truth outcomes once upstream truth has already committed.

---

## 7) Template and rendering safety (mandatory)

Notification rendering is a security-sensitive operation.

### 7.1 Trusted template rule

Only trusted, approved templates may be rendered.

The system must not allow:

- arbitrary template execution
- untrusted runtime template injection
- unrestricted operator-defined template logic without policy and review
- dynamic variable expansion outside the approved contract

### 7.2 Allowlisted variables

Templates must use an explicit allowlist of supported variables.

Rules:

- unsupported variables must be rejected or ignored deterministically
- variables must be bound by known contract
- template rendering must not depend on arbitrary object traversal or unsafe reflection-based expansion

### 7.3 Secret safety

The system must never log, surface, or operationally expose raw secrets such as:

- reset tokens
- verification tokens
- one-time secrets
- raw signed links if policy forbids exposure
- provider credentials

### 7.4 Injection and escaping safety

Any user-controlled or upstream-supplied content inserted into templates must be sanitized and escaped according to the delivery channel.

Examples:

- HTML email content must be safely escaped/sanitized as required
- subject lines must avoid unsafe control characters or injection patterns
- text rendering must not permit template-breaking payloads

**Rule:** template correctness is not enough; rendering must also be safe.

---

## 8) Storage minimization and payload handling

Notifications should minimize sensitive payload retention.

### Requirements

- avoid storing the full rendered email body unless there is a clear approved operational need
- prefer storing metadata, template identifiers, workflow state, correlation identifiers, timestamps, and sanitized delivery diagnostics
- do not retain raw secrets in durable workflow records
- do not persist unsafe provider payloads without explicit policy approval and sanitization controls

### Goal

Reduce:

- privacy risk
- breach impact
- accidental operator exposure
- noisy or unsafe diagnostics

---

## 9) Logging, privacy, and sanitization

Notifications logs and admin/operator read surfaces must follow strict privacy rules.

### 9.1 Must not expose

- raw reset or verification tokens
- unsafe provider payloads
- raw secrets
- stack traces with sensitive values
- full recipient addresses where masking or hashing is required by policy
- internal infrastructure details that create security or privacy risk

### 9.2 Must sanitize

- provider error messages
- provider error codes when surfaced indirectly
- delivery failure reasons presented to operators
- recipient identifiers shown in admin read APIs
- correlation-linked diagnostic content if it may reveal secrets

### 9.3 Masking and hashing rule

Recipient identities in logs and admin APIs should be:

- masked
- hashed
- partially revealed only if policy explicitly allows it

Admin visibility is **not** a justification for exposing raw secrets or raw recipient data by default.

### 9.4 Correlation rule

Safe correlation identifiers may be exposed for troubleshooting.  
Secrets may not.

**Rule:** operational usefulness matters, but privacy and safe disclosure take priority over raw diagnostic detail.

---

## 10) Admin and operator surface safety

Admin/operations endpoints must be protected because they expose delivery workflow state and remediation capabilities.

Requirements:

- all admin/operator endpoints require authentication and explicit policy authorization
- remediation actions such as retry/cancel/remove-suppression must be auditable
- operator actions must respect workflow-state validity
- operational APIs must not reveal sensitive upstream secrets
- admin read APIs must use sanitized recipient and provider-facing data

### Operational state rule

A valid operator action depends on current delivery workflow truth.

Examples:

- retry may be allowed only for retryable workflow states
- cancel may be allowed only for non-terminal workflow states
- suppression removal may be restricted by current policy state

---

## 11) Provider isolation and failure containment

Notifications may depend on third-party providers, but provider behavior must not break stable platform contracts.

### Requirements

- provider-specific failures must not leak raw provider payloads to API clients
- provider outages must degrade notification delivery, not upstream truth ownership
- provider implementation changes must not change stable module-level error semantics
- provider-specific diagnostics may be logged internally only if sanitized and policy-allowed

### Contract rule

Public module behavior should remain stable even if:

- the provider changes
- the provider partially degrades
- retry policy evolves
- fallback routing changes internally

---

## 12) Audit and observability expectations

Security and abuse controls require observability.

The system should emit useful signals for:

- duplicate prevention hits
- retry counts and retry exhaustion
- dead-letter transitions
- queue backlog growth
- provider throttling or timeout patterns
- suppression activity
- unusual send burst patterns
- high failure concentration by template, provider, tenant, or recipient scope

These signals support:

- abuse detection
- operational recovery
- cost control
- provider safety
- capacity planning

**Rule:** observability must support safe operations without exposing secrets.

---

## 13) Recommended abuse signals

Useful abuse/anomaly signals include:

- bursty repeated sends to the same recipient scope
- unusually high dedupe-hit rates
- repeated retries for the same workflow beyond normal transient behavior
- spikes in provider timeout/failure classes
- queue growth inconsistent with expected business traffic
- repetitive resend/reset trigger patterns from the same actor or address family
- abnormal operator retry behavior
- spikes in suppression creation or suppression removal requests

These signals may be used for:

- alerting
- temporary throttling
- circuit breaking
- operator review
- forensic investigation

---

## 14) Rules summary

- Notifications is an async side-effect module and must not become a synchronous dependency for upstream truth success.
- Notifications owns delivery workflow truth, not registration truth, password-reset truth, publication truth, or authorization truth.
- Deterministic dedupe is mandatory to prevent duplicate visible sends under retry, replay, and at-least-once broker delivery.
- Retry behavior must be bounded, policy-driven, and safe; poison messages must be isolated.
- Notifications must enforce downstream throttling and provider protection even if upstream modules already rate-limit end-user actions.
- Only trusted templates and allowlisted variables may be rendered.
- Raw tokens, secrets, unsafe provider payloads, and raw recipient identifiers must not be exposed.
- Logs and admin APIs must use masking, hashing, and sanitization according to policy.
- Provider failures must degrade delivery behavior without redefining upstream business truth.
- Observability must support abuse detection and operational recovery without leaking sensitive information.