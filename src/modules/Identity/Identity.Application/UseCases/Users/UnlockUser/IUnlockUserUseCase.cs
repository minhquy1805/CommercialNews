using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Users.UnlockUser;

namespace Identity.Application.UseCases.Users.UnlockUser;

public interface IUnlockUserUseCase
{
    Task<Result<UnlockUserResponseDto>> ExecuteAsync(
        UnlockUserRequestDto request,
        CancellationToken cancellationToken = default);
}