using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.ActivatePermission
{
    public interface IActivatePermissionUseCase
    {
        Task<ActivatePermissionResponseDto> ExecuteAsync(
            ActivatePermissionRequestDto request,
            CancellationToken cancellationToken);
    }
}