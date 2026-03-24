using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.RevokePermissionFromRole
{
    public interface IRevokePermissionFromRoleUseCase
    {
        Task<RevokePermissionFromRoleResponseDto> ExecuteAsync(
            RevokePermissionFromRoleRequestDto request,
            CancellationToken cancellationToken);
    }
}