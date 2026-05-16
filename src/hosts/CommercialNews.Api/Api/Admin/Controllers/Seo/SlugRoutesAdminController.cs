using CommercialNews.Api.Api.Admin.Contracts.Seo.SlugRoutes.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Seo.SlugRoutes.Responses;
using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Authorization;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;
using Seo.Application.UseCases.SlugRoutes.CheckSlugAvailability;
using Seo.Application.UseCases.SlugRoutes.GenerateSlug;
using Seo.Application.UseCases.SlugRoutes.GetSlugRegistryById;
using Seo.Application.UseCases.SlugRoutes.GetSlugRegistryByResource;
using Seo.Application.UseCases.SlugRoutes.GetSlugRegistryList;

namespace CommercialNews.Api.Api.Admin.Controllers.Seo;

[Authorize]
[ApiController]
[Route("api/v1/admin/seo")]
public sealed class SlugRoutesAdminController : ControllerBase
{
    private readonly IGetSlugRegistryByIdUseCase _getSlugRegistryByIdUseCase;
    private readonly IGetSlugRegistryByResourceUseCase _getSlugRegistryByResourceUseCase;
    private readonly IGetSlugRegistryListUseCase _getSlugRegistryListUseCase;
    private readonly ICheckSlugAvailabilityUseCase _checkSlugAvailabilityUseCase;
    private readonly IGenerateSlugUseCase _generateSlugUseCase;

    public SlugRoutesAdminController(
        IGetSlugRegistryByIdUseCase getSlugRegistryByIdUseCase,
        IGetSlugRegistryByResourceUseCase getSlugRegistryByResourceUseCase,
        IGetSlugRegistryListUseCase getSlugRegistryListUseCase,
        ICheckSlugAvailabilityUseCase checkSlugAvailabilityUseCase,
        IGenerateSlugUseCase generateSlugUseCase)
    {
        _getSlugRegistryByIdUseCase = getSlugRegistryByIdUseCase;
        _getSlugRegistryByResourceUseCase = getSlugRegistryByResourceUseCase;
        _getSlugRegistryListUseCase = getSlugRegistryListUseCase;
        _checkSlugAvailabilityUseCase = checkSlugAvailabilityUseCase;
        _generateSlugUseCase = generateSlugUseCase;
    }

