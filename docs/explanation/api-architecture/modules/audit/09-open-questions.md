# Audit — Open Questions & ADR Hooks (V1)

## ADR-01: Retention and purge policy
- How long are audit logs retained?
- Who can purge and under what conditions?
- Is purge implemented as a scheduled job with explicit approval?

## ADR-02: Tamper-evident strategy (V2)
- Hash chaining per record?
- External anchoring (optional)?
- How to validate integrity during investigations?

## ADR-03: Redaction rules by event type
- For each event type, what fields are stored in `Data`?
- What is masked/removed?

## ADR-04: Audit completeness expectations
- Is “audit lag” acceptable up to what threshold?
- How do we alert on missing critical events?

## ADR-05: Storage choice and indexing strategy
- SQL table vs NoSQL store for large volume?
- Indexes for time range + actor + resource queries.