using Authorization.Application.Contracts.Permissions;
using Authorization.Application.Models.QueryModels;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.Permissions.GetPermissions;

public interface IGetPermissionsUseCase
{
    Task<Result<PagedQueryResult<PermissionListResultItem>>> ExecuteAsync(
        GetPermissionsRequestDto request,
        CancellationToken cancellationToken = default);
}