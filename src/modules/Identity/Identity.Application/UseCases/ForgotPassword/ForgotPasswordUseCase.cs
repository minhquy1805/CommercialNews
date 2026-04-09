using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;

namespace Identity.Application.UseCases.ForgotPassword
{
    public sealed class ForgotPasswordUseCase : IForgotPasswordUseCase
    {
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly IRawTokenGenerator _rawTokenGenerator;
        private readonly ITokenHashProvider _tokenHashProvider;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IIdentityUnitOfWork _unitOfWork;
        private readonly IIdentityNotificationOutboxWriter _notificationOutboxWriter;

        public ForgotPasswordUseCase(
            IUserAccountRepository userAccountRepository,
            IPasswordResetTokenRepository passwordResetTokenRepository,
            IRawTokenGenerator rawTokenGenerator,
            ITokenHashProvider tokenHashProvider,
            IDateTimeProvider dateTimeProvider,
            IIdentityUnitOfWork unitOfWork,
            IIdentityNotificationOutboxWriter notificationOutboxWriter)
        {
            _userAccountRepository = userAccountRepository;
            _passwordResetTokenRepository = passwordResetTokenRepository;
            _rawTokenGenerator = rawTokenGenerator;
            _tokenHashProvider = tokenHashProvider;
            _dateTimeProvider = dateTimeProvider;
            _unitOfWork = unitOfWork;
            _notificationOutboxWriter = notificationOutboxWriter;
        }

        public async Task<Result<ForgotPasswordResponseDto>> ExecuteAsync(
            ForgotPasswordRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return Result<ForgotPasswordResponseDto>.Failure(IdentityErrors.ValidationFailed);
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Result<ForgotPasswordResponseDto>.Failure(IdentityErrors.User.EmailRequired);
            }

            if (request.Email.Trim().Length > 320)
            {
                return Result<ForgotPasswordResponseDto>.Failure(IdentityErrors.User.EmailTooLong);
            }

            try
            {
                string normalizedEmail = NormalizeEmail(request.Email);

                var user = await _userAccountRepository.GetByEmailNormalizedAsync(
                    normalizedEmail,
                    cancellationToken);

                if (user is null)
                {
                    return Result<ForgotPasswordResponseDto>.Success(BuildGenericResponse());
                }

                DateTime nowUtc = _dateTimeProvider.UtcNow;
                string rawResetToken = _rawTokenGenerator.Generate();
                byte[] resetTokenHash = _tokenHashProvider.Hash(rawResetToken);

                PasswordResetToken resetToken = PasswordResetToken.Create(
                    userId: user.UserId,
                    tokenHash: resetTokenHash,
                    createdAt: nowUtc,
                    expiresAt: nowUtc.AddHours(1),
                    createdIp: null,
                    correlationId: null);

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    await _passwordResetTokenRepository.RevokeActiveByUserIdAsync(
                        user.UserId,
                        cancellationToken);

                    await _passwordResetTokenRepository.InsertAsync(
                        resetToken,
                        cancellationToken);

                    await _notificationOutboxWriter.EnqueuePasswordResetEmailAsync(
                        userId: user.UserId,
                        userPublicId: user.PublicId,
                        email: user.Email,
                        fullName: user.FullName,
                        rawResetToken: rawResetToken,
                        occurredAtUtc: nowUtc,
                        cancellationToken: cancellationToken);

                    await _unitOfWork.CommitAsync(cancellationToken);

                    // DEV ONLY: output reset token for local workflow testing
                    Console.WriteLine($"[DEV][RESET] Email={user.Email}; PublicId={user.PublicId}; Token={rawResetToken}");

                    return Result<ForgotPasswordResponseDto>.Success(BuildGenericResponse());
                }
                catch
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (PersistenceException exception)
            {
                return Result<ForgotPasswordResponseDto>.Failure(MapPersistenceException(exception));
            }
            catch (IdentityDomainException exception)
            {
                return Result<ForgotPasswordResponseDto>.Failure(MapDomainException(exception));
            }
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToUpperInvariant();
        }

        private static ForgotPasswordResponseDto BuildGenericResponse()
        {
            return new ForgotPasswordResponseDto
            {
                Requested = true,
                Message = "If the account exists, a password reset email will be sent."
            };
        }

        private static Error MapDomainException(IdentityDomainException exception)
        {
            return exception.Code switch
            {
                "IDENTITY.PASSWORD_RESET_INVALID_USER_ID" => IdentityErrors.PasswordReset.InvalidUserId,
                "IDENTITY.PASSWORD_RESET_TOKEN_HASH_REQUIRED" => IdentityErrors.PasswordReset.TokenHashRequired,
                "IDENTITY.PASSWORD_RESET_TOKEN_HASH_INVALID" => IdentityErrors.PasswordReset.TokenHashInvalid,
                "IDENTITY.PASSWORD_RESET_INVALID_EXPIRES_AT" => IdentityErrors.PasswordReset.InvalidExpiresAt,
                "IDENTITY.PASSWORD_RESET_CREATED_IP_TOO_LONG" => IdentityErrors.PasswordReset.CreatedIpTooLong,
                "IDENTITY.PASSWORD_RESET_CORRELATION_ID_TOO_LONG" => IdentityErrors.PasswordReset.CorrelationIdTooLong,
                _ => IdentityErrors.ValidationFailed
            };
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