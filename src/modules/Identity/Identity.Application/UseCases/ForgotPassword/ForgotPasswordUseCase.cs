using Identity.Application.Contracts.Dtos;
using Identity.Application.Contracts.Ports;
using Identity.Domain.Entities;

namespace Identity.Application.UseCases.ForgotPassword
{
    public sealed class ForgotPasswordUseCase : IForgotPasswordUseCase
    {
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly IRawTokenGenerator _rawTokenGenerator;
        private readonly ITokenHashProvider _tokenHashProvider;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IIdentityEmailSender _identityEmailSender;
        private readonly IIdentityUnitOfWork _unitOfWork;

        public ForgotPasswordUseCase(
            IUserAccountRepository userAccountRepository,
            IPasswordResetTokenRepository passwordResetTokenRepository,
            IRawTokenGenerator rawTokenGenerator,
            ITokenHashProvider tokenHashProvider,
            IDateTimeProvider dateTimeProvider,
            IIdentityEmailSender identityEmailSender,
            IIdentityUnitOfWork unitOfWork)
        {
            _userAccountRepository = userAccountRepository;
            _passwordResetTokenRepository = passwordResetTokenRepository;
            _rawTokenGenerator = rawTokenGenerator;
            _tokenHashProvider = tokenHashProvider;
            _dateTimeProvider = dateTimeProvider;
            _identityEmailSender = identityEmailSender;
            _unitOfWork = unitOfWork;
        }

        public async Task<ForgotPasswordResponseDto> ExecuteAsync(
            ForgotPasswordRequestDto request,
            CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            var normalizedEmail = NormalizeEmail(request.Email);
            var user = await _userAccountRepository.GetByEmailNormalizedAsync(
                normalizedEmail,
                cancellationToken);

            if (user is null)
            {
                return BuildGenericResponse();
            }

            var nowUtc = _dateTimeProvider.UtcNow;
            var rawResetToken = _rawTokenGenerator.Generate();
            var resetTokenHash = _tokenHashProvider.Hash(rawResetToken);

            var resetToken = new PasswordResetToken(
                resetTokenId: 0,
                userId: user.UserId,
                tokenHash: resetTokenHash,
                expiresAt: nowUtc.AddHours(1),
                usedAt: null,
                revokedAt: null,
                createdAt: nowUtc,
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

                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }

            await _identityEmailSender.SendResetPasswordEmailAsync(
                user.Email,
                user.PublicId,
                rawResetToken,
                cancellationToken);

            return BuildGenericResponse();
        }

        private static void ValidateRequest(ForgotPasswordRequestDto request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                throw new ArgumentException("Email is required.", nameof(request.Email));
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
    }
}
