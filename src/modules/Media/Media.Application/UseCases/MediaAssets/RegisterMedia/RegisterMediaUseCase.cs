using CommercialNews.BuildingBlocks.Abstractions.Execution;
using CommercialNews.BuildingBlocks.Abstractions.Identifiers;
using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;
using Media.Application.Errors;
using Media.Application.Ports.Persistence;
using Media.Domain.Entities;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.MediaAssets.RegisterMedia;

public sealed class RegisterMediaUseCase : IRegisterMediaUseCase
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IPublicIdGenerator _publicIdGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;

    public RegisterMediaUseCase(
        IMediaAssetRepository mediaAssetRepository,
        IMediaUnitOfWork unitOfWork,
        IPublicIdGenerator publicIdGenerator,
        IDateTimeProvider dateTimeProvider,
        IRequestContext requestContext)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _unitOfWork = unitOfWork;
        _publicIdGenerator = publicIdGenerator;
        _dateTimeProvider = dateTimeProvider;
        _requestContext = requestContext;
    }

    public async Task<Result<RegisterMediaResponse>> ExecuteAsync(
        RegisterMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string publicId = string.IsNullOrWhiteSpace(request.PublicId)
                ? _publicIdGenerator.NewId()
                : request.PublicId.Trim();

            DateTime nowUtc = _dateTimeProvider.UtcNow;
            long? actorUserId = request.ActorUserId ?? _requestContext.CurrentUserId;

            MediaAsset mediaAsset = MediaAsset.Create(
                publicId: publicId,
                storageProvider: request.StorageProvider,
                url: request.Url,
                mediaType: request.MediaType,
                storagePath: request.StoragePath,
                fileName: request.FileName,
                mimeType: request.MimeType,
                fileSizeBytes: request.FileSizeBytes,
                width: request.Width,
                height: request.Height,
                durationSeconds: request.DurationSeconds,
                altText: request.AltText,
                metadataJson: request.MetadataJson,
                contentHash: request.ContentHash,
                nowUtc: nowUtc,
                actorUserId: actorUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                long mediaId = await _mediaAssetRepository.InsertAsync(
                    mediaAsset,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<RegisterMediaResponse>.Success(new RegisterMediaResponse
                {
                    MediaId = mediaId,
                    PublicId = mediaAsset.PublicId,
                    StorageProvider = mediaAsset.StorageProvider,
                    Url = mediaAsset.Url,
                    StoragePath = mediaAsset.StoragePath,
                    FileName = mediaAsset.FileName,
                    MediaType = mediaAsset.MediaType,
                    MimeType = mediaAsset.MimeType,
                    FileSizeBytes = mediaAsset.FileSizeBytes,
                    Width = mediaAsset.Width,
                    Height = mediaAsset.Height,
                    DurationSeconds = mediaAsset.DurationSeconds,
                    AltText = mediaAsset.AltText,
                    MetadataJson = mediaAsset.MetadataJson,
                    CreatedAt = mediaAsset.CreatedAt,
                    CreatedByUserId = mediaAsset.CreatedByUserId,
                    Version = mediaAsset.Version
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
            return Result<RegisterMediaResponse>.Failure(MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<RegisterMediaResponse>.Failure(MapDomainException(exception));
        }
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.MEDIA_ASSET_PUBLIC_ID_REQUIRED" => MediaErrors.MediaAsset.PublicIdRequired,
            "MEDIA.MEDIA_ASSET_PUBLIC_ID_INVALID" => MediaErrors.MediaAsset.PublicIdInvalid,
            "MEDIA.MEDIA_ASSET_STORAGE_PROVIDER_REQUIRED" => MediaErrors.MediaAsset.StorageProviderRequired,
            "MEDIA.MEDIA_ASSET_STORAGE_PROVIDER_TOO_LONG" => MediaErrors.MediaAsset.StorageProviderTooLong,
            "MEDIA.MEDIA_ASSET_URL_REQUIRED" => MediaErrors.MediaAsset.UrlRequired,
            "MEDIA.MEDIA_ASSET_URL_TOO_LONG" => MediaErrors.MediaAsset.UrlTooLong,
            "MEDIA.TYPE_NOT_ALLOWED" => MediaErrors.MediaAsset.TypeNotAllowed,
            "MEDIA.MEDIA_ASSET_MIME_TYPE_TOO_LONG" => MediaErrors.MediaAsset.MimeTypeTooLong,
            "MEDIA.MEDIA_ASSET_FILE_SIZE_INVALID" => MediaErrors.MediaAsset.FileSizeInvalid,
            "MEDIA.MEDIA_ASSET_DURATION_INVALID" => MediaErrors.MediaAsset.DurationInvalid,
            "MEDIA.MEDIA_ASSET_ALT_TEXT_TOO_LONG" => MediaErrors.MediaAsset.AltTextTooLong,
            "MEDIA.MEDIA_ASSET_FILE_NAME_TOO_LONG" => MediaErrors.MediaAsset.FileNameTooLong,
            "MEDIA.MEDIA_ASSET_ALREADY_DELETED" => MediaErrors.MediaAsset.AlreadyDeleted,
            "MEDIA.MEDIA_ASSET_NOT_DELETED" => MediaErrors.MediaAsset.NotDeleted,
            "MEDIA.RESTORE_WINDOW_EXPIRED" => MediaErrors.MediaAsset.RestoreWindowExpired,
            "MEDIA.MEDIA_ASSET_RESTORE_UNTIL_INVALID" => MediaErrors.MediaAsset.RestoreUntilInvalid,
            _ => MediaErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "MEDIA.CONCURRENCY_CONFLICT" => MediaErrors.ArticleMedia.PrimaryConstraintViolation,
            _ => MediaErrors.ValidationFailed
        };
    }
}