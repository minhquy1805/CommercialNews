# Interaction — Observability & SLO Signals (V1)

This document defines Interaction observability and SLO signals for V1.
It focuses on truth-path health, moderation/report operations, Content-derived eligibility projection, view-count materialization, Reading counter publication, async alert delivery posture, and recovery diagnostics.

Related:

- `01-api-surface.md`
- `02-domain-contracts.md`
- `03-runtime-flows.md`
- `04-errors-status-codes.md`
- `05-security-abuse-controls.md`
- `06-idempotency-consistency.md`
- `08-dependencies-and-ownership.md`
- `09-open-questions.md`
- `10-business-rules.md`
- ADR-0013 — Outbox & Delivery Semantics (V1)
- ADR-0018 — Transaction Boundaries & Consistency Model (V1)
- ADR-0019 — System Model and Fault Assumptions (V1)
- ADR-0020 — Timeout, Retry, and Failure Detection Policy (V1)
- ADR-0022 — Versioning and Fencing Strategy (V1)
- ADR-0025 — Batch Processing and Derived State Policy (V1)
- ADR-0027 — Stream Processing and Derived State Policy (V1)
- ADR-0028 — Consumer Idempotency, Replay, and Rebuild Policy (V1)

---

## 1) Observability Posture

Interaction owns user-engagement and moderation workflow truth while participating in asynchronous projection and side-effect flows.

Observability must distinguish four different health dimensions:

```text
1. Interaction truth/workflow mutation health
2. Derived-state freshness and publication health
3. Async consumer/delivery health
4. Abuse, authorization and moderation-risk signals
```

### 1.1 Truth-path health

Truth-path health covers:

```text
ArticleLike
Comment
CommentReport
CommentModerationCase
CommentModerationActionHistory
```

Failures here affect authoritative Interaction behavior and must be treated as correctness-sensitive.

### 1.2 Derived-state health

Derived-state health covers:

```text
ArticleInteractionTargetProjection
ArticleViewCount
ArticleInteractionStats
Reading-applied counter snapshots
```

Failures or lag here may result in:

- temporarily unavailable new interaction due to fail-closed eligibility;
- stale public counters;
- delayed counter convergence.

Derived-state lag does not automatically imply truth corruption.

### 1.3 Async side-effect health

Async side-effect health covers:

```text
Interaction -> Audit moderation evidence
Interaction -> Notifications admin-alert intent
Interaction -> Reading public counter snapshot
Content -> Interaction eligibility projection
```

Downstream lag may be acceptable temporarily, but must be measurable and bounded operationally.

### 1.4 Core interpretation rule

```text
Operators must be able to distinguish:
- truth mutation failure;
- safe projection/counter lag;
- intentional fail-closed behavior;
- abuse-control rejection;
- downstream side-effect delay;
- real correctness drift requiring repair.
```

---

## 2) SLO Classification

Interaction V1 does not treat every operation with the same SLO severity.

### 2.1 Correctness-critical operations

The following operations are truth/workflow critical:

```text
Like / Unlike
Create Comment
Delete Own Comment
Approve / Reject / Hide / Restore Comment
Create Comment Report
Dismiss / Action Comment Moderation Case
Trigger one-time admin-alert intent
```

For these operations, key concerns are:

- transaction commit success;
- idempotency safety;
- version/conflict correctness;
- no duplicate business effects;
- outbox intent committed when required.

### 2.2 Availability-sensitive but non-blocking operation

```text
Track Article View
```

View tracking must be fast and observable, but failure must not break public article reading.

### 2.3 Derived convergence operations

```text
Article interaction counter materialization
Counter snapshot publication for Reading
Eligibility projection consumption
Counter reconciliation/repair
```

These operations may lag, but prolonged lag or repeated failure must be visible.

### 2.4 Asynchronous notification/evidence operations

```text
Admin alert email delivery through Notifications
Canonical moderation evidence ingestion through Audit
```

Interaction observes event intent publication and downstream lag indicators where exposed, while Notifications and Audit own their internal delivery/persistence SLOs.

---

## 3) Signal Naming Guidance

The metric names below are recommended logical names. Final Prometheus names may follow the project's shared naming convention.

Recommended labels should be bounded and low-cardinality:

```text
operation
outcome
status
event_type
consumer_name
apply_decision
reason_code only when allowlisted and bounded
severity only when allowlisted and bounded
```

Avoid high-cardinality metric labels such as:

