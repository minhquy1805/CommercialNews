using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.CreatePermission
{
    public interface ICreatePermissionUseCase
    {
        Task<CreatePermissionResponseDto> ExecuteAsync(
            CreatePermissionRequestDto request,
            CancellationToken cancellationToken);
    }
}