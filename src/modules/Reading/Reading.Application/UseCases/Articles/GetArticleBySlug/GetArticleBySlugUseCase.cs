using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Contracts.Articles.Requests;
using Reading.Application.Contracts.Articles.Responses;
using Reading.Application.Errors;
using Reading.Application.Models.Queries;
using Reading.Application.Models.Results;
using Reading.Application.Ports.Persistence;
using Reading.Application.Ports.Seo;
using Reading.Application.Validation.Articles;

namespace Reading.Application.UseCases.Articles.GetArticleBySlug;

public sealed class GetArticleBySlugUseCase : IGetArticleBySlugUseCase
{
    private const string ArticleResourceType = "Article";
    private const string ActiveRouteStatus = "Active";

    private readonly ISeoRouteResolver _seoRouteResolver;
    private readonly IArticleReadModelRepository _articleReadModelRepository;

    public GetArticleBySlugUseCase(
        ISeoRouteResolver seoRouteResolver,
        IArticleReadModelRepository articleReadModelRepository)
    {
        _seoRouteResolver = seoRouteResolver;
        _articleReadModelRepository = articleReadModelRepository;
    }

    public async Task<Result<ArticleDetailResponse>> ExecuteAsync(
        GetArticleBySlugRequest request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = GetArticleBySlugValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<ArticleDetailResponse>.Failure(validationError);
        }

        string slug = GetArticleBySlugValidator.NormalizeSlug(request.Slug);

        ResolvedSeoRouteResult? route =
            await _seoRouteResolver.ResolveArticleSlugAsync(
                slug,
                cancellationToken);

        if (route is null)
        {
            return Result<ArticleDetailResponse>.Failure(
                ReadingErrors.Route.RouteNotResolved);
        }

        if (!IsArticleRoute(route))
        {
            return Result<ArticleDetailResponse>.Failure(
                ReadingErrors.Route.RouteResourceTypeInvalid);
        }

        if (!IsActiveRoute(route))
        {
            return Result<ArticleDetailResponse>.Failure(
                ReadingErrors.Route.RouteInactive);
        }

        if (string.IsNullOrWhiteSpace(route.ResourcePublicId))
        {
            return Result<ArticleDetailResponse>.Failure(
                ReadingErrors.Route.RouteNotResolved);
        }

        var query = new GetArticleByPublicIdQuery(
            route.ResourcePublicId.Trim());

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
            MapToResponse(article, route));
    }

    private static bool IsArticleRoute(ResolvedSeoRouteResult route)
    {
        return string.Equals(
            route.ResourceType,
            ArticleResourceType,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsActiveRoute(ResolvedSeoRouteResult route)
    {
        if (string.IsNullOrWhiteSpace(route.Status))
        {
            return false;
        }

        return string.Equals(
            route.Status,
            ActiveRouteStatus,
            StringComparison.OrdinalIgnoreCase);
    }

    private static ArticleDetailResponse MapToResponse(
        ArticleDetailResult article,
        ResolvedSeoRouteResult route)
    {
        return new ArticleDetailResponse
        {
            ArticlePublicId = article.ArticlePublicId,

            // Prefer route slug/canonical from SEO resolve because SEO owns route truth.
            Slug = route.Slug,
            CanonicalUrl = route.CanonicalUrl ?? article.CanonicalUrl,

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

            MetaTitle = article.MetaTitle,
            MetaDescription = article.MetaDescription,

            PublishedAtUtc = article.PublishedAtUtc,
            UpdatedAtUtc = article.UpdatedAtUtc,

            ViewCount = article.ViewCount,
            LikeCount = article.LikeCount,
            CommentCount = article.CommentCount,
            PopularityScore = article.PopularityScore,

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
