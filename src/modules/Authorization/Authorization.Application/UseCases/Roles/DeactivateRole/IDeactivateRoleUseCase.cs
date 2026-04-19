using Authorization.Application.Contracts.Roles;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.Roles.DeactivateRole;

public interface IDeactivateRoleUseCase
{
    Task<Result<DeactivateRoleResponseDto>> ExecuteAsync(
        DeactivateRoleRequestDto request,
        CancellationToken cancellationToken = default);
}