using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;
using Seo.Application.Errors;
using Seo.Application.Models.QueryModels;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SeoSettings.GetSeoMetadataByArticleId;

public sealed class GetSeoMetadataByArticleIdUseCase : IGetSeoMetadataByArticleIdUseCase
{
    private readonly ISeoMetadataRepository _seoMetadataRepository;

    public GetSeoMetadataByArticleIdUseCase(
        ISeoMetadataRepository seoMetadataRepository)
    {
        _seoMetadataRepository = seoMetadataRepository;
    }

    public async Task<Result<GetSeoMetadataByArticleIdResponse>> ExecuteAsync(
        GetSeoMetadataByArticleIdRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.ArticleId <= 0)
            {
                return Result<GetSeoMetadataByArticleIdResponse>.Failure(
                    SeoErrors.Article.InvalidArticleId);
            }

            SeoMetadataResult? result =
                await _seoMetadataRepository.SelectMetadataByArticleIdAsync(
                    request.ArticleId,
                    cancellationToken);

            if (result is null)
            {
                return Result<GetSeoMetadataByArticleIdResponse>.Failure(
                    SeoErrors.SeoMetadata.NotFound);
            }

            return Result<GetSeoMetadataByArticleIdResponse>.Success(
                new GetSeoMetadataByArticleIdResponse
                {
                    ResourceType = result.ResourceType,
                    ResourceId = result.ResourceId,
                    Slug = result.Slug,
                    CanonicalUrl = result.CanonicalUrl,
                    MetaTitle = result.MetaTitle,
                    MetaDescription = result.MetaDescription,
                    OgTitle = result.OgTitle,
                    OgDescription = result.OgDescription,
                    OgImageUrl = result.OgImageUrl,
                    Version = result.Version
                });
        }
        catch (PersistenceException exception)
        {
            return Result<GetSeoMetadataByArticleIdResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<GetSeoMetadataByArticleIdResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.ARTICLE_INVALID_ARTICLE_ID" => SeoErrors.Article.InvalidArticleId,
            "SEO.SEO_METADATA_INVALID_ARTICLE_ID" => SeoErrors.SeoMetadata.InvalidArticleId,
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