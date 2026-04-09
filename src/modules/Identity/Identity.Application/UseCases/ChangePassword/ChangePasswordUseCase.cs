using CommercialNews.BuildingBlocks.Abstractions.Execution;
using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Domain.Enums;

namespace Identity.Application.UseCases.ChangePassword
{
    public sealed class ChangePasswordUseCase : IChangePasswordUseCase
    {
        private readonly IRequestContext _requestContext;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IIdentityUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IIdentityNotificationOutboxWriter _notificationOutboxWriter;

        public ChangePasswordUseCase(
            IRequestContext requestContext,
            IUserAccountRepository userAccountRepository,
            IPasswordHasher passwordHasher,
            IRefreshTokenRepository refreshTokenRepository,
            IIdentityUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider,
            IIdentityNotificationOutboxWriter notificationOutboxWriter)
        {
            _requestContext = requestContext;
            _userAccountRepository = userAccountRepository;
            _passwordHasher = passwordHasher;
            _refreshTokenRepository = refreshTokenRepository;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
            _notificationOutboxWriter = notificationOutboxWriter;
        }

        public async Task<Result<ChangePasswordResponseDto>> ExecuteAsync(
            ChangePasswordRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return Result<ChangePasswordResponseDto>.Failure(IdentityErrors.ValidationFailed);
            }

            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            {
                return Result<ChangePasswordResponseDto>.Failure(IdentityErrors.ValidationFailed);
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return Result<ChangePasswordResponseDto>.Failure(IdentityErrors.PasswordPolicyViolation);
            }

            if (request.NewPassword.Length < 8)
            {
                return Result<ChangePasswordResponseDto>.Failure(IdentityErrors.PasswordPolicyViolation);
            }

            long? currentUserId = _requestContext.CurrentUserId;
            if (currentUserId is null)
            {
                return Result<ChangePasswordResponseDto>.Failure(IdentityErrors.Auth.LogoutFailed);
            }

            try
            {
                var user = await _userAccountRepository.GetByIdAsync(
                    currentUserId.Value,
                    cancellationToken);

                if (user is null)
                {
                    return Result<ChangePasswordResponseDto>.Failure(IdentityErrors.User.NotFound);
                }

                if (!user.IsEmailVerified)
                {
                    return Result<ChangePasswordResponseDto>.Failure(IdentityErrors.Auth.VerificationRequired);
                }

                if (user.Status == UserAccountStatus.Inactive)
                {
                    return Result<ChangePasswordResponseDto>.Failure(IdentityErrors.Auth.AccountInactive);
                }

                if (user.IsLockedAt(_dateTimeProvider.UtcNow))
                {
                    return Result<ChangePasswordResponseDto>.Failure(IdentityErrors.AccountLocked);
                }

                if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
                {
                    return Result<ChangePasswordResponseDto>.Failure(IdentityErrors.InvalidCredentials);
                }

                if (request.CurrentPassword == request.NewPassword)
                {
                    return Result<ChangePasswordResponseDto>.Failure(IdentityErrors.PasswordPolicyViolation);
                }

                string newPasswordHash = _passwordHasher.Hash(request.NewPassword);
                DateTime nowUtc = _dateTimeProvider.UtcNow;

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    bool updated = await _userAccountRepository.UpdatePasswordAsync(
                        user.UserId,
                        newPasswordHash,
                        cancellationToken);

                    if (!updated)
                    {
                        await _unitOfWork.RollbackAsync(cancellationToken);
                        return Result<ChangePasswordResponseDto>.Failure(IdentityErrors.User.NotFound);
                    }

                    await _refreshTokenRepository.RevokeAllActiveByUserIdAsync(
                        user.UserId,
                        "PasswordChanged",
                        cancellationToken);

                    await _notificationOutboxWriter.EnqueuePasswordChangedEmailAsync(
                        userId: user.UserId,
                        userPublicId: user.PublicId,
                        email: user.Email,
                        fullName: user.FullName,
                        occurredAtUtc: nowUtc,
                        cancellationToken: cancellationToken);

                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result<ChangePasswordResponseDto>.Success(new ChangePasswordResponseDto
                    {
                        UserId = user.UserId,
                        PasswordChanged = true
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
                return Result<ChangePasswordResponseDto>.Failure(MapPersistenceException(exception));
            }
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