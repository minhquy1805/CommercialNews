using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.Results;

namespace Authorization.Application.UseCases.AssignRoleToUser
{
    public interface IAssignRoleToUserUseCase
    {
        Task<Result<AssignRoleToUserResponseDto>> ExecuteAsync(
            AssignRoleToUserRequestDto request,
            CancellationToken cancellationToken = default);
    }
}