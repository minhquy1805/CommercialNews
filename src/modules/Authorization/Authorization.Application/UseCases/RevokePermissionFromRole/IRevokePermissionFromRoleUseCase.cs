using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.Results;

namespace Authorization.Application.UseCases.RevokePermissionFromRole
{
    public interface IRevokePermissionFromRoleUseCase
    {
        Task<Result<RevokePermissionFromRoleResponseDto>> ExecuteAsync(
            RevokePermissionFromRoleRequestDto request,
            CancellationToken cancellationToken = default);
    }
}