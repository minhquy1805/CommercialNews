using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.GetUserRoles
{
    public interface IGetUserRolesUseCase
    {
        Task<GetUserRolesResponseDto> ExecuteAsync(
            GetUserRolesRequestDto request,
            CancellationToken cancellationToken);
    }
}