using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.GetUserRoles
{
    public interface IGetUserRolesUseCase
    {
        Task<Result<GetUserRolesResponseDto>> ExecuteAsync(
            GetUserRolesRequestDto request,
            CancellationToken cancellationToken = default);
    }
}