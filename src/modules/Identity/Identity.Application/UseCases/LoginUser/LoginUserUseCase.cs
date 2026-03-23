using Identity.Application.Contracts.Ports;
using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using RefreshTokenEntity = Identity.Domain.Entities.RefreshToken;

namespace Identity.Application.UseCases.LoginUser
{
    public sealed class LoginUserUseCase : ILoginUserUseCase
    {
        private const string FailureInvalidCredentials = "InvalidCredentials";
        private const string FailureInactive = "Inactive";
        private const string FailureLocked = "Locked";
        private const string FailureEmailNotVerified = "EmailNotVerified";

        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ILoginHistoryRepository _loginHistoryRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IRawTokenGenerator _rawTokenGenerator;
        private readonly ITokenHashProvider _tokenHashProvider;
        private readonly IAccessTokenGenerator _accessTokenGenerator;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IRequestContext _requestContext;
        private readonly IIdentityUnitOfWork _unitOfWork;

        public LoginUserUseCase(
            IUserAccountRepository userAccountRepository,
            IRefreshTokenRepository refreshTokenRepository,
            ILoginHistoryRepository loginHistoryRepository,
            IPasswordHasher passwordHasher,
            IRawTokenGenerator rawTokenGenerator,
            ITokenHashProvider tokenHashProvider,
            IAccessTokenGenerator accessTokenGenerator,
            IDateTimeProvider dateTimeProvider,
            IRequestContext requestContext,
            IIdentityUnitOfWork unitOfWork)
        {
            _userAccountRepository = userAccountRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _loginHistoryRepository = loginHistoryRepository;
            _passwordHasher = passwordHasher;
            _rawTokenGenerator = rawTokenGenerator;
            _tokenHashProvider = tokenHashProvider;
            _accessTokenGenerator = accessTokenGenerator;
            _dateTimeProvider = dateTimeProvider;
            _requestContext = requestContext;
            _unitOfWork = unitOfWork;
        }

        public async Task<LoginUserResponseDto> ExecuteAsync(
            LoginUserRequestDto request,
            CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            var normalizedEmail = NormalizeEmail(request.Email);
            var user = await _userAccountRepository.GetByEmailNormalizedAsync(
                normalizedEmail,
                cancellationToken);

            if (user is null)
            {
                await WriteLoginHistoryAsync(
                    userId: null,
                    emailNormalizedAttempted: normalizedEmail,
                    succeeded: false,
                    failureReason: FailureInvalidCredentials,
                    cancellationToken);

                throw new InvalidOperationException("Invalid email or password.");
            }

            if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                await WriteLoginHistoryAsync(
                    userId: user.UserId,
                    emailNormalizedAttempted: normalizedEmail,
                    succeeded: false,
                    failureReason: FailureInvalidCredentials,
                    cancellationToken);

                throw new InvalidOperationException("Invalid email or password.");
            }

            if (user.Status == UserAccountStatus.Inactive)
            {
                await WriteLoginHistoryAsync(
                    userId: user.UserId,
                    emailNormalizedAttempted: normalizedEmail,
                    succeeded: false,
                    failureReason: FailureInactive,
                    cancellationToken);

                throw new InvalidOperationException("Account is inactive.");
            }

            if (user.IsLockedAt(_dateTimeProvider.UtcNow))
            {
                await WriteLoginHistoryAsync(
                    userId: user.UserId,
                    emailNormalizedAttempted: normalizedEmail,
                    succeeded: false,
                    failureReason: FailureLocked,
                    cancellationToken);

                throw new InvalidOperationException("Account is locked.");
            }

            if (!user.IsEmailVerified)
            {
                await WriteLoginHistoryAsync(
                    userId: user.UserId,
                    emailNormalizedAttempted: normalizedEmail,
                    succeeded: false,
                    failureReason: FailureEmailNotVerified,
                    cancellationToken);

                throw new InvalidOperationException("Email is not verified.");
            }

            var nowUtc = _dateTimeProvider.UtcNow;
            var accessToken = _accessTokenGenerator.Generate(user);

            var rawRefreshToken = _rawTokenGenerator.Generate();
            var refreshTokenHash = _tokenHashProvider.Hash(rawRefreshToken);
            var refreshTokenExpiresAtUtc = nowUtc.AddDays(7);

            var refreshToken = new RefreshTokenEntity(
                refreshTokenId: 0,
                userId: user.UserId,
                tokenHash: refreshTokenHash,
                createdAt: nowUtc,
                expiresAt: refreshTokenExpiresAtUtc,
                revokedAt: null,
                revokedReason: null,
                replacedByTokenHash: null,
                createdIp: _requestContext.IpAddress,
                userAgent: _requestContext.UserAgent,
                correlationId: _requestContext.CorrelationId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                await _refreshTokenRepository.InsertAsync(refreshToken, cancellationToken);
                await _userAccountRepository.UpdateLastLoginAsync(user.UserId, cancellationToken);

                await _loginHistoryRepository.InsertAsync(
                    userId: user.UserId,
                    emailNormalizedAttempted: normalizedEmail,
                    succeeded: true,
                    failureReason: null,
                    ipAddress: _requestContext.IpAddress,
                    userAgent: _requestContext.UserAgent,
                    correlationId: _requestContext.CorrelationId,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }

            return new LoginUserResponseDto
            {
                UserId = user.UserId,
                PublicId = user.PublicId,
                Email = user.Email,
                AccessToken = accessToken.AccessToken,
                RefreshToken = rawRefreshToken,
                AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc,
                RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc
            };
        }

        private async Task WriteLoginHistoryAsync(
            long? userId,
            string? emailNormalizedAttempted,
            bool succeeded,
            string? failureReason,
            CancellationToken cancellationToken)
        {
            await _loginHistoryRepository.InsertAsync(
                userId,
                emailNormalizedAttempted,
                succeeded,
                failureReason,
                _requestContext.IpAddress,
                _requestContext.UserAgent,
                _requestContext.CorrelationId,
                cancellationToken);
        }

        private static void ValidateRequest(LoginUserRequestDto request)
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
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToUpperInvariant();
        }
    }
}
