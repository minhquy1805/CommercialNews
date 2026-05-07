using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.LoginHistory.GetUserLoginHistory;

namespace Identity.Application.UseCases.LoginHistory.GetUserLoginHistory;

public interface IGetUserLoginHistoryUseCase
{
    Task<Result<GetUserLoginHistoryResponseDto>> ExecuteAsync(
        GetUserLoginHistoryRequestDto request,
        CancellationToken cancellationToken = default);
}