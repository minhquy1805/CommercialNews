using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Notifications.Application.Models.QueryModels;

namespace Notifications.Application.Ports.Persistence;

public interface IEmailDeliveryQueryRepository
{
    Task<EmailDeliveryDetailResult?> GetByIdAsync(
        long emailDeliveryId,
        CancellationToken cancellationToken = default);

    Task<EmailDeliveryDetailResult?> GetByMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<EmailDeliveryListResultItem>> SelectSkipAndTakeAsync(
        EmailDeliveryListQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailDeliveryAttemptResultItem>> GetAttemptsByEmailDeliveryIdAsync(
        long emailDeliveryId,
        CancellationToken cancellationToken = default);
}