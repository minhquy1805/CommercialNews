using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Models.QueryModels;
using Content.Application.Ports.Persistence;

namespace Content.Application.UseCases.Articles.GetArticles;

public sealed class GetArticlesUseCase : IGetArticlesUseCase
{
    private static readonly HashSet<string> AllowedSorts = new(StringComparer.OrdinalIgnoreCase)
    {
        "-updatedAt",
        "-publishedAt",
        "title"
    };

    private readonly IArticleRepository _articleRepository;

    public GetArticlesUseCase(IArticleRepository articleRepository)
    {
        _articleRepository = articleRepository;
    }

    public async Task<Result<PagedQueryResult<ArticleListItemDto>>> ExecuteAsync(
        GetArticlesRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Page <= 0)
        {
            return Result<PagedQueryResult<ArticleListItemDto>>.Failure(
                Error.Validation(
                    code: "CONTENT.INVALID_PAGE",
                    message: "Page must be greater than zero."));
        }

        if (request.PageSize <= 0)
        {
            return Result<PagedQueryResult<ArticleListItemDto>>.Failure(
                Error.Validation(
                    code: "CONTENT.INVALID_PAGE_SIZE",
                    message: "PageSize must be greater than zero."));
        }

        if (request.PageSize > 100)
        {
            return Result<PagedQueryResult<ArticleListItemDto>>.Failure(
                Error.Validation(
                    code: "CONTENT.INVALID_PAGE_SIZE",
                    message: "PageSize must not exceed 100."));
        }

        string normalizedSort = NormalizeSort(request.Sort);

        if (!AllowedSorts.Contains(normalizedSort))
        {
            return Result<PagedQueryResult<ArticleListItemDto>>.Failure(
                Error.Validation(
                    code: "CONTENT.INVALID_SORT",
                    message: "Sort is not supported.",
                    "Allowed sorts: -updatedAt, -publishedAt, title"));
        }

        var query = new ArticleListQuery
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Status = NormalizeOptional(request.Status),
            CategoryId = request.CategoryId,
            TagId = request.TagId,
            Sort = normalizedSort
        };

        PagedQueryResult<ArticleListResultItem> result = await _articleRepository.GetPagedAsync(
            query,
            cancellationToken);

        var response = new PagedQueryResult<ArticleListItemDto>
        {
            Items = result.Items.Select(static item => new ArticleListItemDto
            {
                ArticleId = item.ArticleId,
                PublicId = item.PublicId,
                Title = item.Title,
                Summary = item.Summary,
                Status = item.Status,
                AuthorUserId = item.AuthorUserId,
                CategoryId = item.CategoryId,
                CoverMediaId = item.CoverMediaId,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                PublishedAt = item.PublishedAt,
                Version = item.Version
            }).ToArray(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalItems = result.TotalItems
        };

        return Result<PagedQueryResult<ArticleListItemDto>>.Success(response);
    }

    private static string NormalizeSort(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return "-updatedAt";
        }

        return sort.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}