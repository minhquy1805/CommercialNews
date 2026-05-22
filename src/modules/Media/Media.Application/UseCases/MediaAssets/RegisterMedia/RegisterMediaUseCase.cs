using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;
using Media.Application.Errors;
using Media.Application.Models.Commands;
using Media.Application.Models.Results;
using Media.Application.Ports.Persistence;
using Media.Application.Ports.Services;
using Media.Domain.Constants;
using Media.Domain.Entities;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.MediaAssets.RegisterMedia;

public sealed class RegisterMediaUseCase : IRegisterMediaUseCase
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IMediaOutboxWriter _mediaOutboxWriter;
    private readonly IPublicIdGenerator _publicIdGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;

    public RegisterMediaUseCase(
        IMediaAssetRepository mediaAssetRepository,
        IMediaUnitOfWork unitOfWork,
        IMediaOutboxWriter mediaOutboxWriter,
        IPublicIdGenerator publicIdGenerator,
        IDateTimeProvider dateTimeProvider,
        IRequestContext requestContext)
    {
        _mediaAssetRepository = mediaAssetRepository
            ?? throw new ArgumentNullException(nameof(mediaAssetRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _mediaOutboxWriter = mediaOutboxWriter
            ?? throw new ArgumentNullException(nameof(mediaOutboxWriter));

        _publicIdGenerator = publicIdGenerator
            ?? throw new ArgumentNullException(nameof(publicIdGenerator));

        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));

        _requestContext = requestContext
            ?? throw new ArgumentNullException(nameof(requestContext));
    }

    public async Task<Result<RegisterMediaResponse>> ExecuteAsync(
        RegisterMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            string publicId = _publicIdGenerator.NewId();
            DateTime nowUtc = _dateTimeProvider.UtcNow;
            long? actorUserId = _requestContext.CurrentUserId;

            if (actorUserId is null or <= 0)
            {
                return Result<RegisterMediaResponse>.Failure(MediaErrors.Actor.NotFound);
            }

            // Domain validation only. Persistence is done through command/proc.
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
                MediaAssetInsertResult insertResult =
                    await _mediaAssetRepository.InsertAsync(
                        new CreateMediaAssetCommand(
                            PublicId: mediaAsset.PublicId,
                            StorageProvider: mediaAsset.StorageProvider,
                            Url: mediaAsset.Url,
                            StoragePath: mediaAsset.StoragePath,
                            FileName: mediaAsset.FileName,
                            MediaType: mediaAsset.MediaType,
                            MimeType: mediaAsset.MimeType,
                            FileSizeBytes: mediaAsset.FileSizeBytes,
                            Width: mediaAsset.Width,
                            Height: mediaAsset.Height,
                            DurationSeconds: mediaAsset.DurationSeconds,
                            AltText: mediaAsset.AltText,
                            MetadataJson: mediaAsset.MetadataJson,
                            ContentHash: mediaAsset.ContentHash,
                            CreatedBy: actorUserId),
                        cancellationToken);

                await _mediaOutboxWriter.EnqueueMediaAssetRegisteredAsync(
                    unitOfWork: _unitOfWork,
                    mediaId: insertResult.MediaId,
                    mediaPublicId: mediaAsset.PublicId,
                    storageProvider: mediaAsset.StorageProvider,
                    url: mediaAsset.Url,
                    storagePath: mediaAsset.StoragePath,
                    fileName: mediaAsset.FileName,
                    mediaType: mediaAsset.MediaType,
                    mimeType: mediaAsset.MimeType,
                    fileSizeBytes: mediaAsset.FileSizeBytes,
                    width: mediaAsset.Width,
                    height: mediaAsset.Height,
                    durationSeconds: mediaAsset.DurationSeconds,
                    altText: mediaAsset.AltText,
                    actorUserId: actorUserId.Value,
                    version: insertResult.NewVersion,
                    registeredAtUtc: nowUtc,
                    correlationId: _requestContext.CorrelationId,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<RegisterMediaResponse>.Success(
                    new RegisterMediaResponse
                    {
                        MediaId = insertResult.MediaId,
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
                        CreatedBy = actorUserId,
                        Version = insertResult.NewVersion
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
            return Result<RegisterMediaResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<RegisterMediaResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.MEDIA_ASSET_PUBLIC_ID_REQUIRED" =>
                MediaErrors.MediaAsset.PublicIdRequired,

            "MEDIA.MEDIA_ASSET_PUBLIC_ID_INVALID" =>
                MediaErrors.MediaAsset.PublicIdInvalid,

            "MEDIA.MEDIA_ASSET_STORAGE_PROVIDER_REQUIRED" =>
                MediaErrors.MediaAsset.StorageProviderRequired,

            "MEDIA.MEDIA_ASSET_STORAGE_PROVIDER_TOO_LONG" =>
                MediaErrors.MediaAsset.StorageProviderTooLong,

            "MEDIA.MEDIA_ASSET_URL_REQUIRED" =>
                MediaErrors.MediaAsset.UrlRequired,

            "MEDIA.MEDIA_ASSET_URL_TOO_LONG" =>
                MediaErrors.MediaAsset.UrlTooLong,

            "MEDIA.MEDIA_ASSET_STORAGE_PATH_TOO_LONG" =>
                MediaErrors.MediaAsset.StoragePathTooLong,

            "MEDIA.TYPE_NOT_ALLOWED" =>
                MediaErrors.MediaAsset.TypeNotAllowed,

            "MEDIA.MEDIA_ASSET_MIME_TYPE_TOO_LONG" =>
                MediaErrors.MediaAsset.MimeTypeTooLong,

            "MEDIA.MEDIA_ASSET_FILE_SIZE_INVALID" =>
                MediaErrors.MediaAsset.FileSizeInvalid,

            "MEDIA.MEDIA_ASSET_DIMENSION_INVALID" =>
                MediaErrors.MediaAsset.DimensionInvalid,

            "MEDIA.MEDIA_ASSET_DURATION_INVALID" =>
                MediaErrors.MediaAsset.DurationInvalid,

            "MEDIA.MEDIA_ASSET_ALT_TEXT_TOO_LONG" =>
                MediaErrors.MediaAsset.AltTextTooLong,

            "MEDIA.MEDIA_ASSET_FILE_NAME_TOO_LONG" =>
                MediaErrors.MediaAsset.FileNameTooLong,

            _ => MediaErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "MEDIA.MEDIA_PUBLIC_ID_ALREADY_EXISTS" =>
                MediaErrors.MediaAsset.PublicIdAlreadyExists,

            "MEDIA.TYPE_NOT_ALLOWED" =>
                MediaErrors.MediaAsset.TypeNotAllowed,

            "MEDIA.MEDIA_ASSET_FILE_SIZE_INVALID" =>
                MediaErrors.MediaAsset.FileSizeInvalid,

            "MEDIA.MEDIA_ASSET_DIMENSION_INVALID" =>
                MediaErrors.MediaAsset.DimensionInvalid,

            "MEDIA.MEDIA_ASSET_DURATION_INVALID" =>
                MediaErrors.MediaAsset.DurationInvalid,

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
