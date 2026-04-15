using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.LogoutAllSessions
{
    public interface ILogoutAllSessionsUseCase
    {
        Task<Result<LogoutAllSessionsResponseDto>> ExecuteAsync(
            CancellationToken cancellationToken = default);
    }
}