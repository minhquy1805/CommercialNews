using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Api.Public.Contracts.Seo.Responses;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;
using Seo.Application.UseCases.SeoSettings.GetSeoMetadataByResource;
using Seo.Application.UseCases.SlugRoutes.ResolveByScopeAndSlug;

namespace CommercialNews.Api.Api.Public.Controllers.Seo;

[ApiController]
[Route("api/v1/seo")]
public sealed class SeoPublicController : ControllerBase
{
    private readonly IResolveByScopeAndSlugUseCase _resolveByScopeAndSlugUseCase;
    private readonly IGetSeoMetadataByResourceUseCase _getSeoMetadataByResourceUseCase;

    public SeoPublicController(
        IResolveByScopeAndSlugUseCase resolveByScopeAndSlugUseCase,
        IGetSeoMetadataByResourceUseCase getSeoMetadataByResourceUseCase)
    {
        _resolveByScopeAndSlugUseCase = resolveByScopeAndSlugUseCase;
        _getSeoMetadataByResourceUseCase = getSeoMetadataByResourceUseCase;
    }

    [HttpGet("resolve")]
    [ProducesResponseType(typeof(ResolveSeoHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveAsync(
        [FromQuery] string slug,
        [FromQuery] string? scope = null,
        CancellationToken cancellationToken = default)
    {
        var useCaseRequest = new ResolveByScopeAndSlugRequest
        {
            Scope = scope,
            Slug = slug
        };

        Result<ResolveByScopeAndSlugResponse> result =
            await _resolveByScopeAndSlugUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<ResolveSeoHttpResponse>.Failure(result.Error!));
        }

        return this.ToActionResult(
            Result<ResolveSeoHttpResponse>.Success(Map(result.Value!)));
    }

    [HttpGet("metadata")]
    [ProducesResponseType(typeof(GetSeoMetadataHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMetadataAsync(
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
            return this.ToActionResult(Result<GetSeoMetadataHttpResponse>.Failure(result.Error!));
        }

        return this.ToActionResult(
            Result<GetSeoMetadataHttpResponse>.Success(Map(result.Value!)));
    }

    private static ResolveSeoHttpResponse Map(ResolveByScopeAndSlugResponse source)
    {
        return new ResolveSeoHttpResponse
        {
            Scope = source.Scope,
            Slug = source.Slug,
            ResourceType = source.ResourceType,
            ResourcePublicId = source.ResourcePublicId,
            CanonicalUrl = source.CanonicalUrl,
            IsIndexable = source.IsIndexable,
            Status = source.Status,
            Version = source.Version
        };
    }

    private static GetSeoMetadataHttpResponse Map(GetSeoMetadataByResourceResponse source)
    {
        return new GetSeoMetadataHttpResponse
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
            Version = source.Version
        };
    }
}
