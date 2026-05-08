using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Identity.Application.Models.QueryModels;
using Identity.Domain.Entities;

namespace Identity.Application.Ports.Persistence;

public interface ILoginHistoryRepository
{
    Task<long> InsertAsync(
        LoginHistory loginHistory,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LoginHistory>> GetByUserIdAsync(
        long userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LoginHistory>> GetRecentAsync(
        int topN = 100,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<LoginHistoryListResultItem>> SelectByUserIdAsync(
        LoginHistoryByUserQuery query,
        CancellationToken cancellationToken = default);
}