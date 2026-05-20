using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.Storage.Abstractions;
using CommercialNews.BuildingBlocks.Storage.Constants;
using CommercialNews.BuildingBlocks.Storage.Models;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;
using Media.Application.Errors;
using Media.Application.Ports.Services.Metadata;
using Media.Application.UseCases.MediaAssets.RegisterMedia;
using Media.Domain.Constants;

namespace Media.Application.UseCases.MediaAssets.UploadMedia;

public sealed class UploadMediaUseCase : IUploadMediaUseCase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IMediaFileMetadataReader _mediaFileMetadataReader;
    private readonly IRegisterMediaUseCase _registerMediaUseCase;

    public UploadMediaUseCase(
        IFileStorageService fileStorageService,
        IMediaFileMetadataReader mediaFileMetadataReader,
        IRegisterMediaUseCase registerMediaUseCase)
    {
        _fileStorageService = fileStorageService
            ?? throw new ArgumentNullException(nameof(fileStorageService));

        _mediaFileMetadataReader = mediaFileMetadataReader
            ?? throw new ArgumentNullException(nameof(mediaFileMetadataReader));

        _registerMediaUseCase = registerMediaUseCase
            ?? throw new ArgumentNullException(nameof(registerMediaUseCase));
    }

    public async Task<Result<UploadMediaResponse>> ExecuteAsync(
        UploadMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Result validationResult = ValidateRequest(request);

        if (validationResult.IsFailure)
        {
            return Result<UploadMediaResponse>.Failure(validationResult.Error!);
        }

        ResetStreamPositionIfPossible(request.Content);

        MediaFileMetadataResult metadata;

        try
        {
            metadata = await _mediaFileMetadataReader.ReadAsync(
                request.Content,
                request.ContentType,
                request.MediaType,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            metadata = MediaFileMetadataResult.Empty;
        }

        ResetStreamPositionIfPossible(request.Content);

        FileStorageUploadResult storageResult;

        try
        {
            storageResult = await _fileStorageService.UploadAsync(
                new FileStorageUploadRequest
                {
                    Content = request.Content,
                    OriginalFileName = request.OriginalFileName,
                    ContentType = request.ContentType,
                    Length = request.Length,
                    Purpose = FileStoragePurposes.MediaArticles,
                    Folder = request.Folder,
                    PreferredFileNameWithoutExtension =
                        request.PreferredFileNameWithoutExtension
                },
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return Result<UploadMediaResponse>.Failure(
                MediaErrors.ValidationFailed);
        }

        Result<RegisterMediaResponse> registerResult =
            await _registerMediaUseCase.ExecuteAsync(
                new RegisterMediaRequest
                {
                    StorageProvider = storageResult.StorageProvider,
                    Url = storageResult.Url,
                    StoragePath = storageResult.StoragePath,
                    FileName = storageResult.FileName,
                    MediaType = request.MediaType,
                    MimeType = storageResult.ContentType,
                    FileSizeBytes = storageResult.FileSizeBytes,
                    Width = metadata.Width,
                    Height = metadata.Height,
                    DurationSeconds = metadata.DurationSeconds,
                    AltText = request.AltText,
                    MetadataJson = request.MetadataJson,
                    ContentHash = storageResult.ContentHash
                },
                cancellationToken);

        if (registerResult.IsFailure)
        {
            await TryDeleteUploadedFileAsync(
                storageResult,
                cancellationToken);

            return Result<UploadMediaResponse>.Failure(registerResult.Error!);
        }

        RegisterMediaResponse value = registerResult.Value!;

        return Result<UploadMediaResponse>.Success(new UploadMediaResponse
        {
            MediaId = value.MediaId,
            PublicId = value.PublicId,
            StorageProvider = value.StorageProvider,
            Url = value.Url,
            StoragePath = value.StoragePath,
            FileName = value.FileName,
            OriginalFileName = storageResult.OriginalFileName,
            MediaType = value.MediaType,
            MimeType = value.MimeType,
            FileSizeBytes = value.FileSizeBytes,
            Width = value.Width,
            Height = value.Height,
            DurationSeconds = value.DurationSeconds,
            AltText = value.AltText,
            MetadataJson = value.MetadataJson,
            CreatedAt = value.CreatedAt,
            CreatedBy = value.CreatedBy,
            Version = value.Version
        });
    }

    private static Result ValidateRequest(
        UploadMediaRequest request)
    {
        if (request.Content == Stream.Null || !request.Content.CanRead)
        {
            return Result.Failure(MediaErrors.ValidationFailed);
        }

        if (request.Length <= 0)
        {
            return Result.Failure(MediaErrors.ValidationFailed);
        }

        if (string.IsNullOrWhiteSpace(request.OriginalFileName))
        {
            return Result.Failure(MediaErrors.ValidationFailed);
        }

        if (string.IsNullOrWhiteSpace(request.MediaType))
        {
            return Result.Failure(MediaErrors.MediaAsset.TypeNotAllowed);
        }

        if (!MediaTypes.IsValid(request.MediaType))
        {
            return Result.Failure(MediaErrors.MediaAsset.TypeNotAllowed);
        }

        return Result.Success();
    }

    private static void ResetStreamPositionIfPossible(
        Stream content)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
        }
    }

    private async Task TryDeleteUploadedFileAsync(
        FileStorageUploadResult storageResult,
        CancellationToken cancellationToken)
    {
        try
        {
            await _fileStorageService.DeleteAsync(
                new FileStorageDeleteRequest
                {
                    StorageProvider = storageResult.StorageProvider,
                    StoragePath = storageResult.StoragePath
                },
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Best-effort cleanup only.
            // The DB insert failed, but upload already succeeded.
            // Later we can add orphan-file cleanup job.
        }
    }
}