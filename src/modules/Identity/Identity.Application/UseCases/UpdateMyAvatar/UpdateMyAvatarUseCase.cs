using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using CommercialNews.BuildingBlocks.Storage.Abstractions;
using CommercialNews.BuildingBlocks.Storage.Constants;
using CommercialNews.BuildingBlocks.Storage.Models;
using Identity.Application.Contracts.UpdateMyAvatar;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Application.Validation.UpdateMyAvatar;
using Identity.Domain.Entities;
using Identity.Domain.Enums;

namespace Identity.Application.UseCases.UpdateMyAvatar;

public sealed class UpdateMyAvatarUseCase : IUpdateMyAvatarUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIdentityOutboxWriter _outboxWriter;
    private readonly IFileStorageService _fileStorageService;

    public UpdateMyAvatarUseCase(
        IRequestContext requestContext,
        IUserAccountRepository userAccountRepository,
        IIdentityUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IIdentityOutboxWriter outboxWriter,
        IFileStorageService fileStorageService)
    {
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _userAccountRepository = userAccountRepository ?? throw new ArgumentNullException(nameof(userAccountRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
    }

    public async Task<Result<UpdateMyAvatarResponseDto>> ExecuteAsync(
        UpdateMyAvatarRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = UpdateMyAvatarValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<UpdateMyAvatarResponseDto>.Failure(validationError);
        }

        long? currentUserId = _requestContext.CurrentUserId;
        if (currentUserId is null)
        {
            return Result<UpdateMyAvatarResponseDto>.Failure(IdentityErrors.Profile.InvalidRequest);
        }

        try
        {
            UserAccount? user = await _userAccountRepository.GetByIdAsync(
                currentUserId.Value,
                cancellationToken);

            if (user is null)
            {
                return Result<UpdateMyAvatarResponseDto>.Failure(IdentityErrors.User.NotFound);
            }

            if (string.Equals(user.Status, UserAccountStatuses.Disabled, StringComparison.OrdinalIgnoreCase))
            {
                return Result<UpdateMyAvatarResponseDto>.Failure(IdentityErrors.Auth.AccountDisabled);
            }

            if (user.IsLockedAt(_dateTimeProvider.UtcNow))
            {
                return Result<UpdateMyAvatarResponseDto>.Failure(IdentityErrors.Auth.AccountLocked);
            }

            FileStorageUploadResult storageResult;

            try
            {
                ResetStreamPositionIfPossible(request.Content);

                storageResult = await _fileStorageService.UploadAsync(
                    new FileStorageUploadRequest
                    {
                        Content = request.Content,
                        OriginalFileName = request.OriginalFileName,
                        ContentType = request.ContentType,
                        Length = request.Length,
                        Purpose = FileStoragePurposes.IdentityAvatars,
                        Folder = user.PublicId,
                        PreferredFileNameWithoutExtension = $"avatar-{Guid.NewGuid():N}"
                    },
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                return Result<UpdateMyAvatarResponseDto>.Failure(IdentityErrors.Profile.AvatarUploadFailed);
            }

            if (storageResult.Url.Length > 800)
            {
                await TryDeleteUploadedFileAsync(storageResult, cancellationToken);
                return Result<UpdateMyAvatarResponseDto>.Failure(IdentityErrors.Profile.AvatarUrlTooLong);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            UserAccount? updatedUser;

            try
            {
                bool updated = await _userAccountRepository.UpdateAvatarAsync(
                    user.UserId,
                    storageResult.Url,
                    cancellationToken);

                if (!updated)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);
                    await TryDeleteUploadedFileAsync(storageResult, cancellationToken);
                    return Result<UpdateMyAvatarResponseDto>.Failure(IdentityErrors.Profile.UpdateFailed);
                }

                updatedUser = await _userAccountRepository.GetByIdAsync(
                    user.UserId,
                    cancellationToken);

                if (updatedUser is null)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);
                    await TryDeleteUploadedFileAsync(storageResult, cancellationToken);
                    return Result<UpdateMyAvatarResponseDto>.Failure(IdentityErrors.User.NotFound);
                }

                await _outboxWriter.EnqueueUserPublicProfileUpdatedAsync(
                    unitOfWork: _unitOfWork,
                    userId: updatedUser.UserId,
                    userPublicId: updatedUser.PublicId,
                    fullName: updatedUser.FullName,
                    avatarUrl: updatedUser.AvatarUrl,
                    version: updatedUser.Version,
                    updatedAtUtc: updatedUser.UpdatedAt,
                    correlationId: _requestContext.CorrelationId,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                await TryDeleteUploadedFileAsync(storageResult, cancellationToken);
                throw;
            }

            return Result<UpdateMyAvatarResponseDto>.Success(MapResponse(updatedUser));
        }
        catch (PersistenceException)
        {
            return Result<UpdateMyAvatarResponseDto>.Failure(IdentityErrors.Profile.UpdateFailed);
        }
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
            // Best-effort cleanup only; a later orphan-file cleanup job can handle misses.
        }
    }

    private static UpdateMyAvatarResponseDto MapResponse(
        UserAccount user)
    {
        return new UpdateMyAvatarResponseDto
        {
            UserId = user.UserId,
            PublicId = user.PublicId,
            Email = user.Email,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            IsEmailVerified = user.IsEmailVerified,
            Status = user.Status,
            UpdatedAt = user.UpdatedAt
        };
    }
}
