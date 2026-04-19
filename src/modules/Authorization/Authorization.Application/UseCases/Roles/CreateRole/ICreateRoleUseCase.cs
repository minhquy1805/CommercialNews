using Authorization.Application.Contracts.Roles;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.Roles.CreateRole;

public interface ICreateRoleUseCase
{
    Task<Result<CreateRoleResponseDto>> ExecuteAsync(
        CreateRoleRequestDto request,
        CancellationToken cancellationToken = default);
}