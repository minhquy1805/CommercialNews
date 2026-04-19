using Authorization.Application.Models.QueryModels;
using Authorization.Domain.Entities;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;

namespace Authorization.Application.Ports.Persistence;

public interface IPermissionRepository
{
    Task<Permission?> GetByIdAsync(
        long permissionId,
        CancellationToken cancellationToken = default);

    Task<Permission?> GetByKeyNormalizedAsync(
        string keyNormalized,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<PermissionListResultItem>> GetPagedAsync(
        int page,
        int pageSize,
        string? query,
        string? module,
        string? action,
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