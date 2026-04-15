using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;

namespace Identity.Application.UseCases.ResetPassword
{
    public sealed class ResetPasswordUseCase : IResetPasswordUseCase
    {
        private readonly ITokenHashProvider _tokenHashProvider;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IIdentityUnitOfWork _unitOfWork;
        private readonly IIdentityNotificationOutboxWriter _notificationOutboxWriter;

        public ResetPasswordUseCase(
            ITokenHashProvider tokenHashProvider,
            IPasswordHasher passwordHasher,
            IPasswordResetTokenRepository passwordResetTokenRepository,
            IUserAccountRepository userAccountRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IIdentityUnitOfWork unitOfWork,
            IIdentityNotificationOutboxWriter notificationOutboxWriter)
        {
            _tokenHashProvider = tokenHashProvider;
            _passwordHasher = passwordHasher;
            _passwordResetTokenRepository = passwordResetTokenRepository;
            _userAccountRepository = userAccountRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _unitOfWork = unitOfWork;
            _notificationOutboxWriter = notificationOutboxWriter;
        }

        public async Task<Result<ResetPasswordResponseDto>> ExecuteAsync(
            ResetPasswordRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return Result<ResetPasswordResponseDto>.Failure(IdentityErrors.ValidationFailed);
            }

            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return Result<ResetPasswordResponseDto>.Failure(IdentityErrors.ValidationFailed);
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return Result<ResetPasswordResponseDto>.Failure(IdentityErrors.PasswordPolicyViolation);
            }

            if (request.NewPassword.Length < 8)
            {
                return Result<ResetPasswordResponseDto>.Failure(IdentityErrors.PasswordPolicyViolation);
            }

            try
            {
                byte[] tokenHash = _tokenHashProvider.Hash(request.Token);

                PasswordResetToken? resetToken =
                    await _passwordResetTokenRepository.GetActiveByTokenHashAsync(
                        tokenHash,
                        cancellationToken);

                if (resetToken is null)
                {
                    return Result<ResetPasswordResponseDto>.Failure(
                        IdentityErrors.PasswordReset.TokenNotFound);
                }

                UserAccount? user = await _userAccountRepository.GetByIdAsync(
                    resetToken.UserId,
                    cancellationToken);

                if (user is null)
                {
                    return Result<ResetPasswordResponseDto>.Failure(
                        IdentityErrors.User.NotFound);
                }

                string newPasswordHash = _passwordHasher.Hash(request.NewPassword);

                resetToken.MarkUsed(DateTime.UtcNow);

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    bool markedUsed = await _passwordResetTokenRepository.MarkUsedAsync(
                        resetToken.ResetTokenId,
                        cancellationToken);

                    if (!markedUsed)
                    {
                        await _unitOfWork.RollbackAsync(cancellationToken);
                        return Result<ResetPasswordResponseDto>.Failure(
                            IdentityErrors.PasswordReset.TokenAlreadyUsed);
                    }

                    bool passwordUpdated = await _userAccountRepository.UpdatePasswordAsync(
                        resetToken.UserId,
                        newPasswordHash,
                        cancellationToken);

                    if (!passwordUpdated)
                    {
                        await _unitOfWork.RollbackAsync(cancellationToken);
                        return Result<ResetPasswordResponseDto>.Failure(
                            IdentityErrors.User.NotFound);
                    }

                    await _refreshTokenRepository.RevokeAllActiveByUserIdAsync(
                        resetToken.UserId,
                        "PasswordReset",
                        cancellationToken);

                    await _notificationOutboxWriter.EnqueuePasswordChangedEmailAsync(
                        userId: user.UserId,
                        userPublicId: user.PublicId,
                        email: user.Email,
                        fullName: user.FullName,
                        occurredAtUtc: DateTime.UtcNow,
                        cancellationToken: cancellationToken);

                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result<ResetPasswordResponseDto>.Success(new ResetPasswordResponseDto
                    {
                        UserId = resetToken.UserId,
                        PasswordReset = true
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
                return Result<ResetPasswordResponseDto>.Failure(MapPersistenceException(exception));
            }
            catch (IdentityDomainException exception)
            {
                return Result<ResetPasswordResponseDto>.Failure(MapDomainException(exception));
            }
        }

        private static Error MapDomainException(IdentityDomainException exception)
        {
            return exception.Code switch
            {
                "IDENTITY.PASSWORD_RESET_TOKEN_ALREADY_USED" => IdentityErrors.PasswordReset.TokenAlreadyUsed,
                "IDENTITY.PASSWORD_RESET_TOKEN_REVOKED" => IdentityErrors.PasswordReset.TokenRevoked,
                "IDENTITY.PASSWORD_RESET_INVALID_USED_AT" => IdentityErrors.PasswordReset.InvalidUsedAt,
                "IDENTITY.PASSWORD_RESET_TOKEN_HASH_REQUIRED" => IdentityErrors.PasswordReset.TokenHashRequired,
                "IDENTITY.PASSWORD_RESET_TOKEN_HASH_INVALID" => IdentityErrors.PasswordReset.TokenHashInvalid,
                _ => IdentityErrors.ValidationFailed
            };
        }

        private static Error MapPersistenceException(PersistenceException exception)
        {
            return exception.Code switch
            {
                "IDENTITY.PASSWORD_RESET_TOKEN_NOT_FOUND" => IdentityErrors.PasswordReset.TokenNotFound,
                "IDENTITY.PASSWORD_RESET_TOKEN_ALREADY_USED" => IdentityErrors.PasswordReset.TokenAlreadyUsed,
                "IDENTITY.PASSWORD_RESET_TOKEN_REVOKED" => IdentityErrors.PasswordReset.TokenRevoked,
                _ => IdentityErrors.ValidationFailed
            };
        }
    }
}