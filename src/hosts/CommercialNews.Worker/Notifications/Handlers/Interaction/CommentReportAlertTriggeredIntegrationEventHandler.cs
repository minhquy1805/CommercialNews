using System.Text.Json;
using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Outbox;
using Notifications.Application.Consumers.Interaction;
using Notifications.Application.Consumers.Interaction.Payloads;
using Notifications.Application.Contracts.Ingestion;

namespace CommercialNews.Worker.Notifications.Handlers.Interaction;

public sealed class CommentReportAlertTriggeredIntegrationEventHandler
    : INotificationsIntegrationEventHandler
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IInteractionNotificationEventIngestionService
        _ingestionService;

    public CommentReportAlertTriggeredIntegrationEventHandler(
        IInteractionNotificationEventIngestionService ingestionService)
    {
        _ingestionService = ingestionService
            ?? throw new ArgumentNullException(nameof(ingestionService));
    }

    public string EventType =>
        InteractionIntegrationEventTypes.CommentReportAlertTriggered;

    public async Task<Result> HandleAsync(
        OutboxIntegrationEventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        CommentReportAlertTriggeredNotificationPayload? payload;

        try
        {
            payload = envelope.Payload
                .Deserialize<CommentReportAlertTriggeredNotificationPayload>(
                    JsonOptions);
        }
        catch (JsonException)
        {
            return Result.Failure(
                Error.Validation(
                    code: "NOTIFICATIONS.INTERACTION_COMMENT_REPORT_ALERT_PAYLOAD_INVALID",
                    message: "Interaction comment report alert payload is invalid."));
        }

        if (payload is null)
        {
            return Result.Failure(
                Error.Validation(
                    code: "NOTIFICATIONS.INTERACTION_COMMENT_REPORT_ALERT_PAYLOAD_REQUIRED",
                    message: "Interaction comment report alert payload is required."));
        }

        Result<NotificationIngestionResult> result =
            await _ingestionService.IngestCommentReportAlertTriggeredAsync(
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
