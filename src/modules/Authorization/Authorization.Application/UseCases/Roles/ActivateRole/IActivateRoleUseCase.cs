using Authorization.Application.Contracts.Roles;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.Roles.ActivateRole;

public interface IActivateRoleUseCase
{
    Task<Result<ActivateRoleResponseDto>> ExecuteAsync(
        ActivateRoleRequestDto request,
        CancellationToken cancellationToken = default);
}