    [Authorize(Policy = AuthorizationPolicies.SeoSlugRoutesRead)]
    [HttpGet("slug-routes/{slugId:long}")]
    [ProducesResponseType(typeof(GetSlugRouteByIdHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(
        [FromRoute] long slugId,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetSlugRegistryByIdRequest
        {
            SlugId = slugId
        };

        Result<GetSlugRegistryByIdResponse> result =
            await _getSlugRegistryByIdUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GetSlugRouteByIdHttpResponse>.Failure(result.Error!));
        }

        return this.ToActionResult(
            Result<GetSlugRouteByIdHttpResponse>.Success(Map(result.Value!)));
    }

    [Authorize(Policy = AuthorizationPolicies.SeoSlugRoutesRead)]
    [HttpGet("slug-routes/by-resource")]
    [ProducesResponseType(typeof(GetSlugRouteByResourceHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByResourceAsync(
        [FromQuery] string resourceType,
        [FromQuery] string resourcePublicId,
        [FromQuery] string? scope = null,
        [FromQuery] bool? onlyActive = null,
        CancellationToken cancellationToken = default)
    {
        var useCaseRequest = new GetSlugRegistryByResourceRequest
        {
            Scope = scope,
            ResourceType = resourceType,
            ResourcePublicId = resourcePublicId,
            OnlyActive = onlyActive
        };

        Result<GetSlugRegistryByResourceResponse> result =
            await _getSlugRegistryByResourceUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GetSlugRouteByResourceHttpResponse>.Failure(result.Error!));
        }

        return this.ToActionResult(
            Result<GetSlugRouteByResourceHttpResponse>.Success(Map(result.Value!)));
    }

    [Authorize(Policy = AuthorizationPolicies.SeoSlugRoutesRead)]
    [HttpGet("slug-routes")]
    [ProducesResponseType(typeof(GetSlugRouteListHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPagedAsync(
        [FromQuery] string? scope = null,
        [FromQuery] string? resourceType = null,
        [FromQuery] string? resourcePublicId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isIndexable = null,
        [FromQuery] string? keyword = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "UpdatedAtUtc",
        [FromQuery] string sortDirection = "DESC",
        CancellationToken cancellationToken = default)
    {
        var useCaseRequest = new GetSlugRegistryListRequest
        {
            Scope = scope,
            ResourceType = resourceType,
            ResourcePublicId = resourcePublicId,
            IsActive = isActive,
            IsIndexable = isIndexable,
            Keyword = keyword,
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        Result<PagedQueryResult<GetSlugRegistryListResponse>> result =
            await _getSlugRegistryListUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GetSlugRouteListHttpResponse>.Failure(result.Error!));
        }

        var response = new GetSlugRouteListHttpResponse
        {
            Items = result.Value!.Items.Select(Map).ToArray(),
            Page = result.Value.Page,
            PageSize = result.Value.PageSize,
            TotalItems = result.Value.TotalItems
        };

        return this.ToActionResult(Result<GetSlugRouteListHttpResponse>.Success(response));
    }

    [Authorize(Policy = AuthorizationPolicies.SeoSlugRoutesRead)]
    [HttpGet("slug-availability")]
    [ProducesResponseType(typeof(CheckSlugAvailabilityHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckAvailabilityAsync(
        [FromQuery] string slug,
        [FromQuery] string? scope = null,
        [FromQuery] string? resourceType = null,
        [FromQuery] string? resourcePublicId = null,
        CancellationToken cancellationToken = default)
    {
        var useCaseRequest = new CheckSlugAvailabilityRequest
        {
            Scope = scope,
            Slug = slug,
            ResourceType = resourceType,
            ResourcePublicId = resourcePublicId
        };

        Result<CheckSlugAvailabilityResponse> result =
            await _checkSlugAvailabilityUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<CheckSlugAvailabilityHttpResponse>.Failure(result.Error!));
        }

        return this.ToActionResult(
            Result<CheckSlugAvailabilityHttpResponse>.Success(Map(result.Value!)));
    }

    [Authorize(Policy = AuthorizationPolicies.SeoSlugRoutesGenerate)]
    [HttpPost("generate-slug")]
    [ProducesResponseType(typeof(GenerateSlugHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateAsync(
        [FromBody] GenerateSlugHttpRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GenerateSlugRequest
        {
            Source = request.Source,
            Scope = request.Scope,
            ResourceType = request.ResourceType,
            ResourcePublicId = request.ResourcePublicId
        };

        Result<GenerateSlugResponse> result =
            await _generateSlugUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GenerateSlugHttpResponse>.Failure(result.Error!));
        }

        return this.ToActionResult(
            Result<GenerateSlugHttpResponse>.Success(Map(result.Value!)));
    }

    private static GetSlugRouteByIdHttpResponse Map(GetSlugRegistryByIdResponse source)
    {
        return new GetSlugRouteByIdHttpResponse
        {
            SlugId = source.SlugId,
            Scope = source.Scope,
            Slug = source.Slug,
            ResourceType = source.ResourceType,
            ResourcePublicId = source.ResourcePublicId,
            CanonicalUrl = source.CanonicalUrl,
            IsIndexable = source.IsIndexable,
            IsActive = source.IsActive,
            SourceAggregateVersion = source.SourceAggregateVersion,
            LastAppliedMessageId = source.LastAppliedMessageId,
            LastSyncedAtUtc = source.LastSyncedAtUtc,
            Version = source.Version,
            CreatedAtUtc = source.CreatedAtUtc,
            CreatedByUserId = source.CreatedByUserId,
            UpdatedAtUtc = source.UpdatedAtUtc,
            UpdatedByUserId = source.UpdatedByUserId
        };
    }

    private static GetSlugRouteByResourceHttpResponse Map(GetSlugRegistryByResourceResponse source)
    {
        return new GetSlugRouteByResourceHttpResponse
        {
            SlugId = source.SlugId,
            Scope = source.Scope,
            Slug = source.Slug,
            ResourceType = source.ResourceType,
            ResourcePublicId = source.ResourcePublicId,
            CanonicalUrl = source.CanonicalUrl,
            IsIndexable = source.IsIndexable,
            IsActive = source.IsActive,
            SourceAggregateVersion = source.SourceAggregateVersion,
            LastAppliedMessageId = source.LastAppliedMessageId,
            LastSyncedAtUtc = source.LastSyncedAtUtc,
            Version = source.Version,
            CreatedAtUtc = source.CreatedAtUtc,
            CreatedByUserId = source.CreatedByUserId,
            UpdatedAtUtc = source.UpdatedAtUtc,
            UpdatedByUserId = source.UpdatedByUserId
        };
    }

    private static GetSlugRouteListItemHttpResponse Map(GetSlugRegistryListResponse source)
    {
        return new GetSlugRouteListItemHttpResponse
        {
            SlugId = source.SlugId,
            Scope = source.Scope,
            Slug = source.Slug,
            ResourceType = source.ResourceType,
            ResourcePublicId = source.ResourcePublicId,
            CanonicalUrl = source.CanonicalUrl,
            IsIndexable = source.IsIndexable,
            IsActive = source.IsActive,
            SourceAggregateVersion = source.SourceAggregateVersion,
            LastAppliedMessageId = source.LastAppliedMessageId,
            LastSyncedAtUtc = source.LastSyncedAtUtc,
            Version = source.Version,
            CreatedAtUtc = source.CreatedAtUtc,
            CreatedByUserId = source.CreatedByUserId,
            UpdatedAtUtc = source.UpdatedAtUtc,
            UpdatedByUserId = source.UpdatedByUserId
        };
    }

    private static CheckSlugAvailabilityHttpResponse Map(CheckSlugAvailabilityResponse source)
    {
        return new CheckSlugAvailabilityHttpResponse
        {
            Scope = source.Scope,
            Slug = source.Slug,
            IsAvailable = source.IsAvailable,
            BelongsToCurrentResource = source.BelongsToCurrentResource,
            ExistingResourceType = source.ExistingResourceType,
            ExistingResourcePublicId = source.ExistingResourcePublicId,
            ExistingSlugId = source.ExistingSlugId
        };
    }

    private static GenerateSlugHttpResponse Map(GenerateSlugResponse source)
    {
        return new GenerateSlugHttpResponse
        {
            Scope = source.Scope,
            Source = source.Source,
            SuggestedSlug = source.SuggestedSlug,
            IsUnique = source.IsUnique,
            ExistingResourceType = source.ExistingResourceType,
            ExistingResourcePublicId = source.ExistingResourcePublicId
        };
    }
}
