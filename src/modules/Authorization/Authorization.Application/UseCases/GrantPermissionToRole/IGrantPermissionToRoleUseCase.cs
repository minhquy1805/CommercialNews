using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.Results;

namespace Authorization.Application.UseCases.GrantPermissionToRole
{
    public interface IGrantPermissionToRoleUseCase
    {
        Task<Result<GrantPermissionToRoleResponseDto>> ExecuteAsync(
            GrantPermissionToRoleRequestDto request,
            CancellationToken cancellationToken = default);
    }
}