using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Notifications.Application.Ports.Services;

public interface IEmailDeliveryProcessingService
{
    Task<Result<int>> ProcessPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default);
}
