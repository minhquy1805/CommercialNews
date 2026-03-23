using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.RefreshToken
{
    public interface IRefreshTokenUseCase
    {
        Task<RefreshTokenResponseDto> ExecuteAsync(
            RefreshTokenRequestDto request,
            CancellationToken cancellationToken);
    }
}