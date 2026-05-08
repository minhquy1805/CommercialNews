using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Users.DisableUser;

namespace Identity.Application.UseCases.Users.DisableUser;

public interface IDisableUserUseCase
{
    Task<Result<DisableUserResponseDto>> ExecuteAsync(
        DisableUserRequestDto request,
        CancellationToken cancellationToken = default);
}