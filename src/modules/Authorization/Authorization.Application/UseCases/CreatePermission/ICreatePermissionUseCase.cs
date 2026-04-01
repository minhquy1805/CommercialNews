using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.Results;

namespace Authorization.Application.UseCases.CreatePermission
{
    public interface ICreatePermissionUseCase
    {
        Task<Result<CreatePermissionResponseDto>> ExecuteAsync(
            CreatePermissionRequestDto request,
            CancellationToken cancellationToken = default);
    }
}