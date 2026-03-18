# Interaction — Open Questions & ADR Hooks (V1)

## ADR-01: View counting semantics
- V1: raw count/log (already)
- V2: unique views (privacy implications, visitorKey strategy)

## ADR-02: Interaction allowed by article state
- Disable interaction when article is unpublished/archived?
- Enforce via checks vs via consuming Content events.

## ADR-03: Comment moderation model (V2)
- States: Visible/Hidden/Pending/Spam?
- Who can hide/approve?
- Enforcement boundaries (Interaction vs Admin governance).

## ADR-04: Counter aggregation approach
- Inline updates vs async aggregation/projections
- How Reading consumes counters (direct read vs projection)

## ADR-05: Anti-spam controls (V2)
- rate limits thresholds
- CAPTCHA triggers
- shadow banning / spam scoring