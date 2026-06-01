using Audit.Domain.Constants.AuditIngestion;
using Audit.Domain.Constants.Common;
using Audit.Domain.Exceptions;
using Audit.Domain.Policies.Ingestion;
using Audit.Domain.ValueObjects.Common;
using Audit.Domain.ValueObjects.Ingestion;

namespace Audit.Domain.Entities;

public sealed class AuditIngestion
{
    public long AuditIngestionId { get; private set; }
    public string PublicId { get; private set; }

    public AuditSourceEvent SourceEvent { get; private set; }
    public AuditAggregateRef AggregateRef { get; private set; }
    public AuditTraceContext TraceContext { get; private set; }

    public string ConsumerName { get; private set; }
    public string Status { get; private set; }

    public int AttemptCount { get; private set; }

    public AuditErrorInfo ErrorInfo { get; private set; }

    public int? SourcePriority => SourceEvent.SourcePriority;
    public DateTime SourceOccurredAtUtc => SourceEvent.SourceOccurredAtUtc;
    public DateTime? SourcePublishedAtUtc => SourceEvent.SourcePublishedAtUtc;

    public DateTime FirstReceivedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? LastAttemptAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public DateTime? DeadLetteredAtUtc { get; private set; }

    private AuditIngestion(
        long auditIngestionId,
        string publicId,
        AuditSourceEvent sourceEvent,
        AuditAggregateRef aggregateRef,
        AuditTraceContext traceContext,
        string consumerName,
        string status,
        int attemptCount,
        AuditErrorInfo errorInfo,
        DateTime firstReceivedAtUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        DateTime? lastAttemptAtUtc,
        DateTime? processedAtUtc,
        DateTime? deadLetteredAtUtc)
    {
        AuditIngestionId = auditIngestionId;
        PublicId = publicId;
        SourceEvent = sourceEvent;
        AggregateRef = aggregateRef;
        TraceContext = traceContext;
        ConsumerName = consumerName;
        Status = status;
        AttemptCount = attemptCount;
        ErrorInfo = errorInfo;
        FirstReceivedAtUtc = firstReceivedAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        LastAttemptAtUtc = lastAttemptAtUtc;
        ProcessedAtUtc = processedAtUtc;
        DeadLetteredAtUtc = deadLetteredAtUtc;
    }

    public static AuditIngestion StartProcessing(
        string? publicId,
        AuditSourceEvent sourceEvent,
        AuditAggregateRef? aggregateRef,
        AuditTraceContext? traceContext,
        string? consumerName,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(sourceEvent);

        var normalizedPublicId = NormalizeRequiredPublicId(publicId);
        var normalizedConsumerName = NormalizeRequiredConsumerName(consumerName);

        EnsureValidTimestamp(nowUtc, nameof(nowUtc));

        return new AuditIngestion(
            auditIngestionId: 0,
            publicId: normalizedPublicId,
            sourceEvent: sourceEvent,
            aggregateRef: aggregateRef ?? AuditAggregateRef.Empty(),
            traceContext: traceContext ?? AuditTraceContext.Empty(),
            consumerName: normalizedConsumerName,
            status: AuditIngestionStatuses.Processing,
            attemptCount: 1,
            errorInfo: AuditErrorInfo.None(),
            firstReceivedAtUtc: nowUtc,
            createdAtUtc: nowUtc,
            updatedAtUtc: nowUtc,
            lastAttemptAtUtc: nowUtc,
            processedAtUtc: null,
            deadLetteredAtUtc: null);
    }

