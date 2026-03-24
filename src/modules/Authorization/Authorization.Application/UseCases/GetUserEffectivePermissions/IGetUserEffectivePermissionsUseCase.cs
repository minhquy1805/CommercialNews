using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.GetUserEffectivePermissions
{
    public interface IGetUserEffectivePermissionsUseCase
    {
        Task<GetUserEffectivePermissionsResponseDto> ExecuteAsync(
            GetUserEffectivePermissionsRequestDto request,
            CancellationToken cancellationToken);
    }
}