```text
ArticlePublicId
CommentPublicId
CasePublicId
UserId
CorrelationId
MessageId
Raw IP
Raw UserAgent
Comment content
Report description
Moderator note
```

Those identifiers belong in structured logs or traces where access is controlled.

---

## 4) API Truth-Path SLIs

### 4.1 General command signals

Every authoritative Interaction command should expose:

```text
interaction_command_requests_total{operation,outcome}
interaction_command_duration_seconds{operation}
interaction_command_failures_total{operation,error_code}
interaction_command_timeouts_total{operation}
```

Recommended operations:

```text
like
unlike
create_comment
delete_own_comment
report_comment
approve_comment
reject_comment
hide_comment
restore_comment
dismiss_moderation_case
hide_reported_comment
```

### 4.2 Interpretation

| Signal pattern | Interpretation |
|---|---|
| Rising `5xx` for truth commands | Possible correctness/availability failure |
| Rising `409` on admin mutations | Possible stale admin UI, concurrent handling or workflow issue |
| Rising command timeout with stable commit success later | Client/network ambiguity or latency issue |
| Stable truth commands but stale public counters | Derived/publication lag, not truth failure |

### 4.3 Suggested alert categories

| Severity | Condition type |
|---|---|
| Critical | Sustained truth-command failure or evidence of duplicate/corrupt truth |
| High | Sustained latency/timeouts on moderation/report flows |
| Medium | Elevated conflicts, rate-limits or temporary dependency degradation |
| Informational | Expected idempotent duplicate/no-op activity within baseline |

---

## 5) Article Eligibility Projection Signals

### 5.1 Why this is critical

Interaction only accepts new public interactions when local Content-derived eligibility is safe.

If this projection lags or becomes uncertain, Interaction must fail closed. That is safe behavior, but excessive frequency harms user experience and signals async convergence problems.

### 5.2 Required metrics

```text
interaction_eligibility_messages_received_total{event_type}
interaction_eligibility_messages_applied_total
interaction_eligibility_messages_duplicate_ignored_total
interaction_eligibility_messages_stale_ignored_total
interaction_eligibility_gap_detected_total
interaction_eligibility_resync_required_total
interaction_eligibility_apply_failures_total

interaction_eligibility_projection_lag_seconds
interaction_eligibility_projection_missing_requests_total{operation}
interaction_eligibility_fail_closed_total{operation,reason}
```

Suggested bounded `reason` values:

```text
projection_missing
projection_requires_resync
projection_unsafe
source_gap_detected
```

### 5.3 Operational questions answered

Operators must be able to determine:

- Are new interaction requests failing because the article is genuinely non-public?
- Are they failing because Content projection has not arrived yet?
- Is a consumer stalled?
- Are stale/duplicate messages being safely ignored?
- Are projection version gaps forcing repeated fail-closed behavior?

### 5.4 Important interpretation rule

```text
A rise in fail-closed requests with healthy Interaction DB writes
usually indicates eligibility-projection freshness trouble,
not corruption of like/comment/report truth.
```

---

## 6) View-Count Signals

### 6.1 V1 view posture

Interaction V1 stores:

```text
ArticleViewCount
```

It does not retain a durable raw row for each view.

The view endpoint may acknowledge a request without exposing whether anti-abuse policy counted it as a new contribution.

### 6.2 Required metrics

```text
interaction_view_requests_total{outcome}
interaction_view_request_duration_seconds
interaction_view_requests_rate_limited_total
interaction_view_requests_eligibility_rejected_total
interaction_view_requests_temporarily_unavailable_total

interaction_view_contributions_applied_total
interaction_view_contributions_suppressed_total{reason}
interaction_view_atomic_increment_failures_total
interaction_view_count_updates_total
```

Suggested bounded suppression reasons:

```text
repeat_window
abuse_policy
malformed_signal
rate_limit
```

### 6.3 Required interpretation distinction

Do not merge these two concepts:

| Signal | Meaning |
|---|---|
| View request accepted by endpoint | Request was handled under public endpoint contract |
| View contribution applied | `ArticleViewCount` was actually incremented |

This distinction is required because client responses must not expose detailed anti-abuse decisions.

### 6.4 Anomaly signals

Operators should observe:

```text
interaction_view_velocity_by_article_anomaly_total
interaction_view_suppression_ratio
interaction_view_rate_limit_ratio
interaction_view_count_update_failure_ratio
```

