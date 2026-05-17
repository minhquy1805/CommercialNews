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
using Seo.Application.UseCases.SeoSettings.GetArticleSeoSettings;
using Seo.Application.UseCases.SeoSettings.GetSeoMetadataById;
using Seo.Application.UseCases.SeoSettings.GetSeoMetadataByResource;
using Seo.Application.UseCases.SeoSettings.GetSeoMetadataList;
using Seo.Application.UseCases.SeoSettings.UpsertArticleSeoSettings;

namespace CommercialNews.Api.Api.Admin.Controllers.Seo;

[Authorize]
[ApiController]
[Route("api/v1/admin/seo")]
public sealed class SeoMetadataAdminController : ControllerBase
{
    private readonly IGetSeoMetadataByIdUseCase _getSeoMetadataByIdUseCase;
    private readonly IGetSeoMetadataByResourceUseCase _getSeoMetadataByResourceUseCase;
    private readonly IGetSeoMetadataListUseCase _getSeoMetadataListUseCase;
    private readonly IGetArticleSeoSettingsUseCase _getArticleSeoSettingsUseCase;
    private readonly IUpsertArticleSeoSettingsUseCase _upsertArticleSeoSettingsUseCase;

    public SeoMetadataAdminController(
        IGetSeoMetadataByIdUseCase getSeoMetadataByIdUseCase,
        IGetSeoMetadataByResourceUseCase getSeoMetadataByResourceUseCase,
        IGetSeoMetadataListUseCase getSeoMetadataListUseCase,
        IGetArticleSeoSettingsUseCase getArticleSeoSettingsUseCase,
        IUpsertArticleSeoSettingsUseCase upsertArticleSeoSettingsUseCase)
    {
        _getSeoMetadataByIdUseCase = getSeoMetadataByIdUseCase;
        _getSeoMetadataByResourceUseCase = getSeoMetadataByResourceUseCase;
        _getSeoMetadataListUseCase = getSeoMetadataListUseCase;
        _getArticleSeoSettingsUseCase = getArticleSeoSettingsUseCase;
        _upsertArticleSeoSettingsUseCase = upsertArticleSeoSettingsUseCase;
    }

    [Authorize(Policy = AuthorizationPolicies.SeoMetadataRead)]
    [HttpGet("metadata/{seoId:long}")]
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

