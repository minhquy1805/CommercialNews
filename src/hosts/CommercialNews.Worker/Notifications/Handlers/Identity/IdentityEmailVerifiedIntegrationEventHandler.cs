using System.Text.Json;
using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Consumers.Identity;
using Notifications.Application.Consumers.Identity.Payloads;

namespace CommercialNews.Worker.Notifications.Handlers.Identity;

public sealed class IdentityEmailVerifiedIntegrationEventHandler
    : INotificationsIntegrationEventHandler
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private const string EventTypeValue = "identity.email_verified";

    private readonly IIdentityEmailEventIngestionService _ingestionService;

    public IdentityEmailVerifiedIntegrationEventHandler(
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

        IdentityEmailVerifiedPayload? payload;

        try
        {
            payload = envelope.Payload.Deserialize<IdentityEmailVerifiedPayload>(
                JsonOptions);
        }
        catch (JsonException)
        {
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

        Result<long> result =
            await _ingestionService.IngestEmailVerifiedAsync(
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