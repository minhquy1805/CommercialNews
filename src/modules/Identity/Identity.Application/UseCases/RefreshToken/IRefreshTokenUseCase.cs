

using Identity.Application.Contracts.Dtos;

namespace Identity.Application.UseCases.RefreshToken
{
    public interface IRefreshTokenUseCase
    {
        Task<RefreshTokenResponseDto> ExecuteAsync(
            RefreshTokenRequestDto request,
            CancellationToken cancellationToken);
    }
}