Article-specific anomaly investigation may use logs or analytics queries rather than high-cardinality metric labels.

### 6.5 Reading isolation guardrail

Correlate view endpoint degradation with public Reading behavior:

```text
View endpoint failure/latency spike
must not correspond to public article-read failure/latency spike.
```

If view handling materially degrades public article serving, treat it as an architectural regression and release blocker.

---

## 7) Like / Unlike Signals

### 7.1 Required metrics

```text
interaction_like_requests_total{outcome}
interaction_unlike_requests_total{outcome}
interaction_like_duration_seconds
interaction_unlike_duration_seconds

interaction_like_idempotent_hits_total
interaction_unlike_idempotent_hits_total
interaction_like_unique_constraint_conflicts_total
interaction_like_unexpected_conflicts_total
interaction_like_rate_limited_total
interaction_like_toggle_anomaly_total
```

### 7.2 Signals of correctness risk

| Signal | Concern |
|---|---|
| Duplicate active-like anomalies | Truth invariant failure |
| Counter decrease below zero | Derived-state correctness failure |
| Large unique-constraint conflict spike | UI retry storm, bot behavior or missing idempotent handling |
| High toggle frequency from bounded actors | Abuse or UI behavior problem |

### 7.3 Truth versus counter diagnosis

Operators must separately inspect:

```text
ArticleLike truth mutation success
ArticleInteractionStats.LikeCount freshness
Reading-applied LikeCount freshness
```

A stale Reading count does not prove like truth mutation failed.

---

## 8) Comment Submission and Author Deletion Signals

### 8.1 Comment creation metrics

```text
interaction_comment_create_requests_total{outcome}
interaction_comment_create_duration_seconds
interaction_comment_created_total
interaction_comment_validation_rejected_total{reason}
interaction_comment_rate_limited_total
interaction_comment_idempotency_hits_total
interaction_comment_idempotency_conflicts_total
```

Suggested bounded validation reasons:

```text
empty_content
content_too_long
reply_not_supported
invalid_payload
```

### 8.2 Top-level-only enforcement metrics

Because `ParentCommentId` is reserved but unsupported for V1 creation:

```text
interaction_comment_reply_attempt_rejected_total
```

A spike may indicate:

- frontend accidentally exposing reply UI too early;
- API misuse;
- probing by external callers.

### 8.3 Delete-own-comment metrics

```text
interaction_comment_delete_own_requests_total{outcome}
interaction_comment_delete_own_duration_seconds
interaction_comment_delete_idempotent_hits_total
interaction_comment_delete_not_owner_concealed_total
interaction_comment_delete_case_closed_total
interaction_comment_delete_version_conflicts_total
```

### 8.4 Interpretation

| Signal pattern | Interpretation |
|---|---|
| Rising delete ownership failures | Possible BOLA/IDOR probing |
| Rising idempotent deletes | Client retries or UI duplicate submissions |
| Rising delete/case-close conflicts | Race between author deletion and moderation |

---

## 9) Comment Moderation Signals

### 9.1 Required metrics

```text
interaction_comment_moderation_requests_total{action,outcome}
interaction_comment_moderation_duration_seconds{action}

interaction_comment_approved_total
interaction_comment_rejected_total
interaction_comment_hidden_total{source}
interaction_comment_restored_total

interaction_comment_version_conflicts_total{action}
interaction_comment_invalid_transition_total{action}
interaction_comment_open_case_requires_resolution_total
interaction_moderation_forbidden_total{action}
interaction_moderation_history_append_failures_total{action}
```

Suggested bounded `source` values for hidden comments:

```text
direct_moderation
reported_case_resolution
```

### 9.2 Moderator workflow interpretation

Operators and administrators should be able to answer:

- How many comments are waiting in `Pending`?
- How quickly are pending comments approved or rejected?
- Are moderators experiencing frequent stale-version conflicts?
- Are comments being directly hidden while report cases exist?
- Did moderation history commit successfully with each required action?

### 9.3 Queue and aging indicators

Recommended admin/workflow gauges:

```text
interaction_comments_pending_count
interaction_comments_pending_oldest_age_seconds
interaction_comments_hidden_count
interaction_comments_rejected_count
```

These are operational gauges; they must not expose content publicly.

### 9.4 Moderation latency signals

Measure workflow timing such as:

```text
interaction_comment_pending_resolution_duration_seconds
interaction_comment_hidden_restore_duration_seconds
```

These help identify backlog or inconsistent moderation behavior.

