using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Users.LockUser;

namespace Identity.Application.UseCases.Users.LockUser;

public interface ILockUserUseCase
{
    Task<Result<LockUserResponseDto>> ExecuteAsync(
        LockUserRequestDto request,
        CancellationToken cancellationToken = default);
}