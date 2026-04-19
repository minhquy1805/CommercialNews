using Authorization.Application.Contracts.Queries;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.Queries.GetUserEffectivePermissions;

public interface IGetUserEffectivePermissionsUseCase
{
    Task<Result<GetUserEffectivePermissionsResponseDto>> ExecuteAsync(
        GetUserEffectivePermissionsRequestDto request,
        CancellationToken cancellationToken = default);
}