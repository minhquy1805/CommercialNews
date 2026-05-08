using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Users.ListUsers;

namespace Identity.Application.UseCases.Users.ListUsers;

public interface IListUsersUseCase
{
    Task<Result<ListUsersResponseDto>> ExecuteAsync(
        ListUsersRequestDto request,
        CancellationToken cancellationToken = default);
}