using Authorization.Application.Models.QueryModels;
using Authorization.Domain.Entities;
using CommercialNews.BuildingBlocks.Contracts.Common;

namespace Authorization.Application.Ports.Persistence;

public interface IPermissionRepository
{
    Task<Permission?> GetByIdAsync(
        long permissionId,
        CancellationToken cancellationToken = default);

    Task<Permission?> GetByNameNormalizedAsync(
        string nameNormalized,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<PermissionListResultItem>> GetPagedAsync(
        int page,
        int pageSize,
        string? query,
        string? module,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<Permission> InsertAsync(
        Permission permission,
        CancellationToken cancellationToken = default);

    Task<Permission> UpdateAsync(
        Permission permission,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        long permissionId,
        CancellationToken cancellationToken = default);
}