using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.Logout
{
    public interface ILogoutUseCase
    {
        Task<LogoutResponseDto> ExecuteAsync(
            LogoutRequestDto request,
            CancellationToken cancellationToken);
    }
}