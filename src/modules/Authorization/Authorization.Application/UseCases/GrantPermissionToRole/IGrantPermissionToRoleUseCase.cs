using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.GrantPermissionToRole
{
    public interface IGrantPermissionToRoleUseCase
    {
        Task<GrantPermissionToRoleResponseDto> ExecuteAsync(
            GrantPermissionToRoleRequestDto request,
            CancellationToken cancellationToken);
    }
}