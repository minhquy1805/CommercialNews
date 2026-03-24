using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.UpdateRole
{
    public interface IUpdateRoleUseCase
    {
        Task<UpdateRoleResponseDto> ExecuteAsync(
            UpdateRoleRequestDto request,
            CancellationToken cancellationToken);
    }
}