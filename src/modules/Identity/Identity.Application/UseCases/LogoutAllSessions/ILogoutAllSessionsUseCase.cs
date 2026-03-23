using Identity.Application.Contracts.Dtos;

namespace Identity.Application.UseCases.LogoutAllSessions
{
    public interface ILogoutAllSessionsUseCase
    {
        Task<LogoutAllSessionsResponseDto> ExecuteAsync(
            CancellationToken cancellationToken);
    }
}