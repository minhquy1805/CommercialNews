using System.Text.Json;
using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.Worker.Seo.Handlers;
using Seo.Application.Consumers.Content;
using Seo.Application.Contracts.EventApply;

namespace CommercialNews.Worker.Seo.Handlers.Content;

public abstract class ContentSeoIntegrationEventHandler<TPayload>
    : ISeoIntegrationEventHandler
    where TPayload : class
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly ILogger _logger;

    protected ContentSeoIntegrationEventHandler(
        IContentSeoEventApplyService applyService,
        ILogger logger)
    {
        ApplyService = applyService
            ?? throw new ArgumentNullException(nameof(applyService));

        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public abstract string EventType { get; }

    protected IContentSeoEventApplyService ApplyService { get; }

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
                "Failed to deserialize content {EventDisplayName} SEO payload. MessageId={MessageId}, EventType={EventType}",
                EventDisplayName,
                envelope.MessageId,
                envelope.EventType);

            return Result.Failure(
                Error.Validation(
                    code: BuildPayloadErrorCode("PAYLOAD_INVALID"),
                    message: $"Content {EventDisplayName} SEO payload is invalid."));
        }

        if (payload is null)
        {
            return Result.Failure(
                Error.Validation(
                    code: BuildPayloadErrorCode("PAYLOAD_REQUIRED"),
                    message: $"Content {EventDisplayName} SEO payload is required."));
        }

        ContentSeoEnvelopeContext context = ContentSeoEnvelopeContext.Create(
            messageId: envelope.MessageId,
            eventType: envelope.EventType,
            aggregateType: envelope.AggregateType,
            aggregateId: envelope.AggregateId,
            aggregatePublicId: envelope.AggregatePublicId,
            aggregateVersion: envelope.AggregateVersion,
            correlationId: envelope.CorrelationId,
            initiatorUserId: envelope.InitiatorUserId,
            occurredAtUtc: envelope.OccurredAtUtc);

        Result<SeoEventApplyResult> result = await ApplyAsync(
            context,
            payload,
            cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;

            _logger.LogWarning(
                "Failed to apply content {EventDisplayName} SEO event. MessageId={MessageId}, EventType={EventType}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                EventDisplayName,
                envelope.MessageId,
                envelope.EventType,
                error.Code,
                error.Message);

            return Result.Failure(error);
        }

        SeoEventApplyResult applyResult = result.Value!;

        _logger.LogInformation(
            "Content {EventDisplayName} SEO event applied. MessageId={MessageId}, ResourceType={ResourceType}, ResourcePublicId={ResourcePublicId}, WasApplied={WasApplied}, WasDeduped={WasDeduped}, WasStaleIgnored={WasStaleIgnored}",
            EventDisplayName,
            envelope.MessageId,
            applyResult.ResourceType,
            applyResult.ResourcePublicId,
            applyResult.WasApplied,
            applyResult.WasDeduped,
            applyResult.WasStaleIgnored);

        return Result.Success();
    }

    protected abstract Task<Result<SeoEventApplyResult>> ApplyAsync(
        ContentSeoEnvelopeContext context,
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

        return $"SEO.CONTENT.{eventKey}_{suffix}";
    }
}