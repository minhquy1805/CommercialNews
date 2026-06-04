using System.Text.Json;
using Audit.Application.Models.Commands.Ingestion;
using Audit.Application.Models.Results.Ingestion;
using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace CommercialNews.Worker.Audit.Handlers;

public sealed class AuditMessageHandler
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuditMessageHandler> _logger;

    public AuditMessageHandler(
        IMediator mediator,
        ILogger<AuditMessageHandler> logger)
    {
        _mediator = mediator
            ?? throw new ArgumentNullException(nameof(mediator));

        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> HandleAsync(
        OutboxIntegrationEventEnvelope envelope,
        string consumerName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        string normalizedConsumerName = NormalizeRequired(
            consumerName,
            nameof(consumerName));

        Result validationResult = ValidateEnvelope(envelope);

        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        IngestAuditEventCommand command = MapToCommand(
            envelope,
            normalizedConsumerName);

        Result<IngestAuditEventResult> result = await _mediator.Send(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            Error error = result.Error!;

            _logger.LogWarning(
                "Audit message ingestion failed. MessageId={MessageId}, EventType={EventType}, ConsumerName={ConsumerName}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                envelope.MessageId,
                envelope.EventType,
                normalizedConsumerName,
                error.Code,
                error.Message);

            return Result.Failure(error);
        }

        IngestAuditEventResult ingestionResult = result.Value!;

        _logger.LogInformation(
            "Audit message ingestion completed. MessageId={MessageId}, EventType={EventType}, ConsumerName={ConsumerName}, Status={Status}, AuditIngestionId={AuditIngestionId}, AuditLogId={AuditLogId}, WasInserted={WasInserted}, Reason={Reason}",
            ingestionResult.MessageId,
            envelope.EventType,
            normalizedConsumerName,
            ingestionResult.Status,
            ingestionResult.AuditIngestionId,
            ingestionResult.AuditLogId,
            ingestionResult.WasInserted,
            ingestionResult.Reason);

        return Result.Success();
    }

    private static Result ValidateEnvelope(
        OutboxIntegrationEventEnvelope envelope)
    {
        if (string.IsNullOrWhiteSpace(envelope.MessageId))
        {
            return Result.Failure(
                Error.Validation(
                    code: "AUDIT.MESSAGE_ID_REQUIRED",
                    message: "Audit message id is required."));
        }

        if (string.IsNullOrWhiteSpace(envelope.EventType))
        {
            return Result.Failure(
                Error.Validation(
                    code: "AUDIT.EVENT_TYPE_REQUIRED",
                    message: "Audit event type is required."));
        }

        if (string.IsNullOrWhiteSpace(envelope.AggregateType))
        {
            return Result.Failure(
                Error.Validation(
                    code: "AUDIT.AGGREGATE_TYPE_REQUIRED",
                    message: "Audit aggregate type is required."));
        }

        if (string.IsNullOrWhiteSpace(envelope.AggregateId))
        {
            return Result.Failure(
                Error.Validation(
                    code: "AUDIT.AGGREGATE_ID_REQUIRED",
                    message: "Audit aggregate id is required."));
        }

        if (envelope.Priority is < 1 or > 9)
        {
            return Result.Failure(
                Error.Validation(
                    code: "AUDIT.INVALID_PRIORITY",
                    message: "Audit event priority must be between 1 and 9."));
        }

        if (envelope.OccurredAtUtc == default)
        {
            return Result.Failure(
                Error.Validation(
                    code: "AUDIT.OCCURRED_AT_UTC_REQUIRED",
                    message: "Audit event occurred time is required."));
        }

        return ValidatePayload(envelope.Payload);
    }

    private static Result ValidatePayload(
        JsonElement payload)
    {
        if (payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return Result.Failure(
                Error.Validation(
                    code: "AUDIT.PAYLOAD_REQUIRED",
                    message: "Audit event payload is required."));
        }

        return Result.Success();
    }

    private static IngestAuditEventCommand MapToCommand(
        OutboxIntegrationEventEnvelope envelope,
        string consumerName)
    {
        return new IngestAuditEventCommand(
            MessageId: NormalizeRequired(
                envelope.MessageId,
                nameof(envelope.MessageId)),
            EventType: NormalizeRequired(
                envelope.EventType,
                nameof(envelope.EventType)),
            AggregateType: NormalizeRequired(
                envelope.AggregateType,
                nameof(envelope.AggregateType)),
            AggregateId: NormalizeRequired(
                envelope.AggregateId,
                nameof(envelope.AggregateId)),
            AggregatePublicId: NormalizeOptional(
                envelope.AggregatePublicId),
            AggregateVersion: envelope.AggregateVersion,
            PayloadJson: envelope.Payload.GetRawText(),
            HeadersJson: envelope.Headers?.GetRawText(),
            CorrelationId: NormalizeOptional(
                envelope.CorrelationId),
            InitiatorUserId: envelope.InitiatorUserId,
            Priority: envelope.Priority,
            OccurredAtUtc: EnsureUtc(
                envelope.OccurredAtUtc,
                nameof(envelope.OccurredAtUtc)),
            PublishedAtUtc: envelope.PublishedAtUtc is null
                ? null
                : EnsureUtc(
                    envelope.PublishedAtUtc.Value,
                    nameof(envelope.PublishedAtUtc)),
            ConsumerName: consumerName);
    }

    private static DateTime EnsureUtc(
        DateTime value,
        string parameterName)
    {
        if (value == default)
        {
            throw new InvalidOperationException(
                $"Audit message field '{parameterName}' is required.");
        }

        if (value.Kind == DateTimeKind.Utc)
        {
            return value;
        }

        if (value.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc);
        }

        return value.ToUniversalTime();
    }

    private static string NormalizeRequired(
        string? value,
        string parameterName)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException(
                $"Audit message field '{parameterName}' is required.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(
        string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }
}
