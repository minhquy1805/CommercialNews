using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Logout;

namespace Identity.Application.UseCases.Logout;

public interface ILogoutUseCase
{
    Task<Result<LogoutResponseDto>> ExecuteAsync(
        LogoutRequestDto request,
        CancellationToken cancellationToken = default);
}