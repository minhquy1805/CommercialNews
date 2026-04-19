using Authorization.Application.Contracts.Roles;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.Roles.UpdateRole;

public interface IUpdateRoleUseCase
{
    Task<Result<UpdateRoleResponseDto>> ExecuteAsync(
        UpdateRoleRequestDto request,
        CancellationToken cancellationToken = default);
}