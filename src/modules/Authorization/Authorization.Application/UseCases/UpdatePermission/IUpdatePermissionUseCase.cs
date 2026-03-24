using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.UpdatePermission
{
    public interface IUpdatePermissionUseCase
    {
        Task<UpdatePermissionResponseDto> ExecuteAsync(
            UpdatePermissionRequestDto request,
            CancellationToken cancellationToken);
    }
}