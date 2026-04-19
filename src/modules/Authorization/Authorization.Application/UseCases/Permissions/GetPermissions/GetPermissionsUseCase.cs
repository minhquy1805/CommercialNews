using Authorization.Application.Contracts.Permissions;
using Authorization.Application.Errors;
using Authorization.Application.Models.QueryModels;
using Authorization.Application.Ports.Persistence;
using Authorization.Application.Validation.Permissions;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.Permissions.GetPermissions;

public sealed class GetPermissionsUseCase : IGetPermissionsUseCase
{
    private readonly IPermissionRepository _permissionRepository;

    public GetPermissionsUseCase(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    public async Task<Result<PagedQueryResult<PermissionListResultItem>>> ExecuteAsync(
        GetPermissionsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = GetPermissionsValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<PagedQueryResult<PermissionListResultItem>>.Failure(validationError);
        }

        var result = await _permissionRepository.GetPagedAsync(
            page: request.Page,
            pageSize: request.PageSize,
            query: request.Query,
            module: request.Module,
            action: request.Action,
            isActive: request.IsActive,
            cancellationToken: cancellationToken);

        return Result<PagedQueryResult<PermissionListResultItem>>.Success(result);
    }
}