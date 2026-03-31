using CommercialNews.BuildingBlocks.Results;
using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.RefreshToken
{
    public interface IRefreshTokenUseCase
    {
        Task<Result<RefreshTokenResponseDto>> ExecuteAsync(
            RefreshTokenRequestDto request,
            CancellationToken cancellationToken = default);
    }
}