---

## 10) Comment Report Signals

### 10.1 Required metrics

```text
interaction_report_requests_total{outcome}
interaction_report_request_duration_seconds
interaction_report_created_total{severity}
interaction_report_duplicate_rejected_total
interaction_report_self_report_rejected_total
interaction_report_not_reportable_total
interaction_report_rate_limited_total
interaction_report_validation_rejected_total{reason}
```

Suggested bounded `reason` values:

```text
invalid_reason_code
missing_other_description
description_too_long
invalid_payload
```

### 10.2 Abuse indicators

Observe:

```text
interaction_report_submission_burst_detected_total
interaction_report_actor_velocity_anomaly_total
interaction_report_comment_velocity_anomaly_total
interaction_report_duplicate_ratio
interaction_report_rate_limit_ratio
```

### 10.3 Interpretation rule

A high report rate does not prove a high violation rate.

```text
Reports are allegations and moderation inputs,
not confirmed abuse findings.
```

Operational dashboards must distinguish:

- reports submitted;
- cases dismissed;
- cases actioned;
- author-deleted closures.

---

## 11) Moderation Case Signals

### 11.1 Required metrics

```text
interaction_moderation_case_opened_total
interaction_moderation_case_joined_report_total
interaction_moderation_case_resolved_total{resolution}
interaction_moderation_case_open_conflicts_total
interaction_moderation_case_version_conflicts_total
interaction_moderation_case_resolution_failures_total{action}
```

Suggested bounded `resolution` values:

```text
dismissed
actioned
closed_by_author_deletion
```

### 11.2 Queue gauges

```text
interaction_moderation_cases_open_count
interaction_moderation_cases_open_oldest_age_seconds
interaction_moderation_cases_high_priority_open_count
interaction_moderation_cases_critical_priority_open_count
interaction_moderation_cases_alerted_open_count
```

### 11.3 Resolution latency

```text
interaction_moderation_case_resolution_duration_seconds{resolution}
```

This metric should measure from case opening to final case resolution.

### 11.4 Correctness indicators

| Signal | Concern |
|---|---|
| More than one open case for a comment found by integrity check | Critical truth invariant violation |
| Case version conflict spike | Concurrent moderation or stale admin UI issue |
| Actioned case without hidden comment | Workflow consistency defect |
| Dismissed case with actioned reports | Workflow consistency defect |
| Closed-by-author-deletion case with visible comment | Workflow consistency defect |

### 11.5 Reconciliation checks

Operational integrity checks should be able to verify:

```text
No comment has more than one Open case.
Every Actioned case has matching report outcomes.
Every ClosedByAuthorDeletion case corresponds to a Deleted comment.
Every alerted case has durable alert-trigger metadata.
```

---

## 12) Admin Alert Intent and Notifications Signals

### 12.1 Interaction-owned trigger metrics

```text
interaction_report_alert_triggered_total{alert_level}
interaction_report_alert_suppressed_total{reason}
interaction_report_alert_trigger_failures_total
interaction_report_alert_outbox_commit_failures_total
```

Suggested bounded suppression reasons:

```text
already_triggered
case_not_open
threshold_not_met
concurrent_winner_already_committed
```

### 12.2 One-alert invariant diagnostics

Operators must be able to verify:

```text
One Open CommentModerationCase
    -> at most one Interaction admin-alert business intent
```

Integrity/diagnostic signals:

```text
interaction_report_alert_duplicate_intent_detected_total
interaction_report_alert_missing_metadata_detected_total
```

### 12.3 Notifications boundary indicators

Where cross-module operational dashboards expose downstream status, correlate:

```text
Interaction alert intent committed
    -> Notifications intent consumed
    -> Email delivery attempted
    -> Email sent / failed / retried
```

Interaction must not treat Notifications delay as report/case transaction failure.

### 12.4 Email amplification/security alerting

Alert when there is:

- sudden increase in alert-trigger rate;
- repeated attempts suppressed for already-alerted cases;
- alert intent committed but Notifications backlog becomes excessive;
- provider failures causing prolonged high-priority-case notification delay.

---

## 13) Counter Materialization and Reading Projection Signals

### 13.1 Interaction stats metrics

```text
interaction_stats_materialization_runs_total{outcome}
interaction_stats_materialization_duration_seconds
interaction_stats_snapshots_changed_total
interaction_stats_snapshots_unchanged_total
interaction_stats_materialization_failures_total

interaction_counter_projection_published_total
interaction_counter_projection_publish_failures_total
interaction_counter_projection_last_published_age_seconds
interaction_stats_version_conflicts_total
```

