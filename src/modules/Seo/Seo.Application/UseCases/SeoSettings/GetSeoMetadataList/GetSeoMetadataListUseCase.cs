using CommercialNews.BuildingBlocks.Contracts.Common;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;
using Seo.Application.Errors;
using Seo.Application.Models.QueryModels;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SeoSettings.GetSeoMetadataList;

public sealed class GetSeoMetadataListUseCase : IGetSeoMetadataListUseCase
{
    private readonly ISeoMetadataRepository _seoMetadataRepository;

    public GetSeoMetadataListUseCase(
        ISeoMetadataRepository seoMetadataRepository)
    {
        _seoMetadataRepository = seoMetadataRepository;
    }

    public async Task<Result<PagedQueryResult<GetSeoMetadataListResponse>>> ExecuteAsync(
        GetSeoMetadataListRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            SeoMetadataListQuery query = new()
            {
                ArticleId = request.ArticleId,
                UpdatedByUserId = request.UpdatedByUserId,
                Keyword = request.Keyword,
                Page = request.Page,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                SortDirection = request.SortDirection
            };

            PagedQueryResult<SeoMetadataListResultItem> result =
                await _seoMetadataRepository.SelectSkipAndTakeAsync(
                    query,
                    cancellationToken);

            PagedQueryResult<GetSeoMetadataListResponse> response = new()
            {
                Page = result.Page,
                PageSize = result.PageSize,
                TotalItems = result.TotalItems,
                Items = result.Items
                    .Select(item => new GetSeoMetadataListResponse
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
                    })
                    .ToArray()
            };

            return Result<PagedQueryResult<GetSeoMetadataListResponse>>.Success(response);
        }
        catch (PersistenceException exception)
        {
            return Result<PagedQueryResult<GetSeoMetadataListResponse>>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<PagedQueryResult<GetSeoMetadataListResponse>>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.SEO_METADATA_INVALID_ARTICLE_ID" => SeoErrors.SeoMetadata.InvalidArticleId,
            "SEO.SEO_METADATA_INVALID_VERSION" => SeoErrors.SeoMetadata.InvalidVersion,
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