using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.Ingestion;

namespace Notifications.Application.Ports.Services;

public interface INotificationIngestionService
{
    Task<Result<NotificationIngestionResult>> IngestEmailAsync(
        EmailNotificationIngestionRequest request,
        CancellationToken cancellationToken = default);
}
