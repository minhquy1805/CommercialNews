using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Contracts.Articles.Requests;
using Reading.Application.Contracts.Articles.Responses;
using Reading.Application.Errors;
using Reading.Application.Models.Queries;
using Reading.Application.Models.Results;
using Reading.Application.Ports.Persistence;
using Reading.Application.Validation.Articles;

namespace Reading.Application.UseCases.Articles.GetArticleByPublicId;

public sealed class GetArticleByPublicIdUseCase : IGetArticleByPublicIdUseCase
{
    private readonly IArticleReadModelRepository _articleReadModelRepository;

    public GetArticleByPublicIdUseCase(
        IArticleReadModelRepository articleReadModelRepository)
    {
        _articleReadModelRepository = articleReadModelRepository
            ?? throw new ArgumentNullException(nameof(articleReadModelRepository));
    }

    public async Task<Result<ArticleDetailResponse>> ExecuteAsync(
        GetArticleByPublicIdRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Error? validationError =
            GetArticleByPublicIdValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<ArticleDetailResponse>.Failure(validationError);
        }

        string articlePublicId =
            GetArticleByPublicIdValidator.NormalizeArticlePublicId(
                request.ArticlePublicId);

        var query = new GetArticleByPublicIdQuery(articlePublicId);

        ArticleDetailResult? article =
            await _articleReadModelRepository.SelectByPublicIdAsync(
                query,
                cancellationToken);

        if (article is null)
        {
            return Result<ArticleDetailResponse>.Failure(
                ReadingErrors.Article.NotFound);
        }

        return Result<ArticleDetailResponse>.Success(
            MapToResponse(article));
    }

    private static ArticleDetailResponse MapToResponse(
        ArticleDetailResult article)
    {
        return new ArticleDetailResponse
        {
            ArticlePublicId = article.ArticlePublicId,
            Slug = article.Slug,

            Title = article.Title,
            Summary = article.Summary,
            Body = article.Body,

            CategoryId = article.CategoryId,
            CategoryName = article.CategoryName,

            AuthorUserId = article.AuthorUserId,
            AuthorDisplayName = article.AuthorDisplayName,

            CoverMediaId = article.CoverMediaId,
            CoverMediaUrl = article.CoverMediaUrl,
            CoverAlt = article.CoverAlt,

            CanonicalUrl = article.CanonicalUrl,
            MetaTitle = article.MetaTitle,
            MetaDescription = article.MetaDescription,

            OgTitle = article.OgTitle,
            OgDescription = article.OgDescription,
            OgImageUrl = article.OgImageUrl,

            TwitterTitle = article.TwitterTitle,
            TwitterDescription = article.TwitterDescription,
            TwitterImageUrl = article.TwitterImageUrl,

            Robots = article.Robots,

            PublishedAtUtc = article.PublishedAtUtc,
            UpdatedAtUtc = article.UpdatedAtUtc,

            ViewCount = article.ViewCount,
            LikeCount = article.LikeCount,
            VisibleCommentCount = article.VisibleCommentCount,

            Tags = article.Tags
                .Select(MapToResponse)
                .ToList(),

            Media = article.Media
                .Select(MapToResponse)
                .ToList()
        };
    }

    private static ArticleTagResponse MapToResponse(
        ArticleTagResult tag)
    {
        return new ArticleTagResponse
        {
            TagId = tag.TagId,
            TagPublicId = tag.TagPublicId,
            Name = tag.Name,
            Slug = tag.Slug
        };
    }

    private static ArticleMediaResponse MapToResponse(
        ArticleMediaResult media)
    {
        return new ArticleMediaResponse
        {
            MediaId = media.MediaId,
            MediaPublicId = media.MediaPublicId,
            Url = media.Url,
            Alt = media.Alt,
            Caption = media.Caption,
            MediaType = media.MediaType,
            IsPrimary = media.IsPrimary,
            SortOrder = media.SortOrder
        };
    }
}
