using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.Results;

namespace Authorization.Application.UseCases.DeactivateRole
{
    public interface IDeactivateRoleUseCase
    {
        Task<Result<DeactivateRoleResponseDto>> ExecuteAsync(
            DeactivateRoleRequestDto request,
            CancellationToken cancellationToken = default);
    }
}