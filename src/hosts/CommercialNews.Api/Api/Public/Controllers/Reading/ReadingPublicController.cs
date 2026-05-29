using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Api.Public.Contracts.Reading.Requests;
using CommercialNews.Api.Api.Public.Contracts.Reading.Responses;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;
using Reading.Application.Contracts.Articles.Requests;
using Reading.Application.Contracts.Articles.Responses;
using Reading.Application.UseCases.Articles.GetArticleByPublicId;
using Reading.Application.UseCases.Articles.GetArticleBySlug;
using Reading.Application.UseCases.Articles.GetArticles;
using Reading.Application.UseCases.Articles.GetRelatedArticles;
using Reading.Application.UseCases.Articles.SearchArticles;

namespace CommercialNews.Api.Api.Public.Controllers.Reading;

[ApiController]
[Route("api/v1/reading")]
public sealed class ReadingPublicController : ControllerBase
{
    private readonly IGetArticlesUseCase _getArticlesUseCase;
    private readonly IGetArticleByPublicIdUseCase _getArticleByPublicIdUseCase;
    private readonly IGetArticleBySlugUseCase _getArticleBySlugUseCase;
    private readonly IGetRelatedArticlesUseCase _getRelatedArticlesUseCase;
    private readonly ISearchArticlesUseCase _searchArticlesUseCase;

    public ReadingPublicController(
        IGetArticlesUseCase getArticlesUseCase,
        IGetArticleByPublicIdUseCase getArticleByPublicIdUseCase,
        IGetArticleBySlugUseCase getArticleBySlugUseCase,
        IGetRelatedArticlesUseCase getRelatedArticlesUseCase,
        ISearchArticlesUseCase searchArticlesUseCase)
    {
        _getArticlesUseCase = getArticlesUseCase
            ?? throw new ArgumentNullException(nameof(getArticlesUseCase));

        _getArticleByPublicIdUseCase = getArticleByPublicIdUseCase
            ?? throw new ArgumentNullException(nameof(getArticleByPublicIdUseCase));

        _getArticleBySlugUseCase = getArticleBySlugUseCase
            ?? throw new ArgumentNullException(nameof(getArticleBySlugUseCase));

        _getRelatedArticlesUseCase = getRelatedArticlesUseCase
            ?? throw new ArgumentNullException(nameof(getRelatedArticlesUseCase));

        _searchArticlesUseCase = searchArticlesUseCase
            ?? throw new ArgumentNullException(nameof(searchArticlesUseCase));
    }

