# Media — Open Questions & ADR Hooks (V1)

## ADR-01: Soft delete retention window
- How long can media be restored?
- When do we hard-delete (if ever)?

## ADR-02: Allowed media types and validation
- Allowed types for V1 (Image only?) vs V2 (Video/File)
- Maximum size limits
- Metadata sanitization rules

## ADR-03: Primary media delete behavior
- Block delete if media is primary?
- Or auto-unset and allow delete?
- Fallback primary selection policy after detach/delete

## ADR-04: Upload strategy
- Direct upload through API vs presigned URLs to object storage (V2+)

## ADR-05: Publication coupling
- Can media exist without an article?
- Should archive/unpublish affect media visibility or cleanup?