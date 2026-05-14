using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.ArticleTags.GetArticleTags;

public interface IGetArticleTagsUseCase
{
    Task<Result<IReadOnlyList<ArticleTagItemDto>>> ExecuteAsync(
        GetArticleTagsRequestDto request,
        CancellationToken cancellationToken = default);
}