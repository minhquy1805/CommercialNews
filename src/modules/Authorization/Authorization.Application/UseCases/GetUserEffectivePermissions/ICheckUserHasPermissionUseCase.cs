using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.CheckUserHasPermission
{
    public interface ICheckUserHasPermissionUseCase
    {
        Task<CheckUserHasPermissionResponseDto> ExecuteAsync(
            CheckUserHasPermissionRequestDto request,
            CancellationToken cancellationToken);
    }
}