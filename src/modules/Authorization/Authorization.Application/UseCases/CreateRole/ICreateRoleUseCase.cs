using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.Results;

namespace Authorization.Application.UseCases.CreateRole
{
    public interface ICreateRoleUseCase
    {
        Task<Result<CreateRoleResponseDto>> ExecuteAsync(
            CreateRoleRequestDto request,
            CancellationToken cancellationToken = default);
    }
}