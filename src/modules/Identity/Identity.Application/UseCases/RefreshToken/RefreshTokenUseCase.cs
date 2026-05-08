using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Configuration;
using Identity.Application.Contracts.RefreshToken;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Application.Ports.Services.Models;
using Identity.Application.Validation.RefreshToken;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Exceptions;
using Microsoft.Extensions.Options;
using RefreshTokenEntity = Identity.Domain.Entities.RefreshToken;

namespace Identity.Application.UseCases.RefreshToken;

public sealed class RefreshTokenUseCase : IRefreshTokenUseCase
{
    private readonly ITokenHashProvider _tokenHashProvider;
    private readonly IRawTokenGenerator _rawTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;
    private readonly IdentityTokenOptions _tokenOptions;

    public RefreshTokenUseCase(
        ITokenHashProvider tokenHashProvider,
        IRawTokenGenerator rawTokenGenerator,
        IRefreshTokenRepository refreshTokenRepository,
        IUserAccountRepository userAccountRepository,
        IAccessTokenGenerator accessTokenGenerator,
        IDateTimeProvider dateTimeProvider,
        IRequestContext requestContext,
        IOptions<IdentityTokenOptions> tokenOptions)
    {
        _tokenHashProvider = tokenHashProvider ?? throw new ArgumentNullException(nameof(tokenHashProvider));
        _rawTokenGenerator = rawTokenGenerator ?? throw new ArgumentNullException(nameof(rawTokenGenerator));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _userAccountRepository = userAccountRepository ?? throw new ArgumentNullException(nameof(userAccountRepository));
        _accessTokenGenerator = accessTokenGenerator ?? throw new ArgumentNullException(nameof(accessTokenGenerator));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _tokenOptions = tokenOptions?.Value ?? throw new ArgumentNullException(nameof(tokenOptions));
    }

    public async Task<Result<RefreshTokenResponseDto>> ExecuteAsync(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = RefreshTokenValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<RefreshTokenResponseDto>.Failure(validationError);
        }

        try
        {
            DateTime nowUtc = _dateTimeProvider.UtcNow;

            byte[] currentTokenHash = _tokenHashProvider.Hash(request.RefreshToken.Trim());

            RefreshTokenEntity? currentRefreshToken = await _refreshTokenRepository.GetActiveByTokenHashAsync(
                currentTokenHash,
                cancellationToken: cancellationToken);

            if (currentRefreshToken is null)
            {
                return Result<RefreshTokenResponseDto>.Failure(IdentityErrors.Refresh.TokenNotFound);
            }

            UserAccount? user = await _userAccountRepository.GetByIdAsync(
                currentRefreshToken.UserId,
                cancellationToken);

            if (user is null)
            {
                return Result<RefreshTokenResponseDto>.Failure(IdentityErrors.User.NotFound);
            }

            if (user.IsLockedAt(nowUtc))
            {
                return Result<RefreshTokenResponseDto>.Failure(IdentityErrors.Auth.AccountLocked);
            }

            if (string.Equals(user.Status, UserAccountStatuses.Disabled, StringComparison.OrdinalIgnoreCase))
            {
                return Result<RefreshTokenResponseDto>.Failure(IdentityErrors.Auth.AccountDisabled);
            }

            if (!user.IsEmailVerified)
            {
                return Result<RefreshTokenResponseDto>.Failure(IdentityErrors.Auth.VerificationRequired);
            }

            string newRawRefreshToken = _rawTokenGenerator.Generate();
            byte[] newRefreshTokenHash = _tokenHashProvider.Hash(newRawRefreshToken);
            DateTime newRefreshTokenExpiresAtUtc = nowUtc.AddDays(_tokenOptions.RefreshTokenLifetimeDays);

            RefreshTokenRotateResult? rotateResult = await _refreshTokenRepository.RotateAsync(
                currentTokenHash: currentTokenHash,
                revokedAtUtc: nowUtc,
                revokedReason: RefreshTokenRevokedReasons.Rotated,
                newTokenHash: newRefreshTokenHash,
                newCreatedAtUtc: nowUtc,
                newExpiresAtUtc: newRefreshTokenExpiresAtUtc,
                createdIp: _requestContext.IpAddress,
                userAgent: _requestContext.UserAgent,
                correlationId: _requestContext.CorrelationId,
                cancellationToken: cancellationToken);

            if (rotateResult is null)
            {
                return Result<RefreshTokenResponseDto>.Failure(IdentityErrors.Refresh.RotationConflict);
            }

            AccessTokenResult accessToken = _accessTokenGenerator.Generate(user);

            return Result<RefreshTokenResponseDto>.Success(new RefreshTokenResponseDto
            {
                UserId = user.UserId,
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
        catch (IdentityDomainException)
        {
            return Result<RefreshTokenResponseDto>.Failure(IdentityErrors.ValidationFailed);
        }
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
            "IDENTITY.REFRESH_REUSE_DETECTED" => IdentityErrors.Refresh.ReuseDetected,
            _ => IdentityErrors.Refresh.RotationConflict
        };
    }
}
