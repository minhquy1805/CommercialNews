using Audit.Application.Abstractions.Normalization;
using Audit.Application.Abstractions.Persistence;
using Audit.Application.Errors;
using Audit.Application.Models.Commands.Ingestion;
using Audit.Application.Models.Results.Ingestion;
using Audit.Application.Services.Evidence;
using Audit.Application.Services.Normalization;
using Audit.Application.Services.Redaction;
using Audit.Domain.Constants.AuditIngestion;
using Audit.Domain.Constants.Events;
using Audit.Domain.Entities;
using Audit.Domain.ValueObjects.Common;
using Audit.Domain.ValueObjects.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;

namespace Audit.Application.Services.Ingestion;

public sealed class AuditIngestionApplicationService
    : IAuditIngestionApplicationService
{
    private readonly IAuditIngestionRepository _auditIngestionRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IAuditEventNormalizerRegistry _normalizerRegistry;
    private readonly IAuditRedactionService _redactionService;
    private readonly IAuditEvidenceBuilder _evidenceBuilder;
    private readonly IAuditIngestionFailureClassifier _failureClassifier;
    private readonly IPublicIdGenerator _publicIdGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuditIngestionApplicationService(
        IAuditIngestionRepository auditIngestionRepository,
        IAuditLogRepository auditLogRepository,
        IAuditEventNormalizerRegistry normalizerRegistry,
        IAuditRedactionService redactionService,
        IAuditEvidenceBuilder evidenceBuilder,
        IAuditIngestionFailureClassifier failureClassifier,
        IPublicIdGenerator publicIdGenerator,
        IDateTimeProvider dateTimeProvider)
    {
        _auditIngestionRepository = auditIngestionRepository
            ?? throw new ArgumentNullException(nameof(auditIngestionRepository));

        _auditLogRepository = auditLogRepository
            ?? throw new ArgumentNullException(nameof(auditLogRepository));

        _normalizerRegistry = normalizerRegistry
            ?? throw new ArgumentNullException(nameof(normalizerRegistry));

        _redactionService = redactionService
            ?? throw new ArgumentNullException(nameof(redactionService));

        _evidenceBuilder = evidenceBuilder
            ?? throw new ArgumentNullException(nameof(evidenceBuilder));

        _failureClassifier = failureClassifier
            ?? throw new ArgumentNullException(nameof(failureClassifier));

        _publicIdGenerator = publicIdGenerator
            ?? throw new ArgumentNullException(nameof(publicIdGenerator));

        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<IngestAuditEventResult>> IngestAsync(
        IngestAuditEventCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        long? auditIngestionId = null;

        try
        {
            var nowUtc = _dateTimeProvider.UtcNow;

            var sourceModule = AuditSourceModuleResolver.Resolve(
                command.EventType);

            if (sourceModule is null)
            {
                return await MarkUnsupportedEventAsIgnoredAsync(
                    command,
                    cancellationToken);
            }

            var sourceEvent = AuditSourceEvent.Create(
                messageId: command.MessageId,
                eventType: command.EventType,
                eventVersion: null,
                sourceModule: sourceModule,
                sourcePriority: command.Priority,
                sourceOccurredAtUtc: command.OccurredAtUtc,
                sourcePublishedAtUtc: command.PublishedAtUtc);

            var aggregateRef = AuditAggregateRef.Create(
                aggregateType: command.AggregateType,
                aggregateId: command.AggregateId,
                aggregatePublicId: command.AggregatePublicId,
                aggregateVersion: command.AggregateVersion);

            var traceContext = AuditTraceContext.Create(
                correlationId: command.CorrelationId,
                causationId: null,
                traceId: null);

            var ingestion = AuditIngestion.StartProcessing(
                publicId: _publicIdGenerator.NewId(),
                sourceEvent: sourceEvent,
                aggregateRef: aggregateRef,
                traceContext: traceContext,
                consumerName: command.ConsumerName,
                nowUtc: nowUtc);

            var upsertResult = await _auditIngestionRepository.UpsertProcessingAsync(
                ingestion,
                cancellationToken);

            auditIngestionId = upsertResult.AuditIngestionId;

            if (AuditIngestionStatuses.IsTerminal(upsertResult.CurrentStatus))
            {
                var existingAuditLog = await _auditLogRepository.GetByMessageIdAsync(
                    command.MessageId,
                    cancellationToken);

                return Result<IngestAuditEventResult>.Success(
                    IngestAuditEventResult.AlreadyProcessed(
                        messageId: command.MessageId,
                        auditIngestionId: upsertResult.AuditIngestionId,
                        auditLogId: existingAuditLog?.AuditLogId,
                        currentStatus: upsertResult.CurrentStatus,
                        reason: "Audit message has already reached a terminal ingestion status."));
            }

            var normalizer = _normalizerRegistry.Resolve(
                command.EventType);

            if (normalizer is null)
            {
                await _auditIngestionRepository.MarkIgnoredAsync(
                    command.MessageId,
                    cancellationToken);

                return Result<IngestAuditEventResult>.Success(
                    IngestAuditEventResult.Ignored(
                        messageId: command.MessageId,
                        auditIngestionId: upsertResult.AuditIngestionId,
                        reason: $"Audit event type '{command.EventType}' is not supported."));
            }

            var normalizationContext = new AuditEventNormalizationContext(
                SourceEvent: sourceEvent,
                AggregateRef: aggregateRef,
                TraceContext: traceContext,
                PayloadJson: command.PayloadJson,
                HeadersJson: command.HeadersJson,
                InitiatorUserId: command.InitiatorUserId);

            var normalizedEvent = normalizer.Normalize(
                normalizationContext);

            var redactionOutput = _redactionService.Redact(
                new AuditRedactionInput(
                    SourceModule: sourceEvent.SourceModule,
                    EventType: sourceEvent.EventType,
                    PayloadJson: normalizedEvent.JsonPayload.SanitizedPayloadJson,
                    HeadersJson: normalizedEvent.JsonPayload.HeadersJson,
                    MetadataJson: normalizedEvent.JsonPayload.MetadataJson,
                    BeforeJson: normalizedEvent.JsonPayload.BeforeJson,
                    AfterJson: normalizedEvent.JsonPayload.AfterJson,
                    ChangesJson: normalizedEvent.JsonPayload.ChangesJson));

            if (!redactionOutput.IsAllowed)
            {
                var errorInfo = _failureClassifier.RedactionBlocked(
                    redactionOutput.Reason);

                await _auditIngestionRepository.MarkFailedAsync(
                    command.MessageId,
                    errorInfo,
                    cancellationToken);

                return Result<IngestAuditEventResult>.Success(
                    IngestAuditEventResult.Failed(
                        messageId: command.MessageId,
                        auditIngestionId: upsertResult.AuditIngestionId,
                        reason: redactionOutput.Reason));
            }

            if (redactionOutput.JsonPayload is null)
            {
                var errorInfo = AuditErrorInfo.Redaction(
                    errorCode: "AUDIT_REDACTION_OUTPUT_MISSING",
                    errorMessage: "Audit redaction output did not contain a sanitized JSON payload.");

                await _auditIngestionRepository.MarkFailedAsync(
                    command.MessageId,
                    errorInfo,
                    cancellationToken);

                return Result<IngestAuditEventResult>.Failure(
                    AuditErrors.Redaction.Violation);
            }

            var auditLog = _evidenceBuilder.Build(
                new AuditEvidenceBuildInput(
                    PublicId: _publicIdGenerator.NewId(),
                    SourceEvent: sourceEvent,
                    AggregateRef: aggregateRef,
                    TraceContext: traceContext,
                    NormalizedEvent: normalizedEvent,
                    JsonPayload: redactionOutput.JsonPayload,
                    IngestedAtUtc: nowUtc));

            var insertResult = await _auditLogRepository.InsertAsync(
                auditLog,
                cancellationToken);

            if (!insertResult.WasInserted)
            {
                await _auditIngestionRepository.MarkDuplicateAsync(
                    command.MessageId,
                    cancellationToken);

                var existingAuditLog = await _auditLogRepository.GetByMessageIdAsync(
                    command.MessageId,
                    cancellationToken);

                return Result<IngestAuditEventResult>.Success(
                    IngestAuditEventResult.Duplicate(
                        messageId: command.MessageId,
                        auditIngestionId: upsertResult.AuditIngestionId,
                        auditLogId: existingAuditLog?.AuditLogId,
                        reason: "Audit log already exists for this message id."));
            }

            await _auditIngestionRepository.MarkSucceededAsync(
                command.MessageId,
                cancellationToken);

            return Result<IngestAuditEventResult>.Success(
                IngestAuditEventResult.Inserted(
                    messageId: command.MessageId,
                    auditIngestionId: upsertResult.AuditIngestionId,
                    auditLogId: insertResult.AuditLogId));
        }
        catch (Exception exception)
        {
            var errorInfo = _failureClassifier.Classify(
                exception);

            await TryMarkFailedAsync(
                command.MessageId,
                errorInfo,
                cancellationToken);

            return Result<IngestAuditEventResult>.Success(
                IngestAuditEventResult.Failed(
                    messageId: command.MessageId,
                    auditIngestionId: auditIngestionId,
                    reason: errorInfo.LastErrorMessage));
        }
    }

    private async Task<Result<IngestAuditEventResult>> MarkUnsupportedEventAsIgnoredAsync(
        IngestAuditEventCommand command,
        CancellationToken cancellationToken)
    {
        var nowUtc = _dateTimeProvider.UtcNow;

        var fallbackSourceEvent = AuditSourceEvent.Create(
            messageId: command.MessageId,
            eventType: command.EventType,
            eventVersion: null,
            sourceModule: AuditSourceModules.System,
            sourcePriority: command.Priority,
            sourceOccurredAtUtc: command.OccurredAtUtc,
            sourcePublishedAtUtc: command.PublishedAtUtc);

        var aggregateRef = AuditAggregateRef.Create(
            aggregateType: command.AggregateType,
            aggregateId: command.AggregateId,
            aggregatePublicId: command.AggregatePublicId,
            aggregateVersion: command.AggregateVersion);

        var traceContext = AuditTraceContext.Create(
            correlationId: command.CorrelationId,
            causationId: null,
            traceId: null);

        var ingestion = AuditIngestion.StartProcessing(
            publicId: _publicIdGenerator.NewId(),
            sourceEvent: fallbackSourceEvent,
            aggregateRef: aggregateRef,
            traceContext: traceContext,
            consumerName: command.ConsumerName,
            nowUtc: nowUtc);

        var upsertResult = await _auditIngestionRepository.UpsertProcessingAsync(
            ingestion,
            cancellationToken);

        if (!AuditIngestionStatuses.IsTerminal(upsertResult.CurrentStatus))
        {
            await _auditIngestionRepository.MarkIgnoredAsync(
                command.MessageId,
                cancellationToken);
        }

        return Result<IngestAuditEventResult>.Success(
            IngestAuditEventResult.Ignored(
                messageId: command.MessageId,
                auditIngestionId: upsertResult.AuditIngestionId,
                reason: $"Audit event type '{command.EventType}' is not supported."));
    }

    private async Task TryMarkFailedAsync(
        string messageId,
        AuditErrorInfo errorInfo,
        CancellationToken cancellationToken)
    {
        try
        {
            await _auditIngestionRepository.MarkFailedAsync(
                messageId,
                errorInfo,
                cancellationToken);
        }
        catch
        {
            // Do not hide the original ingestion failure.
            // Add infrastructure logging/metrics later.
        }
    }
}