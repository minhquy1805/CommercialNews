using System.Text.Json;
using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Consumers.Content;
using Interaction.Application.Contracts.ArticleInteractionTargets.ApplyArticleInteractionTargetProjection;

namespace CommercialNews.Worker.Interaction.Handlers.Content;

public abstract class ContentInteractionIntegrationEventHandler<TPayload>
    : IInteractionIntegrationEventHandler
    where TPayload : class
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly ILogger _logger;

    protected ContentInteractionIntegrationEventHandler(
        IContentInteractionEventIngestionService ingestionService,
        ILogger logger)
    {
        IngestionService = ingestionService
            ?? throw new ArgumentNullException(nameof(ingestionService));

        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public abstract string EventType { get; }

    protected IContentInteractionEventIngestionService IngestionService { get; }

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
                "Failed to deserialize content {EventDisplayName} interaction payload. MessageId={MessageId}, EventType={EventType}",
                EventDisplayName,
                envelope.MessageId,
                envelope.EventType);

            return Result.Failure(
                Error.Validation(
                    code: BuildPayloadErrorCode("PAYLOAD_INVALID"),
                    message: $"Content {EventDisplayName} interaction payload is invalid."));
        }

        if (payload is null)
        {
            return Result.Failure(
                Error.Validation(
                    code: BuildPayloadErrorCode("PAYLOAD_REQUIRED"),
                    message: $"Content {EventDisplayName} interaction payload is required."));
        }

        ContentInteractionEnvelopeContext context =
            ContentInteractionEnvelopeContext.Create(
                messageId: envelope.MessageId,
                eventType: envelope.EventType,
                aggregateType: envelope.AggregateType,
                aggregateId: envelope.AggregateId,
                aggregatePublicId: envelope.AggregatePublicId,
                aggregateVersion: envelope.AggregateVersion,
                correlationId: envelope.CorrelationId,
                initiatorUserId: envelope.InitiatorUserId,
                occurredAtUtc: envelope.OccurredAtUtc);

        Result<ApplyArticleInteractionTargetProjectionResponseDto> result =
            await IngestAsync(
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
                "Failed to ingest content {EventDisplayName} interaction event. MessageId={MessageId}, EventType={EventType}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}, ErrorDetails={ErrorDetails}",
                EventDisplayName,
                envelope.MessageId,
                envelope.EventType,
                error.Code,
                error.Message,
                errorDetails);

            return Result.Failure(error);
        }

        ApplyArticleInteractionTargetProjectionResponseDto applyResult =
            result.Value!;

        _logger.LogInformation(
            "Content {EventDisplayName} interaction event ingested. MessageId={MessageId}, ArticlePublicId={ArticlePublicId}, ApplyDecision={ApplyDecision}, SourceStatus={SourceStatus}, IsInteractionEnabled={IsInteractionEnabled}, LastSourceVersion={LastSourceVersion}",
            EventDisplayName,
            envelope.MessageId,
            applyResult.ArticlePublicId,
            applyResult.ApplyDecision,
            applyResult.SourceStatus,
            applyResult.IsInteractionEnabled,
            applyResult.LastSourceVersion);

        return Result.Success();
    }

    protected abstract Task<Result<ApplyArticleInteractionTargetProjectionResponseDto>> IngestAsync(
        ContentInteractionEnvelopeContext context,
        TPayload payload,
        CancellationToken cancellationToken);

    private string BuildPayloadErrorCode(string suffix)
    {
        string eventKey = EventType.Trim();

        if (eventKey.StartsWith("content.", StringComparison.OrdinalIgnoreCase))
        {
            eventKey = eventKey.Substring("content.".Length);
        }

        eventKey = eventKey
            .Replace('.', '_')
            .Replace('-', '_')
            .ToUpperInvariant();

        return $"INTERACTION.CONTENT.{eventKey}_{suffix}";
    }
}
