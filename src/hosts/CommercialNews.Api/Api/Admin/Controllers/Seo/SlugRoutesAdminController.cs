using CommercialNews.Api.Api.Admin.Contracts.Seo.SlugRoutes.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Seo.SlugRoutes.Responses;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.BuildingBlocks.Contracts.Common;
using CommercialNews.BuildingBlocks.Results;
using Microsoft.AspNetCore.Mvc;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;
using Seo.Application.UseCases.SlugRoutes.ActivateSlugRegistry;
using Seo.Application.UseCases.SlugRoutes.CreateSlugRegistry;
using Seo.Application.UseCases.SlugRoutes.DeactivateSlugRegistry;
using Seo.Application.UseCases.SlugRoutes.GenerateSlug;
using Seo.Application.UseCases.SlugRoutes.GetSlugRegistryByArticleId;
using Seo.Application.UseCases.SlugRoutes.GetSlugRegistryById;
using Seo.Application.UseCases.SlugRoutes.GetSlugRegistryList;
using Seo.Application.UseCases.SlugRoutes.UpdateSlugRegistry;

namespace CommercialNews.Api.Api.Admin.Controllers.Seo;

[ApiController]
[Route("api/v1/admin/seo/slug-routes")]
public sealed class SlugRoutesAdminController : ControllerBase
{
    private const string GetSlugRouteByIdRouteName = "AdminSeoSlugRoutes.GetById";

    private readonly ICreateSlugRegistryUseCase _createSlugRegistryUseCase;
    private readonly IUpdateSlugRegistryUseCase _updateSlugRegistryUseCase;
    private readonly IActivateSlugRegistryUseCase _activateSlugRegistryUseCase;
    private readonly IDeactivateSlugRegistryUseCase _deactivateSlugRegistryUseCase;
    private readonly IGetSlugRegistryByIdUseCase _getSlugRegistryByIdUseCase;
    private readonly IGetSlugRegistryByArticleIdUseCase _getSlugRegistryByArticleIdUseCase;
    private readonly IGetSlugRegistryListUseCase _getSlugRegistryListUseCase;
    private readonly IGenerateSlugUseCase _generateSlugUseCase;

