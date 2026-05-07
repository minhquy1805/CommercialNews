using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Users.GetUserSecuritySummary;

namespace Identity.Application.UseCases.Users.GetUserSecuritySummary;

public interface IGetUserSecuritySummaryUseCase
{
    Task<Result<GetUserSecuritySummaryResponseDto>> ExecuteAsync(
        GetUserSecuritySummaryRequestDto request,
        CancellationToken cancellationToken = default);
}