using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;

namespace Identity.Application.UseCases.RefreshToken
{
    public sealed class RefreshTokenUseCase : IRefreshTokenUseCase
    {
        private readonly ITokenHashProvider _tokenHashProvider;
        private readonly IRawTokenGenerator _rawTokenGenerator;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IAccessTokenGenerator _accessTokenGenerator;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IRequestContext _requestContext;

        public RefreshTokenUseCase(
            ITokenHashProvider tokenHashProvider,
            IRawTokenGenerator rawTokenGenerator,
            IRefreshTokenRepository refreshTokenRepository,
            IUserAccountRepository userAccountRepository,
            IAccessTokenGenerator accessTokenGenerator,
            IDateTimeProvider dateTimeProvider,
            IRequestContext requestContext)
        {
            _tokenHashProvider = tokenHashProvider;
            _rawTokenGenerator = rawTokenGenerator;
            _refreshTokenRepository = refreshTokenRepository;
            _userAccountRepository = userAccountRepository;
            _accessTokenGenerator = accessTokenGenerator;
            _dateTimeProvider = dateTimeProvider;
            _requestContext = requestContext;
        }

        public async Task<Result<RefreshTokenResponseDto>> ExecuteAsync(
            RefreshTokenRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return Result<RefreshTokenResponseDto>.Failure(IdentityErrors.ValidationFailed);
            }

            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Result<RefreshTokenResponseDto>.Failure(IdentityErrors.Refresh.TokenHashRequired);
            }

            try
            {
                byte[] currentTokenHash = _tokenHashProvider.Hash(request.RefreshToken);

                string newRawRefreshToken = _rawTokenGenerator.Generate();
                byte[] newRefreshTokenHash = _tokenHashProvider.Hash(newRawRefreshToken);
                DateTime newRefreshTokenExpiresAtUtc = _dateTimeProvider.UtcNow.AddDays(7);

                long? userId = await _refreshTokenRepository.RotateAsync(
                    currentTokenHash: currentTokenHash,
                    newTokenHash: newRefreshTokenHash,
                    newExpiresAtUtc: newRefreshTokenExpiresAtUtc,
                    createdIp: _requestContext.IpAddress,
                    userAgent: _requestContext.UserAgent,
                    correlationId: _requestContext.CorrelationId,
                    cancellationToken: cancellationToken);

                if (userId is null)
                {
                    return Result<RefreshTokenResponseDto>.Failure(IdentityErrors.Refresh.TokenNotFound);
                }

                UserAccount? user = await _userAccountRepository.GetByIdAsync(
                    userId.Value,
                    cancellationToken);

                if (user is null)
                {
                    return Result<RefreshTokenResponseDto>.Failure(IdentityErrors.User.NotFound);
                }

                var accessToken = _accessTokenGenerator.Generate(user);

                return Result<RefreshTokenResponseDto>.Success(new RefreshTokenResponseDto
                {
                    UserId = userId.Value,
                    AccessToken = accessToken.AccessToken,
                    RefreshToken = newRawRefreshToken,
                    AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc,
                    RefreshTokenExpiresAtUtc = newRefreshTokenExpiresAtUtc
                });
            }
            catch (PersistenceException exception)
            {
                return Result<RefreshTokenResponseDto>.Failure(MapPersistenceException(exception));
            }
            catch (IdentityDomainException exception)
            {
                return Result<RefreshTokenResponseDto>.Failure(MapDomainException(exception));
            }
        }

        private static Error MapDomainException(IdentityDomainException exception)
        {
            return exception.Code switch
            {
                "IDENTITY.REFRESH_TOKEN_HASH_REQUIRED" => IdentityErrors.Refresh.TokenHashRequired,
                "IDENTITY.REFRESH_TOKEN_HASH_INVALID" => IdentityErrors.Refresh.TokenHashInvalid,
                "IDENTITY.REFRESH_INVALID_EXPIRES_AT" => IdentityErrors.Refresh.InvalidExpiresAt,
                "IDENTITY.REFRESH_CREATED_IP_TOO_LONG" => IdentityErrors.Refresh.CreatedIpTooLong,
                "IDENTITY.REFRESH_USER_AGENT_TOO_LONG" => IdentityErrors.Refresh.UserAgentTooLong,
                "IDENTITY.REFRESH_CORRELATION_ID_TOO_LONG" => IdentityErrors.Refresh.CorrelationIdTooLong,
                _ => IdentityErrors.ValidationFailed
            };
        }

        private static Error MapPersistenceException(PersistenceException exception)
        {
            return exception.Code switch
            {
                "IDENTITY.REFRESH_TOKEN_NOT_FOUND" => IdentityErrors.Refresh.TokenNotFound,
                "IDENTITY.REFRESH_TOKEN_REVOKED" => IdentityErrors.Refresh.TokenRevoked,
                "IDENTITY.REFRESH_TOKEN_EXPIRED" => IdentityErrors.Refresh.TokenExpired,
                "IDENTITY.REFRESH_TOKEN_REPLACED" => IdentityErrors.Refresh.TokenReplaced,
                "IDENTITY.REFRESH_ROTATION_CONFLICT" => IdentityErrors.Refresh.RotationConflict,
                "IDENTITY.REFRESH_REUSE_DETECTED" => IdentityErrors.RefreshReuseDetected,
                _ => IdentityErrors.ValidationFailed
            };
        }
    }
}