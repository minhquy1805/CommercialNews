using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Api.Public.Contracts.Reading.Requests;
using CommercialNews.Api.Api.Public.Contracts.Reading.Responses;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;
using Reading.Application.Contracts.Requests;
using Reading.Application.Contracts.Responses;
using Reading.Application.UseCases.GetArticleById;
using Reading.Application.UseCases.GetArticleBySlug;
using Reading.Application.UseCases.GetArticles;
using Reading.Application.UseCases.GetRelatedArticles;
using Reading.Application.UseCases.SearchArticles;

namespace CommercialNews.Api.Api.Public.Controllers.Reading;

[ApiController]
[Route("api/v1/reading")]
public sealed class ReadingPublicController : ControllerBase
{
    private readonly IGetArticlesUseCase _getArticlesUseCase;
    private readonly IGetArticleByIdUseCase _getArticleByIdUseCase;
    private readonly IGetArticleBySlugUseCase _getArticleBySlugUseCase;
    private readonly IGetRelatedArticlesUseCase _getRelatedArticlesUseCase;
    private readonly ISearchArticlesUseCase _searchArticlesUseCase;

    public ReadingPublicController(
        IGetArticlesUseCase getArticlesUseCase,
        IGetArticleByIdUseCase getArticleByIdUseCase,
        IGetArticleBySlugUseCase getArticleBySlugUseCase,
        IGetRelatedArticlesUseCase getRelatedArticlesUseCase,
        ISearchArticlesUseCase searchArticlesUseCase)
    {
        _getArticlesUseCase = getArticlesUseCase;
        _getArticleByIdUseCase = getArticleByIdUseCase;
        _getArticleBySlugUseCase = getArticleBySlugUseCase;
        _getRelatedArticlesUseCase = getRelatedArticlesUseCase;
        _searchArticlesUseCase = searchArticlesUseCase;
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
            Q = request.Q,
            Sort = request.Sort
        };

