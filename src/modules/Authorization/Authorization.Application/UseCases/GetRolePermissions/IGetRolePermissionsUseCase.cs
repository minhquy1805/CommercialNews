using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.Results;

namespace Authorization.Application.UseCases.GetRolePermissions
{
    public interface IGetRolePermissionsUseCase
    {
        Task<Result<GetRolePermissionsResponseDto>> ExecuteAsync(
            GetRolePermissionsRequestDto request,
            CancellationToken cancellationToken = default);
    }
}