    [HttpGet("articles")]
    [ProducesResponseType(typeof(GetArticlesHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetArticlesAsync(
        [FromQuery] GetArticlesHttpRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetArticlesRequest
        {
            Page = request.Page,
            PageSize = request.PageSize,
            CategoryId = request.CategoryId,
            TagId = request.TagId,
            Keyword = request.Keyword,
            Sort = request.Sort ?? string.Empty
        };

        Result<GetArticlesResponse> result =
            await _getArticlesUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetArticlesHttpResponse>.Failure(result.Error!));
        }

        GetArticlesHttpResponse response = MapGetArticlesResponse(result.Value);

        return this.ToActionResult(
            Result<GetArticlesHttpResponse>.Success(response));
    }

    [HttpGet("articles/search")]
    [ProducesResponseType(typeof(GetArticlesHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchArticlesAsync(
        [FromQuery] SearchArticlesHttpRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new SearchArticlesRequest
        {
            Query = request.Query,
            Page = request.Page,
            PageSize = request.PageSize,
            Sort = request.Sort ?? string.Empty
        };

        Result<GetArticlesResponse> result =
            await _searchArticlesUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetArticlesHttpResponse>.Failure(result.Error!));
        }

        GetArticlesHttpResponse response = MapGetArticlesResponse(result.Value);

        return this.ToActionResult(
            Result<GetArticlesHttpResponse>.Success(response));
    }

    [HttpGet("articles/slug/{slug}")]
    [ProducesResponseType(typeof(ArticleDetailHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArticleBySlugAsync(
        [FromRoute] string slug,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetArticleBySlugRequest
        {
            Slug = slug
        };

        Result<ArticleDetailResponse> result =
            await _getArticleBySlugUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<ArticleDetailHttpResponse>.Failure(result.Error!));
        }

        ArticleDetailHttpResponse response = MapArticleDetail(result.Value);

        return this.ToActionResult(
            Result<ArticleDetailHttpResponse>.Success(response));
    }

    [HttpGet("articles/{articlePublicId}")]
    [ProducesResponseType(typeof(ArticleDetailHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArticleByPublicIdAsync(
        [FromRoute] string articlePublicId,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetArticleByPublicIdRequest
        {
            ArticlePublicId = articlePublicId
        };

        Result<ArticleDetailResponse> result =
            await _getArticleByPublicIdUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<ArticleDetailHttpResponse>.Failure(result.Error!));
        }

        ArticleDetailHttpResponse response = MapArticleDetail(result.Value);

        return this.ToActionResult(
            Result<ArticleDetailHttpResponse>.Success(response));
    }

    [HttpGet("articles/{articlePublicId}/related")]
    [ProducesResponseType(typeof(GetRelatedArticlesHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRelatedArticlesAsync(
        [FromRoute] string articlePublicId,
        [FromQuery] GetRelatedArticlesHttpRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetRelatedArticlesRequest
        {
            ArticlePublicId = articlePublicId,
            Limit = request.Limit
        };

        Result<IReadOnlyList<ArticleListItemResponse>> result =
            await _getRelatedArticlesUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetRelatedArticlesHttpResponse>.Failure(result.Error!));
        }

        var response = new GetRelatedArticlesHttpResponse
        {
            Items = result.Value
                .Select(MapArticleListItem)
                .ToArray()
        };

        return this.ToActionResult(
            Result<GetRelatedArticlesHttpResponse>.Success(response));
    }

    private static GetArticlesHttpResponse MapGetArticlesResponse(
        GetArticlesResponse source)
    {
        return new GetArticlesHttpResponse
        {
            Items = source.Items
                .Select(MapArticleListItem)
                .ToArray(),
            Page = source.Page,
            PageSize = source.PageSize,
            TotalItems = source.TotalItems,
            TotalPages = source.TotalPages
        };
    }

    private static ArticleDetailHttpResponse MapArticleDetail(
        ArticleDetailResponse source)
    {
        return new ArticleDetailHttpResponse
        {
            ArticlePublicId = source.ArticlePublicId,
            Slug = source.Slug,

            Title = source.Title,
            Summary = source.Summary,
            Body = source.Body,

            CategoryId = source.CategoryId,
            CategoryName = source.CategoryName,

            AuthorUserId = source.AuthorUserId,
            AuthorDisplayName = source.AuthorDisplayName,

            CoverMediaId = source.CoverMediaId,
            CoverMediaUrl = source.CoverMediaUrl,
            CoverAlt = source.CoverAlt,

            CanonicalUrl = source.CanonicalUrl,
            MetaTitle = source.MetaTitle,
            MetaDescription = source.MetaDescription,
            OgTitle = source.OgTitle,
            OgDescription = source.OgDescription,
            OgImageUrl = source.OgImageUrl,
            TwitterTitle = source.TwitterTitle,
            TwitterDescription = source.TwitterDescription,
            TwitterImageUrl = source.TwitterImageUrl,
            Robots = source.Robots,

            PublishedAtUtc = source.PublishedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc,

            Counters = new ArticleCountersHttpResponse
            {
                Views = source.ViewCount,
                Likes = source.LikeCount,
                VisibleCommentCount = source.VisibleCommentCount,
                CountersPartial = source.CountersPartial
            },

            Tags = source.Tags
                .Select(MapArticleTag)
                .ToArray(),

            Media = source.Media
                .Select(MapArticleMedia)
                .ToArray()
        };
    }

    private static ArticleListItemHttpResponse MapArticleListItem(
        ArticleListItemResponse source)
    {
        return new ArticleListItemHttpResponse
        {
            ArticlePublicId = source.ArticlePublicId,
            Slug = source.Slug,

            Title = source.Title,
            Summary = source.Summary,

            CategoryId = source.CategoryId,
            CategoryName = source.CategoryName,

            AuthorUserId = source.AuthorUserId,
            AuthorDisplayName = source.AuthorDisplayName,

            CoverMediaId = source.CoverMediaId,
            CoverMediaUrl = source.CoverMediaUrl,
            CoverAlt = source.CoverAlt,

            PublishedAtUtc = source.PublishedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc,

            Counters = new ArticleCountersHttpResponse
            {
                Views = source.ViewCount,
                Likes = source.LikeCount,
                VisibleCommentCount = source.VisibleCommentCount,
                CountersPartial = source.CountersPartial
            }
        };
    }

    private static ArticleTagHttpResponse MapArticleTag(
        ArticleTagResponse source)
    {
        return new ArticleTagHttpResponse
        {
            TagId = source.TagId,
            TagPublicId = source.TagPublicId,
            Name = source.Name,
            Slug = source.Slug
        };
    }

    private static ArticleMediaHttpResponse MapArticleMedia(
        ArticleMediaResponse source)
    {
        return new ArticleMediaHttpResponse
        {
            MediaId = source.MediaId,
            MediaPublicId = source.MediaPublicId,
            Url = source.Url,
            Alt = source.Alt,
            Caption = source.Caption,
            MediaType = source.MediaType,
            IsPrimary = source.IsPrimary,
            SortOrder = source.SortOrder
        };
    }
}
