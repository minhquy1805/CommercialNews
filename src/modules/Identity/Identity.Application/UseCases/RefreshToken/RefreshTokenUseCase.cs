using Identity.Application.Contracts.Ports;
using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.RefreshToken
{
    public sealed class RefreshTokenUseCase : IRefreshTokenUseCase
    {
        private readonly ITokenHashProvider _tokenHashProvider;
        private readonly IRawTokenGenerator _rawTokenGenerator;
        private readonly IRefreshTokenRotationService _refreshTokenRotationService;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IAccessTokenGenerator _accessTokenGenerator;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IRequestContext _requestContext;

        public RefreshTokenUseCase(
            ITokenHashProvider tokenHashProvider,
            IRawTokenGenerator rawTokenGenerator,
            IRefreshTokenRotationService refreshTokenRotationService,
            IUserAccountRepository userAccountRepository,
            IAccessTokenGenerator accessTokenGenerator,
            IDateTimeProvider dateTimeProvider,
            IRequestContext requestContext)
        {
            _tokenHashProvider = tokenHashProvider;
            _rawTokenGenerator = rawTokenGenerator;
            _refreshTokenRotationService = refreshTokenRotationService;
            _userAccountRepository = userAccountRepository;
            _accessTokenGenerator = accessTokenGenerator;
            _dateTimeProvider = dateTimeProvider;
            _requestContext = requestContext;
        }

        public async Task<RefreshTokenResponseDto> ExecuteAsync(
            RefreshTokenRequestDto request,
            CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            var currentTokenHash = _tokenHashProvider.Hash(request.RefreshToken);

            var newRawRefreshToken = _rawTokenGenerator.Generate();
            var newRefreshTokenHash = _tokenHashProvider.Hash(newRawRefreshToken);
            var newRefreshTokenExpiresAtUtc = _dateTimeProvider.UtcNow.AddDays(7);

            var userId = await _refreshTokenRotationService.RotateAsync(
                currentTokenHash: currentTokenHash,
                newTokenHash: newRefreshTokenHash,
                newExpiresAtUtc: newRefreshTokenExpiresAtUtc,
                createdIp: _requestContext.IpAddress,
                userAgent: _requestContext.UserAgent,
                correlationId: _requestContext.CorrelationId,
                cancellationToken: cancellationToken);

            var user = await _userAccountRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                throw new InvalidOperationException("User not found for rotated refresh token.");
            }

            var accessToken = _accessTokenGenerator.Generate(user);

            return new RefreshTokenResponseDto
            {
                UserId = userId,
                AccessToken = accessToken.AccessToken,
                RefreshToken = newRawRefreshToken,
                AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc,
                RefreshTokenExpiresAtUtc = newRefreshTokenExpiresAtUtc
            };
        }

        private static void ValidateRequest(RefreshTokenRequestDto request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                throw new ArgumentException("Refresh token is required.", nameof(request.RefreshToken));
            }
        }
    }

    
}