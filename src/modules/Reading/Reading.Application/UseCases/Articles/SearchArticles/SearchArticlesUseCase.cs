using CommercialNews.BuildingBlocks.SharedKernel.Paging;
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
        _articleReadModelRepository = articleReadModelRepository
            ?? throw new ArgumentNullException(nameof(articleReadModelRepository));
    }

    public async Task<Result<GetArticlesResponse>> ExecuteAsync(
        SearchArticlesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Error? validationError =
            SearchArticlesValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<GetArticlesResponse>.Failure(validationError);
        }

        var query = new SearchArticlesQuery(
            Keyword: SearchArticlesValidator.NormalizeQuery(request.Query),
            Page: request.Page,
            PageSize: request.PageSize,
            Sort: SearchArticlesValidator.NormalizeSort(request.Sort));

        PagedQueryResult<ArticleListItemResult> result =
            await _articleReadModelRepository.SearchAsync(
                query,
                cancellationToken);

        return Result<GetArticlesResponse>.Success(
            MapToResponse(result));
    }

    private static GetArticlesResponse MapToResponse(
        PagedQueryResult<ArticleListItemResult> result)
    {
        return new GetArticlesResponse
        {
            Items = result.Items
                .Select(MapToResponse)
                .ToList(),

            Page = result.Page,
            PageSize = result.PageSize,
            TotalItems = result.TotalItems,
            TotalPages = CalculateTotalPages(
                result.TotalItems,
                result.PageSize)
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
            VisibleCommentCount = item.VisibleCommentCount,
            CountersPartial = item.CountersPartial
        };
    }

    private static int CalculateTotalPages(
        int totalItems,
        int pageSize)
    {
        if (totalItems <= 0 || pageSize <= 0)
        {
            return 0;
        }

        return (int)Math.Ceiling(totalItems / (double)pageSize);
    }
}
