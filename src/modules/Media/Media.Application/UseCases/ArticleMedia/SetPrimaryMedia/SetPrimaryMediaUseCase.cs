using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;
using Media.Application.Errors;
using Media.Application.Models.Commands;
using Media.Application.Models.Results;
using Media.Application.Ports.Persistence;
using Media.Application.Ports.Services;
using Media.Domain.Constants;
using Media.Domain.Entities;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.ArticleMedia.SetPrimaryMedia;

public sealed class SetPrimaryMediaUseCase : ISetPrimaryMediaUseCase
{
    private readonly IArticleMediaRepository _articleMediaRepository;
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IMediaOutboxWriter _mediaOutboxWriter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;

    public SetPrimaryMediaUseCase(
        IArticleMediaRepository articleMediaRepository,
        IMediaAssetRepository mediaAssetRepository,
        IMediaUnitOfWork unitOfWork,
        IMediaOutboxWriter mediaOutboxWriter,
        IDateTimeProvider dateTimeProvider,
        IRequestContext requestContext)
    {
        _articleMediaRepository = articleMediaRepository
            ?? throw new ArgumentNullException(nameof(articleMediaRepository));

        _mediaAssetRepository = mediaAssetRepository
            ?? throw new ArgumentNullException(nameof(mediaAssetRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _mediaOutboxWriter = mediaOutboxWriter
            ?? throw new ArgumentNullException(nameof(mediaOutboxWriter));

        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));

        _requestContext = requestContext
            ?? throw new ArgumentNullException(nameof(requestContext));
    }