        Result<GetArticlesResponse> result =
            await _getArticlesUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GetArticlesHttpResponse>.Failure(result.Error!));
        }

        var response = new GetArticlesHttpResponse
        {
            Items = result.Value!.Items
                .Select(MapArticleListItem)
                .ToArray(),
            PageInfo = MapPageInfo(result.Value.PageInfo)
        };

        return this.ToActionResult(Result<GetArticlesHttpResponse>.Success(response));
    }

    [HttpGet("articles/{articleId:long}")]
    [ProducesResponseType(typeof(GetArticleByIdHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArticleByIdAsync(
        [FromRoute] long articleId,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetArticleByIdRequest
        {
            ArticleId = articleId
        };

        Result<GetArticleByIdResponse> result =
            await _getArticleByIdUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GetArticleByIdHttpResponse>.Failure(result.Error!));
        }

        var response = new GetArticleByIdHttpResponse
        {
            ArticleId = result.Value!.ArticleId,
            Title = result.Value.Title,
            Summary = result.Value.Summary,
            Body = result.Value.Body,
            Slug = result.Value.Slug,
            PublishedAt = result.Value.PublishedAt,
            Category = result.Value.Category is null
                ? null
                : new CategorySummaryHttpResponse
                {
                    CategoryId = result.Value.Category.CategoryId,
                    Name = result.Value.Category.Name
                },
            Tags = result.Value.Tags
                .Select(static tag => new TagSummaryHttpResponse
                {
                    TagId = tag.TagId,
                    Name = tag.Name
                })
                .ToArray(),
            Media = result.Value.Media
                .Select(static media => new MediaSummaryHttpResponse
                {
                    MediaId = media.MediaId,
                    Url = media.Url,
                    Alt = media.Alt,
                    IsPrimary = media.IsPrimary,
                    Order = media.Order
                })
                .ToArray(),
            Seo = result.Value.Seo is null
                ? null
                : new SeoSummaryHttpResponse
                {
                    CanonicalUrl = result.Value.Seo.CanonicalUrl,
                    MetaTitle = result.Value.Seo.MetaTitle,
                    MetaDescription = result.Value.Seo.MetaDescription
                },
            Counters = result.Value.Counters is null
                ? null
                : new ArticleCountersHttpResponse
                {
                    Views = result.Value.Counters.Views,
                    Likes = result.Value.Counters.Likes,
                    CountersPartial = result.Value.Counters.CountersPartial
                }
        };

        return this.ToActionResult(Result<GetArticleByIdHttpResponse>.Success(response));
    }

    [HttpGet("articles/slug")]
    [ProducesResponseType(typeof(GetArticleBySlugHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArticleBySlugAsync(
        [FromQuery] GetArticleBySlugHttpRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetArticleBySlugRequest
        {
            Scope = request.Scope,
            Slug = request.Slug
        };

        Result<GetArticleBySlugResponse> result =
            await _getArticleBySlugUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GetArticleBySlugHttpResponse>.Failure(result.Error!));
        }

        var response = new GetArticleBySlugHttpResponse
        {
            ArticleId = result.Value!.ArticleId,
            Title = result.Value.Title,
            Summary = result.Value.Summary,
            Body = result.Value.Body,
            Slug = result.Value.Slug,
            PublishedAt = result.Value.PublishedAt,
            Category = result.Value.Category is null
                ? null
                : new CategorySummaryHttpResponse
                {
                    CategoryId = result.Value.Category.CategoryId,
                    Name = result.Value.Category.Name
                },
            Tags = result.Value.Tags
                .Select(static tag => new TagSummaryHttpResponse
                {
                    TagId = tag.TagId,
                    Name = tag.Name
                })
                .ToArray(),
            Media = result.Value.Media
                .Select(static media => new MediaSummaryHttpResponse
                {
                    MediaId = media.MediaId,
                    Url = media.Url,
                    Alt = media.Alt,
                    IsPrimary = media.IsPrimary,
                    Order = media.Order
                })
                .ToArray(),
            Seo = result.Value.Seo is null
                ? null
                : new SeoSummaryHttpResponse
                {
                    CanonicalUrl = result.Value.Seo.CanonicalUrl,
                    MetaTitle = result.Value.Seo.MetaTitle,
                    MetaDescription = result.Value.Seo.MetaDescription
                },
            Counters = result.Value.Counters is null
                ? null
                : new ArticleCountersHttpResponse
                {
                    Views = result.Value.Counters.Views,
                    Likes = result.Value.Counters.Likes,
                    CountersPartial = result.Value.Counters.CountersPartial
                }
        };

        return this.ToActionResult(Result<GetArticleBySlugHttpResponse>.Success(response));
    }

    [HttpGet("articles/{articleId:long}/related")]
    [ProducesResponseType(typeof(GetRelatedArticlesHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRelatedArticlesAsync(
        [FromRoute] long articleId,
        [FromQuery] GetRelatedArticlesHttpRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetRelatedArticlesRequest
        {
            ArticleId = articleId,
            Limit = request.Limit
        };

        Result<GetRelatedArticlesResponse> result =
            await _getRelatedArticlesUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GetRelatedArticlesHttpResponse>.Failure(result.Error!));
        }

        var response = new GetRelatedArticlesHttpResponse
        {
            Items = result.Value!.Items
                .Select(MapArticleListItem)
                .ToArray()
        };

        return this.ToActionResult(Result<GetRelatedArticlesHttpResponse>.Success(response));
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(SearchArticlesHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchArticlesAsync(
        [FromQuery] SearchArticlesHttpRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new SearchArticlesRequest
        {
            Q = request.Q,
            Page = request.Page,
            PageSize = request.PageSize,
            Sort = request.Sort
        };

        Result<SearchArticlesResponse> result =
            await _searchArticlesUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<SearchArticlesHttpResponse>.Failure(result.Error!));
        }

        var response = new SearchArticlesHttpResponse
        {
            Items = result.Value!.Items
                .Select(MapArticleListItem)
                .ToArray(),
            PageInfo = MapPageInfo(result.Value.PageInfo)
        };

        return this.ToActionResult(Result<SearchArticlesHttpResponse>.Success(response));
    }

    private static ArticleListItemHttpResponse MapArticleListItem(ArticleListItemResponse source)
    {
        return new ArticleListItemHttpResponse
        {
            ArticleId = source.ArticleId,
            Title = source.Title,
            Summary = source.Summary,
            Slug = source.Slug,
            PublishedAt = source.PublishedAt,
            Category = source.Category is null
                ? null
                : new CategorySummaryHttpResponse
                {
                    CategoryId = source.Category.CategoryId,
                    Name = source.Category.Name
                },
            Cover = source.Cover is null
                ? null
                : new MediaSummaryHttpResponse
                {
                    MediaId = source.Cover.MediaId,
                    Url = source.Cover.Url,
                    Alt = source.Cover.Alt,
                    IsPrimary = source.Cover.IsPrimary,
                    Order = source.Cover.Order
                },
            Counters = source.Counters is null
                ? null
                : new ArticleCountersHttpResponse
                {
                    Views = source.Counters.Views,
                    Likes = source.Counters.Likes,
                    CountersPartial = source.Counters.CountersPartial
                }
        };
    }

    private static PageInfo MapPageInfo(PageInfo source)
    {
        return new PageInfo
        {
            Page = source.Page,
            PageSize = source.PageSize,
            TotalItems = source.TotalItems,
            TotalPages = source.TotalPages
        };
    }
}