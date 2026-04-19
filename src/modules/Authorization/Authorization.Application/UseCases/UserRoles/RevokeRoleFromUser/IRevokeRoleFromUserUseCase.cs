using Authorization.Application.Contracts.UserRoles;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.UserRoles.RevokeRoleFromUser;

public interface IRevokeRoleFromUserUseCase
{
    Task<Result<RevokeRoleFromUserResponseDto>> ExecuteAsync(
        RevokeRoleFromUserRequestDto request,
        CancellationToken cancellationToken = default);
}