using System.Text.Json;
using Audit.Application.Consumers.Media;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.Worker.Audit.Handlers;

namespace CommercialNews.Worker.Audit.Handlers.Media;

public abstract class MediaAuditIntegrationEventHandler<TPayload>
    : IAuditIntegrationEventHandler
    where TPayload : class
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly ILogger _logger;

    protected MediaAuditIntegrationEventHandler(
        IMediaAuditEventIngestionService ingestionService,
        ILogger logger)
    {
        IngestionService = ingestionService
            ?? throw new ArgumentNullException(nameof(ingestionService));

        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public abstract string EventType { get; }

    protected IMediaAuditEventIngestionService IngestionService { get; }

    protected abstract string EventDisplayName { get; }

    public async Task<Result> HandleAsync(
        OutboxIntegrationEventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        TPayload? payload;

        try
        {
            payload = envelope.Payload.Deserialize<TPayload>(JsonOptions);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to deserialize media {EventDisplayName} audit payload. MessageId={MessageId}, EventType={EventType}",
                EventDisplayName,
                envelope.MessageId,
                envelope.EventType);

            return Result.Failure(
                Error.Validation(
                    code: BuildPayloadErrorCode("PAYLOAD_INVALID"),
                    message: $"Media {EventDisplayName} audit payload is invalid."));
        }

        if (payload is null)
        {
            return Result.Failure(
                Error.Validation(
                    code: BuildPayloadErrorCode("PAYLOAD_REQUIRED"),
                    message: $"Media {EventDisplayName} audit payload is required."));
        }

        MediaAuditEnvelopeContext context = MediaAuditEnvelopeContext.Create(
            messageId: envelope.MessageId,
            eventType: envelope.EventType,
            aggregateType: envelope.AggregateType,
            aggregateId: envelope.AggregateId,
            aggregatePublicId: envelope.AggregatePublicId,
            aggregateVersion: envelope.AggregateVersion,
            correlationId: envelope.CorrelationId,
            initiatorUserId: envelope.InitiatorUserId,
            occurredAtUtc: envelope.OccurredAtUtc);

        Result<AuditIngestionResult> result = await IngestAsync(
            context,
            payload,
            cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;

            _logger.LogWarning(
                "Failed to ingest media {EventDisplayName} audit event. MessageId={MessageId}, EventType={EventType}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                EventDisplayName,
                envelope.MessageId,
                envelope.EventType,
                error.Code,
                error.Message);

            return Result.Failure(error);
        }

        var ingestionResult = result.Value!;

        _logger.LogInformation(
            "Media {EventDisplayName} audit event ingested. MessageId={MessageId}, AuditId={AuditId}, WasInserted={WasInserted}, WasDeduped={WasDeduped}",
            EventDisplayName,
            envelope.MessageId,
            ingestionResult.AuditId,
            ingestionResult.WasInserted,
            ingestionResult.WasDeduped);

        return Result.Success();
    }

    protected abstract Task<Result<AuditIngestionResult>> IngestAsync(
        MediaAuditEnvelopeContext context,
        TPayload payload,
        CancellationToken cancellationToken);

    private string BuildPayloadErrorCode(string suffix)
    {
        string eventKey = EventType.Trim();

        if (eventKey.StartsWith("media.", StringComparison.OrdinalIgnoreCase))
        {
            eventKey = eventKey.Substring("media.".Length);
        }

        eventKey = eventKey
            .Replace('.', '_')
            .Replace('-', '_')
            .ToUpperInvariant();

        return $"AUDIT.MEDIA.{eventKey}_{suffix}";
    }
}