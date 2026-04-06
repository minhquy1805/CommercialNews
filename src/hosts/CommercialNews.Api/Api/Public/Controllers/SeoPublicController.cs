using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Api.Public.Contracts.Seo.Responses;
using CommercialNews.BuildingBlocks.Contracts.Common;
using CommercialNews.BuildingBlocks.Results;
using Microsoft.AspNetCore.Mvc;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;
using Seo.Application.UseCases.SeoSettings.GetSeoMetadataByArticleId;
using Seo.Application.UseCases.SlugRoutes.ResolveByScopeAndSlug;

namespace CommercialNews.Api.Api.Public.Controllers.Seo;

[ApiController]
[Route("api/v1/seo")]
public sealed class SeoPublicController : ControllerBase
{
    private readonly IResolveByScopeAndSlugUseCase _resolveByScopeAndSlugUseCase;
    private readonly IGetSeoMetadataByArticleIdUseCase _getSeoMetadataByArticleIdUseCase;

    public SeoPublicController(
        IResolveByScopeAndSlugUseCase resolveByScopeAndSlugUseCase,
        IGetSeoMetadataByArticleIdUseCase getSeoMetadataByArticleIdUseCase)
    {
        _resolveByScopeAndSlugUseCase = resolveByScopeAndSlugUseCase;
        _getSeoMetadataByArticleIdUseCase = getSeoMetadataByArticleIdUseCase;
    }

    [HttpGet("resolve")]
    [ProducesResponseType(typeof(ResolveSeoHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveAsync(
        [FromQuery] string scope,
        [FromQuery] string slug,
        CancellationToken cancellationToken)
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

        var response = new ResolveSeoHttpResponse
        {
            Scope = result.Value!.Scope,
            Slug = result.Value.Slug,
            ResourceType = result.Value.ResourceType,
            ResourceId = result.Value.ResourceId,
            CanonicalUrl = result.Value.CanonicalUrl,
            IsIndexable = result.Value.IsIndexable,
            Status = result.Value.Status,
            Version = result.Value.Version
        };

        return this.ToActionResult(Result<ResolveSeoHttpResponse>.Success(response));
    }

    [HttpGet("metadata")]
    [ProducesResponseType(typeof(GetSeoMetadataHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMetadataAsync(
        [FromQuery] long articleId,
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
            return this.ToActionResult(Result<GetSeoMetadataHttpResponse>.Failure(result.Error!));
        }

        var response = new GetSeoMetadataHttpResponse
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

        return this.ToActionResult(Result<GetSeoMetadataHttpResponse>.Success(response));
    }
}