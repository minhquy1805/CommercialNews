using System.Text.Json;
using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Consumers.Stats;
using Interaction.Application.Contracts.ArticleInteractionStats.MaterializeArticleInteractionStats;

namespace CommercialNews.Worker.Interaction.Handlers.Stats;

public abstract class InteractionStatsIntegrationEventHandler<TPayload>
    : IInteractionIntegrationEventHandler
    where TPayload : class
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly ILogger _logger;

    protected InteractionStatsIntegrationEventHandler(
        IInteractionStatsEventIngestionService ingestionService,
        ILogger logger)
    {
        IngestionService = ingestionService
            ?? throw new ArgumentNullException(nameof(ingestionService));

        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public abstract string EventType { get; }

    protected IInteractionStatsEventIngestionService IngestionService { get; }

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
                "Failed to deserialize interaction stats {EventDisplayName} payload. MessageId={MessageId}, EventType={EventType}",
                EventDisplayName,
                envelope.MessageId,
                envelope.EventType);

            return Result.Failure(
                Error.Validation(
                    code: BuildPayloadErrorCode("PAYLOAD_INVALID"),
                    message: $"Interaction stats {EventDisplayName} payload is invalid."));
        }

        if (payload is null)
        {
            return Result.Failure(
                Error.Validation(
                    code: BuildPayloadErrorCode("PAYLOAD_REQUIRED"),
                    message: $"Interaction stats {EventDisplayName} payload is required."));
        }

        string articlePublicId = GetArticlePublicId(payload);

        Result<MaterializeArticleInteractionStatsResponseDto> result =
            await IngestionService.IngestAsync(
                articlePublicId,
                cancellationToken);

        if (result.IsFailure)
        {
            Error error = result.Error!;
            string errorDetails = error.Details.Count > 0
                ? string.Join(" | ", error.Details)
                : string.Empty;

            _logger.LogWarning(
                "Failed to materialize interaction stats from {EventDisplayName} event. MessageId={MessageId}, EventType={EventType}, ArticlePublicId={ArticlePublicId}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}, ErrorDetails={ErrorDetails}",
                EventDisplayName,
                envelope.MessageId,
                envelope.EventType,
                articlePublicId,
                error.Code,
                error.Message,
                errorDetails);

            return Result.Failure(error);
        }

        MaterializeArticleInteractionStatsResponseDto materializeResult =
            result.Value!;

        _logger.LogInformation(
            "Interaction stats {EventDisplayName} event materialized. MessageId={MessageId}, EventType={EventType}, ArticlePublicId={ArticlePublicId}, SnapshotChanged={SnapshotChanged}, StatsVersion={StatsVersion}, ViewCount={ViewCount}, LikeCount={LikeCount}, VisibleCommentCount={VisibleCommentCount}",
            EventDisplayName,
            envelope.MessageId,
            envelope.EventType,
            materializeResult.ArticlePublicId,
            materializeResult.SnapshotChanged,
            materializeResult.StatsVersion,
            materializeResult.ViewCount,
            materializeResult.LikeCount,
            materializeResult.VisibleCommentCount);

        return Result.Success();
    }

    protected abstract string GetArticlePublicId(TPayload payload);

    private string BuildPayloadErrorCode(string suffix)
    {
        string eventKey = EventType.Trim();

        if (eventKey.StartsWith("interaction.", StringComparison.OrdinalIgnoreCase))
        {
            eventKey = eventKey.Substring("interaction.".Length);
        }

        eventKey = eventKey
            .Replace('.', '_')
            .Replace('-', '_')
            .ToUpperInvariant();

        return $"INTERACTION.STATS.{eventKey}_{suffix}";
    }
}