    public static AuditIngestion Rehydrate(
        long auditIngestionId,
        string publicId,
        AuditSourceEvent sourceEvent,
        AuditAggregateRef aggregateRef,
        AuditTraceContext traceContext,
        string consumerName,
        string status,
        int attemptCount,
        AuditErrorInfo errorInfo,
        DateTime firstReceivedAtUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        DateTime? lastAttemptAtUtc,
        DateTime? processedAtUtc,
        DateTime? deadLetteredAtUtc)
    {
        if (auditIngestionId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(auditIngestionId),
                "Audit ingestion id must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(sourceEvent);
        ArgumentNullException.ThrowIfNull(aggregateRef);
        ArgumentNullException.ThrowIfNull(traceContext);
        ArgumentNullException.ThrowIfNull(errorInfo);

        var normalizedPublicId = NormalizeRequiredPublicId(publicId);
        var normalizedConsumerName = NormalizeRequiredConsumerName(consumerName);
        var normalizedStatus = NormalizeRequiredStatus(status);

        if (attemptCount < 0)
        {
            throw AuditDomainException.AttemptCountInvalid();
        }

        EnsureValidFirstReceivedAtUtc(firstReceivedAtUtc);
        EnsureValidTimestamp(createdAtUtc, nameof(createdAtUtc));
        EnsureValidTimestamp(updatedAtUtc, nameof(updatedAtUtc));
        EnsureValidOptionalTimestamp(lastAttemptAtUtc, nameof(lastAttemptAtUtc));
        EnsureValidOptionalTimestamp(processedAtUtc, nameof(processedAtUtc));
        EnsureValidOptionalTimestamp(deadLetteredAtUtc, nameof(deadLetteredAtUtc));

        return new AuditIngestion(
            auditIngestionId: auditIngestionId,
            publicId: normalizedPublicId,
            sourceEvent: sourceEvent,
            aggregateRef: aggregateRef,
            traceContext: traceContext,
            consumerName: normalizedConsumerName,
            status: normalizedStatus,
            attemptCount: attemptCount,
            errorInfo: errorInfo,
            firstReceivedAtUtc: firstReceivedAtUtc,
            createdAtUtc: createdAtUtc,
            updatedAtUtc: updatedAtUtc,
            lastAttemptAtUtc: lastAttemptAtUtc,
            processedAtUtc: processedAtUtc,
            deadLetteredAtUtc: deadLetteredAtUtc);
    }

    public void MarkProcessing(
        DateTime nowUtc,
        AuditIngestionTransitionResult transitionResult)
    {
        EnsureTransitionAllowed(AuditIngestionStatuses.Processing, transitionResult);
        EnsureValidTimestamp(nowUtc, nameof(nowUtc));

        Status = AuditIngestionStatuses.Processing;
        AttemptCount += 1;
        ErrorInfo = AuditErrorInfo.None();
        LastAttemptAtUtc = nowUtc;
        ProcessedAtUtc = null;
        DeadLetteredAtUtc = null;
        UpdatedAtUtc = nowUtc;
    }

    public void MarkSucceeded(
        DateTime nowUtc,
        AuditIngestionTransitionResult transitionResult)
    {
        EnsureTransitionAllowed(AuditIngestionStatuses.Succeeded, transitionResult);
        EnsureValidTimestamp(nowUtc, nameof(nowUtc));

        Status = AuditIngestionStatuses.Succeeded;
        ErrorInfo = AuditErrorInfo.None();
        LastAttemptAtUtc = nowUtc;
        ProcessedAtUtc = nowUtc;
        DeadLetteredAtUtc = null;
        UpdatedAtUtc = nowUtc;
    }

    public void MarkDuplicate(
        DateTime nowUtc,
        AuditIngestionTransitionResult transitionResult)
    {
        EnsureTransitionAllowed(AuditIngestionStatuses.Duplicate, transitionResult);
        EnsureValidTimestamp(nowUtc, nameof(nowUtc));

        Status = AuditIngestionStatuses.Duplicate;
        ErrorInfo = AuditErrorInfo.None();
        LastAttemptAtUtc = nowUtc;
        ProcessedAtUtc = nowUtc;
        DeadLetteredAtUtc = null;
        UpdatedAtUtc = nowUtc;
    }

    public void MarkIgnored(
        DateTime nowUtc,
        AuditIngestionTransitionResult transitionResult)
    {
        EnsureTransitionAllowed(AuditIngestionStatuses.Ignored, transitionResult);
        EnsureValidTimestamp(nowUtc, nameof(nowUtc));

        Status = AuditIngestionStatuses.Ignored;
        ErrorInfo = AuditErrorInfo.None();
        LastAttemptAtUtc = nowUtc;
        ProcessedAtUtc = nowUtc;
        DeadLetteredAtUtc = null;
        UpdatedAtUtc = nowUtc;
    }

    public void MarkFailed(
        AuditErrorInfo errorInfo,
        DateTime nowUtc,
        AuditIngestionTransitionResult transitionResult)
    {
        ArgumentNullException.ThrowIfNull(errorInfo);

        EnsureTransitionAllowed(AuditIngestionStatuses.Failed, transitionResult);
        EnsureValidTimestamp(nowUtc, nameof(nowUtc));

        Status = AuditIngestionStatuses.Failed;
        ErrorInfo = errorInfo;
        LastAttemptAtUtc = nowUtc;
        DeadLetteredAtUtc = null;
        UpdatedAtUtc = nowUtc;
    }

    public void MarkDeadLettered(
        AuditErrorInfo errorInfo,
        DateTime nowUtc,
        AuditIngestionTransitionResult transitionResult)
    {
        ArgumentNullException.ThrowIfNull(errorInfo);

        EnsureTransitionAllowed(AuditIngestionStatuses.DeadLettered, transitionResult);
        EnsureValidTimestamp(nowUtc, nameof(nowUtc));

        Status = AuditIngestionStatuses.DeadLettered;
        ErrorInfo = errorInfo;
        LastAttemptAtUtc = nowUtc;
        ProcessedAtUtc = nowUtc;
        DeadLetteredAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public bool IsTerminal()
    {
        return AuditIngestionStatuses.IsTerminal(Status);
    }

    private void EnsureTransitionAllowed(
        string nextStatus,
        AuditIngestionTransitionResult transitionResult)
    {
        ArgumentNullException.ThrowIfNull(transitionResult);

        if (transitionResult.IsAllowed)
        {
            return;
        }

        throw AuditDomainException.InvalidStatusTransition(
            Status,
            nextStatus,
            transitionResult.Reason);
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

    private static string NormalizeRequiredConsumerName(string? consumerName)
    {
        var normalizedConsumerName = consumerName?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedConsumerName))
        {
            throw AuditDomainException.ConsumerNameRequired();
        }

        if (normalizedConsumerName.Length > AuditConstants.MaxConsumerNameLength)
        {
            throw AuditDomainException.ConsumerNameTooLong();
        }

        return normalizedConsumerName;
    }

    private static string NormalizeRequiredStatus(string? status)
    {
        var normalizedStatus = status?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedStatus))
        {
            throw AuditDomainException.IngestionStatusRequired();
        }

