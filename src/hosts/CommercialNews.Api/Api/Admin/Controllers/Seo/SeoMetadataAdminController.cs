using CommercialNews.Api.Api.Admin.Contracts.Seo.SeoMetadata.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Seo.SeoMetadata.Responses;
using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Authorization;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;
using Seo.Application.UseCases.SeoSettings.CreateSeoMetadata;
using Seo.Application.UseCases.SeoSettings.GetArticleSeoSettings;
using Seo.Application.UseCases.SeoSettings.GetSeoMetadataByArticleId;
using Seo.Application.UseCases.SeoSettings.GetSeoMetadataById;
using Seo.Application.UseCases.SeoSettings.GetSeoMetadataList;
using Seo.Application.UseCases.SeoSettings.UpdateSeoMetadata;
using Seo.Application.UseCases.SeoSettings.UpsertArticleSeoSettings;

namespace CommercialNews.Api.Api.Admin.Controllers.Seo;

[Authorize]
[ApiController]
[Route("api/v1/admin/seo/metadata")]
public sealed class SeoMetadataAdminController : ControllerBase
{
    private const string GetSeoMetadataByIdRouteName = "AdminSeoMetadata.GetById";

    private readonly ICreateSeoMetadataUseCase _createSeoMetadataUseCase;
    private readonly IGetSeoMetadataByIdUseCase _getSeoMetadataByIdUseCase;
    private readonly IGetSeoMetadataByArticleIdUseCase _getSeoMetadataByArticleIdUseCase;
    private readonly IGetSeoMetadataListUseCase _getSeoMetadataListUseCase;
    private readonly IUpdateSeoMetadataUseCase _updateSeoMetadataUseCase;
    private readonly IGetArticleSeoSettingsUseCase _getArticleSeoSettingsUseCase;
    private readonly IUpsertArticleSeoSettingsUseCase _upsertArticleSeoSettingsUseCase;

    public SeoMetadataAdminController(
        ICreateSeoMetadataUseCase createSeoMetadataUseCase,
        IGetSeoMetadataByIdUseCase getSeoMetadataByIdUseCase,
        IGetSeoMetadataByArticleIdUseCase getSeoMetadataByArticleIdUseCase,
        IGetSeoMetadataListUseCase getSeoMetadataListUseCase,
        IUpdateSeoMetadataUseCase updateSeoMetadataUseCase,
        IGetArticleSeoSettingsUseCase getArticleSeoSettingsUseCase,
        IUpsertArticleSeoSettingsUseCase upsertArticleSeoSettingsUseCase)
    {
        _createSeoMetadataUseCase = createSeoMetadataUseCase;
        _getSeoMetadataByIdUseCase = getSeoMetadataByIdUseCase;
        _getSeoMetadataByArticleIdUseCase = getSeoMetadataByArticleIdUseCase;
        _getSeoMetadataListUseCase = getSeoMetadataListUseCase;
        _updateSeoMetadataUseCase = updateSeoMetadataUseCase;
        _getArticleSeoSettingsUseCase = getArticleSeoSettingsUseCase;
        _upsertArticleSeoSettingsUseCase = upsertArticleSeoSettingsUseCase;
    }

