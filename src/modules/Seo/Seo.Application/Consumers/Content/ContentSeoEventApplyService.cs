using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Consumers.Content.Payloads;
using Seo.Application.Contracts.EventApply;
using Seo.Application.Errors;
using Seo.Application.Models.Commands;
using Seo.Application.Models.Results;
using Seo.Application.Ports.Persistence;
using Seo.Application.Ports.Services;
using Seo.Domain.Constants;
using Seo.Domain.Entities;
using Seo.Domain.Exceptions;

namespace Seo.Application.Consumers.Content;

public sealed class ContentSeoEventApplyService : IContentSeoEventApplyService
{
    private const string SlugRouteOperation = "SlugRoute";
    private const string MetadataOperation = "Metadata";

    private readonly ISlugRegistryRepository _slugRegistryRepository;
    private readonly ISeoMetadataRepository _seoMetadataRepository;
    private readonly ISeoOutboxWriter _seoOutboxWriter;
    private readonly ISeoUnitOfWork _unitOfWork;

    public ContentSeoEventApplyService(
        ISlugRegistryRepository slugRegistryRepository,
        ISeoMetadataRepository seoMetadataRepository,
        ISeoOutboxWriter seoOutboxWriter,
        ISeoUnitOfWork unitOfWork)
    {
        _slugRegistryRepository = slugRegistryRepository
            ?? throw new ArgumentNullException(nameof(slugRegistryRepository));

        _seoMetadataRepository = seoMetadataRepository
            ?? throw new ArgumentNullException(nameof(seoMetadataRepository));

        _seoOutboxWriter = seoOutboxWriter
            ?? throw new ArgumentNullException(nameof(seoOutboxWriter));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public Task<Result<SeoEventApplyResult>> ApplyArticleCreatedAsync(
        ContentSeoEnvelopeContext context,
        ArticleCreatedSeoPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return ApplyMetadataOnlyAsync(
            context,
            payload.ArticlePublicId,
            payload.Version,
            payload.Title,
            payload.Summary,
            payload.CoverImageUrl,
            cancellationToken);
    }

    public Task<Result<SeoEventApplyResult>> ApplyArticleUpdatedAsync(
        ContentSeoEnvelopeContext context,
        ArticleUpdatedSeoPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return ApplyMetadataOnlyAsync(
            context,
            payload.ArticlePublicId,
            payload.Version,
            payload.Title,
            payload.Summary,
            payload.CoverImageUrl,
            cancellationToken);
    }

    public Task<Result<SeoEventApplyResult>> ApplyArticlePublishedAsync(
        ContentSeoEnvelopeContext context,
        ArticlePublishedSeoPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return ApplyRouteAndMetadataAsync(
            context,
            payload.ArticlePublicId,
            payload.Version,
            payload.Title,
            payload.Summary,
            payload.CoverImageUrl,
            isActive: true,
            isIndexable: true,
            cancellationToken);
    }

    public Task<Result<SeoEventApplyResult>> ApplyArticleUnpublishedAsync(
        ContentSeoEnvelopeContext context,
        ArticleUnpublishedSeoPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return ApplyRouteOnlyAsync(
            context,
            payload.ArticlePublicId,
            payload.Version,
            isActive: false,
            isIndexable: false,
            cancellationToken);
    }

    public Task<Result<SeoEventApplyResult>> ApplyArticleArchivedAsync(
        ContentSeoEnvelopeContext context,
        ArticleArchivedSeoPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return ApplyRouteOnlyAsync(
            context,
            payload.ArticlePublicId,
            payload.Version,
            isActive: false,
            isIndexable: false,
            cancellationToken);
    }

    public Task<Result<SeoEventApplyResult>> ApplyArticleSoftDeletedAsync(
        ContentSeoEnvelopeContext context,
        ArticleSoftDeletedSeoPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        return ApplyRouteOnlyAsync(
            context,
            payload.ArticlePublicId,
            payload.Version,
            isActive: false,
            isIndexable: false,
            cancellationToken);
    }

    private async Task<Result<SeoEventApplyResult>> ApplyMetadataOnlyAsync(
        ContentSeoEnvelopeContext context,
        string payloadArticlePublicId,
        long payloadVersion,
        string? title,
        string? summary,
        string? coverImageUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            string articlePublicId = ResolveArticlePublicId(context, payloadArticlePublicId);

            if (!IsValidPublicId(articlePublicId))
            {
                return Result<SeoEventApplyResult>.Failure(
                    SeoErrors.Article.InvalidArticlePublicId);
            }

            long sourceAggregateVersion = ResolveSourceAggregateVersion(context, payloadVersion);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                SeoApplyResultModel metadataResult = await ApplyMetadataDefaultsAsync(
                    context,
                    articlePublicId,
                    sourceAggregateVersion,
                    title,
                    summary,
                    coverImageUrl,
                    cancellationToken);

                await EnqueueMetadataUpdatedIfAppliedAsync(
                    context,
                    articlePublicId,
                    metadataResult,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<SeoEventApplyResult>.Success(
                    BuildResult(
                        context,
                        articlePublicId,
                        slugRouteResult: null,
                        metadataResult));
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException exception)
        {
            return Result<SeoEventApplyResult>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<SeoEventApplyResult>.Failure(
                MapDomainException(exception));
        }
    }

    private async Task<Result<SeoEventApplyResult>> ApplyRouteOnlyAsync(
        ContentSeoEnvelopeContext context,
        string payloadArticlePublicId,
        long payloadVersion,
        bool isActive,
        bool isIndexable,
        CancellationToken cancellationToken)
    {
        try
        {
            string articlePublicId = ResolveArticlePublicId(context, payloadArticlePublicId);

            if (!IsValidPublicId(articlePublicId))
            {
                return Result<SeoEventApplyResult>.Failure(
                    SeoErrors.Article.InvalidArticlePublicId);
            }

            long sourceAggregateVersion = ResolveSourceAggregateVersion(context, payloadVersion);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                SeoApplyResultModel slugRouteResult = await ApplyVisibilityAsync(
                    context,
                    articlePublicId,
                    sourceAggregateVersion,
                    isActive,
                    isIndexable,
                    cancellationToken);

                await EnqueueSlugRouteEventIfAppliedAsync(
                    context,
                    articlePublicId,
                    slugRouteResult,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<SeoEventApplyResult>.Success(
                    BuildResult(
                        context,
                        articlePublicId,
                        slugRouteResult,
                        metadataResult: null));
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException exception)
        {
            return Result<SeoEventApplyResult>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<SeoEventApplyResult>.Failure(
                MapDomainException(exception));
        }
    }

    private async Task<Result<SeoEventApplyResult>> ApplyRouteAndMetadataAsync(
        ContentSeoEnvelopeContext context,
        string payloadArticlePublicId,
        long payloadVersion,
        string? title,
        string? summary,
        string? coverImageUrl,
        bool isActive,
        bool isIndexable,
        CancellationToken cancellationToken)
    {
        try
        {
            string articlePublicId = ResolveArticlePublicId(context, payloadArticlePublicId);

            if (!IsValidPublicId(articlePublicId))
            {
                return Result<SeoEventApplyResult>.Failure(
                    SeoErrors.Article.InvalidArticlePublicId);
            }

            long sourceAggregateVersion = ResolveSourceAggregateVersion(context, payloadVersion);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                SeoApplyResultModel slugRouteResult = await ApplyVisibilityAsync(
                    context,
                    articlePublicId,
                    sourceAggregateVersion,
                    isActive,
                    isIndexable,
                    cancellationToken);

                SeoApplyResultModel metadataResult = await ApplyMetadataDefaultsAsync(
                    context,
                    articlePublicId,
                    sourceAggregateVersion,
                    title,
                    summary,
                    coverImageUrl,
                    cancellationToken);

                await EnqueueSlugRouteEventIfAppliedAsync(
                    context,
                    articlePublicId,
                    slugRouteResult,
                    cancellationToken);

                await EnqueueMetadataUpdatedIfAppliedAsync(
                    context,
                    articlePublicId,
                    metadataResult,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<SeoEventApplyResult>.Success(
                    BuildResult(
                        context,
                        articlePublicId,
                        slugRouteResult,
                        metadataResult));
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException exception)
        {
            return Result<SeoEventApplyResult>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<SeoEventApplyResult>.Failure(
                MapDomainException(exception));
        }
    }

    private async Task<SeoApplyResultModel> ApplyVisibilityAsync(
        ContentSeoEnvelopeContext context,
        string articlePublicId,
        long sourceAggregateVersion,
        bool isActive,
        bool isIndexable,
        CancellationToken cancellationToken)
    {
        SlugRegistry? existingRoute =
            await _slugRegistryRepository.GetByResourceAsync(
                scope: SeoScopes.Public,
                resourceType: SeoResourceTypes.Article,
                resourcePublicId: articlePublicId,
                onlyActive: null,
                cancellationToken: cancellationToken);

        if (existingRoute is null)
        {
            if (isActive)
            {
                return new SeoApplyResultModel
                {
                    ApplyResult = SeoApplyResults.NoRouteToActivate,
                    SourceAggregateVersion = sourceAggregateVersion,
                    LastAppliedMessageId = context.MessageId,
                    LastSyncedAtUtc = context.OccurredAtUtc
                };
            }

            return new SeoApplyResultModel
            {
                ApplyResult = SeoApplyResults.NoRouteToDeactivate,
                SourceAggregateVersion = sourceAggregateVersion,
                LastAppliedMessageId = context.MessageId,
                LastSyncedAtUtc = context.OccurredAtUtc
            };
        }

        ApplyContentVisibilityCommand command = new(
            Scope: existingRoute.Scope,
            Slug: existingRoute.Slug,
            ResourceType: existingRoute.ResourceType,
            ResourcePublicId: existingRoute.ResourcePublicId,
            CanonicalUrl: existingRoute.CanonicalUrl,
            IsIndexable: isIndexable,
            IsActive: isActive,
            SourceAggregateVersion: sourceAggregateVersion,
            LastAppliedMessageId: context.MessageId,
            LastSyncedAtUtc: context.OccurredAtUtc);

        return await _slugRegistryRepository.ApplyContentVisibilityAsync(
            command,
            cancellationToken);
    }

    private async Task<SeoApplyResultModel> ApplyMetadataDefaultsAsync(
        ContentSeoEnvelopeContext context,
        string articlePublicId,
        long sourceAggregateVersion,
        string? title,
        string? summary,
        string? coverImageUrl,
        CancellationToken cancellationToken)
    {
        (string? slug, string? canonicalUrl) =
            await ResolveCurrentRouteMetadataAsync(
                articlePublicId,
                cancellationToken);

        ApplyContentMetadataDefaultsCommand command = new(
            Scope: SeoScopes.Public,
            ResourceType: SeoResourceTypes.Article,
            ResourcePublicId: articlePublicId,
            Slug: slug,
            CanonicalUrl: canonicalUrl,
            MetaTitle: title,
            MetaDescription: summary,
            OgTitle: title,
            OgDescription: summary,
            OgImageUrl: coverImageUrl,
            SourceAggregateVersion: sourceAggregateVersion,
            LastAppliedMessageId: context.MessageId,
            LastSyncedAtUtc: context.OccurredAtUtc);

        return await _seoMetadataRepository.ApplyContentDefaultsAsync(
            command,
            cancellationToken);
    }

    private async Task<(string? Slug, string? CanonicalUrl)> ResolveCurrentRouteMetadataAsync(
        string articlePublicId,
        CancellationToken cancellationToken)
    {
        SlugRegistry? route =
            await _slugRegistryRepository.GetByResourceAsync(
                scope: SeoScopes.Public,
                resourceType: SeoResourceTypes.Article,
                resourcePublicId: articlePublicId,
                onlyActive: null,
                cancellationToken: cancellationToken);

        return route is null
            ? (null, null)
            : (route.Slug, route.CanonicalUrl);
    }

    private async Task EnqueueSlugRouteEventIfAppliedAsync(
        ContentSeoEnvelopeContext context,
        string articlePublicId,
        SeoApplyResultModel slugRouteResult,
        CancellationToken cancellationToken)
    {
        if (!IsApplied(slugRouteResult))
        {
            return;
        }

        SlugRegistry? route = await _slugRegistryRepository.GetByResourceAsync(
            scope: SeoScopes.Public,
            resourceType: SeoResourceTypes.Article,
            resourcePublicId: articlePublicId,
            onlyActive: null,
            cancellationToken: cancellationToken);

        if (route is null)
        {
            throw new InvalidOperationException(
                "SEO slug route was applied but could not be loaded for outbound event publishing.");
        }

        if (route.IsActive)
        {
            await _seoOutboxWriter.EnqueueSlugRouteChangedAsync(
                _unitOfWork,
                route,
                context.InitiatorUserId,
                context.CorrelationId,
                cancellationToken);

            return;
        }

        await _seoOutboxWriter.EnqueueSlugRouteDeactivatedAsync(
            _unitOfWork,
            route,
            context.InitiatorUserId,
            context.CorrelationId,
            cancellationToken);
    }

    private async Task EnqueueMetadataUpdatedIfAppliedAsync(
        ContentSeoEnvelopeContext context,
        string articlePublicId,
        SeoApplyResultModel metadataResult,
        CancellationToken cancellationToken)
    {
        if (!IsApplied(metadataResult))
        {
            return;
        }

        SeoMetadata? metadata = await _seoMetadataRepository.GetByResourceAsync(
            scope: SeoScopes.Public,
            resourceType: SeoResourceTypes.Article,
            resourcePublicId: articlePublicId,
            cancellationToken: cancellationToken);

        if (metadata is null)
        {
            throw new InvalidOperationException(
                "SEO metadata was applied but could not be loaded for outbound event publishing.");
        }

        await _seoOutboxWriter.EnqueueMetadataUpdatedAsync(
            _unitOfWork,
            metadata,
            context.InitiatorUserId,
            context.CorrelationId,
            cancellationToken);
    }

    private static bool IsApplied(SeoApplyResultModel result)
    {
        return string.Equals(
            result.ApplyResult,
            SeoApplyResults.Applied,
            StringComparison.OrdinalIgnoreCase);
    }

    private static SeoEventApplyResult BuildResult(
        ContentSeoEnvelopeContext context,
        string articlePublicId,
        SeoApplyResultModel? slugRouteResult,
        SeoApplyResultModel? metadataResult)
    {
        return SeoEventApplyResult.From(
            messageId: context.MessageId,
            eventType: context.EventType,
            resourceType: SeoResourceTypes.Article,
            resourcePublicId: articlePublicId,
            slugRoute: slugRouteResult is null
                ? null
                : SeoApplyOperationResult.From(SlugRouteOperation, slugRouteResult),
            metadata: metadataResult is null
                ? null
                : SeoApplyOperationResult.From(MetadataOperation, metadataResult));
    }

    private static string ResolveArticlePublicId(
        ContentSeoEnvelopeContext context,
        string payloadArticlePublicId)
    {
        if (!string.IsNullOrWhiteSpace(payloadArticlePublicId))
        {
            return payloadArticlePublicId.Trim();
        }

        return context.AggregatePublicId?.Trim() ?? string.Empty;
    }

    private static long ResolveSourceAggregateVersion(
        ContentSeoEnvelopeContext context,
        long payloadVersion)
    {
        if (context.AggregateVersion.HasValue && context.AggregateVersion.Value > 0)
        {
            return context.AggregateVersion.Value;
        }

        return payloadVersion;
    }

    private static bool IsValidPublicId(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Trim().Length == 26;
    }

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.INVALID_SCOPE" => SeoErrors.SlugRegistry.InvalidScope,

            "SEO.INVALID_SLUG" => SeoErrors.SlugRegistry.SlugRequired,
            "SEO.SLUG_TOO_LONG" => SeoErrors.SlugRegistry.SlugTooLong,

            "SEO.INVALID_RESOURCE_TYPE" => SeoErrors.Resource.InvalidResourceType,
            "SEO.INVALID_RESOURCE_PUBLIC_ID" => SeoErrors.Resource.InvalidResourcePublicId,

            "SEO.CANONICAL_URL_TOO_LONG" => SeoErrors.SeoMetadata.CanonicalUrlTooLong,
            "SEO.INVALID_CANONICAL_URL" => SeoErrors.SeoMetadata.InvalidCanonicalUrl,

            "SEO.META_TITLE_TOO_LONG" => SeoErrors.SeoMetadata.MetaTitleTooLong,
            "SEO.META_DESCRIPTION_TOO_LONG" => SeoErrors.SeoMetadata.MetaDescriptionTooLong,
            "SEO.OG_TITLE_TOO_LONG" => SeoErrors.SeoMetadata.OgTitleTooLong,
            "SEO.OG_DESCRIPTION_TOO_LONG" => SeoErrors.SeoMetadata.OgDescriptionTooLong,
            "SEO.OG_IMAGE_URL_TOO_LONG" => SeoErrors.SeoMetadata.OgImageUrlTooLong,

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

            "SEO.METADATA_ALREADY_EXISTS" => SeoErrors.SeoMetadata.AlreadyExists,

            "SEO.INVALID_SCOPE" => SeoErrors.SlugRegistry.InvalidScope,
            "SEO.INVALID_RESOURCE_TYPE" => SeoErrors.Resource.InvalidResourceType,
            "SEO.INVALID_RESOURCE_PUBLIC_ID" => SeoErrors.Resource.InvalidResourcePublicId,
            "SEO.INVALID_SLUG" => SeoErrors.SlugRegistry.SlugRequired,
            "SEO.INVALID_CANONICAL_URL" => SeoErrors.SeoMetadata.InvalidCanonicalUrl,

            "SEO.VERSION_MISMATCH" => SeoErrors.SeoMetadata.VersionMismatch,
            "SEO.STALE_WRITE_CONFLICT" => SeoErrors.SeoMetadata.StaleWriteConflict,

            "SEO.INVALID_SOURCE_AGGREGATE_VERSION" => SeoErrors.Sync.InvalidSourceAggregateVersion,
            "SEO.INVALID_LAST_APPLIED_MESSAGE_ID" => SeoErrors.Sync.InvalidLastAppliedMessageId,

            "SEO.STORE_UNAVAILABLE" => SeoErrors.Infrastructure.StoreUnavailable,

            _ => SeoErrors.ValidationFailed
        };
    }
}