    public async Task<Result<SetPrimaryMediaResponse>> ExecuteAsync(
        SetPrimaryMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (request.ArticleId <= 0)
            {
                return Result<SetPrimaryMediaResponse>.Failure(
                    MediaErrors.ArticleMedia.InvalidArticleId);
            }

            if (request.MediaId <= 0)
            {
                return Result<SetPrimaryMediaResponse>.Failure(
                    MediaErrors.ArticleMedia.InvalidMediaId);
            }

            if (request.ExpectedVersion is null)
            {
                return Result<SetPrimaryMediaResponse>.Failure(
                    MediaErrors.ExpectedVersionRequired);
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;
            long? actorUserId = _requestContext.CurrentUserId;

            if (actorUserId is null or <= 0)
            {
                return Result<SetPrimaryMediaResponse>.Failure(
                    MediaErrors.Actor.NotFound);
            }

            Error? primaryMediaError = await ValidatePrimaryMediaAsync(
                request.MediaId,
                cancellationToken);

            if (primaryMediaError is not null)
            {
                return Result<SetPrimaryMediaResponse>.Failure(
                    primaryMediaError);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                ArticleMediaMutationResult setPrimaryResult =
                    await _articleMediaRepository.SetPrimaryAsync(
                        new SetPrimaryArticleMediaCommand(
                            ArticleId: request.ArticleId,
                            MediaId: request.MediaId,
                            ExpectedVersion: request.ExpectedVersion,
                            UpdatedBy: actorUserId),
                        cancellationToken);

                if (!setPrimaryResult.Succeeded)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<SetPrimaryMediaResponse>.Failure(
                        MapMutationResultCode(setPrimaryResult.ResultCode));
                }

                int attachmentSetVersion = setPrimaryResult.NewVersion
                    ?? throw new InvalidOperationException(
                        "Media_ArticleMedia_SetPrimary did not return NewVersion.");

                if (setPrimaryResult.AffectedRows > 0)
                {
                    ArticleMediaListResultItem? primaryMedia =
                        await _articleMediaRepository.GetPrimaryByArticleIdAsync(
                            request.ArticleId,
                            cancellationToken);

                    if (primaryMedia is null || primaryMedia.MediaId != request.MediaId)
                    {
                        await _unitOfWork.RollbackAsync(cancellationToken);

                        return Result<SetPrimaryMediaResponse>.Failure(
                            MediaErrors.PersistenceError);
                    }

                    await _mediaOutboxWriter.EnqueueArticlePrimaryMediaSetAsync(
                        unitOfWork: _unitOfWork,
                        articleId: request.ArticleId,
                        mediaId: request.MediaId,
                        mediaPublicId: primaryMedia.PublicId,
                        url: primaryMedia.Url,
                        mediaType: primaryMedia.MediaType,
                        altText: primaryMedia.DefaultAltText,
                        altTextOverride: primaryMedia.AltTextOverride,
                        caption: primaryMedia.Caption,
                        sortOrder: primaryMedia.SortOrder,
                        actorUserId: actorUserId.Value,
                        attachmentSetVersion: attachmentSetVersion,
                        primarySetAtUtc: nowUtc,
                        correlationId: _requestContext.CorrelationId,
                        cancellationToken: cancellationToken);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<SetPrimaryMediaResponse>.Success(
                    new SetPrimaryMediaResponse
                    {
                        ArticleId = request.ArticleId,
                        MediaId = request.MediaId,
                        PrimarySet = setPrimaryResult.AffectedRows > 0,
                        AffectedRows = setPrimaryResult.AffectedRows,
                        AttachmentSetVersion = attachmentSetVersion
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
            return Result<SetPrimaryMediaResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<SetPrimaryMediaResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapMutationResultCode(int resultCode)
    {
        return resultCode switch
        {
            1 => MediaErrors.ArticleMedia.NotFound,
            2 => MediaErrors.MediaAsset.Deleted,
            3 => MediaErrors.VersionConflict,
            6 => MediaErrors.ExpectedVersionRequired,
            7 => MediaErrors.ArticleMedia.PrimaryMustBeImage,
            _ => MediaErrors.PersistenceError
        };
    }

    private async Task<Error?> ValidatePrimaryMediaAsync(
        long mediaId,
        CancellationToken cancellationToken)
    {
        MediaAsset? mediaAsset = await _mediaAssetRepository.GetByIdAsync(
            mediaId,
            cancellationToken);

        if (mediaAsset is null)
        {
            return MediaErrors.MediaAsset.NotFound;
        }

        if (mediaAsset.IsDeleted)
        {
            return MediaErrors.MediaAsset.Deleted;
        }

        if (!string.Equals(
                mediaAsset.MediaType,
                MediaTypes.Image,
                StringComparison.OrdinalIgnoreCase))
        {
            return MediaErrors.ArticleMedia.PrimaryMustBeImage;
        }

        return null;
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.ARTICLE_MEDIA_INVALID_ARTICLE_ID" =>
                MediaErrors.ArticleMedia.InvalidArticleId,

            "MEDIA.ARTICLE_MEDIA_INVALID_MEDIA_ID" =>
                MediaErrors.ArticleMedia.InvalidMediaId,

            "MEDIA.EXPECTED_VERSION_REQUIRED" =>
                MediaErrors.ExpectedVersionRequired,

            "MEDIA.VERSION_CONFLICT" =>
                MediaErrors.VersionConflict,

            "MEDIA.PRIMARY_CONSTRAINT_VIOLATION" =>
                MediaErrors.ArticleMedia.PrimaryConstraintViolation,

            _ => MediaErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "MEDIA.ARTICLE_NOT_FOUND" =>
                MediaErrors.Article.NotFound,

            "MEDIA.MEDIA_NOT_FOUND" =>
                MediaErrors.MediaAsset.NotFound,

            "MEDIA.MEDIA_DELETED" =>
                MediaErrors.MediaAsset.Deleted,

            "MEDIA.ATTACHMENT_NOT_FOUND" =>
                MediaErrors.ArticleMedia.NotFound,

            "MEDIA.PRIMARY_CONSTRAINT_VIOLATION" =>
                MediaErrors.ArticleMedia.PrimaryConstraintViolation,

            "MEDIA.ACTOR_NOT_FOUND" =>
                MediaErrors.Actor.NotFound,

            "MEDIA.CONSTRAINT_VIOLATION" =>
                MediaErrors.ConstraintViolation,

            "MEDIA.CONCURRENT_MODIFICATION" =>
                MediaErrors.ConcurrentModification,

            "MEDIA.DEPENDENCY_UNAVAILABLE" =>
                MediaErrors.DependencyUnavailable,

            "MEDIA.PERSISTENCE_ERROR" =>
                MediaErrors.PersistenceError,

            _ => MediaErrors.PersistenceError
        };
    }
}