### 13.2 Counter freshness dimensions

Observe separately:

| Layer | Freshness meaning |
|---|---|
| `ArticleViewCount` update freshness | Accepted view contributions materialized locally |
| `ArticleInteractionStats` freshness | Interaction public counter snapshot materialized |
| Outbox/RabbitMQ freshness | Snapshot publication transport progress |
| Reading-applied freshness | Website-serving projection applied latest known snapshot |

### 13.3 Required lag signals

```text
interaction_stats_materialization_lag_seconds
interaction_counter_projection_outbox_oldest_pending_age_seconds
interaction_counter_projection_consumer_lag_seconds
reading_interaction_counter_projection_freshness_age_seconds
```

The Reading-owned metric may live in Reading dashboards, but Interaction operational analysis should correlate with it.

### 13.4 StatsVersion correctness

Recommended signals:

```text
interaction_counter_projection_stale_rejected_total
interaction_counter_projection_duplicate_ignored_total
interaction_counter_projection_version_gap_detected_total
```

These may be emitted by Reading consumer while still appearing in end-to-end Interaction/Reading dashboards.

### 13.5 Interpretation rule

```text
Counter lag is acceptable degraded behavior.
Counter application that overwrites newer state or diverges from truth-derived values is a correctness incident.
```

---

## 14) Consumer, Outbox and Broker Signals

### 14.1 Outbox publication metrics

For Interaction-emitted events, observe:

```text
interaction_outbox_messages_created_total{event_type}
interaction_outbox_messages_pending_count{event_type}
interaction_outbox_oldest_pending_age_seconds{event_type}
interaction_outbox_publish_attempts_total{event_type,outcome}
interaction_outbox_publish_failures_total{event_type,failure_code}
interaction_outbox_dead_lettered_total{event_type}
```

Important event types include:

```text
interaction.article_liked
interaction.article_unliked
interaction.comment_created
interaction.comment_approved
interaction.comment_rejected
interaction.comment_hidden
interaction.comment_restored
interaction.comment_deleted_by_author
interaction.comment_reported
interaction.comment_reports_dismissed
interaction.comment_report_alert_triggered
interaction.article_counters_projection_published
```

### 14.2 Interaction consumer metrics

For Content → Interaction eligibility consumption:

```text
interaction_consumer_messages_received_total{consumer_name,event_type}
interaction_consumer_messages_applied_total{consumer_name,event_type}
interaction_consumer_duplicate_ignored_total{consumer_name,event_type}
interaction_consumer_stale_ignored_total{consumer_name,event_type}
interaction_consumer_gap_detected_total{consumer_name,event_type}
interaction_consumer_failures_total{consumer_name,event_type,failure_code}
interaction_consumer_processing_duration_seconds{consumer_name,event_type}
```

### 14.3 Broker/queue health

Where RabbitMQ queues are used, observe:

```text
consumer_queue_ready_count
consumer_queue_unacked_count
consumer_queue_oldest_message_age_seconds
consumer_retry_count
consumer_dlq_count
consumer_dlq_oldest_message_age_seconds
```

Dashboard filters must allow operators to isolate:

- Content eligibility messages consumed by Interaction;
- Interaction counter snapshots consumed by Reading;
- Interaction alert events consumed by Notifications;
- Interaction moderation facts consumed by Audit.

### 14.4 Delivery interpretation

Operators must be able to locate the delay:

```text
Truth committed but Outbox pending?
Outbox published but broker queue accumulating?
Broker delivering but consumer failing?
Consumer applied but downstream serving projection stale?
```

---

## 15) Idempotency and Version-Safety Signals

### 15.1 Command-level idempotency

```text
interaction_like_idempotent_hits_total
interaction_unlike_idempotent_hits_total
interaction_comment_create_idempotency_hits_total
interaction_comment_create_idempotency_conflicts_total
interaction_comment_delete_idempotent_hits_total
interaction_report_duplicate_rejected_total
```

### 15.2 Workflow version conflicts

```text
interaction_comment_version_conflicts_total{action}
interaction_moderation_case_version_conflicts_total{action}
interaction_stats_version_conflicts_total
interaction_eligibility_source_version_stale_total
```

### 15.3 Consumer apply outcomes

