using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Contracts.Articles.Requests;
using Reading.Application.Contracts.Articles.Responses;
using Reading.Application.Errors;
using Reading.Application.Models.Queries;
using Reading.Application.Models.Results;
using Reading.Application.Ports.Persistence;
using Reading.Domain.Constants;

namespace Reading.Application.UseCases.Articles.GetArticles;

public sealed class GetArticlesUseCase : IGetArticlesUseCase
{
    private const int MaxPageSize = 100;
    private const int MaxKeywordLength = 300;

    private readonly IArticleReadModelRepository _articleReadModelRepository;

    public GetArticlesUseCase(
        IArticleReadModelRepository articleReadModelRepository)
    {
        _articleReadModelRepository = articleReadModelRepository
            ?? throw new ArgumentNullException(nameof(articleReadModelRepository));
    }

    public async Task<Result<GetArticlesResponse>> ExecuteAsync(
        GetArticlesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Page < 1)
        {
            return Result<GetArticlesResponse>.Failure(
                ReadingErrors.Query.InvalidPage);
        }

        if (request.PageSize <= 0)
        {
            return Result<GetArticlesResponse>.Failure(
                ReadingErrors.Query.InvalidPageSize);
        }

        if (request.PageSize > MaxPageSize)
        {
            return Result<GetArticlesResponse>.Failure(
                ReadingErrors.Query.PageSizeTooLarge);
        }

        if (request.CategoryId is <= 0)
        {
            return Result<GetArticlesResponse>.Failure(
                ReadingErrors.Query.InvalidCategoryId);
        }

        if (request.TagId is <= 0)
        {
            return Result<GetArticlesResponse>.Failure(
                ReadingErrors.Query.InvalidTagId);
        }

        string? keyword = NormalizeNullable(request.Keyword);

        if (keyword is not null && keyword.Length > MaxKeywordLength)
        {
            return Result<GetArticlesResponse>.Failure(
                ReadingErrors.Query.SearchQueryTooLong);
        }

        string sort = string.IsNullOrWhiteSpace(request.Sort)
            ? ReadingSortValues.Default
            : request.Sort.Trim();

        if (!ReadingSortValues.IsValid(sort))
        {
            return Result<GetArticlesResponse>.Failure(
                ReadingErrors.Query.InvalidSort);
        }

        var query = new GetArticlesQuery(
            Page: request.Page,
            PageSize: request.PageSize,
            CategoryId: request.CategoryId,
            TagId: request.TagId,
            Keyword: keyword,
            Sort: sort);

        PagedQueryResult<ArticleListItemResult> result =
            await _articleReadModelRepository.SelectSkipAndTakeAsync(
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
            VisibleCommentCount = item.VisibleCommentCount
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

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
