using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Contracts.Requests;
using Reading.Application.Contracts.Responses;
using Reading.Application.Errors;
using Reading.Application.Ports.Persistence;

namespace Reading.Application.UseCases.GetArticleBySlug;

public sealed class GetArticleBySlugUseCase : IGetArticleBySlugUseCase
{
    private readonly IReadingQueryRepository _readingQueryRepository;

    public GetArticleBySlugUseCase(IReadingQueryRepository readingQueryRepository)
    {
        _readingQueryRepository = readingQueryRepository;
    }

    public async Task<Result<GetArticleBySlugResponse>> ExecuteAsync(
        GetArticleBySlugRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Scope))
        {
            return Result<GetArticleBySlugResponse>.Failure(
                ReadingErrors.Article.ScopeRequired);
        }

        if (request.Scope.Trim().Length > 100)
        {
            return Result<GetArticleBySlugResponse>.Failure(
                ReadingErrors.Article.ScopeTooLong);
        }

        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            return Result<GetArticleBySlugResponse>.Failure(
                ReadingErrors.Article.SlugRequired);
        }

        if (request.Slug.Trim().Length > 200)
        {
            return Result<GetArticleBySlugResponse>.Failure(
                ReadingErrors.Article.SlugTooLong);
        }

        string scope = request.Scope.Trim();
        string slug = request.Slug.Trim();

        var detail = await _readingQueryRepository.GetArticleBySlugAsync(
            scope,
            slug,
            cancellationToken);

        if (detail is null)
        {
            return Result<GetArticleBySlugResponse>.Failure(
                ReadingErrors.Article.NotFound);
        }

        var response = new GetArticleBySlugResponse
        {
            ArticleId = detail.ArticleId,
            Title = detail.Title,
            Summary = detail.Summary,
            Body = detail.Body,
            Slug = detail.Slug,
            PublishedAt = detail.PublishedAt,
            Category = detail.Category is null
                ? null
                : new CategorySummaryResponse
                {
                    CategoryId = detail.Category.CategoryId,
                    Name = detail.Category.Name
                },
            Tags = detail.Tags
                .Select(static tag => new TagSummaryResponse
                {
                    TagId = tag.TagId,
                    Name = tag.Name
                })
                .ToArray(),
            Media = detail.Media
                .Select(static media => new MediaSummaryResponse
                {
                    MediaId = media.MediaId,
                    Url = media.Url,
                    Alt = media.Alt,
                    IsPrimary = media.IsPrimary,
                    Order = media.Order
                })
                .ToArray(),
            Seo = detail.Seo is null
                ? null
                : new SeoSummaryResponse
                {
                    CanonicalUrl = detail.Seo.CanonicalUrl,
                    MetaTitle = detail.Seo.MetaTitle,
                    MetaDescription = detail.Seo.MetaDescription
                },
            Counters = detail.Counters is null
                ? null
                : new ArticleCountersResponse
                {
                    Views = detail.Counters.Views,
                    Likes = detail.Counters.Likes,
                    CountersPartial = detail.Counters.CountersPartial
                }
        };

        return Result<GetArticleBySlugResponse>.Success(response);
    }
}