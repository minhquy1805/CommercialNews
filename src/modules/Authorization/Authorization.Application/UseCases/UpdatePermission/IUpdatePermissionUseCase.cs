using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.Results;

namespace Authorization.Application.UseCases.UpdatePermission
{
    public interface IUpdatePermissionUseCase
    {
        Task<Result<UpdatePermissionResponseDto>> ExecuteAsync(
            UpdatePermissionRequestDto request,
            CancellationToken cancellationToken = default);
    }
}