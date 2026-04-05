using CommercialNews.BuildingBlocks.Abstractions.Execution;
using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;
using Seo.Application.Errors;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Entities;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SeoSettings.CreateSeoMetadata;

public sealed class CreateSeoMetadataUseCase : ICreateSeoMetadataUseCase
{
    private readonly ISeoMetadataRepository _seoMetadataRepository;
    private readonly ISeoUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;

    public CreateSeoMetadataUseCase(
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

    public async Task<Result<CreateSeoMetadataResponse>> ExecuteAsync(
        CreateSeoMetadataRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.ArticleId <= 0)
            {
                return Result<CreateSeoMetadataResponse>.Failure(
                    SeoErrors.Article.InvalidArticleId);
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;
            long? actorUserId = request.UpdatedByUserId ?? _requestContext.CurrentUserId;

            SeoMetadata seoMetadata = SeoMetadata.Create(
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

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                long seoId = await _seoMetadataRepository.InsertAsync(
                    seoMetadata,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<CreateSeoMetadataResponse>.Success(
                    new CreateSeoMetadataResponse
                    {
                        SeoId = seoId,
                        ArticleId = seoMetadata.ArticleId,
                        CanonicalUrl = seoMetadata.CanonicalUrl,
                        MetaTitle = seoMetadata.MetaTitle,
                        MetaDescription = seoMetadata.MetaDescription,
                        OgTitle = seoMetadata.OgTitle,
                        OgDescription = seoMetadata.OgDescription,
                        OgImageUrl = seoMetadata.OgImageUrl,
                        TwitterTitle = seoMetadata.TwitterTitle,
                        TwitterDescription = seoMetadata.TwitterDescription,
                        TwitterImageUrl = seoMetadata.TwitterImageUrl,
                        Version = seoMetadata.Version,
                        UpdatedAt = seoMetadata.UpdatedAt,
                        UpdatedByUserId = seoMetadata.UpdatedByUserId
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
            return Result<CreateSeoMetadataResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<CreateSeoMetadataResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.ARTICLE_INVALID_ARTICLE_ID" => SeoErrors.Article.InvalidArticleId,

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