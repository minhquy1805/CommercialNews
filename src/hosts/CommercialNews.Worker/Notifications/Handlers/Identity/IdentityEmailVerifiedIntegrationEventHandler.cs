using System.Text.Json;
using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Consumers.Identity;
using Notifications.Application.Consumers.Identity.Payloads;
using Notifications.Application.Contracts.Ingestion;

namespace CommercialNews.Worker.Notifications.Handlers.Identity;

public sealed class IdentityEmailVerifiedIntegrationEventHandler
    : INotificationsIntegrationEventHandler
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private const string EventTypeValue = "identity.email_verified";

    private readonly IIdentityEmailEventIngestionService _ingestionService;
    private readonly ILogger<IdentityEmailVerifiedIntegrationEventHandler> _logger;

    public IdentityEmailVerifiedIntegrationEventHandler(
        IIdentityEmailEventIngestionService ingestionService,
        ILogger<IdentityEmailVerifiedIntegrationEventHandler> logger)
    {
        _ingestionService = ingestionService
            ?? throw new ArgumentNullException(nameof(ingestionService));

        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public string EventType => EventTypeValue;

    public async Task<Result> HandleAsync(
        OutboxIntegrationEventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        IdentityEmailVerifiedPayload? payload;

        try
        {
            payload = envelope.Payload.Deserialize<IdentityEmailVerifiedPayload>(
                JsonOptions);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to deserialize identity email verified payload. MessageId={MessageId}, EventType={EventType}",
                envelope.MessageId,
                envelope.EventType);

            return Result.Failure(
                Error.Validation(
                    code: "NOTIFICATIONS.IDENTITY_EMAIL_VERIFIED_PAYLOAD_INVALID",
                    message: "Identity email verified payload is invalid."));
        }

        if (payload is null)
        {
            return Result.Failure(
                Error.Validation(
                    code: "NOTIFICATIONS.IDENTITY_EMAIL_VERIFIED_PAYLOAD_REQUIRED",
                    message: "Identity email verified payload is required."));
        }

        Result<NotificationIngestionResult> result =
            await _ingestionService.IngestEmailVerifiedAsync(
                messageId: envelope.MessageId,
                correlationId: envelope.CorrelationId,
                payload: payload,
                cancellationToken: cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure(result.Error!);
        }

        NotificationIngestionResult ingestionResult = result.Value!;

        _logger.LogInformation(
            "Identity email verified notification ingested. MessageId={MessageId}, EmailDeliveryId={EmailDeliveryId}, WasInserted={WasInserted}, WasDeduped={WasDeduped}",
            envelope.MessageId,
            ingestionResult.EmailDeliveryId,
            ingestionResult.WasInserted,
            ingestionResult.WasDeduped);

        return Result.Success();
    }
}