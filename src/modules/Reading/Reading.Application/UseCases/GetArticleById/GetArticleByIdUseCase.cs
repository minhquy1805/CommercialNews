using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Contracts.Requests;
using Reading.Application.Contracts.Responses;
using Reading.Application.Errors;
using Reading.Application.Ports.Persistence;

namespace Reading.Application.UseCases.GetArticleById;

public sealed class GetArticleByIdUseCase : IGetArticleByIdUseCase
{
    private readonly IReadingQueryRepository _readingQueryRepository;

    public GetArticleByIdUseCase(IReadingQueryRepository readingQueryRepository)
    {
        _readingQueryRepository = readingQueryRepository;
    }

    public async Task<Result<GetArticleByIdResponse>> ExecuteAsync(
        GetArticleByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ArticleId <= 0)
        {
            return Result<GetArticleByIdResponse>.Failure(
                ReadingErrors.Article.InvalidArticleId);
        }

        var detail = await _readingQueryRepository.GetArticleByIdAsync(
            request.ArticleId,
            cancellationToken);

        if (detail is null)
        {
            return Result<GetArticleByIdResponse>.Failure(
                ReadingErrors.Article.NotFound);
        }

        var response = new GetArticleByIdResponse
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

        return Result<GetArticleByIdResponse>.Success(response);
    }
}