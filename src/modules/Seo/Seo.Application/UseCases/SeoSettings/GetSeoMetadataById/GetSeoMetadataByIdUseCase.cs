using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;
using Seo.Application.Errors;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Entities;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SeoSettings.GetSeoMetadataById;

public sealed class GetSeoMetadataByIdUseCase : IGetSeoMetadataByIdUseCase
{
    private readonly ISeoMetadataRepository _seoMetadataRepository;

    public GetSeoMetadataByIdUseCase(
        ISeoMetadataRepository seoMetadataRepository)
    {
        _seoMetadataRepository = seoMetadataRepository
            ?? throw new ArgumentNullException(nameof(seoMetadataRepository));
    }

    public async Task<Result<GetSeoMetadataByIdResponse>> ExecuteAsync(
        GetSeoMetadataByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (request.SeoId <= 0)
            {
                return Result<GetSeoMetadataByIdResponse>.Failure(
                    SeoErrors.SeoMetadata.InvalidSeoId);
            }

            SeoMetadata? existing = await _seoMetadataRepository.GetByIdAsync(
                request.SeoId,
                cancellationToken);

            if (existing is null)
            {
                return Result<GetSeoMetadataByIdResponse>.Failure(
                    SeoErrors.SeoMetadata.NotFound);
            }

            return Result<GetSeoMetadataByIdResponse>.Success(
                new GetSeoMetadataByIdResponse
                {
                    SeoId = existing.SeoId,

                    Scope = existing.Scope,
                    ResourceType = existing.ResourceType,
                    ResourcePublicId = existing.ResourcePublicId,

                    Slug = existing.Slug,
                    CanonicalUrl = existing.CanonicalUrl,

                    MetaTitle = existing.MetaTitle,
                    MetaDescription = existing.MetaDescription,

                    OgTitle = existing.OgTitle,
                    OgDescription = existing.OgDescription,
                    OgImageUrl = existing.OgImageUrl,

                    TwitterTitle = existing.TwitterTitle,
                    TwitterDescription = existing.TwitterDescription,
                    TwitterImageUrl = existing.TwitterImageUrl,

                    Robots = existing.Robots,

                    IsManualOverride = existing.IsManualOverride,

                    SourceAggregateVersion = existing.SourceAggregateVersion,
                    LastAppliedMessageId = existing.LastAppliedMessageId,
                    LastSyncedAtUtc = existing.LastSyncedAtUtc,

                    Version = existing.Version,

                    CreatedAtUtc = existing.CreatedAtUtc,
                    UpdatedAtUtc = existing.UpdatedAtUtc,
                    UpdatedByUserId = existing.UpdatedByUserId
                });
        }
        catch (PersistenceException exception)
        {
            return Result<GetSeoMetadataByIdResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<GetSeoMetadataByIdResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.SEO_METADATA_INVALID_SEO_ID" => SeoErrors.SeoMetadata.InvalidSeoId,
            "SEO.INVALID_SCOPE" => SeoErrors.SeoMetadata.InvalidScope,
            "SEO.INVALID_RESOURCE_TYPE" => SeoErrors.Resource.InvalidResourceType,
            "SEO.INVALID_RESOURCE_PUBLIC_ID" => SeoErrors.Resource.InvalidResourcePublicId,
            "SEO.SEO_METADATA_INVALID_VERSION" => SeoErrors.SeoMetadata.InvalidVersion,
            "SEO.SEO_METADATA_INVALID_UPDATED_AT" => SeoErrors.SeoMetadata.InvalidUpdatedAt,

            "SEO.CANONICAL_URL_TOO_LONG" => SeoErrors.SeoMetadata.CanonicalUrlTooLong,
            "SEO.INVALID_CANONICAL_URL" => SeoErrors.SeoMetadata.InvalidCanonicalUrl,

            "SEO.META_TITLE_TOO_LONG" => SeoErrors.SeoMetadata.MetaTitleTooLong,
            "SEO.META_DESCRIPTION_TOO_LONG" => SeoErrors.SeoMetadata.MetaDescriptionTooLong,
            "SEO.OG_TITLE_TOO_LONG" => SeoErrors.SeoMetadata.OgTitleTooLong,
            "SEO.OG_DESCRIPTION_TOO_LONG" => SeoErrors.SeoMetadata.OgDescriptionTooLong,
            "SEO.OG_IMAGE_URL_TOO_LONG" => SeoErrors.SeoMetadata.OgImageUrlTooLong,
            "SEO.TWITTER_TITLE_TOO_LONG" => SeoErrors.SeoMetadata.TwitterTitleTooLong,
            "SEO.TWITTER_DESCRIPTION_TOO_LONG" => SeoErrors.SeoMetadata.TwitterDescriptionTooLong,
            "SEO.TWITTER_IMAGE_URL_TOO_LONG" => SeoErrors.SeoMetadata.TwitterImageUrlTooLong,
            "SEO.ROBOTS_TOO_LONG" => SeoErrors.SeoMetadata.RobotsTooLong,

            "SEO.INVALID_SOURCE_AGGREGATE_VERSION" => SeoErrors.Sync.InvalidSourceAggregateVersion,
            "SEO.INVALID_LAST_APPLIED_MESSAGE_ID" => SeoErrors.Sync.InvalidLastAppliedMessageId,

            _ => SeoErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "SEO.INVALID_SCOPE" => SeoErrors.SeoMetadata.InvalidScope,
            "SEO.INVALID_RESOURCE_TYPE" => SeoErrors.Resource.InvalidResourceType,
            "SEO.INVALID_RESOURCE_PUBLIC_ID" => SeoErrors.Resource.InvalidResourcePublicId,
            "SEO.VERSION_MISMATCH" => SeoErrors.SeoMetadata.VersionMismatch,
            "SEO.STORE_UNAVAILABLE" => SeoErrors.Infrastructure.StoreUnavailable,

            _ => SeoErrors.ValidationFailed
        };
    }
}