    public SlugRoutesAdminController(
        ICreateSlugRegistryUseCase createSlugRegistryUseCase,
        IUpdateSlugRegistryUseCase updateSlugRegistryUseCase,
        IActivateSlugRegistryUseCase activateSlugRegistryUseCase,
        IDeactivateSlugRegistryUseCase deactivateSlugRegistryUseCase,
        IGetSlugRegistryByIdUseCase getSlugRegistryByIdUseCase,
        IGetSlugRegistryByArticleIdUseCase getSlugRegistryByArticleIdUseCase,
        IGetSlugRegistryListUseCase getSlugRegistryListUseCase,
        IGenerateSlugUseCase generateSlugUseCase)
    {
        _createSlugRegistryUseCase = createSlugRegistryUseCase;
        _updateSlugRegistryUseCase = updateSlugRegistryUseCase;
        _activateSlugRegistryUseCase = activateSlugRegistryUseCase;
        _deactivateSlugRegistryUseCase = deactivateSlugRegistryUseCase;
        _getSlugRegistryByIdUseCase = getSlugRegistryByIdUseCase;
        _getSlugRegistryByArticleIdUseCase = getSlugRegistryByArticleIdUseCase;
        _getSlugRegistryListUseCase = getSlugRegistryListUseCase;
        _generateSlugUseCase = generateSlugUseCase;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateSlugRouteHttpResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateSlugRouteHttpRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new CreateSlugRegistryRequest
        {
            ArticleId = request.ArticleId,
            Slug = request.Slug,
            Scope = request.Scope,
            CanonicalUrl = request.CanonicalUrl,
            IsIndexable = request.IsIndexable,
            IsActive = request.IsActive
        };

        Result<CreateSlugRegistryResponse> result =
            await _createSlugRegistryUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<CreateSlugRouteHttpResponse>.Failure(result.Error!));
        }

        var response = new CreateSlugRouteHttpResponse
        {
            SlugId = result.Value!.SlugId,
            ArticleId = result.Value.ArticleId,
            Slug = result.Value.Slug,
            Scope = result.Value.Scope,
            CanonicalUrl = result.Value.CanonicalUrl,
            IsIndexable = result.Value.IsIndexable,
            IsActive = result.Value.IsActive,
            Version = result.Value.Version,
            CreatedAt = result.Value.CreatedAt,
            CreatedByUserId = result.Value.CreatedByUserId,
            UpdatedAt = result.Value.UpdatedAt,
            UpdatedByUserId = result.Value.UpdatedByUserId
        };

        return CreatedAtRoute(
            GetSlugRouteByIdRouteName,
            new { slugId = response.SlugId },
            response);
    }

    [HttpGet("{slugId:long}", Name = GetSlugRouteByIdRouteName)]
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

        var response = new GetSlugRouteByIdHttpResponse
        {
            SlugId = result.Value!.SlugId,
            ArticleId = result.Value.ArticleId,
            Slug = result.Value.Slug,
            Scope = result.Value.Scope,
            CanonicalUrl = result.Value.CanonicalUrl,
            IsIndexable = result.Value.IsIndexable,
            IsActive = result.Value.IsActive,
            Version = result.Value.Version,
            CreatedAt = result.Value.CreatedAt,
            CreatedByUserId = result.Value.CreatedByUserId,
            UpdatedAt = result.Value.UpdatedAt,
            UpdatedByUserId = result.Value.UpdatedByUserId
        };

        return this.ToActionResult(Result<GetSlugRouteByIdHttpResponse>.Success(response));
    }

    [HttpGet("by-article/{articleId:long}")]
    [ProducesResponseType(typeof(GetSlugRoutesByArticleIdHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByArticleIdAsync(
        [FromRoute] long articleId,
        [FromQuery] string? scope = null,
        [FromQuery] bool? onlyActive = null,
        CancellationToken cancellationToken = default)
    {
        var useCaseRequest = new GetSlugRegistryByArticleIdRequest
        {
            ArticleId = articleId,
            Scope = scope,
            OnlyActive = onlyActive
        };

        Result<IReadOnlyList<GetSlugRegistryByArticleIdResponse>> result =
            await _getSlugRegistryByArticleIdUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GetSlugRoutesByArticleIdHttpResponse>.Failure(result.Error!));
        }

        var response = new GetSlugRoutesByArticleIdHttpResponse
        {
            Items = result.Value!.Select(static item => new GetSlugRoutesByArticleIdItemHttpResponse
            {
                SlugId = item.SlugId,
                ArticleId = item.ArticleId,
                Slug = item.Slug,
                Scope = item.Scope,
                CanonicalUrl = item.CanonicalUrl,
                IsIndexable = item.IsIndexable,
                IsActive = item.IsActive,
                Version = item.Version,
                CreatedAt = item.CreatedAt,
                CreatedByUserId = item.CreatedByUserId,
                UpdatedAt = item.UpdatedAt,
                UpdatedByUserId = item.UpdatedByUserId
            }).ToArray()
        };

        return this.ToActionResult(Result<GetSlugRoutesByArticleIdHttpResponse>.Success(response));
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetSlugRouteListHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPagedAsync(
        [FromQuery] long? articleId = null,
        [FromQuery] string? scope = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isIndexable = null,
        [FromQuery] string? keyword = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "UpdatedAt",
        [FromQuery] string sortDirection = "DESC",
        CancellationToken cancellationToken = default)
    {
        var useCaseRequest = new GetSlugRegistryListRequest
        {
            ArticleId = articleId,
            Scope = scope,
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
            Items = result.Value!.Items.Select(static item => new GetSlugRouteListItemHttpResponse
            {
                SlugId = item.SlugId,
                ArticleId = item.ArticleId,
                Slug = item.Slug,
                Scope = item.Scope,
                CanonicalUrl = item.CanonicalUrl,
                IsIndexable = item.IsIndexable,
                IsActive = item.IsActive,
                Version = item.Version,
                CreatedAt = item.CreatedAt,
                CreatedByUserId = item.CreatedByUserId,
                UpdatedAt = item.UpdatedAt,
                UpdatedByUserId = item.UpdatedByUserId
            }).ToArray(),
            Page = result.Value.Page,
            PageSize = result.Value.PageSize,
            TotalItems = result.Value.TotalItems
        };

        return this.ToActionResult(Result<GetSlugRouteListHttpResponse>.Success(response));
    }

    [HttpPut("{slugId:long}")]
    [ProducesResponseType(typeof(UpdateSlugRouteHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] long slugId,
        [FromBody] UpdateSlugRouteHttpRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new UpdateSlugRegistryRequest
        {
            SlugId = slugId,
            Slug = request.Slug,
            Scope = request.Scope,
            CanonicalUrl = request.CanonicalUrl,
            IsIndexable = request.IsIndexable,
            IsActive = request.IsActive,
            ExpectedVersion = request.ExpectedVersion
        };

        Result<UpdateSlugRegistryResponse> result =
            await _updateSlugRegistryUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<UpdateSlugRouteHttpResponse>.Failure(result.Error!));
        }

        var response = new UpdateSlugRouteHttpResponse
        {
            SlugId = result.Value!.SlugId,
            ArticleId = result.Value.ArticleId,
            Slug = result.Value.Slug,
            Scope = result.Value.Scope,
            CanonicalUrl = result.Value.CanonicalUrl,
            IsIndexable = result.Value.IsIndexable,
            IsActive = result.Value.IsActive,
            Version = result.Value.Version,
            UpdatedAt = result.Value.UpdatedAt,
            UpdatedByUserId = result.Value.UpdatedByUserId
        };

        return this.ToActionResult(Result<UpdateSlugRouteHttpResponse>.Success(response));
    }

    [HttpPost("{slugId:long}:activate")]
    [ProducesResponseType(typeof(ActivateSlugRouteHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ActivateAsync(
        [FromRoute] long slugId,
        [FromBody] int expectedVersion,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new ActivateSlugRegistryRequest
        {
            SlugId = slugId,
            ExpectedVersion = expectedVersion
        };

        Result<ActivateSlugRegistryResponse> result =
            await _activateSlugRegistryUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<ActivateSlugRouteHttpResponse>.Failure(result.Error!));
        }

        var response = new ActivateSlugRouteHttpResponse
        {
            SlugId = result.Value!.SlugId,
            ArticleId = result.Value.ArticleId,
            Slug = result.Value.Slug,
            Scope = result.Value.Scope,
            IsActive = result.Value.IsActive,
            IsIndexable = result.Value.IsIndexable,
            Version = result.Value.Version,
            UpdatedAt = result.Value.UpdatedAt,
            UpdatedByUserId = result.Value.UpdatedByUserId
        };

        return this.ToActionResult(Result<ActivateSlugRouteHttpResponse>.Success(response));
    }

    [HttpPost("{slugId:long}:deactivate")]
    [ProducesResponseType(typeof(DeactivateSlugRouteHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateAsync(
        [FromRoute] long slugId,
        [FromBody] int expectedVersion,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new DeactivateSlugRegistryRequest
        {
            SlugId = slugId,
            ExpectedVersion = expectedVersion
        };

        Result<DeactivateSlugRegistryResponse> result =
            await _deactivateSlugRegistryUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<DeactivateSlugRouteHttpResponse>.Failure(result.Error!));
        }

        var response = new DeactivateSlugRouteHttpResponse
        {
            SlugId = result.Value!.SlugId,
            ArticleId = result.Value.ArticleId,
            Slug = result.Value.Slug,
            Scope = result.Value.Scope,
            IsActive = result.Value.IsActive,
            IsIndexable = result.Value.IsIndexable,
            Version = result.Value.Version,
            UpdatedAt = result.Value.UpdatedAt,
            UpdatedByUserId = result.Value.UpdatedByUserId
        };

        return this.ToActionResult(Result<DeactivateSlugRouteHttpResponse>.Success(response));
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(GenerateSlugHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateAsync(
        [FromBody] GenerateSlugHttpRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GenerateSlugRequest
        {
            Source = request.Source,
            Scope = request.Scope
        };

        Result<GenerateSlugResponse> result =
            await _generateSlugUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GenerateSlugHttpResponse>.Failure(result.Error!));
        }

        var response = new GenerateSlugHttpResponse
        {
            Scope = result.Value!.Scope,
            Source = result.Value.Source,
            SuggestedSlug = result.Value.SuggestedSlug,
            IsUnique = result.Value.IsUnique
        };

        return this.ToActionResult(Result<GenerateSlugHttpResponse>.Success(response));
    }
}