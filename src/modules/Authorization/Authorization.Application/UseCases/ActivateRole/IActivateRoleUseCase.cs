using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.ActivateRole
{
    public interface IActivateRoleUseCase
    {
        Task<Result<ActivateRoleResponseDto>> ExecuteAsync(
            ActivateRoleRequestDto request,
            CancellationToken cancellationToken = default);
    }
}