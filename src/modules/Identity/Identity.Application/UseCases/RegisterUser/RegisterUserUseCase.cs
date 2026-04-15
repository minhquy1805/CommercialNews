using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;

namespace Identity.Application.UseCases.RegisterUser
{
    public sealed class RegisterUserUseCase : IRegisterUserUseCase
    {
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
        private readonly IIdentityUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IPublicIdGenerator _publicIdGenerator;
        private readonly IRawTokenGenerator _rawTokenGenerator;
        private readonly ITokenHashProvider _tokenHashProvider;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IIdentityNotificationOutboxWriter _notificationOutboxWriter;

        public RegisterUserUseCase(
            IUserAccountRepository userAccountRepository,
            IEmailVerificationTokenRepository emailVerificationTokenRepository,
            IIdentityUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IPublicIdGenerator publicIdGenerator,
            IRawTokenGenerator rawTokenGenerator,
            ITokenHashProvider tokenHashProvider,
            IDateTimeProvider dateTimeProvider,
            IIdentityNotificationOutboxWriter notificationOutboxWriter)
        {
            _userAccountRepository = userAccountRepository;
            _emailVerificationTokenRepository = emailVerificationTokenRepository;
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _publicIdGenerator = publicIdGenerator;
            _rawTokenGenerator = rawTokenGenerator;
            _tokenHashProvider = tokenHashProvider;
            _dateTimeProvider = dateTimeProvider;
            _notificationOutboxWriter = notificationOutboxWriter;
        }

        public async Task<Result<RegisterUserResponseDto>> ExecuteAsync(
            RegisterUserRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return Result<RegisterUserResponseDto>.Failure(IdentityErrors.ValidationFailed);
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Result<RegisterUserResponseDto>.Failure(IdentityErrors.User.EmailRequired);
            }

            if (request.Email.Trim().Length > 320)
            {
                return Result<RegisterUserResponseDto>.Failure(IdentityErrors.User.EmailTooLong);
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return Result<RegisterUserResponseDto>.Failure(IdentityErrors.ValidationFailed);
            }

            if (request.Password.Length < 8)
            {
                return Result<RegisterUserResponseDto>.Failure(IdentityErrors.PasswordPolicyViolation);
            }

            if (!string.IsNullOrWhiteSpace(request.FullName) &&
                request.FullName.Trim().Length > 200)
            {
                return Result<RegisterUserResponseDto>.Failure(IdentityErrors.User.FullNameTooLong);
            }

            try
            {
                string normalizedEmail = NormalizeEmail(request.Email);

                UserAccount? existingUser = await _userAccountRepository.GetByEmailNormalizedAsync(
                    normalizedEmail,
                    cancellationToken);

                if (existingUser is not null)
                {
                    return Result<RegisterUserResponseDto>.Failure(IdentityErrors.EmailAlreadyExists);
                }

                DateTime nowUtc = _dateTimeProvider.UtcNow;
                string publicId = _publicIdGenerator.NewId();
                string passwordHash = _passwordHasher.Hash(request.Password);

                UserAccount user = UserAccount.Create(
                    publicId: publicId,
                    email: request.Email.Trim(),
                    emailNormalized: normalizedEmail,
                    passwordHash: passwordHash,
                    fullName: request.FullName,
                    avatarUrl: null,
                    nowUtc: nowUtc);

                string rawVerificationToken = _rawTokenGenerator.Generate();
                byte[] verificationTokenHash = _tokenHashProvider.Hash(rawVerificationToken);

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    long userId = await _userAccountRepository.InsertAsync(
                        user,
                        cancellationToken);

                    EmailVerificationToken verificationToken = EmailVerificationToken.Create(
                        userId: userId,
                        tokenHash: verificationTokenHash,
                        createdAt: nowUtc,
                        expiresAt: nowUtc.AddHours(24),
                        createdIp: null,
                        correlationId: null);

                    await _emailVerificationTokenRepository.InsertAsync(
                        verificationToken,
                        cancellationToken);

                    // Important:
                    // Phase 1 writes an outbox message here so Notifications worker can
                    // send the verification email asynchronously after the identity truth commits.
                    await _notificationOutboxWriter.EnqueueVerificationEmailAsync(
                        userId: userId,
                        userPublicId: user.PublicId,
                        email: user.Email,
                        fullName: user.FullName,
                        rawVerificationToken: rawVerificationToken,
                        occurredAtUtc: nowUtc,
                        cancellationToken: cancellationToken);

                    await _unitOfWork.CommitAsync(cancellationToken);

                    // DEV ONLY: output verification token for local workflow testing
                    Console.WriteLine($"[DEV][VERIFY] Email={user.Email}; PublicId={user.PublicId}; Token={rawVerificationToken}");

                    return Result<RegisterUserResponseDto>.Success(new RegisterUserResponseDto
                    {
                        UserId = userId,
                        PublicId = user.PublicId,
                        Email = user.Email,
                        RequiresEmailVerification = true
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
                return Result<RegisterUserResponseDto>.Failure(MapPersistenceException(exception));
            }
            catch (IdentityDomainException exception)
            {
                return Result<RegisterUserResponseDto>.Failure(MapDomainException(exception));
            }
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToUpperInvariant();
        }

        private static Error MapDomainException(IdentityDomainException exception)
        {
            return exception.Code switch
            {
                "IDENTITY.USER_PUBLIC_ID_REQUIRED" => IdentityErrors.User.PublicIdRequired,
                "IDENTITY.USER_PUBLIC_ID_INVALID" => IdentityErrors.User.PublicIdInvalid,
                "IDENTITY.USER_EMAIL_REQUIRED" => IdentityErrors.User.EmailRequired,
                "IDENTITY.USER_EMAIL_TOO_LONG" => IdentityErrors.User.EmailTooLong,
                "IDENTITY.USER_EMAIL_NORMALIZED_REQUIRED" => IdentityErrors.User.EmailNormalizedRequired,
                "IDENTITY.USER_EMAIL_NORMALIZED_TOO_LONG" => IdentityErrors.User.EmailNormalizedTooLong,
                "IDENTITY.USER_PASSWORD_HASH_REQUIRED" => IdentityErrors.User.PasswordHashRequired,
                "IDENTITY.USER_PASSWORD_HASH_TOO_LONG" => IdentityErrors.User.PasswordHashTooLong,
                "IDENTITY.USER_FULL_NAME_TOO_LONG" => IdentityErrors.User.FullNameTooLong,
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
                "IDENTITY.EMAIL_EXISTS" => IdentityErrors.EmailAlreadyExists,
                _ => IdentityErrors.ValidationFailed
            };
        }
    }
}