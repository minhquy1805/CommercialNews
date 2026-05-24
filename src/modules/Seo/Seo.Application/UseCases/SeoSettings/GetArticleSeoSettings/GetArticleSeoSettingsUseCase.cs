using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SeoMetadata.Responses;
using Seo.Application.Errors;
using Seo.Application.Models.Results;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Constants;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SeoSettings.GetArticleSeoSettings;

public sealed class GetArticleSeoSettingsUseCase : IGetArticleSeoSettingsUseCase
{
    private readonly ISeoMetadataRepository _seoMetadataRepository;

    public GetArticleSeoSettingsUseCase(
        ISeoMetadataRepository seoMetadataRepository)
    {
        _seoMetadataRepository = seoMetadataRepository
            ?? throw new ArgumentNullException(nameof(seoMetadataRepository));
    }

    public async Task<Result<GetArticleSeoSettingsResponse>> ExecuteAsync(
        string articlePublicId,
        string? scope = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(articlePublicId) || articlePublicId.Trim().Length != 26)
            {
                return Result<GetArticleSeoSettingsResponse>.Failure(
                    SeoErrors.Article.InvalidArticlePublicId);
            }

            string normalizedScope = string.IsNullOrWhiteSpace(scope)
                ? SeoScopes.Public
                : scope.Trim();

            if (!SeoScopes.IsValid(normalizedScope))
            {
                return Result<GetArticleSeoSettingsResponse>.Failure(
                    SeoErrors.SlugRegistry.InvalidScope);
            }

            ArticleSeoSettingsResult? result =
                await _seoMetadataRepository.GetArticleSeoSettingsByArticlePublicIdAsync(
                    articlePublicId: articlePublicId.Trim(),
                    scope: normalizedScope,
                    cancellationToken: cancellationToken);

            if (result is null)
            {
                return Result<GetArticleSeoSettingsResponse>.Failure(
                    SeoErrors.Article.NotFound);
            }

            return Result<GetArticleSeoSettingsResponse>.Success(
                new GetArticleSeoSettingsResponse
                {
                    Scope = result.Scope,
                    ResourceType = result.ResourceType,
                    ResourcePublicId = result.ResourcePublicId,
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
                    Robots = result.Robots,
                    IsManualOverride = result.IsManualOverride,
                    IsIndexable = result.IsIndexable,
                    IsActive = result.IsActive,
                    SourceAggregateVersion = result.SourceAggregateVersion,
                    LastAppliedMessageId = result.LastAppliedMessageId,
                    LastSyncedAtUtc = result.LastSyncedAtUtc,
                    SlugRouteVersion = result.SlugRouteVersion,
                    SeoMetadataVersion = result.SeoMetadataVersion,
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
            "SEO.INVALID_SCOPE" => SeoErrors.SlugRegistry.InvalidScope,
            "SEO.INVALID_RESOURCE_TYPE" => SeoErrors.Resource.InvalidResourceType,
            "SEO.INVALID_RESOURCE_PUBLIC_ID" => SeoErrors.Resource.InvalidResourcePublicId,
            _ => SeoErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "SEO.INVALID_SCOPE" => SeoErrors.SlugRegistry.InvalidScope,
            "SEO.INVALID_RESOURCE_TYPE" => SeoErrors.Resource.InvalidResourceType,
            "SEO.INVALID_RESOURCE_PUBLIC_ID" => SeoErrors.Resource.InvalidResourcePublicId,
            "SEO.STORE_UNAVAILABLE" => SeoErrors.Infrastructure.StoreUnavailable,
            _ => SeoErrors.ValidationFailed
        };
    }
}
