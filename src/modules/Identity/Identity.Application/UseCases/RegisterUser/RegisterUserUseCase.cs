using Identity.Application.Contracts.Dtos;
using Identity.Application.Contracts.Ports;
using Identity.Domain.Entities;
using Identity.Domain.Enums;

namespace Identity.Application.UseCases.RegisterUser
{
    public sealed class RegisterUserUseCase : IRegisterUserUseCase
    {
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IPublicIdGenerator _publicIdGenerator;
        private readonly IRawTokenGenerator _rawTokenGenerator;
        private readonly ITokenHashProvider _tokenHashProvider;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IIdentityEmailSender _identityEmailSender;
        private readonly IIdentityUnitOfWork _unitOfWork;

        public RegisterUserUseCase(
            IUserAccountRepository userAccountRepository,
            IEmailVerificationTokenRepository emailVerificationTokenRepository,
            IPasswordHasher passwordHasher,
            IPublicIdGenerator publicIdGenerator,
            IRawTokenGenerator rawTokenGenerator,
            ITokenHashProvider tokenHashProvider,
            IDateTimeProvider dateTimeProvider,
            IIdentityEmailSender identityEmailSender,
            IIdentityUnitOfWork unitOfWork)
        {
            _userAccountRepository = userAccountRepository;
            _emailVerificationTokenRepository = emailVerificationTokenRepository;
            _passwordHasher = passwordHasher;
            _publicIdGenerator = publicIdGenerator;
            _rawTokenGenerator = rawTokenGenerator;
            _tokenHashProvider = tokenHashProvider;
            _dateTimeProvider = dateTimeProvider;
            _identityEmailSender = identityEmailSender;
            _unitOfWork = unitOfWork;
        }

        public async Task<RegisterUserResponseDto> ExecuteAsync(
            RegisterUserRequestDto request,
            CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            var normalizedEmail = NormalizeEmail(request.Email);

            var existingUser = await _userAccountRepository.GetByEmailNormalizedAsync(
                normalizedEmail,
                cancellationToken);

            if (existingUser is not null)
            {
                throw new InvalidOperationException("An account with this email already exists.");
            }

            var nowUtc = _dateTimeProvider.UtcNow;
            var publicId = _publicIdGenerator.NewId();
            var passwordHash = _passwordHasher.Hash(request.Password);

            var user = new UserAccount(
                userId: 0,
                publicId: publicId,
                email: request.Email.Trim(),
                emailNormalized: normalizedEmail,
                passwordHash: passwordHash,
                fullName: string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim(),
                avatarUrl: null,
                isEmailVerified: false,
                emailVerifiedAt: null,
                status: UserAccountStatus.Active,
                lockedUntil: null,
                createdAt: nowUtc,
                updatedAt: nowUtc,
                lastLoginAt: null,
                version: 1);

            var rawVerificationToken = _rawTokenGenerator.Generate();
            var verificationTokenHash = _tokenHashProvider.Hash(rawVerificationToken);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var userId = await _userAccountRepository.InsertAsync(user, cancellationToken);

                var verificationToken = new EmailVerificationToken(
                    verificationTokenId: 0,
                    userId: userId,
                    tokenHash: verificationTokenHash,
                    expiresAt: nowUtc.AddHours(24),
                    usedAt: null,
                    createdAt: nowUtc,
                    createdIp: null,
                    correlationId: null);

                await _emailVerificationTokenRepository.InsertAsync(
                    verificationToken,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                await _identityEmailSender.SendVerificationEmailAsync(
                    user.Email,
                    user.PublicId,
                    rawVerificationToken,
                    cancellationToken);

                return new RegisterUserResponseDto
                {
                    UserId = userId,
                    PublicId = user.PublicId,
                    Email = user.Email,
                    RequiresEmailVerification = true
                };
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static void ValidateRequest(RegisterUserRequestDto request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                throw new ArgumentException("Email is required.", nameof(request.Email));
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("Password is required.", nameof(request.Password));
            }

            if (request.Password.Length < 8)
            {
                throw new ArgumentException("Password must be at least 8 characters long.", nameof(request.Password));
            }
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToUpperInvariant();
        }
    }
}
