using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Consumers.Interaction.Payloads;
using Notifications.Application.Contracts.Ingestion;

namespace Notifications.Application.Consumers.Interaction;

public interface IInteractionNotificationEventIngestionService
{
    Task<Result<NotificationIngestionResult>> IngestCommentReportAlertTriggeredAsync(
        string messageId,
        string? correlationId,
        CommentReportAlertTriggeredNotificationPayload payload,
        CancellationToken cancellationToken = default);
}
