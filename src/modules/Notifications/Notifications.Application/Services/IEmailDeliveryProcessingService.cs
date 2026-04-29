using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Notifications.Application.Services;

public interface IEmailDeliveryProcessingService
{
    Task<Result<int>> ProcessPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default);
}