        if (!AuditIngestionStatuses.IsValid(normalizedStatus))
        {
            throw AuditDomainException.IngestionStatusInvalid(normalizedStatus);
        }

        return normalizedStatus;
    }

    private static void EnsureValidTimestamp(DateTime value, string parameterName)
    {
        if (value == default)
        {
            throw AuditDomainException.TimestampRequired(parameterName);
        }

        if (value.Kind != DateTimeKind.Utc)
        {
            throw AuditDomainException.TimestampMustBeUtc(parameterName);
        }
    }

    private static void EnsureValidFirstReceivedAtUtc(DateTime firstReceivedAtUtc)
    {
        if (firstReceivedAtUtc == default)
        {
            throw AuditDomainException.FirstReceivedAtUtcRequired();
        }

        if (firstReceivedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw AuditDomainException.TimestampMustBeUtc(nameof(firstReceivedAtUtc));
        }
    }

    private static void EnsureValidOptionalTimestamp(DateTime? value, string parameterName)
    {
        if (value is null)
        {
            return;
        }

        if (value.Value == default)
        {
            throw AuditDomainException.TimestampRequired(parameterName);
        }

        if (value.Value.Kind != DateTimeKind.Utc)
        {
            throw AuditDomainException.TimestampMustBeUtc(parameterName);
        }
    }
}
