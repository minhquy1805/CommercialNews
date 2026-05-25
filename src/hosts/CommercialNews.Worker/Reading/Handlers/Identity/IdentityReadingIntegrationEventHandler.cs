using System.Text.Json;
using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Consumers.Identity;
using Reading.Application.Models.Results;

namespace CommercialNews.Worker.Reading.Handlers.Identity;

public abstract class IdentityReadingIntegrationEventHandler<TPayload>
    : IReadingIntegrationEventHandler
    where TPayload : class
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly ILogger _logger;

    protected IdentityReadingIntegrationEventHandler(
        IIdentityReadingEventIngestionService ingestionService,
        ILogger logger)
    {
        IngestionService = ingestionService
            ?? throw new ArgumentNullException(nameof(ingestionService));

        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public abstract string EventType { get; }

    protected IIdentityReadingEventIngestionService IngestionService { get; }

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
                "Failed to deserialize identity {EventDisplayName} reading payload. MessageId={MessageId}, EventType={EventType}",
                EventDisplayName,
                envelope.MessageId,
                envelope.EventType);

            return Result.Failure(
                Error.Validation(
                    code: BuildPayloadErrorCode("PAYLOAD_INVALID"),
                    message: $"Identity {EventDisplayName} reading payload is invalid."));
        }

        if (payload is null)
        {
            return Result.Failure(
                Error.Validation(
                    code: BuildPayloadErrorCode("PAYLOAD_REQUIRED"),
                    message: $"Identity {EventDisplayName} reading payload is required."));
        }

        IdentityReadingEnvelopeContext context = IdentityReadingEnvelopeContext.Create(
            messageId: envelope.MessageId,
            eventType: envelope.EventType,
            aggregateType: envelope.AggregateType,
            aggregateId: envelope.AggregateId,
            aggregatePublicId: envelope.AggregatePublicId,
            aggregateVersion: envelope.AggregateVersion,
            correlationId: envelope.CorrelationId,
            initiatorUserId: envelope.InitiatorUserId,
            occurredAtUtc: envelope.OccurredAtUtc);

        Result<ArticleProjectionApplyResult> result = await IngestAsync(
            context,
            payload,
            cancellationToken);

        if (result.IsFailure)
        {
            Error error = result.Error!;
            string errorDetails = error.Details.Count > 0
                ? string.Join(" | ", error.Details)
                : string.Empty;

            _logger.LogWarning(
                "Failed to ingest identity {EventDisplayName} reading event. MessageId={MessageId}, EventType={EventType}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}, ErrorDetails={ErrorDetails}",
                EventDisplayName,
                envelope.MessageId,
                envelope.EventType,
                error.Code,
                error.Message,
                errorDetails);

            return Result.Failure(error);
        }

        ArticleProjectionApplyResult applyResult = result.Value;

        _logger.LogInformation(
            "Identity {EventDisplayName} reading event ingested. MessageId={MessageId}, Applied={Applied}, Decision={Decision}, PreviousSourceVersion={PreviousSourceVersion}, IncomingSourceVersion={IncomingSourceVersion}",
            EventDisplayName,
            envelope.MessageId,
            applyResult.Applied,
            applyResult.Decision,
            applyResult.PreviousSourceVersion,
            applyResult.IncomingSourceVersion);

        return Result.Success();
    }

    protected abstract Task<Result<ArticleProjectionApplyResult>> IngestAsync(
        IdentityReadingEnvelopeContext context,
        TPayload payload,
        CancellationToken cancellationToken);

    private string BuildPayloadErrorCode(string suffix)
    {
        string eventKey = EventType.Trim();

        if (eventKey.StartsWith("identity.", StringComparison.OrdinalIgnoreCase))
        {
            eventKey = eventKey.Substring("identity.".Length);
        }

        eventKey = eventKey
            .Replace('.', '_')
            .Replace('-', '_')
            .ToUpperInvariant();

        return $"READING.IDENTITY.{eventKey}_{suffix}";
    }
}
