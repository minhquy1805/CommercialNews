using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.LogoutAllSessions
{
    public interface ILogoutAllSessionsUseCase
    {
        Task<LogoutAllSessionsResponseDto> ExecuteAsync(
            CancellationToken cancellationToken);
    }
}