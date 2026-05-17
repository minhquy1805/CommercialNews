using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;
using Seo.Application.Errors;
using Seo.Application.Models.Commands;
using Seo.Application.Models.Results;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Constants;
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
        _seoMetadataRepository = seoMetadataRepository ?? throw new ArgumentNullException(nameof(seoMetadataRepository));
        _slugRegistryRepository = slugRegistryRepository ?? throw new ArgumentNullException(nameof(slugRegistryRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
    }

    public async Task<Result<UpsertArticleSeoSettingsResponse>> ExecuteAsync(
        string articlePublicId,
        UpsertArticleSeoSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (string.IsNullOrWhiteSpace(articlePublicId) || articlePublicId.Trim().Length != 26)
            {
                return Result<UpsertArticleSeoSettingsResponse>.Failure(
                    SeoErrors.Article.InvalidArticlePublicId);
            }

            string scope = string.IsNullOrWhiteSpace(request.Scope)
                ? SeoScopes.Public
                : request.Scope.Trim();

            if (!SeoScopes.IsValid(scope))
            {
                return Result<UpsertArticleSeoSettingsResponse>.Failure(
                    SeoErrors.SlugRegistry.InvalidScope);
            }

            string resourceType = SeoResourceTypes.Article;
            string resourcePublicId = articlePublicId.Trim();

            long? actorUserId = request.ActorUserId ?? _requestContext.CurrentUserId;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                if (!string.IsNullOrWhiteSpace(request.Slug))
                {
                    SlugRegistryUpsertCommand slugCommand = new(
                        Scope: scope,
                        Slug: request.Slug.Trim(),
                        ResourceType: resourceType,
                        ResourcePublicId: resourcePublicId,
                        CanonicalUrl: request.CanonicalUrl,
                        IsIndexable: request.IsIndexable,
                        IsActive: request.IsActive,
                        ActorUserId: actorUserId,
                        ExpectedVersion: request.ExpectedSlugVersion);

                    await _slugRegistryRepository.UpsertAsync(
                        slugCommand,
                        cancellationToken);
                }

                bool hasMetadataPayload =
                    !string.IsNullOrWhiteSpace(request.Slug) ||
                    !string.IsNullOrWhiteSpace(request.CanonicalUrl) ||
                    !string.IsNullOrWhiteSpace(request.MetaTitle) ||
                    !string.IsNullOrWhiteSpace(request.MetaDescription) ||
                    !string.IsNullOrWhiteSpace(request.OgTitle) ||
                    !string.IsNullOrWhiteSpace(request.OgDescription) ||
                    !string.IsNullOrWhiteSpace(request.OgImageUrl) ||
                    !string.IsNullOrWhiteSpace(request.TwitterTitle) ||
                    !string.IsNullOrWhiteSpace(request.TwitterDescription) ||
                    !string.IsNullOrWhiteSpace(request.TwitterImageUrl) ||
                    !string.IsNullOrWhiteSpace(request.Robots);

                if (hasMetadataPayload)
                {
                    SeoMetadataUpsertCommand metadataCommand = new(
                        Scope: scope,
                        ResourceType: resourceType,
                        ResourcePublicId: resourcePublicId,
                        Slug: request.Slug,
                        CanonicalUrl: request.CanonicalUrl,
                        MetaTitle: request.MetaTitle,
                        MetaDescription: request.MetaDescription,
                        OgTitle: request.OgTitle,
                        OgDescription: request.OgDescription,
                        OgImageUrl: request.OgImageUrl,
                        TwitterTitle: request.TwitterTitle,
                        TwitterDescription: request.TwitterDescription,
                        TwitterImageUrl: request.TwitterImageUrl,
                        Robots: request.Robots,
                        IsManualOverride: true,
                        UpdatedByUserId: actorUserId,
                        ExpectedVersion: request.ExpectedSeoMetadataVersion);

                    await _seoMetadataRepository.UpsertAsync(
                        metadataCommand,
                        cancellationToken);
                }

                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }

            ArticleSeoSettingsResult? aggregate =
                await _seoMetadataRepository.GetArticleSeoSettingsByArticlePublicIdAsync(
                    articlePublicId: resourcePublicId,
                    scope: scope,
                    cancellationToken: cancellationToken);

            if (aggregate is null)
            {
                return Result<UpsertArticleSeoSettingsResponse>.Failure(
                    SeoErrors.Article.NotFound);
            }

            return Result<UpsertArticleSeoSettingsResponse>.Success(
                new UpsertArticleSeoSettingsResponse
                {
                    Updated = true,
                    Scope = aggregate.Scope,
                    ResourceType = aggregate.ResourceType,
                    ResourcePublicId = aggregate.ResourcePublicId,
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
                    Robots = aggregate.Robots,
                    IsManualOverride = aggregate.IsManualOverride ?? true,
                    IsIndexable = aggregate.IsIndexable,
                    IsActive = aggregate.IsActive,
                    SourceAggregateVersion = aggregate.SourceAggregateVersion,
                    LastAppliedMessageId = aggregate.LastAppliedMessageId,
                    LastSyncedAtUtc = aggregate.LastSyncedAtUtc,
                    Version = aggregate.Version
                });
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
            "SEO.INVALID_SCOPE" => SeoErrors.SlugRegistry.InvalidScope,

            "SEO.INVALID_RESOURCE_TYPE" => SeoErrors.Resource.InvalidResourceType,
            "SEO.INVALID_RESOURCE_PUBLIC_ID" => SeoErrors.Resource.InvalidResourcePublicId,

            "SEO.INVALID_SLUG" => SeoErrors.SlugRegistry.SlugRequired,
            "SEO.SLUG_TOO_LONG" => SeoErrors.SlugRegistry.SlugTooLong,
            "SEO.SLUG_REGISTRY_INVALID_SLUG_ID" => SeoErrors.SlugRegistry.InvalidSlugId,
            "SEO.SLUG_REGISTRY_INVALID_VERSION" => SeoErrors.SlugRegistry.InvalidVersion,
            "SEO.SLUG_REGISTRY_INVALID_UPDATED_AT" => SeoErrors.SlugRegistry.InvalidUpdatedAt,
            "SEO.SLUG_REGISTRY_ALREADY_ACTIVE" => SeoErrors.SlugRegistry.AlreadyActive,
            "SEO.SLUG_REGISTRY_ALREADY_INACTIVE" => SeoErrors.SlugRegistry.AlreadyInactive,
            "SEO.SLUG_REGISTRY_INACTIVE" => SeoErrors.SlugRegistry.Inactive,

            "SEO.CANONICAL_URL_TOO_LONG" => SeoErrors.SeoMetadata.CanonicalUrlTooLong,
            "SEO.INVALID_CANONICAL_URL" => SeoErrors.SeoMetadata.InvalidCanonicalUrl,

            "SEO.SEO_METADATA_INVALID_SEO_ID" => SeoErrors.SeoMetadata.InvalidSeoId,
            "SEO.SEO_METADATA_INVALID_VERSION" => SeoErrors.SeoMetadata.InvalidVersion,
            "SEO.SEO_METADATA_INVALID_UPDATED_AT" => SeoErrors.SeoMetadata.InvalidUpdatedAt,

            "SEO.META_TITLE_TOO_LONG" => SeoErrors.SeoMetadata.MetaTitleTooLong,
            "SEO.META_DESCRIPTION_TOO_LONG" => SeoErrors.SeoMetadata.MetaDescriptionTooLong,
            "SEO.OG_TITLE_TOO_LONG" => SeoErrors.SeoMetadata.OgTitleTooLong,
            "SEO.OG_DESCRIPTION_TOO_LONG" => SeoErrors.SeoMetadata.OgDescriptionTooLong,
            "SEO.OG_IMAGE_URL_TOO_LONG" => SeoErrors.SeoMetadata.OgImageUrlTooLong,
            "SEO.TWITTER_TITLE_TOO_LONG" => SeoErrors.SeoMetadata.TwitterTitleTooLong,
            "SEO.TWITTER_DESCRIPTION_TOO_LONG" => SeoErrors.SeoMetadata.TwitterDescriptionTooLong,
            "SEO.TWITTER_IMAGE_URL_TOO_LONG" => SeoErrors.SeoMetadata.TwitterImageUrlTooLong,
            "SEO.ROBOTS_TOO_LONG" => SeoErrors.SeoMetadata.RobotsTooLong,
            "SEO.INVALID_ROBOTS_DIRECTIVE" => SeoErrors.SeoMetadata.InvalidRobotsDirective,

            "SEO.INVALID_SOURCE_AGGREGATE_VERSION" => SeoErrors.Sync.InvalidSourceAggregateVersion,
            "SEO.INVALID_LAST_APPLIED_MESSAGE_ID" => SeoErrors.Sync.InvalidLastAppliedMessageId,
            "SEO.EVENT_STALE_IGNORED" => SeoErrors.Sync.EventStaleIgnored,

            _ => SeoErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "SEO.SLUG_CONFLICT" => SeoErrors.SlugRegistry.Conflict,
            "SEO.ACTIVE_ROUTE_ALREADY_EXISTS" => SeoErrors.SlugRegistry.ActiveRouteAlreadyExists,
            "SEO.ROUTE_OWNERSHIP_CONFLICT" => SeoErrors.SlugRegistry.RouteOwnershipConflict,

            "SEO.METADATA_ALREADY_EXISTS" => SeoErrors.SeoMetadata.AlreadyExists,
            "SEO.METADATA_OWNERSHIP_CONFLICT" => SeoErrors.SeoMetadata.MetadataOwnershipConflict,

            "SEO.INVALID_SCOPE" => SeoErrors.SlugRegistry.InvalidScope,
            "SEO.INVALID_RESOURCE_TYPE" => SeoErrors.Resource.InvalidResourceType,
            "SEO.INVALID_RESOURCE_PUBLIC_ID" => SeoErrors.Resource.InvalidResourcePublicId,
            "SEO.INVALID_SLUG" => SeoErrors.SlugRegistry.SlugRequired,
            "SEO.INVALID_CANONICAL_URL" => SeoErrors.SeoMetadata.InvalidCanonicalUrl,

            "SEO.VERSION_MISMATCH" => SeoErrors.SeoMetadata.VersionMismatch,
            "SEO.STALE_WRITE_CONFLICT" => SeoErrors.SeoMetadata.StaleWriteConflict,

            "SEO.INVALID_SOURCE_AGGREGATE_VERSION" => SeoErrors.Sync.InvalidSourceAggregateVersion,
            "SEO.INVALID_LAST_APPLIED_MESSAGE_ID" => SeoErrors.Sync.InvalidLastAppliedMessageId,

            _ => SeoErrors.ValidationFailed
        };
    }
}