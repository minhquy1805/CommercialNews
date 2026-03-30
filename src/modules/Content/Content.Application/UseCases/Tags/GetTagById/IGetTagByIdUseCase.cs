using CommercialNews.BuildingBlocks.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Tags.GetTagById
{
    public interface IGetTagByIdUseCase
    {
        Task<Result<GetTagByIdResponseDto>> ExecuteAsync(
            GetTagByIdRequestDto request,
            CancellationToken cancellationToken = default);
    }
}