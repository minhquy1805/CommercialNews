using Authorization.Application.Contracts.UserRoles;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.UserRoles.GetUserRoles;

public interface IGetUserRolesUseCase
{
    Task<Result<GetUserRolesResponseDto>> ExecuteAsync(
        GetUserRolesRequestDto request,
        CancellationToken cancellationToken = default);
}