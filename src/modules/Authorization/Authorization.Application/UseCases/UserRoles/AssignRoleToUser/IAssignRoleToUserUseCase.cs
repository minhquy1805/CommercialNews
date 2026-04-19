using Authorization.Application.Contracts.UserRoles;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.UserRoles.AssignRoleToUser;

public interface IAssignRoleToUserUseCase
{
    Task<Result<AssignRoleToUserResponseDto>> ExecuteAsync(
        AssignRoleToUserRequestDto request,
        CancellationToken cancellationToken = default);
}