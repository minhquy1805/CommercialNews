using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;
using Seo.Application.Errors;
using Seo.Application.Models.QueryModels;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SlugRoutes.GetSlugRegistryList;

public sealed class GetSlugRegistryListUseCase : IGetSlugRegistryListUseCase
{
    private readonly ISlugRegistryRepository _slugRegistryRepository;

    public GetSlugRegistryListUseCase(
        ISlugRegistryRepository slugRegistryRepository)
    {
        _slugRegistryRepository = slugRegistryRepository;
    }

    public async Task<Result<PagedQueryResult<GetSlugRegistryListResponse>>> ExecuteAsync(
        GetSlugRegistryListRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            SlugRegistryListQuery query = new()
            {
                ArticleId = request.ArticleId,
                Scope = request.Scope,
                IsActive = request.IsActive,
                IsIndexable = request.IsIndexable,
                Keyword = request.Keyword,
                Page = request.Page,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                SortDirection = request.SortDirection
            };

            PagedQueryResult<SlugRegistryListResultItem> result =
                await _slugRegistryRepository.SelectSkipAndTakeAsync(
                    query,
                    cancellationToken);

            PagedQueryResult<GetSlugRegistryListResponse> response = new()
            {
                Page = result.Page,
                PageSize = result.PageSize,
                TotalItems = result.TotalItems,
                Items = result.Items
                    .Select(item => new GetSlugRegistryListResponse
                    {
                        SlugId = item.SlugId,
                        ArticleId = item.ArticleId,
                        Slug = item.Slug,
                        Scope = item.Scope,
                        CanonicalUrl = item.CanonicalUrl,
                        IsIndexable = item.IsIndexable,
                        IsActive = item.IsActive,
                        CreatedAt = item.CreatedAt,
                        CreatedByUserId = item.CreatedByUserId,
                        UpdatedAt = item.UpdatedAt,
                        UpdatedByUserId = item.UpdatedByUserId,
                        Version = item.Version
                    })
                    .ToArray()
            };

            return Result<PagedQueryResult<GetSlugRegistryListResponse>>.Success(response);
        }
        catch (PersistenceException exception)
        {
            return Result<PagedQueryResult<GetSlugRegistryListResponse>>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<PagedQueryResult<GetSlugRegistryListResponse>>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.SLUG_REGISTRY_INVALID_ARTICLE_ID" => SeoErrors.SlugRegistry.InvalidArticleId,
            "SEO.INVALID_SCOPE" => SeoErrors.SlugRegistry.InvalidScope,
            "SEO.SLUG_REGISTRY_INVALID_VERSION" => SeoErrors.SlugRegistry.InvalidVersion,
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