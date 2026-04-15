using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;
using Seo.Application.Errors;
using Seo.Application.Models.QueryModels;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SlugRoutes.ResolveByScopeAndSlug;

public sealed class ResolveByScopeAndSlugUseCase : IResolveByScopeAndSlugUseCase
{
    private readonly ISlugRegistryRepository _slugRegistryRepository;

    public ResolveByScopeAndSlugUseCase(
        ISlugRegistryRepository slugRegistryRepository)
    {
        _slugRegistryRepository = slugRegistryRepository;
    }

    public async Task<Result<ResolveByScopeAndSlugResponse>> ExecuteAsync(
        ResolveByScopeAndSlugRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Scope))
            {
                return Result<ResolveByScopeAndSlugResponse>.Failure(
                    SeoErrors.SlugRegistry.InvalidScope);
            }

            if (string.IsNullOrWhiteSpace(request.Slug))
            {
                return Result<ResolveByScopeAndSlugResponse>.Failure(
                    SeoErrors.SlugRegistry.SlugRequired);
            }

            ResolveSeoRouteResult? result =
                await _slugRegistryRepository.ResolveByScopeAndSlugAsync(
                    request.Scope.Trim(),
                    request.Slug.Trim(),
                    cancellationToken);

            if (result is null)
            {
                return Result<ResolveByScopeAndSlugResponse>.Failure(
                    SeoErrors.SlugRegistry.NotFound);
            }

            return Result<ResolveByScopeAndSlugResponse>.Success(
                new ResolveByScopeAndSlugResponse
                {
                    Scope = result.Scope,
                    Slug = result.Slug,
                    ResourceType = result.ResourceType,
                    ResourceId = result.ResourceId,
                    CanonicalUrl = result.CanonicalUrl,
                    IsIndexable = result.IsIndexable,
                    Status = result.Status,
                    Version = result.Version
                });
        }
        catch (PersistenceException exception)
        {
            return Result<ResolveByScopeAndSlugResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<ResolveByScopeAndSlugResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.INVALID_SCOPE" => SeoErrors.SlugRegistry.InvalidScope,
            "SEO.INVALID_SLUG" => SeoErrors.SlugRegistry.SlugRequired,
            "SEO.SLUG_TOO_LONG" => SeoErrors.SlugRegistry.SlugTooLong,
            "SEO.SLUG_REGISTRY_INVALID_ARTICLE_ID" => SeoErrors.SlugRegistry.InvalidArticleId,
            "SEO.SLUG_REGISTRY_INVALID_VERSION" => SeoErrors.SlugRegistry.InvalidVersion,
            "SEO.CANONICAL_URL_TOO_LONG" => SeoErrors.SlugRegistry.CanonicalUrlTooLong,
            _ => SeoErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            _ => SeoErrors.ValidationFailed
        };
    }
}