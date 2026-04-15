using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.GetUserEffectivePermissions
{
    public interface IGetUserEffectivePermissionsUseCase
    {
        Task<Result<GetUserEffectivePermissionsResponseDto>> ExecuteAsync(
            GetUserEffectivePermissionsRequestDto request,
            CancellationToken cancellationToken = default);
    }
}