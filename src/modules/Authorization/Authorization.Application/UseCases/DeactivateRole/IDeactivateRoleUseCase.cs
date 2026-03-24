using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.DeactivateRole
{
    public interface IDeactivateRoleUseCase
    {
        Task<DeactivateRoleResponseDto> ExecuteAsync(
            DeactivateRoleRequestDto request,
            CancellationToken cancellationToken);
    }
}