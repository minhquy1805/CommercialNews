using Authorization.Application.Contracts.Roles;
using Authorization.Application.Models.QueryModels;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.Roles.GetRoles;

public interface IGetRolesUseCase
{
    Task<Result<PagedQueryResult<RoleListResultItem>>> ExecuteAsync(
        GetRolesRequestDto request,
        CancellationToken cancellationToken = default);
}