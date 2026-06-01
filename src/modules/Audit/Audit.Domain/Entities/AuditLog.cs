using Audit.Domain.Constants.Common;
using Audit.Domain.Exceptions;
using Audit.Domain.Policies.Evidence;
using Audit.Domain.ValueObjects.Common;
using Audit.Domain.ValueObjects.Evidence;

namespace Audit.Domain.Entities;

public sealed class AuditLog
{
    public long AuditLogId { get; private set; }
    public string PublicId { get; private set; }

    public AuditSourceEvent SourceEvent { get; private set; }
    public AuditAggregateRef AggregateRef { get; private set; }
    public AuditTraceContext TraceContext { get; private set; }

    public AuditActor Actor { get; private set; }
    public AuditResource Resource { get; private set; }
    public AuditRisk Risk { get; private set; }
    public AuditRequestContext RequestContext { get; private set; }
    public AuditJsonPayload JsonPayload { get; private set; }

    public string Action { get; private set; }
    public string? ActionCategory { get; private set; }

    public string Summary { get; private set; }
    public string? Reason { get; private set; }

    public DateTime OccurredAtUtc => SourceEvent.SourceOccurredAtUtc;
    public int? SourcePriority => SourceEvent.SourcePriority;
    public DateTime IngestedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private AuditLog(
        long auditLogId,
        string publicId,
        AuditSourceEvent sourceEvent,
        AuditAggregateRef aggregateRef,
        AuditTraceContext traceContext,
        AuditActor actor,
        AuditResource resource,
        AuditRisk risk,
        AuditRequestContext requestContext,
        AuditJsonPayload jsonPayload,
        string action,
        string? actionCategory,
        string summary,
        string? reason,
        DateTime ingestedAtUtc,
        DateTime createdAtUtc)
    {
        AuditLogId = auditLogId;
        PublicId = publicId;
        SourceEvent = sourceEvent;
        AggregateRef = aggregateRef;
        TraceContext = traceContext;
        Actor = actor;
        Resource = resource;
        Risk = risk;
        RequestContext = requestContext;
        JsonPayload = jsonPayload;
        Action = action;
        ActionCategory = actionCategory;
        Summary = summary;
        Reason = reason;
        IngestedAtUtc = ingestedAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public static AuditLog Create(
        string? publicId,
        AuditSourceEvent sourceEvent,
        AuditAggregateRef? aggregateRef,
        AuditTraceContext? traceContext,
        AuditActor actor,
        AuditResource resource,
        AuditRisk risk,
        AuditRequestContext? requestContext,
        AuditJsonPayload? jsonPayload,
        AuditActionClassificationResult actionClassification,
        string? summary,
        string? reason,
        DateTime ingestedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(sourceEvent);
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(risk);
        ArgumentNullException.ThrowIfNull(actionClassification);

        var normalizedPublicId = NormalizeRequiredPublicId(publicId);
        var normalizedSummary = NormalizeRequiredSummary(summary);
        var normalizedReason = NormalizeOptional(reason);

        EnsureValidIngestedAtUtc(ingestedAtUtc);

        return new AuditLog(
            auditLogId: 0,
            publicId: normalizedPublicId,
            sourceEvent: sourceEvent,
            aggregateRef: aggregateRef ?? AuditAggregateRef.Empty(),
            traceContext: traceContext ?? AuditTraceContext.Empty(),
            actor: actor,
            resource: resource,
            risk: risk,
            requestContext: requestContext ?? AuditRequestContext.Empty(),
            jsonPayload: jsonPayload ?? AuditJsonPayload.Empty(),
            action: actionClassification.Action,
            actionCategory: actionClassification.ActionCategory,
            summary: normalizedSummary,
            reason: normalizedReason,
            ingestedAtUtc: ingestedAtUtc,
            createdAtUtc: ingestedAtUtc);
    }

    public static AuditLog Rehydrate(
        long auditLogId,
        string publicId,
        AuditSourceEvent sourceEvent,
        AuditAggregateRef aggregateRef,
        AuditTraceContext traceContext,
        AuditActor actor,
        AuditResource resource,
        AuditRisk risk,
        AuditRequestContext requestContext,
        AuditJsonPayload jsonPayload,
        string action,
        string? actionCategory,
        string summary,
        string? reason,
        DateTime ingestedAtUtc,
        DateTime createdAtUtc)
    {
        if (auditLogId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(auditLogId), "Audit log id must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(sourceEvent);
        ArgumentNullException.ThrowIfNull(aggregateRef);
        ArgumentNullException.ThrowIfNull(traceContext);
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(risk);
        ArgumentNullException.ThrowIfNull(requestContext);
        ArgumentNullException.ThrowIfNull(jsonPayload);

        var normalizedPublicId = NormalizeRequiredPublicId(publicId);
        var normalizedActionClassification = AuditActionClassificationResult.Create(action, actionCategory);
        var normalizedSummary = NormalizeRequiredSummary(summary);
        var normalizedReason = NormalizeOptional(reason);

        EnsureValidIngestedAtUtc(ingestedAtUtc);
        EnsureValidCreatedAtUtc(createdAtUtc);

        return new AuditLog(
            auditLogId: auditLogId,
            publicId: normalizedPublicId,
            sourceEvent: sourceEvent,
            aggregateRef: aggregateRef,
            traceContext: traceContext,
            actor: actor,
            resource: resource,
            risk: risk,
            requestContext: requestContext,
            jsonPayload: jsonPayload,
            action: normalizedActionClassification.Action,
            actionCategory: normalizedActionClassification.ActionCategory,
            summary: normalizedSummary,
            reason: normalizedReason,
            ingestedAtUtc: ingestedAtUtc,
            createdAtUtc: createdAtUtc);
    }

    private static string NormalizeRequiredPublicId(string? publicId)
    {
        var normalizedPublicId = publicId?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedPublicId))
        {
            throw AuditDomainException.PublicIdRequired();
        }

        if (normalizedPublicId.Length != AuditConstants.PublicIdLength)
        {
            throw AuditDomainException.PublicIdInvalidLength();
        }

        return normalizedPublicId;
    }

    private static string NormalizeRequiredSummary(string? summary)
    {
        var normalizedSummary = summary?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedSummary))
        {
            throw AuditDomainException.SummaryRequired();
        }

        if (normalizedSummary.Length > AuditConstants.MaxSummaryLength)
        {
            throw AuditDomainException.SummaryTooLong();
        }

        return normalizedSummary;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }

    private static void EnsureValidIngestedAtUtc(DateTime ingestedAtUtc)
    {
        if (ingestedAtUtc == default)
        {
            throw AuditDomainException.IngestedAtUtcRequired();
        }

        if (ingestedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw AuditDomainException.TimestampMustBeUtc(nameof(ingestedAtUtc));
        }
    }

    private static void EnsureValidCreatedAtUtc(DateTime createdAtUtc)
    {
        if (createdAtUtc == default)
        {
            throw AuditDomainException.TimestampRequired(nameof(createdAtUtc));
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw AuditDomainException.TimestampMustBeUtc(nameof(createdAtUtc));
        }
    }
}
