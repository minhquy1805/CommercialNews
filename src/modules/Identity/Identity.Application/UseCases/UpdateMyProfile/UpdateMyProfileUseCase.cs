using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Contracts.UpdateMyProfile;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Validation.UpdateMyProfile;
using Identity.Domain.Enums;

namespace Identity.Application.UseCases.UpdateMyProfile;

public sealed class UpdateMyProfileUseCase : IUpdateMyProfileUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateMyProfileUseCase(
        IRequestContext requestContext,
        IUserAccountRepository userAccountRepository,
        IIdentityUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _userAccountRepository = userAccountRepository ?? throw new ArgumentNullException(nameof(userAccountRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<UpdateMyProfileResponseDto>> ExecuteAsync(
        UpdateMyProfileRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = UpdateMyProfileValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<UpdateMyProfileResponseDto>.Failure(validationError);
        }

        long? currentUserId = _requestContext.CurrentUserId;
        if (currentUserId is null)
        {
            return Result<UpdateMyProfileResponseDto>.Failure(IdentityErrors.Profile.InvalidRequest);
        }

        string? normalizedFullName = UpdateMyProfileValidator.Normalize(request.FullName);
        string? normalizedAvatarUrl = UpdateMyProfileValidator.Normalize(request.AvatarUrl);

        try
        {
            var user = await _userAccountRepository.GetByIdAsync(
                currentUserId.Value,
                cancellationToken);

            if (user is null)
            {
                return Result<UpdateMyProfileResponseDto>.Failure(IdentityErrors.User.NotFound);
            }

            if (string.Equals(user.Status, UserAccountStatuses.Disabled, StringComparison.OrdinalIgnoreCase))
            {
                return Result<UpdateMyProfileResponseDto>.Failure(IdentityErrors.Auth.AccountDisabled);
            }

            if (user.IsLockedAt(_dateTimeProvider.UtcNow))
            {
                return Result<UpdateMyProfileResponseDto>.Failure(IdentityErrors.Auth.AccountLocked);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                bool updated = await _userAccountRepository.UpdateProfileAsync(
                    user.UserId,
                    normalizedFullName,
                    normalizedAvatarUrl,
                    cancellationToken);

                if (!updated)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);
                    return Result<UpdateMyProfileResponseDto>.Failure(IdentityErrors.Profile.UpdateFailed);
                }

                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }

            var updatedUser = await _userAccountRepository.GetByIdAsync(
                user.UserId,
                cancellationToken);

            if (updatedUser is null)
            {
                return Result<UpdateMyProfileResponseDto>.Failure(IdentityErrors.User.NotFound);
            }

            return Result<UpdateMyProfileResponseDto>.Success(new UpdateMyProfileResponseDto
            {
                UserId = updatedUser.UserId,
                PublicId = updatedUser.PublicId,
                Email = updatedUser.Email,
                FullName = updatedUser.FullName,
                AvatarUrl = updatedUser.AvatarUrl,
                IsEmailVerified = updatedUser.IsEmailVerified,
                Status = updatedUser.Status,
                UpdatedAt = updatedUser.UpdatedAt
            });
        }
        catch (PersistenceException)
        {
            return Result<UpdateMyProfileResponseDto>.Failure(IdentityErrors.Profile.UpdateFailed);
        }
    }
}