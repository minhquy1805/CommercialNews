using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.CreateRole
{
    public interface ICreateRoleUseCase
    {
        Task<CreateRoleResponseDto> ExecuteAsync(
            CreateRoleRequestDto request,
            CancellationToken cancellationToken);
    }
}