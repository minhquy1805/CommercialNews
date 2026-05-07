using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Users.ActivateUser;

namespace Identity.Application.UseCases.Users.ActivateUser;

public interface IActivateUserUseCase
{
    Task<Result<ActivateUserResponseDto>> ExecuteAsync(
        ActivateUserRequestDto request,
        CancellationToken cancellationToken = default);
}