```text
interaction_consumed_message_decisions_total{consumer_name,apply_decision}
```

Recommended `apply_decision` values:

```text
Applied
DuplicateIgnored
StaleIgnored
GapDetected
ResyncRequired
Failed
```

### 15.4 Correct interpretation

| Pattern | Possible cause |
|---|---|
| Idempotency hits increase but truth is healthy | Client retry/UI retry; inspect but not necessarily incident |
| Version conflicts spike on admin operations | Stale admin UI or multiple moderators handling same queue |
| Consumer duplicate ignores increase | Expected redelivery or broker/worker retries |
| Consumer stale ignores increase | Out-of-order delivery, replay or resync issue |
| Gap/resync signals increase | Eligibility correctness risk; new interactions may fail closed |

---

## 16) Reconciliation and Repair Signals

### 16.1 V1 reconciliation scope

V1 reconciliation covers:

```text
ArticleInteractionTargetProjection resync
LikeCount reconciliation from ArticleLike truth
VisibleCommentCount reconciliation from Comment truth
ArticleInteractionStats repair and counter snapshot republish
```

### 16.2 View-count limitation

V1 does not retain individual raw view history.

Therefore:

```text
ViewCount is preserved/read from durable ArticleViewCount state.
It is not exactly rebuilt from raw per-view facts.
```

Observability must not claim raw-view rebuild capability that does not exist.

### 16.3 Required metrics

```text
interaction_eligibility_reconciliation_runs_total{outcome}
interaction_eligibility_reconciliation_duration_seconds
interaction_eligibility_records_repaired_total

interaction_stats_reconciliation_runs_total{outcome}
interaction_stats_reconciliation_duration_seconds
interaction_stats_mismatch_detected_total{counter_type}
interaction_stats_records_repaired_total{counter_type}
interaction_stats_republish_total
interaction_stats_republish_failures_total
```

Allowed bounded `counter_type` values:

```text
like_count
visible_comment_count
public_snapshot
```

Do not report `view_count_rebuilt_from_raw` in V1, because that recovery path does not exist.

### 16.4 Repair interpretation

| Observation | Meaning |
|---|---|
| `LikeCount` mismatch repaired | Derived stats drifted from like truth |
| `VisibleCommentCount` mismatch repaired | Derived stats drifted from comment truth |
| `ArticleViewCount` inaccessible/corrupt | Higher-severity operational recovery problem |
| Stats republished after repair | Reading should eventually converge by newer `StatsVersion` |
| Repeated mismatch after successful repair | Materialization logic or consumer issue remains unresolved |

### 16.5 Batch posture

V1 does not require metrics for:

```text
Trending candidate generation
Large dataset cutover
Raw view replay
Scheduled full rebuild orchestration
Exclusive rebuild owner/fencing
```

Those signals become required only if those deferred capabilities are later implemented.

---

## 17) Reading Isolation and Public Experience Signals

### 17.1 Core guardrail

Interaction degradation must not materially degrade public article reading.

Recommended correlated dashboard signals:

```text
Reading public article request success/error rate
Reading public article latency P95/P99
Interaction view endpoint success/error/latency
Interaction eligibility fail-closed rate
Interaction counter freshness lag
Reading applied-counter freshness lag
```

### 17.2 Acceptable degraded behavior

The following may occur temporarily without violating correctness:

- a view is not counted;
- new interaction is temporarily rejected because eligibility is unsafe;
- public counters show older values;
- admin alert email arrives later;
- Audit evidence appears later.

### 17.3 Unacceptable behavior

Treat the following as release-blocking or incident-worthy:

- public article rendering blocks on view tracking;
- Interaction failures cause public article availability failures;
- stale counters control public article visibility;
- Reading applies older counter snapshot over newer `StatsVersion`;
- non-public article accepts new public interaction due to stale projection;
- failed moderation state is represented publicly as successful.

---

## 18) Security and Abuse Anomaly Signals

### 18.1 View abuse

Monitor:

```text
High request volume with low accepted-contribution ratio
Repeated suppression bursts
Article-specific view velocity anomalies
Rate-limit spikes
Atomic update failure spikes
```

### 18.2 Like/unlike abuse

Monitor:

```text
High-frequency toggling from one actor/article scope
Unexpected unique-conflict spikes
Large retry storms
Derived counter drift after toggle bursts
```

### 18.3 Comment abuse

Monitor:

```text
Comment creation burst by actor
Validation rejection spikes
Rate-limit spikes
High Pending backlog growth
Reply-attempt rejection spikes in V1
```