    [Authorize(Policy = AuthorizationPolicies.SeoMetadataCreate)]
    [HttpPost]
    [ProducesResponseType(typeof(CreateSeoMetadataHttpResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateSeoMetadataHttpRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new CreateSeoMetadataRequest
        {
            ArticleId = request.ArticleId,
            CanonicalUrl = request.CanonicalUrl,
            MetaTitle = request.MetaTitle,
            MetaDescription = request.MetaDescription,
            OgTitle = request.OgTitle,
            OgDescription = request.OgDescription,
            OgImageUrl = request.OgImageUrl,
            TwitterTitle = request.TwitterTitle,
            TwitterDescription = request.TwitterDescription,
            TwitterImageUrl = request.TwitterImageUrl
        };

        Result<CreateSeoMetadataResponse> result =
            await _createSeoMetadataUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<CreateSeoMetadataHttpResponse>.Failure(result.Error!));
        }

        var response = new CreateSeoMetadataHttpResponse
        {
            SeoId = result.Value!.SeoId,
            ArticleId = result.Value.ArticleId,
            CanonicalUrl = result.Value.CanonicalUrl,
            MetaTitle = result.Value.MetaTitle,
            MetaDescription = result.Value.MetaDescription,
            OgTitle = result.Value.OgTitle,
            OgDescription = result.Value.OgDescription,
            OgImageUrl = result.Value.OgImageUrl,
            TwitterTitle = result.Value.TwitterTitle,
            TwitterDescription = result.Value.TwitterDescription,
            TwitterImageUrl = result.Value.TwitterImageUrl,
            Version = result.Value.Version,
            UpdatedAt = result.Value.UpdatedAt,
            UpdatedByUserId = result.Value.UpdatedByUserId
        };

        return CreatedAtRoute(
            GetSeoMetadataByIdRouteName,
            new { seoId = response.SeoId },
            response);
    }

