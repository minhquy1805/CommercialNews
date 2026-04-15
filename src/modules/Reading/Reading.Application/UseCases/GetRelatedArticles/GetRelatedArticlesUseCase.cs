using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Contracts.Requests;
using Reading.Application.Contracts.Responses;
using Reading.Application.Errors;
using Reading.Application.Models.QueryModels;
using Reading.Application.Ports.Persistence;

namespace Reading.Application.UseCases.GetRelatedArticles;

public sealed class GetRelatedArticlesUseCase : IGetRelatedArticlesUseCase
{
    private const int MaxLimit = 20;

    private readonly IReadingQueryRepository _readingQueryRepository;

    public GetRelatedArticlesUseCase(IReadingQueryRepository readingQueryRepository)
    {
        _readingQueryRepository = readingQueryRepository;
    }

    public async Task<Result<GetRelatedArticlesResponse>> ExecuteAsync(
        GetRelatedArticlesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ArticleId <= 0)
        {
            return Result<GetRelatedArticlesResponse>.Failure(
                ReadingErrors.Article.InvalidArticleId);
        }

        if (request.Limit <= 0)
        {
            return Result<GetRelatedArticlesResponse>.Failure(
                ReadingErrors.Query.InvalidLimit);
        }

        if (request.Limit > MaxLimit)
        {
            return Result<GetRelatedArticlesResponse>.Failure(
                ReadingErrors.Query.LimitTooLarge);
        }

        var query = new ReadingRelatedArticlesQuery
        {
            ArticleId = request.ArticleId,
            Limit = request.Limit
        };

        IReadOnlyList<ReadingArticleListItem> result =
            await _readingQueryRepository.GetRelatedArticlesAsync(query, cancellationToken);

        var response = new GetRelatedArticlesResponse
        {
            Items = result
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
                .ToArray()
        };

        return Result<GetRelatedArticlesResponse>.Success(response);
    }
}