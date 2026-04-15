using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Domain.Enums;

namespace Identity.Application.UseCases.UpdateMyProfile
{
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
            _requestContext = requestContext;
            _userAccountRepository = userAccountRepository;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<UpdateMyProfileResponseDto>> ExecuteAsync(
            UpdateMyProfileRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return Result<UpdateMyProfileResponseDto>.Failure(IdentityErrors.ValidationFailed);
            }

            string? normalizedFullName;
            string? normalizedAvatarUrl;

            try
            {
                normalizedFullName = NormalizeOptional(request.FullName, 200);
                normalizedAvatarUrl = NormalizeOptional(request.AvatarUrl, 800);
            }
            catch (ArgumentException)
            {
                return Result<UpdateMyProfileResponseDto>.Failure(IdentityErrors.ValidationFailed);
            }

            long? currentUserId = _requestContext.CurrentUserId;
            if (currentUserId is null)
            {
                return Result<UpdateMyProfileResponseDto>.Failure(IdentityErrors.ValidationFailed);
            }

            try
            {
                var user = await _userAccountRepository.GetByIdAsync(
                    currentUserId.Value,
                    cancellationToken);

                if (user is null)
                {
                    return Result<UpdateMyProfileResponseDto>.Failure(IdentityErrors.User.NotFound);
                }

                if (user.Status == UserAccountStatus.Inactive)
                {
                    return Result<UpdateMyProfileResponseDto>.Failure(IdentityErrors.Auth.AccountInactive);
                }

                if (user.IsLockedAt(_dateTimeProvider.UtcNow))
                {
                    return Result<UpdateMyProfileResponseDto>.Failure(IdentityErrors.AccountLocked);
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
                        return Result<UpdateMyProfileResponseDto>.Failure(IdentityErrors.User.NotFound);
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
                    Status = updatedUser.Status.ToString(),
                    UpdatedAt = updatedUser.UpdatedAt
                });
            }
            catch (PersistenceException exception)
            {
                return Result<UpdateMyProfileResponseDto>.Failure(MapPersistenceException(exception));
            }
        }

        private static string? NormalizeOptional(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string trimmed = value.Trim();

            if (trimmed.Length > maxLength)
            {
                throw new ArgumentException($"Value must not exceed {maxLength} characters.");
            }

            return trimmed;
        }

        private static Error MapPersistenceException(PersistenceException exception)
        {
            return exception.Code switch
            {
                _ => IdentityErrors.ValidationFailed
            };
        }
    }
}