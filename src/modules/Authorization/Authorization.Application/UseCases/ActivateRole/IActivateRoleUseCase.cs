using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.ActivateRole
{
    public interface IActivateRoleUseCase
    {
        Task<ActivateRoleResponseDto> ExecuteAsync(
            ActivateRoleRequestDto request,
            CancellationToken cancellationToken);
    }
}