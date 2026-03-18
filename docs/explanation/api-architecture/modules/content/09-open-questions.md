# Content — Open Questions & ADR Hooks (V1)

## ADR-01: Unpublish semantics
- Does unpublish revert to Draft or become a separate state?
- How do we represent "non-public but not draft"?

## ADR-02: Edit history strategy
- Snapshot vs diff?
- What is the retention policy?
- Do we need tamper-evident chaining (hash chain) in V2?

## ADR-03: Archive vs delete
- Do we allow delete in V1?
- Is archive the only non-public terminal state?

## ADR-04: Update rules for Published articles
- Allow edits after publish?
- If yes, do we create a revision and keep published content consistent?

## ADR-05: Taxonomy deletion policy
- If a Category/Tag is deleted, what happens to existing articles?
- Block deletion if referenced vs soft delete vs reassign strategy?