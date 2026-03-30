using CommercialNews.BuildingBlocks.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Tags.RestoreTag
{
    public interface IRestoreTagUseCase
    {
        Task<Result<RestoreTagResponseDto>> ExecuteAsync(
            RestoreTagRequestDto request,
            CancellationToken cancellationToken = default);
    }
}