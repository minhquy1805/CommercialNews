using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.LoginHistory.GetMyLoginHistory;

namespace Identity.Application.UseCases.LoginHistory.GetMyLoginHistory;

public interface IGetMyLoginHistoryUseCase
{
    Task<Result<GetMyLoginHistoryResponseDto>> ExecuteAsync(
        GetMyLoginHistoryRequestDto request,
        CancellationToken cancellationToken = default);
}