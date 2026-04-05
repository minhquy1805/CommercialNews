using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;
using Seo.Application.Errors;
using Seo.Application.Models.QueryModels;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SeoSettings.GetArticleSeoSettings;

public sealed class GetArticleSeoSettingsUseCase : IGetArticleSeoSettingsUseCase
{
    private readonly ISeoMetadataRepository _seoMetadataRepository;

    public GetArticleSeoSettingsUseCase(
        ISeoMetadataRepository seoMetadataRepository)
    {
        _seoMetadataRepository = seoMetadataRepository;
    }

    public async Task<Result<GetArticleSeoSettingsResponse>> ExecuteAsync(
        GetArticleSeoSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.ArticleId <= 0)
            {
                return Result<GetArticleSeoSettingsResponse>.Failure(
                    SeoErrors.Article.InvalidArticleId);
            }

            ArticleSeoSettingsResult? result =
                await _seoMetadataRepository.GetArticleSeoSettingsByArticleIdAsync(
                    request.ArticleId,
                    cancellationToken);

            if (result is null)
            {
                return Result<GetArticleSeoSettingsResponse>.Failure(
                    SeoErrors.Article.NotFound);
            }

            return Result<GetArticleSeoSettingsResponse>.Success(
                new GetArticleSeoSettingsResponse
                {
                    ArticleId = result.ArticleId,
                    Scope = result.Scope,
                    Slug = result.Slug,
                    CanonicalUrl = result.CanonicalUrl,
                    MetaTitle = result.MetaTitle,
                    MetaDescription = result.MetaDescription,
                    OgTitle = result.OgTitle,
                    OgDescription = result.OgDescription,
                    OgImageUrl = result.OgImageUrl,
                    TwitterTitle = result.TwitterTitle,
                    TwitterDescription = result.TwitterDescription,
                    TwitterImageUrl = result.TwitterImageUrl,
                    IsIndexable = result.IsIndexable,
                    IsActive = result.IsActive,
                    Version = result.Version
                });
        }
        catch (PersistenceException exception)
        {
            return Result<GetArticleSeoSettingsResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<GetArticleSeoSettingsResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.ARTICLE_INVALID_ARTICLE_ID" => SeoErrors.Article.InvalidArticleId,
            "SEO.SEO_METADATA_INVALID_ARTICLE_ID" => SeoErrors.SeoMetadata.InvalidArticleId,
            "SEO.SLUG_REGISTRY_INVALID_ARTICLE_ID" => SeoErrors.SlugRegistry.InvalidArticleId,
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