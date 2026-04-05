using CommercialNews.BuildingBlocks.Abstractions.Execution;
using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;
using Seo.Application.Errors;
using Seo.Application.Models.QueryModels;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Entities;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SeoSettings.UpsertArticleSeoSettings;

public sealed class UpsertArticleSeoSettingsUseCase : IUpsertArticleSeoSettingsUseCase
{
    private readonly ISeoMetadataRepository _seoMetadataRepository;
    private readonly ISlugRegistryRepository _slugRegistryRepository;
    private readonly ISeoUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;

    public UpsertArticleSeoSettingsUseCase(
        ISeoMetadataRepository seoMetadataRepository,
        ISlugRegistryRepository slugRegistryRepository,
        ISeoUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IRequestContext requestContext)
    {
        _seoMetadataRepository = seoMetadataRepository;
        _slugRegistryRepository = slugRegistryRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _requestContext = requestContext;
    }

    public async Task<Result<UpsertArticleSeoSettingsResponse>> ExecuteAsync(
        UpsertArticleSeoSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.ArticleId <= 0)
            {
                return Result<UpsertArticleSeoSettingsResponse>.Failure(
                    SeoErrors.Article.InvalidArticleId);
            }

            if (string.IsNullOrWhiteSpace(request.Scope))
            {
                return Result<UpsertArticleSeoSettingsResponse>.Failure(
                    SeoErrors.SlugRegistry.InvalidScope);
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;
            long? actorUserId = request.UpdatedByUserId ?? _requestContext.CurrentUserId;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                SlugRegistry? slugEntity = null;
                SeoMetadata? seoMetadataEntity = await _seoMetadataRepository.GetByArticleIdAsync(
                    request.ArticleId,
                    cancellationToken);

                if (!string.IsNullOrWhiteSpace(request.Slug))
                {
                    IReadOnlyList<SlugRegistryListResultItem> existingRoutes =
                        await _slugRegistryRepository.SelectByArticleIdAsync(
                            request.ArticleId,
                            request.Scope.Trim(),
                            onlyActive: true,
                            cancellationToken);

                    SlugRegistryListResultItem? activeRoute = existingRoutes.FirstOrDefault();

                    if (activeRoute is null)
                    {
                        slugEntity = SlugRegistry.Create(
                            articleId: request.ArticleId,
                            slug: request.Slug,
                            scope: request.Scope,
                            canonicalUrl: request.CanonicalUrl,
                            isIndexable: request.IsIndexable,
                            isActive: request.IsActive,
                            nowUtc: nowUtc,
                            actorUserId: actorUserId);

                        long slugId = await _slugRegistryRepository.InsertAsync(
                            slugEntity,
                            cancellationToken);

                        slugEntity = SlugRegistry.Rehydrate(
                            slugId: slugId,
                            articleId: slugEntity.ArticleId,
                            slug: slugEntity.Slug,
                            scope: slugEntity.Scope,
                            canonicalUrl: slugEntity.CanonicalUrl,
                            isIndexable: slugEntity.IsIndexable,
                            isActive: slugEntity.IsActive,
                            version: slugEntity.Version,
                            createdAt: slugEntity.CreatedAt,
                            createdByUserId: slugEntity.CreatedByUserId,
                            updatedAt: slugEntity.UpdatedAt,
                            updatedByUserId: slugEntity.UpdatedByUserId);
                    }
                    else
                    {
                        if (!request.ExpectedSlugVersion.HasValue)
                        {
                            await _unitOfWork.RollbackAsync(cancellationToken);

                            return Result<UpsertArticleSeoSettingsResponse>.Failure(
                                SeoErrors.SlugRegistry.VersionMismatch);
                        }

                        SlugRegistry? persistedRoute = await _slugRegistryRepository.GetByIdAsync(
                            activeRoute.SlugId,
                            cancellationToken);

                        if (persistedRoute is null)
                        {
                            await _unitOfWork.RollbackAsync(cancellationToken);

                            return Result<UpsertArticleSeoSettingsResponse>.Failure(
                                SeoErrors.SlugRegistry.NotFound);
                        }

                        persistedRoute.UpdateRouting(
                            slug: request.Slug,
                            scope: request.Scope,
                            canonicalUrl: request.CanonicalUrl,
                            isIndexable: request.IsIndexable,
                            isActive: request.IsActive,
                            nowUtc: nowUtc,
                            actorUserId: actorUserId);

                        int affectedRows = await _slugRegistryRepository.UpdateAsync(
                            persistedRoute,
                            request.ExpectedSlugVersion.Value,
                            cancellationToken);

                        if (affectedRows <= 0)
                        {
                            await _unitOfWork.RollbackAsync(cancellationToken);

                            return Result<UpsertArticleSeoSettingsResponse>.Failure(
                                SeoErrors.SlugRegistry.VersionMismatch);
                        }

                        slugEntity = persistedRoute;
                    }
                }

                bool hasMetadataPayload =
                    !string.IsNullOrWhiteSpace(request.CanonicalUrl) ||
                    !string.IsNullOrWhiteSpace(request.MetaTitle) ||
                    !string.IsNullOrWhiteSpace(request.MetaDescription) ||
                    !string.IsNullOrWhiteSpace(request.OgTitle) ||
                    !string.IsNullOrWhiteSpace(request.OgDescription) ||
                    !string.IsNullOrWhiteSpace(request.OgImageUrl) ||
                    !string.IsNullOrWhiteSpace(request.TwitterTitle) ||
                    !string.IsNullOrWhiteSpace(request.TwitterDescription) ||
                    !string.IsNullOrWhiteSpace(request.TwitterImageUrl);

                if (seoMetadataEntity is null)
                {
                    if (hasMetadataPayload)
                    {
                        seoMetadataEntity = SeoMetadata.Create(
                            articleId: request.ArticleId,
                            canonicalUrl: request.CanonicalUrl,
                            metaTitle: request.MetaTitle,
                            metaDescription: request.MetaDescription,
                            ogTitle: request.OgTitle,
                            ogDescription: request.OgDescription,
                            ogImageUrl: request.OgImageUrl,
                            twitterTitle: request.TwitterTitle,
                            twitterDescription: request.TwitterDescription,
                            twitterImageUrl: request.TwitterImageUrl,
                            nowUtc: nowUtc,
                            actorUserId: actorUserId);

                        long seoId = await _seoMetadataRepository.InsertAsync(
                            seoMetadataEntity,
                            cancellationToken);

                        seoMetadataEntity = SeoMetadata.Rehydrate(
                            seoId: seoId,
                            articleId: seoMetadataEntity.ArticleId,
                            canonicalUrl: seoMetadataEntity.CanonicalUrl,
                            metaTitle: seoMetadataEntity.MetaTitle,
                            metaDescription: seoMetadataEntity.MetaDescription,
                            ogTitle: seoMetadataEntity.OgTitle,
                            ogDescription: seoMetadataEntity.OgDescription,
                            ogImageUrl: seoMetadataEntity.OgImageUrl,
                            twitterTitle: seoMetadataEntity.TwitterTitle,
                            twitterDescription: seoMetadataEntity.TwitterDescription,
                            twitterImageUrl: seoMetadataEntity.TwitterImageUrl,
                            version: seoMetadataEntity.Version,
                            updatedAt: seoMetadataEntity.UpdatedAt,
                            updatedByUserId: seoMetadataEntity.UpdatedByUserId);
                    }
                }
                else
                {
                    if (!request.ExpectedSeoMetadataVersion.HasValue)
                    {
                        await _unitOfWork.RollbackAsync(cancellationToken);

                        return Result<UpsertArticleSeoSettingsResponse>.Failure(
                            SeoErrors.SeoMetadata.VersionMismatch);
                    }

                    seoMetadataEntity.Update(
                        canonicalUrl: request.CanonicalUrl,
                        metaTitle: request.MetaTitle,
                        metaDescription: request.MetaDescription,
                        ogTitle: request.OgTitle,
                        ogDescription: request.OgDescription,
                        ogImageUrl: request.OgImageUrl,
                        twitterTitle: request.TwitterTitle,
                        twitterDescription: request.TwitterDescription,
                        twitterImageUrl: request.TwitterImageUrl,
                        nowUtc: nowUtc,
                        actorUserId: actorUserId);

                    int affectedRows = await _seoMetadataRepository.UpdateAsync(
                        seoMetadataEntity,
                        request.ExpectedSeoMetadataVersion.Value,
                        cancellationToken);

                    if (affectedRows <= 0)
                    {
                        await _unitOfWork.RollbackAsync(cancellationToken);

                        return Result<UpsertArticleSeoSettingsResponse>.Failure(
                            SeoErrors.SeoMetadata.VersionMismatch);
                    }
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                ArticleSeoSettingsResult? aggregate =
                    await _seoMetadataRepository.GetArticleSeoSettingsByArticleIdAsync(
                        request.ArticleId,
                        cancellationToken);

                if (aggregate is null)
                {
                    return Result<UpsertArticleSeoSettingsResponse>.Failure(
                        SeoErrors.Article.NotFound);
                }

                return Result<UpsertArticleSeoSettingsResponse>.Success(
                    new UpsertArticleSeoSettingsResponse
                    {
                        Updated = true,
                        ArticleId = aggregate.ArticleId,
                        Scope = aggregate.Scope,
                        Slug = aggregate.Slug,
                        CanonicalUrl = aggregate.CanonicalUrl,
                        MetaTitle = aggregate.MetaTitle,
                        MetaDescription = aggregate.MetaDescription,
                        OgTitle = aggregate.OgTitle,
                        OgDescription = aggregate.OgDescription,
                        OgImageUrl = aggregate.OgImageUrl,
                        TwitterTitle = aggregate.TwitterTitle,
                        TwitterDescription = aggregate.TwitterDescription,
                        TwitterImageUrl = aggregate.TwitterImageUrl,
                        IsIndexable = aggregate.IsIndexable,
                        IsActive = aggregate.IsActive,
                        Version = aggregate.Version
                    });
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException exception)
        {
            return Result<UpsertArticleSeoSettingsResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<UpsertArticleSeoSettingsResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.ARTICLE_INVALID_ARTICLE_ID" => SeoErrors.Article.InvalidArticleId,

            "SEO.INVALID_SCOPE" => SeoErrors.SlugRegistry.InvalidScope,
            "SEO.INVALID_SLUG" => SeoErrors.SlugRegistry.SlugRequired,
            "SEO.SLUG_TOO_LONG" => SeoErrors.SlugRegistry.SlugTooLong,
            "SEO.SLUG_REGISTRY_INVALID_SLUG_ID" => SeoErrors.SlugRegistry.InvalidSlugId,
            "SEO.SLUG_REGISTRY_INVALID_ARTICLE_ID" => SeoErrors.SlugRegistry.InvalidArticleId,
            "SEO.SLUG_REGISTRY_INVALID_VERSION" => SeoErrors.SlugRegistry.InvalidVersion,
            "SEO.SLUG_REGISTRY_INVALID_UPDATED_AT" => SeoErrors.SlugRegistry.InvalidUpdatedAt,
            "SEO.SLUG_REGISTRY_ALREADY_ACTIVE" => SeoErrors.SlugRegistry.AlreadyActive,
            "SEO.SLUG_REGISTRY_ALREADY_INACTIVE" => SeoErrors.SlugRegistry.AlreadyInactive,
            "SEO.SLUG_REGISTRY_INACTIVE" => SeoErrors.SlugRegistry.Inactive,
            "SEO.CANONICAL_URL_TOO_LONG" => SeoErrors.SlugRegistry.CanonicalUrlTooLong,

            "SEO.SEO_METADATA_INVALID_SEO_ID" => SeoErrors.SeoMetadata.InvalidSeoId,
            "SEO.SEO_METADATA_INVALID_ARTICLE_ID" => SeoErrors.SeoMetadata.InvalidArticleId,
            "SEO.SEO_METADATA_INVALID_VERSION" => SeoErrors.SeoMetadata.InvalidVersion,
            "SEO.META_TITLE_TOO_LONG" => SeoErrors.SeoMetadata.MetaTitleTooLong,
            "SEO.META_DESCRIPTION_TOO_LONG" => SeoErrors.SeoMetadata.MetaDescriptionTooLong,
            "SEO.OG_TITLE_TOO_LONG" => SeoErrors.SeoMetadata.OgTitleTooLong,
            "SEO.OG_DESCRIPTION_TOO_LONG" => SeoErrors.SeoMetadata.OgDescriptionTooLong,
            "SEO.OG_IMAGE_URL_TOO_LONG" => SeoErrors.SeoMetadata.OgImageUrlTooLong,
            "SEO.TWITTER_TITLE_TOO_LONG" => SeoErrors.SeoMetadata.TwitterTitleTooLong,
            "SEO.TWITTER_DESCRIPTION_TOO_LONG" => SeoErrors.SeoMetadata.TwitterDescriptionTooLong,
            "SEO.TWITTER_IMAGE_URL_TOO_LONG" => SeoErrors.SeoMetadata.TwitterImageUrlTooLong,

            _ => SeoErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "SEO.SLUG_CONFLICT" => SeoErrors.SlugRegistry.Conflict,
            "SEO.METADATA_ALREADY_EXISTS" => SeoErrors.SeoMetadata.AlreadyExists,
            "SEO.VERSION_MISMATCH" => SeoErrors.SeoMetadata.VersionMismatch,
            "SEO.STALE_WRITE_CONFLICT" => SeoErrors.SeoMetadata.StaleWriteConflict,
            _ => SeoErrors.ValidationFailed
        };
    }
}