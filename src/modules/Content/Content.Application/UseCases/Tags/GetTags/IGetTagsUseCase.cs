using CommercialNews.BuildingBlocks.Results;
using Content.Application.Models.QueryModels;

namespace Content.Application.UseCases.Tags.GetTags
{
    public interface IGetTagsUseCase
    {
        Task<Result<PagedQueryResult<TagListResultItem>>> ExecuteAsync(
            TagListQuery query,
            CancellationToken cancellationToken = default);
    }
}