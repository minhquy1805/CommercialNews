using System.Text.Json;
using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Consumers.Identity;
using Notifications.Application.Consumers.Identity.Payloads;
using Notifications.Application.Contracts.Ingestion;

namespace CommercialNews.Worker.Notifications.Handlers.Identity;

public sealed class IdentityPasswordResetRequestedIntegrationEventHandler
    : INotificationsIntegrationEventHandler
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private const string EventTypeValue = "identity.password_reset_requested";

    private readonly IIdentityEmailEventIngestionService _ingestionService;

    public IdentityPasswordResetRequestedIntegrationEventHandler(
        IIdentityEmailEventIngestionService ingestionService)
    {
        _ingestionService = ingestionService
            ?? throw new ArgumentNullException(nameof(ingestionService));
    }

    public string EventType => EventTypeValue;

    public async Task<Result> HandleAsync(
        OutboxIntegrationEventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        IdentityPasswordResetRequestedPayload? payload;

        try
        {
            payload = envelope.Payload.Deserialize<IdentityPasswordResetRequestedPayload>(
                JsonOptions);
        }
        catch (JsonException)
        {
            return Result.Failure(
                Error.Validation(
                    code: "NOTIFICATIONS.IDENTITY_PASSWORD_RESET_PAYLOAD_INVALID",
                    message: "Identity password reset requested payload is invalid."));
        }

        if (payload is null)
        {
            return Result.Failure(
                Error.Validation(
                    code: "NOTIFICATIONS.IDENTITY_PASSWORD_RESET_PAYLOAD_REQUIRED",
                    message: "Identity password reset requested payload is required."));
        }

        Result<NotificationIngestionResult> result =
            await _ingestionService.IngestPasswordResetRequestedAsync(
                messageId: envelope.MessageId,
                correlationId: envelope.CorrelationId,
                payload: payload,
                cancellationToken: cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure(result.Error!);
        }

        return Result.Success();
    }
}