### 18.4 Report abuse

Monitor:

```text
Duplicate-report spikes
Self-report attempts
Report bursts against one comment
Many reports followed by Dismissed cases
Repeated attempts to trigger already-triggered alerts
```

A high dismissal rate after report bursts may suggest coordinated false-report abuse, but must not automatically produce penalties in V1.

### 18.5 Admin-security anomalies

Monitor:

```text
Admin forbidden attempts
Unusual moderation action velocity
Repeated version-conflict attempts
Repeated hide/restore cycles
Unexpected access attempts to reports/history
```

---

## 19) Structured Logging Requirements

### 19.1 Common log context

Logs should include, where applicable:

```text
CorrelationId
MessageId
EventType
ConsumerName
ApplyDecision
Operation
Outcome
ArticlePublicId
CommentPublicId
CommentModerationCasePublicId
ActorUserId where appropriate and protected
ExpectedVersion
CurrentVersion
IncomingVersion
ReasonCode where allowlisted and necessary
AlertLevel
```

### 19.2 Sensitive values not logged by default

Do not log by default:

```text
Raw comment content
Raw report description
Raw moderator notes
Raw IP address
Raw UserAgent
Authentication tokens
Refresh tokens
Email body
Full outbox payload containing user-generated text
```

### 19.3 Correlation usage

Correlation identifiers should allow operators to trace:

```text
API command
    -> Interaction state commit
    -> Outbox message creation
    -> Worker publication
    -> Downstream consumer apply
```

without requiring raw user content in logs.

---

## 20) Dashboards

### 20.1 Interaction Truth Dashboard

Must show:

```text
Like/unlike request rate, errors and latency
Comment create/delete request rate, errors and latency
Report request rate, errors and latency
Moderation command rate, errors and latency
Version conflicts
Invariant/uniqueness conflicts
Truth transaction failures
Outbox commit failures
```

### 20.2 Eligibility Projection Dashboard

Must show:

```text
Content eligibility messages received/applied
Duplicate and stale ignores
Projection lag
Fail-closed requests
Gap/resync signals
Projection apply failures
```

### 20.3 Moderation Operations Dashboard

Must show:

```text
Pending comment queue count and oldest age
Open moderation case count and oldest age
High/Critical priority open cases
Approve/reject/hide/restore volumes
Dismissed/actioned/author-deleted case outcomes
Resolution latency
Alert-trigger volumes and suppression
```

### 20.4 Counter and Reading Convergence Dashboard

Must show:

```text
ArticleViewCount contribution/update health
Stats materialization runs/failures
Counter snapshot publication
Outbox/broker/consumer lag
Reading projection freshness where available
StatsVersion stale/duplicate rejection
Reconciliation repairs and republishes
```

### 20.5 Abuse/Security Dashboard

Must show:

```text
Rate-limit activity by capability
View suppression/anomaly signals
Like/unlike toggle anomalies
Comment spam signals
Report abuse signals
Admin forbidden attempts
Sensitive workflow conflict spikes
```

---

## 21) Suggested Initial SLO / Alerting Posture

Exact numerical thresholds should be finalized after local/staging load tests and initial production baseline. V1 docs should distinguish **hard correctness gates** from **tunable operational targets**.

### 21.1 Hard correctness gates

The following must be treated as unacceptable regardless of baseline:

```text
Duplicate active likes for the same (ArticlePublicId, UserId)
More than one Open moderation case for the same Comment
Duplicate admin-alert business intent for one Open case
Reading applying stale counter snapshots over newer StatsVersion
Non-public article accepting new public interaction due to unsafe eligibility apply
Required moderation history missing after a successful moderation workflow mutation
Truth mutation committed without required Outbox intent
```

### 21.2 Tunable operational targets

Set measurable targets after baseline collection for:

```text
Command endpoint latency P95/P99
Command endpoint 5xx rate
View endpoint latency and failure rate
Eligibility projection lag
Outbox oldest pending age
Counter snapshot freshness lag
Open moderation-case oldest age
Admin alert delivery lag
Reconciliation failure rate
```

### 21.3 Rollout watch period

During initial rollout, observe more aggressively:

- new eligibility fail-closed rates;
- case creation and alert dedupe correctness;
- moderation version conflicts;
- public counter convergence to Reading;
- view suppression ratios and possible false positives.

---

## 22) Release Gates

