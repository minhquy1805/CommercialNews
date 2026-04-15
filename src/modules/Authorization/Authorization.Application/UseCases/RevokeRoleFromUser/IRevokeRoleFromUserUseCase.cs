using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.RevokeRoleFromUser
{
    public interface IRevokeRoleFromUserUseCase
    {
        Task<Result<RevokeRoleFromUserResponseDto>> ExecuteAsync(
            RevokeRoleFromUserRequestDto request,
            CancellationToken cancellationToken = default);
    }
}