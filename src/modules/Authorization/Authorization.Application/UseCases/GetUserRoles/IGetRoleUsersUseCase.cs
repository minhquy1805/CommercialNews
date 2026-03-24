using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.GetRoleUsers
{
    public interface IGetRoleUsersUseCase
    {
        Task<GetRoleUsersResponseDto> ExecuteAsync(
            GetRoleUsersRequestDto request,
            CancellationToken cancellationToken);
    }
}