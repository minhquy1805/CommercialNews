using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;
using Seo.Application.Errors;
using Seo.Application.Models.QueryModels;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SlugRoutes.GetSlugRegistryByArticleId;

public sealed class GetSlugRegistryByArticleIdUseCase : IGetSlugRegistryByArticleIdUseCase
{
    private readonly ISlugRegistryRepository _slugRegistryRepository;

    public GetSlugRegistryByArticleIdUseCase(
        ISlugRegistryRepository slugRegistryRepository)
    {
        _slugRegistryRepository = slugRegistryRepository;
    }

    public async Task<Result<IReadOnlyList<GetSlugRegistryByArticleIdResponse>>> ExecuteAsync(
        GetSlugRegistryByArticleIdRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.ArticleId <= 0)
            {
                return Result<IReadOnlyList<GetSlugRegistryByArticleIdResponse>>.Failure(
                    SeoErrors.Article.InvalidArticleId);
            }

            IReadOnlyList<SlugRegistryListResultItem> result =
                await _slugRegistryRepository.SelectByArticleIdAsync(
                    request.ArticleId,
                    request.Scope,
                    request.OnlyActive,
                    cancellationToken);

            IReadOnlyList<GetSlugRegistryByArticleIdResponse> response = result
                .Select(item => new GetSlugRegistryByArticleIdResponse
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
                })
                .ToList();

            return Result<IReadOnlyList<GetSlugRegistryByArticleIdResponse>>.Success(response);
        }
        catch (PersistenceException exception)
        {
            return Result<IReadOnlyList<GetSlugRegistryByArticleIdResponse>>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<IReadOnlyList<GetSlugRegistryByArticleIdResponse>>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.ARTICLE_INVALID_ARTICLE_ID" => SeoErrors.Article.InvalidArticleId,
            "SEO.SLUG_REGISTRY_INVALID_ARTICLE_ID" => SeoErrors.SlugRegistry.InvalidArticleId,
            "SEO.INVALID_SCOPE" => SeoErrors.SlugRegistry.InvalidScope,
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