using Identity.Application.Contracts.Dtos;

namespace Identity.Application.UseCases.Logout
{
    public interface ILogoutUseCase
    {
        Task<LogoutResponseDto> ExecuteAsync(
            LogoutRequestDto request,
            CancellationToken cancellationToken);
    }
}