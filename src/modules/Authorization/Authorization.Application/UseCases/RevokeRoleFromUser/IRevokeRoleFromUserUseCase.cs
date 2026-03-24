using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.RevokeRoleFromUser
{
    public interface IRevokeRoleFromUserUseCase
    {
        Task<RevokeRoleFromUserResponseDto> ExecuteAsync(
            RevokeRoleFromUserRequestDto request,
            CancellationToken cancellationToken);
    }
}