using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.DeactivatePermission
{
    public interface IDeactivatePermissionUseCase
    {
        Task<DeactivatePermissionResponseDto> ExecuteAsync(
            DeactivatePermissionRequestDto request,
            CancellationToken cancellationToken);
    }
}