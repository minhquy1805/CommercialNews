# Authorization — Open Questions & ADR Hooks (V1)

## ADR-01: Permission naming & versioning strategy
- Format: `module:action` vs `module.resource:action`
- How do we deprecate/rename permissions safely?

## ADR-02: Built-in roles protection policy
- Which roles are immutable (Admin/User/Moderator/Author)?
- Can they be renamed/deleted?

## ADR-03: ABAC support scope (V1 vs V2)
- When to introduce subject/resource/environment attributes in policies?
- Which modules need ABAC earliest (Content publish, Comment moderation)?

## ADR-04: Audit coverage policy
- Which governance actions must always be audited (recommended: all mutations)?