using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.GetRolePermissions
{
    public interface IGetRolePermissionsUseCase
    {
        Task<GetRolePermissionsResponseDto> ExecuteAsync(
            GetRolePermissionsRequestDto request,
            CancellationToken cancellationToken);
    }
}