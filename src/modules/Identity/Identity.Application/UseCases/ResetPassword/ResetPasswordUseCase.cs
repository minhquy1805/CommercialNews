using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Contracts.ResetPassword;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Application.Validation.ResetPassword;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Exceptions;

namespace Identity.Application.UseCases.ResetPassword;

public sealed class ResetPasswordUseCase : IResetPasswordUseCase
{
    private readonly ITokenHashProvider _tokenHashProvider;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IIdentityOutboxWriter _outboxWriter;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ResetPasswordUseCase(
        ITokenHashProvider tokenHashProvider,
        IPasswordHasher passwordHasher,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IUserAccountRepository userAccountRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IIdentityUnitOfWork unitOfWork,
        IIdentityOutboxWriter outboxWriter,
        IDateTimeProvider dateTimeProvider)
    {
        _tokenHashProvider = tokenHashProvider ?? throw new ArgumentNullException(nameof(tokenHashProvider));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _passwordResetTokenRepository = passwordResetTokenRepository ?? throw new ArgumentNullException(nameof(passwordResetTokenRepository));
        _userAccountRepository = userAccountRepository ?? throw new ArgumentNullException(nameof(userAccountRepository));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<ResetPasswordResponseDto>> ExecuteAsync(
        ResetPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = ResetPasswordValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<ResetPasswordResponseDto>.Failure(validationError);
        }

        try
        {
            DateTime nowUtc = _dateTimeProvider.UtcNow;
            byte[] tokenHash = _tokenHashProvider.Hash(request.Token.Trim());

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

            if (string.Equals(user.Status, UserAccountStatuses.Disabled, StringComparison.OrdinalIgnoreCase))
            {
                return Result<ResetPasswordResponseDto>.Failure(
                    IdentityErrors.Auth.AccountDisabled);
            }

            if (user.IsLockedAt(nowUtc))
            {
                return Result<ResetPasswordResponseDto>.Failure(
                    IdentityErrors.Auth.AccountLocked);
            }

            string newPasswordHash = _passwordHasher.Hash(request.NewPassword);
            resetToken.MarkUsed(nowUtc);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                bool markedUsed = await _passwordResetTokenRepository.MarkUsedAsync(
                    resetToken.ResetTokenId,
                    nowUtc,
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
                    nowUtc,
                    RefreshTokenRevokedReasons.PasswordReset,
                    cancellationToken);

                await _outboxWriter.EnqueuePasswordChangedAsync(
                    unitOfWork: _unitOfWork,
                    userId: user.UserId,
                    userPublicId: user.PublicId,
                    email: user.Email,
                    fullName: user.FullName,
                    reason: PasswordChangedReasons.ResetByUser,
                    occurredAtUtc: nowUtc,
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
            "IDENTITY.PASSWORD_RESET_TOKEN_EXPIRED" => IdentityErrors.PasswordReset.TokenExpired,
            _ => IdentityErrors.PasswordReset.ResetFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "IDENTITY.PASSWORD_RESET_TOKEN_NOT_FOUND" => IdentityErrors.PasswordReset.TokenNotFound,
            "IDENTITY.PASSWORD_RESET_TOKEN_ALREADY_USED" => IdentityErrors.PasswordReset.TokenAlreadyUsed,
            "IDENTITY.PASSWORD_RESET_TOKEN_REVOKED" => IdentityErrors.PasswordReset.TokenRevoked,
            _ => IdentityErrors.PasswordReset.ResetFailed
        };
    }
}