    [Authorize(Policy = AuthorizationPolicies.SeoMetadataRead)]
    [HttpGet("{seoId:long}", Name = GetSeoMetadataByIdRouteName)]
    [ProducesResponseType(typeof(GetSeoMetadataByIdHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(
        [FromRoute] long seoId,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetSeoMetadataByIdRequest
        {
            SeoId = seoId
        };

        Result<GetSeoMetadataByIdResponse> result =
            await _getSeoMetadataByIdUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GetSeoMetadataByIdHttpResponse>.Failure(result.Error!));
        }

        var response = new GetSeoMetadataByIdHttpResponse
        {
            SeoId = result.Value!.SeoId,
            ArticleId = result.Value.ArticleId,
            CanonicalUrl = result.Value.CanonicalUrl,
            MetaTitle = result.Value.MetaTitle,
            MetaDescription = result.Value.MetaDescription,
            OgTitle = result.Value.OgTitle,
            OgDescription = result.Value.OgDescription,
            OgImageUrl = result.Value.OgImageUrl,
            TwitterTitle = result.Value.TwitterTitle,
            TwitterDescription = result.Value.TwitterDescription,
            TwitterImageUrl = result.Value.TwitterImageUrl,
            Version = result.Value.Version,
            UpdatedAt = result.Value.UpdatedAt,
            UpdatedByUserId = result.Value.UpdatedByUserId
        };

        return this.ToActionResult(Result<GetSeoMetadataByIdHttpResponse>.Success(response));
    }

    [Authorize(Policy = AuthorizationPolicies.SeoMetadataRead)]
    [HttpGet("by-article/{articleId:long}")]
    [ProducesResponseType(typeof(GetSeoMetadataByArticleIdHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByArticleIdAsync(
        [FromRoute] long articleId,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetSeoMetadataByArticleIdRequest
        {
            ArticleId = articleId
        };

        Result<GetSeoMetadataByArticleIdResponse> result =
            await _getSeoMetadataByArticleIdUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GetSeoMetadataByArticleIdHttpResponse>.Failure(result.Error!));
        }

        var response = new GetSeoMetadataByArticleIdHttpResponse
        {
            ResourceType = result.Value!.ResourceType,
            ResourceId = result.Value.ResourceId,
            Slug = result.Value.Slug,
            CanonicalUrl = result.Value.CanonicalUrl,
            MetaTitle = result.Value.MetaTitle,
            MetaDescription = result.Value.MetaDescription,
            OgTitle = result.Value.OgTitle,
            OgDescription = result.Value.OgDescription,
            OgImageUrl = result.Value.OgImageUrl,
            Version = result.Value.Version
        };

        return this.ToActionResult(Result<GetSeoMetadataByArticleIdHttpResponse>.Success(response));
    }

    [Authorize(Policy = AuthorizationPolicies.SeoMetadataRead)]
    [HttpGet]
    [ProducesResponseType(typeof(GetSeoMetadataListHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPagedAsync(
        [FromQuery] long? articleId = null,
        [FromQuery] long? updatedByUserId = null,
        [FromQuery] string? keyword = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "UpdatedAt",
        [FromQuery] string sortDirection = "DESC",
        CancellationToken cancellationToken = default)
    {
        var useCaseRequest = new GetSeoMetadataListRequest
        {
            ArticleId = articleId,
            UpdatedByUserId = updatedByUserId,
            Keyword = keyword,
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        Result<PagedQueryResult<GetSeoMetadataListResponse>> result =
            await _getSeoMetadataListUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GetSeoMetadataListHttpResponse>.Failure(result.Error!));
        }

        var response = new GetSeoMetadataListHttpResponse
        {
            Items = result.Value!.Items.Select(static item => new GetSeoMetadataListItemHttpResponse
            {
                SeoId = item.SeoId,
                ArticleId = item.ArticleId,
                CanonicalUrl = item.CanonicalUrl,
                MetaTitle = item.MetaTitle,
                MetaDescription = item.MetaDescription,
                OgTitle = item.OgTitle,
                OgDescription = item.OgDescription,
                OgImageUrl = item.OgImageUrl,
                TwitterTitle = item.TwitterTitle,
                TwitterDescription = item.TwitterDescription,
                TwitterImageUrl = item.TwitterImageUrl,
                Version = item.Version,
                UpdatedAt = item.UpdatedAt,
                UpdatedByUserId = item.UpdatedByUserId
            }).ToArray(),
            Page = result.Value.Page,
            PageSize = result.Value.PageSize,
            TotalItems = result.Value.TotalItems
        };

        return this.ToActionResult(Result<GetSeoMetadataListHttpResponse>.Success(response));
    }

    [Authorize(Policy = AuthorizationPolicies.SeoMetadataUpdate)]
    [HttpPut("{seoId:long}")]
    [ProducesResponseType(typeof(UpdateSeoMetadataHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] long seoId,
        [FromBody] UpdateSeoMetadataHttpRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new UpdateSeoMetadataRequest
        {
            SeoId = seoId,
            CanonicalUrl = request.CanonicalUrl,
            MetaTitle = request.MetaTitle,
            MetaDescription = request.MetaDescription,
            OgTitle = request.OgTitle,
            OgDescription = request.OgDescription,
            OgImageUrl = request.OgImageUrl,
            TwitterTitle = request.TwitterTitle,
            TwitterDescription = request.TwitterDescription,
            TwitterImageUrl = request.TwitterImageUrl,
            ExpectedVersion = request.ExpectedVersion
        };

        Result<UpdateSeoMetadataResponse> result =
            await _updateSeoMetadataUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<UpdateSeoMetadataHttpResponse>.Failure(result.Error!));
        }

        var response = new UpdateSeoMetadataHttpResponse
        {
            SeoId = result.Value!.SeoId,
            ArticleId = result.Value.ArticleId,
            CanonicalUrl = result.Value.CanonicalUrl,
            MetaTitle = result.Value.MetaTitle,
            MetaDescription = result.Value.MetaDescription,
            OgTitle = result.Value.OgTitle,
            OgDescription = result.Value.OgDescription,
            OgImageUrl = result.Value.OgImageUrl,
            TwitterTitle = result.Value.TwitterTitle,
            TwitterDescription = result.Value.TwitterDescription,
            TwitterImageUrl = result.Value.TwitterImageUrl,
            Version = result.Value.Version,
            UpdatedAt = result.Value.UpdatedAt,
            UpdatedByUserId = result.Value.UpdatedByUserId
        };

        return this.ToActionResult(Result<UpdateSeoMetadataHttpResponse>.Success(response));
    }

    [Authorize(Policy = AuthorizationPolicies.SeoMetadataRead)]
    [HttpGet("articles/{articleId:long}/settings")]
    [ProducesResponseType(typeof(GetArticleSeoSettingsHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArticleSettingsAsync(
        [FromRoute] long articleId,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetArticleSeoSettingsRequest
        {
            ArticleId = articleId
        };

        Result<GetArticleSeoSettingsResponse> result =
            await _getArticleSeoSettingsUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GetArticleSeoSettingsHttpResponse>.Failure(result.Error!));
        }

        var response = new GetArticleSeoSettingsHttpResponse
        {
            ArticleId = result.Value!.ArticleId,
            Scope = result.Value.Scope,
            Slug = result.Value.Slug,
            CanonicalUrl = result.Value.CanonicalUrl,
            MetaTitle = result.Value.MetaTitle,
            MetaDescription = result.Value.MetaDescription,
            OgTitle = result.Value.OgTitle,
            OgDescription = result.Value.OgDescription,
            OgImageUrl = result.Value.OgImageUrl,
            TwitterTitle = result.Value.TwitterTitle,
            TwitterDescription = result.Value.TwitterDescription,
            TwitterImageUrl = result.Value.TwitterImageUrl,
            IsIndexable = result.Value.IsIndexable,
            IsActive = result.Value.IsActive,
            Version = result.Value.Version
        };

        return this.ToActionResult(Result<GetArticleSeoSettingsHttpResponse>.Success(response));
    }

    [Authorize(Policy = AuthorizationPolicies.SeoMetadataUpdate)]
    [HttpPut("articles/{articleId:long}/settings")]
    [ProducesResponseType(typeof(UpsertArticleSeoSettingsHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpsertArticleSettingsAsync(
        [FromRoute] long articleId,
        [FromBody] UpsertArticleSeoSettingsHttpRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new UpsertArticleSeoSettingsRequest
        {
            ArticleId = articleId,
            Slug = request.Slug,
            Scope = request.Scope,
            CanonicalUrl = request.CanonicalUrl,
            MetaTitle = request.MetaTitle,
            MetaDescription = request.MetaDescription,
            OgTitle = request.OgTitle,
            OgDescription = request.OgDescription,
            OgImageUrl = request.OgImageUrl,
            TwitterTitle = request.TwitterTitle,
            TwitterDescription = request.TwitterDescription,
            TwitterImageUrl = request.TwitterImageUrl,
            IsIndexable = request.IsIndexable,
            IsActive = request.IsActive,
            ExpectedSlugVersion = request.ExpectedSlugVersion,
            ExpectedSeoMetadataVersion = request.ExpectedSeoMetadataVersion
        };

        Result<UpsertArticleSeoSettingsResponse> result =
            await _upsertArticleSeoSettingsUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<UpsertArticleSeoSettingsHttpResponse>.Failure(result.Error!));
        }

        var response = new UpsertArticleSeoSettingsHttpResponse
        {
            Updated = result.Value!.Updated,
            ArticleId = result.Value.ArticleId,
            Scope = result.Value.Scope,
            Slug = result.Value.Slug,
            CanonicalUrl = result.Value.CanonicalUrl,
            MetaTitle = result.Value.MetaTitle,
            MetaDescription = result.Value.MetaDescription,
            OgTitle = result.Value.OgTitle,
            OgDescription = result.Value.OgDescription,
            OgImageUrl = result.Value.OgImageUrl,
            TwitterTitle = result.Value.TwitterTitle,
            TwitterDescription = result.Value.TwitterDescription,
            TwitterImageUrl = result.Value.TwitterImageUrl,
            IsIndexable = result.Value.IsIndexable,
            IsActive = result.Value.IsActive,
            Version = result.Value.Version
        };

        return this.ToActionResult(Result<UpsertArticleSeoSettingsHttpResponse>.Success(response));
    }
}