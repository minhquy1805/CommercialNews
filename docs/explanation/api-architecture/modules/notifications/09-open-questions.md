# Notifications — Open Questions & ADR Hooks (V1)

## ADR-01: Dedupe key strategy
- Use EventId only?
- Or (template + recipient + tokenId) for resend semantics?

## ADR-02: Retry and DLQ policy
- max attempts
- backoff strategy
- DLQ handling and alert thresholds

## ADR-03: New-article notification policy (optional)
- recipients (all verified users? subscribers only?)
- opt-out/unsubscribe (V2)
- batching/throttling strategy

## ADR-04: Template engine and safe variables
- template storage strategy
- allowlist of variables per template
- redaction rules for logging and admin views

## ADR-05: Provider choice and failover
- single provider vs fallback provider
- operational runbook expectations