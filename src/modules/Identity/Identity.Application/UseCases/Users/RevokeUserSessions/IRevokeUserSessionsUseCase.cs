using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Users.RevokeUserSessions;

namespace Identity.Application.UseCases.Users.RevokeUserSessions;

public interface IRevokeUserSessionsUseCase
{
    Task<Result<RevokeUserSessionsResponseDto>> ExecuteAsync(
        RevokeUserSessionsRequestDto request,
        CancellationToken cancellationToken = default);
}