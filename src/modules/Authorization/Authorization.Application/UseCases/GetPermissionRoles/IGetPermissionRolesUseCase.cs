using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.GetPermissionRoles
{
    public interface IGetPermissionRolesUseCase
    {
        Task<GetPermissionRolesResponseDto> ExecuteAsync(
            GetPermissionRolesRequestDto request,
            CancellationToken cancellationToken);
    }
}