        return this.ToActionResult(
            Result<GetSeoMetadataByIdHttpResponse>.Success(Map(result.Value!)));
    }

    [Authorize(Policy = AuthorizationPolicies.SeoMetadataRead)]
    [HttpGet("metadata/by-resource")]
    [ProducesResponseType(typeof(GetSeoMetadataByResourceHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByResourceAsync(
        [FromQuery] string resourceType,
        [FromQuery] string resourcePublicId,
        [FromQuery] string? scope,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetSeoMetadataByResourceRequest
        {
            Scope = scope,
            ResourceType = resourceType,
            ResourcePublicId = resourcePublicId
        };

        Result<GetSeoMetadataByResourceResponse> result =
            await _getSeoMetadataByResourceUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GetSeoMetadataByResourceHttpResponse>.Failure(result.Error!));
        }

        return this.ToActionResult(
            Result<GetSeoMetadataByResourceHttpResponse>.Success(Map(result.Value!)));
    }

    [Authorize(Policy = AuthorizationPolicies.SeoMetadataRead)]
    [HttpGet("metadata")]
    [ProducesResponseType(typeof(GetSeoMetadataListHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPagedAsync(
        [FromQuery] string? scope = null,
        [FromQuery] string? resourceType = null,
        [FromQuery] string? resourcePublicId = null,
        [FromQuery] bool? isManualOverride = null,
        [FromQuery] long? updatedByUserId = null,
        [FromQuery] string? keyword = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "UpdatedAtUtc",
        [FromQuery] string sortDirection = "DESC",
        CancellationToken cancellationToken = default)
    {
        var useCaseRequest = new GetSeoMetadataListRequest
        {
            Scope = scope,
            ResourceType = resourceType,
            ResourcePublicId = resourcePublicId,
            IsManualOverride = isManualOverride,
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
            Items = result.Value!.Items.Select(Map).ToArray(),
            Page = result.Value.Page,
            PageSize = result.Value.PageSize,
            TotalItems = result.Value.TotalItems
        };

        return this.ToActionResult(Result<GetSeoMetadataListHttpResponse>.Success(response));
    }

    [Authorize(Policy = AuthorizationPolicies.SeoArticleSettingsRead)]
    [HttpGet("articles/{articlePublicId}")]
    [ProducesResponseType(typeof(GetArticleSeoSettingsHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArticleSettingsAsync(
        [FromRoute] string articlePublicId,
        [FromQuery] string? scope,
        CancellationToken cancellationToken)
    {
        Result<GetArticleSeoSettingsResponse> result =
            await _getArticleSeoSettingsUseCase.ExecuteAsync(articlePublicId, scope, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GetArticleSeoSettingsHttpResponse>.Failure(result.Error!));
        }

        return this.ToActionResult(
            Result<GetArticleSeoSettingsHttpResponse>.Success(Map(result.Value!)));
    }

    [Authorize(Policy = AuthorizationPolicies.SeoArticleSettingsUpsert)]
    [HttpPut("articles/{articlePublicId}")]
    [ProducesResponseType(typeof(UpsertArticleSeoSettingsHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> UpsertArticleSettingsAsync(
        [FromRoute] string articlePublicId,
        [FromBody] UpsertArticleSeoSettingsHttpRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new UpsertArticleSeoSettingsRequest
        {
            Scope = request.Scope,
            Slug = request.Slug,
            CanonicalUrl = request.CanonicalUrl,
            MetaTitle = request.MetaTitle,
            MetaDescription = request.MetaDescription,
            OgTitle = request.OgTitle,
            OgDescription = request.OgDescription,
            OgImageUrl = request.OgImageUrl,
            TwitterTitle = request.TwitterTitle,
            TwitterDescription = request.TwitterDescription,
            TwitterImageUrl = request.TwitterImageUrl,
            Robots = request.Robots,
            IsIndexable = request.IsIndexable,
            IsActive = request.IsActive,
            ExpectedSlugVersion = request.ExpectedSlugVersion,
            ExpectedSeoMetadataVersion = request.ExpectedSeoMetadataVersion
        };

        Result<UpsertArticleSeoSettingsResponse> result =
            await _upsertArticleSeoSettingsUseCase.ExecuteAsync(
                articlePublicId,
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<UpsertArticleSeoSettingsHttpResponse>.Failure(result.Error!));
        }

        return this.ToActionResult(
            Result<UpsertArticleSeoSettingsHttpResponse>.Success(Map(result.Value!)));
    }

    private static GetSeoMetadataByIdHttpResponse Map(GetSeoMetadataByIdResponse source)
    {
        return new GetSeoMetadataByIdHttpResponse
        {
            SeoId = source.SeoId,
            Scope = source.Scope,
            ResourceType = source.ResourceType,
            ResourcePublicId = source.ResourcePublicId,
            Slug = source.Slug,
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
            IsManualOverride = source.IsManualOverride,
            SourceAggregateVersion = source.SourceAggregateVersion,
            LastAppliedMessageId = source.LastAppliedMessageId,
            LastSyncedAtUtc = source.LastSyncedAtUtc,
            Version = source.Version,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc,
            UpdatedByUserId = source.UpdatedByUserId
        };
    }

    private static GetSeoMetadataByResourceHttpResponse Map(GetSeoMetadataByResourceResponse source)
    {
        return new GetSeoMetadataByResourceHttpResponse
        {
            Scope = source.Scope,
            ResourceType = source.ResourceType,
            ResourcePublicId = source.ResourcePublicId,
            Slug = source.Slug,
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
            IsManualOverride = source.IsManualOverride,
            SourceAggregateVersion = source.SourceAggregateVersion,
            LastAppliedMessageId = source.LastAppliedMessageId,
            LastSyncedAtUtc = source.LastSyncedAtUtc,
            Version = source.Version
        };
    }

    private static GetSeoMetadataListItemHttpResponse Map(GetSeoMetadataListResponse source)
    {
        return new GetSeoMetadataListItemHttpResponse
        {
            SeoId = source.SeoId,
            Scope = source.Scope,
            ResourceType = source.ResourceType,
            ResourcePublicId = source.ResourcePublicId,
            Slug = source.Slug,
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
            IsManualOverride = source.IsManualOverride,
            SourceAggregateVersion = source.SourceAggregateVersion,
            LastAppliedMessageId = source.LastAppliedMessageId,
            LastSyncedAtUtc = source.LastSyncedAtUtc,
            Version = source.Version,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc,
            UpdatedByUserId = source.UpdatedByUserId
        };
    }

    private static GetArticleSeoSettingsHttpResponse Map(GetArticleSeoSettingsResponse source)
    {
        return new GetArticleSeoSettingsHttpResponse
        {
            ArticlePublicId = source.ArticlePublicId,
            Scope = source.Scope,
            ResourceType = source.ResourceType,
            ResourcePublicId = source.ResourcePublicId,
            Slug = source.Slug,
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
            IsManualOverride = source.IsManualOverride,
            IsIndexable = source.IsIndexable,
            IsActive = source.IsActive,
            SourceAggregateVersion = source.SourceAggregateVersion,
            LastAppliedMessageId = source.LastAppliedMessageId,
            LastSyncedAtUtc = source.LastSyncedAtUtc,
            Version = source.Version
        };
    }

    private static UpsertArticleSeoSettingsHttpResponse Map(UpsertArticleSeoSettingsResponse source)
    {
        return new UpsertArticleSeoSettingsHttpResponse
        {
            Updated = source.Updated,
            ArticlePublicId = source.ArticlePublicId,
            Scope = source.Scope,
            ResourceType = source.ResourceType,
            ResourcePublicId = source.ResourcePublicId,
            Slug = source.Slug,
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
            IsManualOverride = source.IsManualOverride,
            IsIndexable = source.IsIndexable,
            IsActive = source.IsActive,
            SourceAggregateVersion = source.SourceAggregateVersion,
            LastAppliedMessageId = source.LastAppliedMessageId,
            LastSyncedAtUtc = source.LastSyncedAtUtc,
            Version = source.Version
        };
    }
}
