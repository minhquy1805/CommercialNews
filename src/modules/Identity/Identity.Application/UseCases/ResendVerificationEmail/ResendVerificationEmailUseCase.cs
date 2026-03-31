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

namespace Identity.Application.UseCases.ResendVerificationEmail
{
    public sealed class ResendVerificationEmailUseCase : IResendVerificationEmailUseCase
    {
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
        private readonly IRawTokenGenerator _rawTokenGenerator;
        private readonly ITokenHashProvider _tokenHashProvider;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IIdentityUnitOfWork _unitOfWork;

        public ResendVerificationEmailUseCase(
            IUserAccountRepository userAccountRepository,
            IEmailVerificationTokenRepository emailVerificationTokenRepository,
            IRawTokenGenerator rawTokenGenerator,
            ITokenHashProvider tokenHashProvider,
            IDateTimeProvider dateTimeProvider,
            IIdentityUnitOfWork unitOfWork)
        {
            _userAccountRepository = userAccountRepository;
            _emailVerificationTokenRepository = emailVerificationTokenRepository;
            _rawTokenGenerator = rawTokenGenerator;
            _tokenHashProvider = tokenHashProvider;
            _dateTimeProvider = dateTimeProvider;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<ResendVerificationEmailResponseDto>> ExecuteAsync(
            ResendVerificationEmailRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return Result<ResendVerificationEmailResponseDto>.Failure(IdentityErrors.ValidationFailed);
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Result<ResendVerificationEmailResponseDto>.Failure(IdentityErrors.User.EmailRequired);
            }

            if (request.Email.Trim().Length > 320)
            {
                return Result<ResendVerificationEmailResponseDto>.Failure(IdentityErrors.User.EmailTooLong);
            }

            try
            {
                string normalizedEmail = NormalizeEmail(request.Email);

                var user = await _userAccountRepository.GetByEmailNormalizedAsync(
                    normalizedEmail,
                    cancellationToken);

                if (user is null)
                {
                    return Result<ResendVerificationEmailResponseDto>.Success(BuildGenericResponse());
                }

                if (user.IsEmailVerified)
                {
                    return Result<ResendVerificationEmailResponseDto>.Success(BuildGenericResponse());
                }

                DateTime nowUtc = _dateTimeProvider.UtcNow;
                string rawVerificationToken = _rawTokenGenerator.Generate();
                byte[] verificationTokenHash = _tokenHashProvider.Hash(rawVerificationToken);

                EmailVerificationToken verificationToken = EmailVerificationToken.Create(
                    userId: user.UserId,
                    tokenHash: verificationTokenHash,
                    createdAt: nowUtc,
                    expiresAt: nowUtc.AddHours(24),
                    createdIp: null,
                    correlationId: null);

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    await _emailVerificationTokenRepository.InsertAsync(
                        verificationToken,
                        cancellationToken);

                    await _unitOfWork.CommitAsync(cancellationToken);

                    // DEV ONLY: output resent verification token for local workflow testing
                    Console.WriteLine($"[DEV][VERIFY-RESEND] Email={user.Email}; PublicId={user.PublicId}; Token={rawVerificationToken}");

                    return Result<ResendVerificationEmailResponseDto>.Success(BuildGenericResponse());
                }
                catch
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (PersistenceException exception)
            {
                return Result<ResendVerificationEmailResponseDto>.Failure(MapPersistenceException(exception));
            }
            catch (IdentityDomainException exception)
            {
                return Result<ResendVerificationEmailResponseDto>.Failure(MapDomainException(exception));
            }
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToUpperInvariant();
        }

        private static ResendVerificationEmailResponseDto BuildGenericResponse()
        {
            return new ResendVerificationEmailResponseDto
            {
                Requested = true,
                Message = "If the account exists and is not yet verified, a verification email will be sent."
            };
        }

        private static Error MapDomainException(IdentityDomainException exception)
        {
            return exception.Code switch
            {
                "IDENTITY.EMAIL_VERIFICATION_INVALID_USER_ID" => IdentityErrors.EmailVerification.InvalidUserId,
                "IDENTITY.EMAIL_VERIFICATION_TOKEN_HASH_REQUIRED" => IdentityErrors.EmailVerification.TokenHashRequired,
                "IDENTITY.EMAIL_VERIFICATION_TOKEN_HASH_INVALID" => IdentityErrors.EmailVerification.TokenHashInvalid,
                "IDENTITY.EMAIL_VERIFICATION_INVALID_EXPIRES_AT" => IdentityErrors.EmailVerification.InvalidExpiresAt,
                "IDENTITY.EMAIL_VERIFICATION_CREATED_IP_TOO_LONG" => IdentityErrors.EmailVerification.CreatedIpTooLong,
                "IDENTITY.EMAIL_VERIFICATION_CORRELATION_ID_TOO_LONG" => IdentityErrors.EmailVerification.CorrelationIdTooLong,
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