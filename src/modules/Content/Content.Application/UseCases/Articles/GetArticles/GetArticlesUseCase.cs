using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Models.QueryModels;
using Content.Application.Ports.Persistence;

namespace Content.Application.UseCases.Articles.GetArticles;

public sealed class GetArticlesUseCase : IGetArticlesUseCase
{
    private static readonly HashSet<string> AllowedSorts = new(StringComparer.OrdinalIgnoreCase)
    {
        "createdAt",
        "-createdAt",
        "updatedAt",
        "-updatedAt",
        "publishedAt",
        "-publishedAt",
        "title",
        "-title"
    };

    private readonly IArticleRepository _articleRepository;

    public GetArticlesUseCase(IArticleRepository articleRepository)
    {
        _articleRepository = articleRepository ?? throw new ArgumentNullException(nameof(articleRepository));
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
                ContentErrors.InvalidSortField);
        }

        if (request.CategoryId.HasValue && request.CategoryId.Value <= 0)
        {
            return Result<PagedQueryResult<ArticleListItemDto>>.Failure(
                ContentErrors.Article.CategoryIdInvalid);
        }

        if (request.AuthorUserId.HasValue && request.AuthorUserId.Value <= 0)
        {
            return Result<PagedQueryResult<ArticleListItemDto>>.Failure(
                ContentErrors.Article.AuthorUserIdInvalid);
        }

        var query = new ArticleListQuery
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Keyword = NormalizeOptional(request.Keyword),
            Status = NormalizeOptional(request.Status),
            CategoryId = request.CategoryId,
            AuthorUserId = request.AuthorUserId,
            IsDeleted = request.IsDeleted,
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
                ArticlePublicId = item.ArticlePublicId,
                Title = item.Title,
                Summary = item.Summary,
                Status = item.Status,
                AuthorUserId = item.AuthorUserId,
                CategoryId = item.CategoryId,
                CoverMediaId = item.CoverMediaId,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                PublishedAt = item.PublishedAt,
                UnpublishedAt = item.UnpublishedAt,
                ArchivedAt = item.ArchivedAt,
                IsDeleted = item.IsDeleted,
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
