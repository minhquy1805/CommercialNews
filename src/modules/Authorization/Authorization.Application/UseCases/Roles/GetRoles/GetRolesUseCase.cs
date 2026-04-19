using Authorization.Application.Contracts.Roles;
using Authorization.Application.Models.QueryModels;
using Authorization.Application.Ports.Persistence;
using Authorization.Application.Validation.Roles;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.Roles.GetRoles;

public sealed class GetRolesUseCase : IGetRolesUseCase
{
    private readonly IRoleRepository _roleRepository;

    public GetRolesUseCase(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<Result<PagedQueryResult<RoleListResultItem>>> ExecuteAsync(
        GetRolesRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = GetRolesValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<PagedQueryResult<RoleListResultItem>>.Failure(validationError);
        }

        var result = await _roleRepository.GetPagedAsync(
            page: request.Page,
            pageSize: request.PageSize,
            query: request.Query,
            isActive: request.IsActive,
            cancellationToken: cancellationToken);

        return Result<PagedQueryResult<RoleListResultItem>>.Success(result);
    }
}