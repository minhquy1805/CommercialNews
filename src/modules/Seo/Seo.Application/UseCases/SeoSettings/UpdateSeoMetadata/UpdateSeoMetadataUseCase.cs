using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;
using Seo.Application.Errors;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Entities;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SeoSettings.UpdateSeoMetadata;

public sealed class UpdateSeoMetadataUseCase : IUpdateSeoMetadataUseCase
{
    private readonly ISeoMetadataRepository _seoMetadataRepository;
    private readonly ISeoUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;

    public UpdateSeoMetadataUseCase(
        ISeoMetadataRepository seoMetadataRepository,
        ISeoUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IRequestContext requestContext)
    {
        _seoMetadataRepository = seoMetadataRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _requestContext = requestContext;
    }

    public async Task<Result<UpdateSeoMetadataResponse>> ExecuteAsync(
        UpdateSeoMetadataRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.SeoId <= 0)
            {
                return Result<UpdateSeoMetadataResponse>.Failure(
                    SeoErrors.SeoMetadata.InvalidSeoId);
            }

            SeoMetadata? existing = await _seoMetadataRepository.GetByIdAsync(
                request.SeoId,
                cancellationToken);

            if (existing is null)
            {
                return Result<UpdateSeoMetadataResponse>.Failure(
                    SeoErrors.SeoMetadata.NotFound);
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;
            long? actorUserId = request.UpdatedByUserId ?? _requestContext.CurrentUserId;

            existing.Update(
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

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                int affectedRows = await _seoMetadataRepository.UpdateAsync(
                    existing,
                    request.ExpectedVersion,
                    cancellationToken);

                if (affectedRows <= 0)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<UpdateSeoMetadataResponse>.Failure(
                        SeoErrors.SeoMetadata.VersionMismatch);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<UpdateSeoMetadataResponse>.Success(
                    new UpdateSeoMetadataResponse
                    {
                        SeoId = existing.SeoId,
                        ArticleId = existing.ArticleId,
                        CanonicalUrl = existing.CanonicalUrl,
                        MetaTitle = existing.MetaTitle,
                        MetaDescription = existing.MetaDescription,
                        OgTitle = existing.OgTitle,
                        OgDescription = existing.OgDescription,
                        OgImageUrl = existing.OgImageUrl,
                        TwitterTitle = existing.TwitterTitle,
                        TwitterDescription = existing.TwitterDescription,
                        TwitterImageUrl = existing.TwitterImageUrl,
                        Version = existing.Version,
                        UpdatedAt = existing.UpdatedAt,
                        UpdatedByUserId = existing.UpdatedByUserId
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
            return Result<UpdateSeoMetadataResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<UpdateSeoMetadataResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.SEO_METADATA_INVALID_SEO_ID" => SeoErrors.SeoMetadata.InvalidSeoId,
            "SEO.SEO_METADATA_INVALID_ARTICLE_ID" => SeoErrors.SeoMetadata.InvalidArticleId,
            "SEO.SEO_METADATA_INVALID_VERSION" => SeoErrors.SeoMetadata.InvalidVersion,
            "SEO.CANONICAL_URL_TOO_LONG" => SeoErrors.SeoMetadata.CanonicalUrlTooLong,
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
            "SEO.METADATA_ALREADY_EXISTS" => SeoErrors.SeoMetadata.AlreadyExists,
            "SEO.VERSION_MISMATCH" => SeoErrors.SeoMetadata.VersionMismatch,
            "SEO.STALE_WRITE_CONFLICT" => SeoErrors.SeoMetadata.StaleWriteConflict,
            _ => SeoErrors.ValidationFailed
        };
    }
}