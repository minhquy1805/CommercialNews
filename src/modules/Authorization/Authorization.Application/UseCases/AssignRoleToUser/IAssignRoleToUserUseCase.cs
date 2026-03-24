using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.AssignRoleToUser
{
    public interface IAssignRoleToUserUseCase
    {
        Task<AssignRoleToUserResponseDto> ExecuteAsync(
            AssignRoleToUserRequestDto request,
            CancellationToken cancellationToken);
    }
}