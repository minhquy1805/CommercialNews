using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Contracts.Requests;
using Reading.Application.Contracts.Responses;
using Reading.Application.Errors;
using Reading.Application.Models.QueryModels;
using Reading.Application.Ports.Persistence;
using Reading.Domain.Policies;

namespace Reading.Application.UseCases.SearchArticles;

public sealed class SearchArticlesUseCase : ISearchArticlesUseCase
{
    private const int MaxPageSize = 100;

    private readonly IReadingQueryRepository _readingQueryRepository;

    public SearchArticlesUseCase(IReadingQueryRepository readingQueryRepository)
    {
        _readingQueryRepository = readingQueryRepository;
    }

    public async Task<Result<SearchArticlesResponse>> ExecuteAsync(
        SearchArticlesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Q))
        {
            return Result<SearchArticlesResponse>.Failure(
                ReadingErrors.Query.SearchKeywordRequired);
        }

        string keyword = request.Q.Trim();

        if (keyword.Length > 200)
        {
            return Result<SearchArticlesResponse>.Failure(
                ReadingErrors.Query.SearchKeywordTooLong);
        }

        if (request.Page < 1)
        {
            return Result<SearchArticlesResponse>.Failure(
                ReadingErrors.Query.InvalidPage);
        }

        if (request.PageSize <= 0)
        {
            return Result<SearchArticlesResponse>.Failure(
                ReadingErrors.Query.InvalidPageSize);
        }

        if (request.PageSize > MaxPageSize)
        {
            return Result<SearchArticlesResponse>.Failure(
                ReadingErrors.Query.PageSizeTooLarge);
        }

        if (!ReadingSortPolicy.IsAllowed(request.Sort))
        {
            return Result<SearchArticlesResponse>.Failure(
                ReadingErrors.Query.InvalidSortField);
        }

        var query = new ReadingSearchArticlesQuery
        {
            Q = keyword,
            Page = request.Page,
            PageSize = request.PageSize,
            Sort = ReadingSortPolicy.Normalize(request.Sort)
        };

        PagedQueryResult<ReadingArticleListItem> result =
            await _readingQueryRepository.SearchArticlesAsync(query, cancellationToken);

        var response = new SearchArticlesResponse
        {
            Items = result.Items
                .Select(static item => new ArticleListItemResponse
                {
                    ArticleId = item.ArticleId,
                    Title = item.Title,
                    Summary = item.Summary,
                    Slug = item.Slug,
                    PublishedAt = item.PublishedAt,
                    Category = item.Category is null
                        ? null
                        : new CategorySummaryResponse
                        {
                            CategoryId = item.Category.CategoryId,
                            Name = item.Category.Name
                        },
                    Cover = item.Cover is null
                        ? null
                        : new MediaSummaryResponse
                        {
                            MediaId = item.Cover.MediaId,
                            Url = item.Cover.Url,
                            Alt = item.Cover.Alt,
                            IsPrimary = item.Cover.IsPrimary,
                            Order = item.Cover.Order
                        },
                    Counters = item.Counters is null
                        ? null
                        : new ArticleCountersResponse
                        {
                            Views = item.Counters.Views,
                            Likes = item.Counters.Likes,
                            CountersPartial = item.Counters.CountersPartial
                        }
                })
                .ToArray(),
            PageInfo = new PageInfo
            {
                Page = result.Page,
                PageSize = result.PageSize,
                TotalItems = result.TotalItems,
                TotalPages = result.PageSize <= 0
                    ? 0
                    : (int)Math.Ceiling((double)result.TotalItems / result.PageSize)
            }
        };

        return Result<SearchArticlesResponse>.Success(response);
    }
}