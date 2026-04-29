using System.Text.Json;
using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Consumers.Identity;
using Notifications.Application.Consumers.Identity.Payloads;

namespace CommercialNews.Worker.Notifications.Handlers.Identity;

public sealed class IdentityPasswordChangedIntegrationEventHandler
    : INotificationsIntegrationEventHandler
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private const string EventTypeValue = "identity.password_changed";

    private readonly IIdentityEmailEventIngestionService _ingestionService;

    public IdentityPasswordChangedIntegrationEventHandler(
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

        IdentityPasswordChangedPayload? payload;

        try
        {
            payload = envelope.Payload.Deserialize<IdentityPasswordChangedPayload>(
                JsonOptions);
        }
        catch (JsonException)
        {
            return Result.Failure(
                Error.Validation(
                    code: "NOTIFICATIONS.IDENTITY_PASSWORD_CHANGED_PAYLOAD_INVALID",
                    message: "Identity password changed payload is invalid."));
        }

        if (payload is null)
        {
            return Result.Failure(
                Error.Validation(
                    code: "NOTIFICATIONS.IDENTITY_PASSWORD_CHANGED_PAYLOAD_REQUIRED",
                    message: "Identity password changed payload is required."));
        }

        Result<long> result =
            await _ingestionService.IngestPasswordChangedAsync(
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