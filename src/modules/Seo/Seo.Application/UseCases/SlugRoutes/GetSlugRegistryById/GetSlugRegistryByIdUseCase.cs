using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;
using Seo.Application.Errors;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Entities;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SlugRoutes.GetSlugRegistryById;

public sealed class GetSlugRegistryByIdUseCase : IGetSlugRegistryByIdUseCase
{
    private readonly ISlugRegistryRepository _slugRegistryRepository;

    public GetSlugRegistryByIdUseCase(
        ISlugRegistryRepository slugRegistryRepository)
    {
        _slugRegistryRepository = slugRegistryRepository;
    }

    public async Task<Result<GetSlugRegistryByIdResponse>> ExecuteAsync(
        GetSlugRegistryByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.SlugId <= 0)
            {
                return Result<GetSlugRegistryByIdResponse>.Failure(
                    SeoErrors.SlugRegistry.InvalidSlugId);
            }

            SlugRegistry? existing = await _slugRegistryRepository.GetByIdAsync(
                request.SlugId,
                cancellationToken);

            if (existing is null)
            {
                return Result<GetSlugRegistryByIdResponse>.Failure(
                    SeoErrors.SlugRegistry.NotFound);
            }

            return Result<GetSlugRegistryByIdResponse>.Success(
                new GetSlugRegistryByIdResponse
                {
                    SlugId = existing.SlugId,
                    ArticleId = existing.ArticleId,
                    Slug = existing.Slug,
                    Scope = existing.Scope,
                    CanonicalUrl = existing.CanonicalUrl,
                    IsIndexable = existing.IsIndexable,
                    IsActive = existing.IsActive,
                    Version = existing.Version,
                    CreatedAt = existing.CreatedAt,
                    CreatedByUserId = existing.CreatedByUserId,
                    UpdatedAt = existing.UpdatedAt,
                    UpdatedByUserId = existing.UpdatedByUserId
                });
        }
        catch (PersistenceException exception)
        {
            return Result<GetSlugRegistryByIdResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<GetSlugRegistryByIdResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.SLUG_REGISTRY_INVALID_SLUG_ID" => SeoErrors.SlugRegistry.InvalidSlugId,
            "SEO.SLUG_REGISTRY_INVALID_ARTICLE_ID" => SeoErrors.SlugRegistry.InvalidArticleId,
            "SEO.INVALID_SCOPE" => SeoErrors.SlugRegistry.InvalidScope,
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