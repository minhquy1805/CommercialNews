using Audit.Domain.Policies.Evidence;
using Audit.Domain.ValueObjects.Evidence;

namespace Audit.Application.Services.Normalization;

public sealed record AuditNormalizedEvent(
    AuditActor Actor,
    AuditResource Resource,
    AuditRiskClassificationResult RiskClassification,
    AuditRequestContext RequestContext,
    AuditActionClassificationResult ActionClassification,
    AuditJsonPayload JsonPayload,
    string Summary,
    string? Reason);