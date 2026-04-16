using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.RefreshToken;

namespace Identity.Application.UseCases.RefreshToken;

public interface IRefreshTokenUseCase
{
    Task<Result<RefreshTokenResponseDto>> ExecuteAsync(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default);
}