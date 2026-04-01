using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.Results;

namespace Authorization.Application.UseCases.UpdateRole
{
    public interface IUpdateRoleUseCase
    {
        Task<Result<UpdateRoleResponseDto>> ExecuteAsync(
            UpdateRoleRequestDto request,
            CancellationToken cancellationToken = default);
    }
}