### 22.1 Release-blocking conditions

Pause rollout or rollback if any of the following occurs:

```text
Public article reading materially regresses due to Interaction dependency.
Interaction accepts new engagement for confirmed non-public articles.
Duplicate active-like truth is observed.
Multiple Open moderation cases exist for one comment.
One case produces duplicate admin-alert intents.
Moderator actions apply stale/invalid transitions successfully.
Required local moderation history is missing for successful moderation actions.
Reading counter projection applies stale snapshots over newer versions.
Truth mutation succeeds while required outbox intent is missing.
```

### 22.2 High-priority operational conditions

Investigate immediately if:

```text
Eligibility fail-closed rate rises sharply.
Outbox oldest pending age grows continuously.
Counter publication lag becomes sustained.
Open moderation cases age beyond operational target.
Admin alert intents succeed but Notifications delivery backlog rises.
Reconciliation repeatedly repairs the same counters.
Report bursts are predominantly dismissed.
```

### 22.3 Degraded-but-acceptable conditions

Temporarily acceptable when observable and within operational policy:

```text
View contribution is dropped/suppressed while public article reading succeeds.
Counters lag behind committed Interaction truth.
Audit evidence arrives later than moderation commit.
Email alert delivery lags while case remains visible in admin queue.
Interaction rejects new commands temporarily while eligibility resync is required.
```

---

## 23) Operator Questions This Module Must Answer

Interaction observability must enable operators to answer:

1. Did the authoritative Interaction command commit successfully?
2. Was the required Outbox intent written with that mutation?
3. Are user-facing failures caused by business rejection, abuse control, projection lag or internal failure?
4. Is an article rejecting interaction because it is genuinely non-public or because eligibility projection is temporarily unsafe?
5. Did accepted view requests actually contribute to `ArticleViewCount`, or were they suppressed by policy?
6. Are like/unlike truths correct even if counters lag?
7. Is the pending-comment moderation queue healthy?
8. Are report cases being resolved promptly and consistently?
9. Did any open case generate duplicate admin-alert intent?
10. Is Notifications delaying email while the moderation queue still remains available?
11. Is Reading serving stale counters because Interaction failed to publish, transport lagged or Reading failed to apply?
12. Are consumer duplicates/stale messages safely ignored?
13. Does a counter mismatch require bounded repair?
14. Is a spike legitimate traffic, UI retry behavior, replay activity or abuse?
15. Is the system safely degraded, or is authoritative correctness at risk?

---

## 24) V1 Deferred Observability Signals

Do not treat the following as currently required V1 metrics unless their capabilities are later implemented:

| Deferred capability | Deferred observability |
|---|---|
| Reply comments | Reply depth, reply moderation and thread-health signals |
| Comment editing | Revision/re-moderation/edit-conflict signals |
| Raw per-view history | Raw-view retention/replay/rebuild signals |
| Trending/popularity scoring | Ranking freshness and manipulation metrics |
| Auto-hide after reports | Automated-decision false-positive/appeal signals |
| Bulk moderation | Batch item outcome and partial-failure metrics |
| Full scheduled rebuild orchestration | Batch ownership/fencing/candidate-cutover metrics |
| Article-author moderation | Scoped-author permission anomaly signals |

---

## 25) Summary

Interaction V1 observability follows these rules:

1. Observe truth/workflow health separately from derived-state freshness.
2. Track Content-derived eligibility lag because unsafe eligibility must fail closed.
3. View tracking must be observable but must never harm public article reading.
4. `ArticleViewCount` measures accepted materialized contributions; V1 does not claim raw-view replay or exact historical rebuild.
5. Like/unlike truth must remain deterministic under retry and concurrency.
6. Comment submission, moderation and author deletion require latency, conflict and idempotency signals.
7. Reports require abuse, duplicate and rate-limit monitoring.
8. Moderation cases require queue aging, resolution consistency and one-alert-invariant monitoring.
9. Admin alert intent and Notifications delivery are separate observable stages.
10. Counter materialization and Reading projection freshness must be tracked separately.
11. Consumers must expose duplicate, stale, gap and resync outcomes.
12. Reconciliation repairs derived counters; it must never conceal truth corruption.
13. Logs must support tracing without exposing raw sensitive user-generated content.
14. Hard correctness invariants are release gates; operational latency/lag targets are tuned after baseline testing.
15. Unpublish disables new interaction while preserving counters and workflow history; observability must reflect that distinction.
