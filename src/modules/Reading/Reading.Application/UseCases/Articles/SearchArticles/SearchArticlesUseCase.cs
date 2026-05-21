using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Contracts.Articles.Requests;
using Reading.Application.Contracts.Articles.Responses;
using Reading.Application.Models.Queries;
using Reading.Application.Models.Results;
using Reading.Application.Ports.Persistence;
using Reading.Application.Validation.Articles;

namespace Reading.Application.UseCases.Articles.SearchArticles;

public sealed class SearchArticlesUseCase : ISearchArticlesUseCase
{
    private readonly IArticleReadModelRepository _articleReadModelRepository;

    public SearchArticlesUseCase(
        IArticleReadModelRepository articleReadModelRepository)
    {
        _articleReadModelRepository = articleReadModelRepository;
    }

    public async Task<Result<GetArticlesResponse>> ExecuteAsync(
        SearchArticlesRequest request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError =
            SearchArticlesValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<GetArticlesResponse>.Failure(validationError);
        }

        var query = new SearchArticlesQuery(
            Query: SearchArticlesValidator.NormalizeQuery(request.Query),
            Page: request.Page,
            PageSize: request.PageSize,
            Sort: SearchArticlesValidator.NormalizeSort(request.Sort));

        PagedReadingResult<ArticleListItemResult> result =
            await _articleReadModelRepository.SearchAsync(
                query,
                cancellationToken);

        return Result<GetArticlesResponse>.Success(
            MapToResponse(result));
    }

    private static GetArticlesResponse MapToResponse(
        PagedReadingResult<ArticleListItemResult> result)
    {
        return new GetArticlesResponse
        {
            Page = result.Page,
            PageSize = result.PageSize,
            TotalItems = result.TotalItems,
            TotalPages = result.TotalPages,
            Items = result.Items
                .Select(MapToResponse)
                .ToList()
        };
    }

    private static ArticleListItemResponse MapToResponse(
        ArticleListItemResult item)
    {
        return new ArticleListItemResponse
        {
            ArticlePublicId = item.ArticlePublicId,
            Slug = item.Slug,

            Title = item.Title,
            Summary = item.Summary,

            CategoryId = item.CategoryId,
            CategoryName = item.CategoryName,

            AuthorUserId = item.AuthorUserId,
            AuthorDisplayName = item.AuthorDisplayName,

            CoverMediaId = item.CoverMediaId,
            CoverMediaUrl = item.CoverMediaUrl,
            CoverAlt = item.CoverAlt,

            PublishedAtUtc = item.PublishedAtUtc,
            UpdatedAtUtc = item.UpdatedAtUtc,

            ViewCount = item.ViewCount,
            LikeCount = item.LikeCount,
            CommentCount = item.CommentCount,
            PopularityScore = item.PopularityScore
        };
    }
}