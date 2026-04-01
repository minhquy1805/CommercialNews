using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.Results;

namespace Authorization.Application.UseCases.ActivatePermission
{
    public interface IActivatePermissionUseCase
    {
        Task<Result<ActivatePermissionResponseDto>> ExecuteAsync(
            ActivatePermissionRequestDto request,
            CancellationToken cancellationToken = default);
    }
}