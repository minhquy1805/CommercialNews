using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Contracts.Requests;
using Reading.Application.Contracts.Responses;
using Reading.Application.Errors;
using Reading.Application.Models.QueryModels;
using Reading.Application.Ports.Persistence;
using Reading.Domain.Policies;

namespace Reading.Application.UseCases.GetArticles;

public sealed class GetArticlesUseCase : IGetArticlesUseCase
{
    private const int MaxPageSize = 100;

    private readonly IReadingQueryRepository _readingQueryRepository;

    public GetArticlesUseCase(IReadingQueryRepository readingQueryRepository)
    {
        _readingQueryRepository = readingQueryRepository;
    }

    public async Task<Result<GetArticlesResponse>> ExecuteAsync(
        GetArticlesRequest request,
        CancellationToken cancellationToken = default)
    {
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

        if (request.CategoryId.HasValue && request.CategoryId.Value <= 0)
        {
            return Result<GetArticlesResponse>.Failure(
                ReadingErrors.Query.InvalidCategoryId);
        }

        if (request.TagId.HasValue && request.TagId.Value <= 0)
        {
            return Result<GetArticlesResponse>.Failure(
                ReadingErrors.Query.InvalidTagId);
        }

        if (!ReadingSortPolicy.IsAllowed(request.Sort))
        {
            return Result<GetArticlesResponse>.Failure(
                ReadingErrors.Query.InvalidSortField);
        }

        var query = new ReadingArticleListQuery
        {
            Page = request.Page,
            PageSize = request.PageSize,
            CategoryId = request.CategoryId,
            TagId = request.TagId,
            Q = string.IsNullOrWhiteSpace(request.Q) ? null : request.Q.Trim(),
            Sort = ReadingSortPolicy.Normalize(request.Sort)
        };

        PagedQueryResult<ReadingArticleListItem> result =
            await _readingQueryRepository.GetArticlesAsync(query, cancellationToken);

        var response = new GetArticlesResponse
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

        return Result<GetArticlesResponse>.Success(response);
    }
}