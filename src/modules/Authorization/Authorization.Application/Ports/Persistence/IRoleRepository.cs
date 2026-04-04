using Authorization.Application.Models.QueryModels;
using Authorization.Domain.Entities;
using CommercialNews.BuildingBlocks.Contracts.Common;

namespace Authorization.Application.Ports.Persistence;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(
        long roleId,
        CancellationToken cancellationToken = default);

    Task<Role?> GetByNameNormalizedAsync(
        string nameNormalized,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<RoleListResultItem>> GetPagedAsync(
        int page,
        int pageSize,
        string? query,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<Role> InsertAsync(
        Role role,
        CancellationToken cancellationToken = default);

    Task<Role> UpdateAsync(
        Role role,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        long roleId,
        CancellationToken cancellationToken = default);
}