using Audit.Application.Abstractions.Normalization;
using Audit.Application.Abstractions.Serialization;
using Audit.Application.Services.Normalization;
using Audit.Domain.Policies.Evidence;
using Audit.Domain.ValueObjects.Evidence;

namespace Audit.Infrastructure.Normalization.Common;

internal abstract class AuditNormalizerBase : IAuditEventNormalizer
{
    private readonly IAuditJsonSerializer _jsonSerializer;
    private readonly IAuditActionClassificationPolicy _actionClassificationPolicy;
    private readonly IAuditRiskClassificationPolicy _riskClassificationPolicy;

    protected AuditNormalizerBase(
        IAuditJsonSerializer jsonSerializer,
        IAuditActionClassificationPolicy actionClassificationPolicy,
        IAuditRiskClassificationPolicy riskClassificationPolicy)
    {
        _jsonSerializer = jsonSerializer
            ?? throw new ArgumentNullException(nameof(jsonSerializer));

        _actionClassificationPolicy = actionClassificationPolicy
            ?? throw new ArgumentNullException(nameof(actionClassificationPolicy));

        _riskClassificationPolicy = riskClassificationPolicy
            ?? throw new ArgumentNullException(nameof(riskClassificationPolicy));
    }

    public abstract string SourceModule { get; }

    public abstract bool CanHandle(
        string eventType);

    public AuditNormalizedEvent Normalize(
        AuditEventNormalizationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return NormalizeCore(context);
    }

    protected abstract AuditNormalizedEvent NormalizeCore(
        AuditEventNormalizationContext context);

    protected TPayload? DeserializePayload<TPayload>(
        AuditEventNormalizationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _jsonSerializer.Deserialize<TPayload>(
            context.PayloadJson);
    }

    protected TPayload RequirePayload<TPayload>(
        AuditEventNormalizationContext context)
    {
        var payload = DeserializePayload<TPayload>(context);

        if (payload is null)
        {
            throw new InvalidOperationException(
                $"Audit payload is required for event type '{context.SourceEvent.EventType}'.");
        }

        return payload;
    }

    protected string? Serialize<TValue>(
        TValue? value)
    {
        return _jsonSerializer.Serialize(value);
    }

    protected AuditJsonPayload BuildDefaultJsonPayload(
        AuditEventNormalizationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return AuditJsonPayload.Create(
            metadataJson: null,
            headersJson: context.HeadersJson,
            sanitizedPayloadJson: context.PayloadJson,
            beforeJson: null,
            afterJson: null,
            changesJson: null);
    }

    protected AuditActionClassificationResult ClassifyAction(
        string eventType)
    {
        return _actionClassificationPolicy.Classify(
            SourceModule,
            eventType);
    }

    protected AuditRiskClassificationResult ClassifyRisk(
        string eventType,
        AuditActionClassificationResult actionClassification)
    {
        ArgumentNullException.ThrowIfNull(actionClassification);

        return _riskClassificationPolicy.Classify(
            SourceModule,
            eventType,
            actionClassification.Action,
            actionClassification.ActionCategory);
    }
}