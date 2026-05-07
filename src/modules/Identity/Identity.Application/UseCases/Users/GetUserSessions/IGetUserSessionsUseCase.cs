using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Users.GetUserSessions;

namespace Identity.Application.UseCases.Users.GetUserSessions;

public interface IGetUserSessionsUseCase
{
    Task<Result<GetUserSessionsResponseDto>> ExecuteAsync(
        GetUserSessionsRequestDto request,
        CancellationToken cancellationToken = default);
}