using CommercialNews.BuildingBlocks.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Tags.DeleteTag
{
    public interface IDeleteTagUseCase
    {
        Task<Result<DeleteTagResponseDto>> ExecuteAsync(
            DeleteTagRequestDto request,
            CancellationToken cancellationToken = default);
    }
}