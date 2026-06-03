using Audit.Domain.Entities;

namespace Audit.Application.Services.Evidence;

public sealed class AuditEvidenceBuilder : IAuditEvidenceBuilder
{
    public AuditLog Build(
        AuditEvidenceBuildInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.SourceEvent);
        ArgumentNullException.ThrowIfNull(input.AggregateRef);
        ArgumentNullException.ThrowIfNull(input.TraceContext);
        ArgumentNullException.ThrowIfNull(input.NormalizedEvent);
        ArgumentNullException.ThrowIfNull(input.JsonPayload);

        return AuditLog.Create(
            publicId: input.PublicId,
            sourceEvent: input.SourceEvent,
            aggregateRef: input.AggregateRef,
            traceContext: input.TraceContext,
            actor: input.NormalizedEvent.Actor,
            resource: input.NormalizedEvent.Resource,
            risk: input.NormalizedEvent.RiskClassification.Risk,
            requestContext: input.NormalizedEvent.RequestContext,
            jsonPayload: input.JsonPayload,
            actionClassification: input.NormalizedEvent.ActionClassification,
            summary: input.NormalizedEvent.Summary,
            reason: input.NormalizedEvent.Reason,
            ingestedAtUtc: input.IngestedAtUtc);
    }
}