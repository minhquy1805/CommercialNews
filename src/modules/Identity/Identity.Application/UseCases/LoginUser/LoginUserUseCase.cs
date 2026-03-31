using CommercialNews.BuildingBlocks.Abstractions.Execution;
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
using Identity.Domain.Enums;
using RefreshTokenEntity = Identity.Domain.Entities.RefreshToken;

namespace Identity.Application.UseCases.LoginUser
{
    public sealed class LoginUserUseCase : ILoginUserUseCase
    {
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
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
            _passwordHasher = passwordHasher;
            _rawTokenGenerator = rawTokenGenerator;
            _tokenHashProvider = tokenHashProvider;
            _accessTokenGenerator = accessTokenGenerator;
            _dateTimeProvider = dateTimeProvider;
            _requestContext = requestContext;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<LoginUserResponseDto>> ExecuteAsync(
            LoginUserRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return Result<LoginUserResponseDto>.Failure(IdentityErrors.ValidationFailed);
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Result<LoginUserResponseDto>.Failure(IdentityErrors.User.EmailRequired);
            }

            if (request.Email.Trim().Length > 320)
            {
                return Result<LoginUserResponseDto>.Failure(IdentityErrors.User.EmailTooLong);
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return Result<LoginUserResponseDto>.Failure(IdentityErrors.ValidationFailed);
            }

            try
            {
                string normalizedEmail = NormalizeEmail(request.Email);

                UserAccount? user = await _userAccountRepository.GetByEmailNormalizedAsync(
                    normalizedEmail,
                    cancellationToken);

                if (user is null)
                {
                    return Result<LoginUserResponseDto>.Failure(IdentityErrors.InvalidCredentials);
                }

                if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
                {
                    return Result<LoginUserResponseDto>.Failure(IdentityErrors.InvalidCredentials);
                }

                if (user.Status == UserAccountStatus.Inactive)
                {
                    return Result<LoginUserResponseDto>.Failure(IdentityErrors.Auth.AccountInactive);
                }

                DateTime nowUtc = _dateTimeProvider.UtcNow;

                if (user.IsLockedAt(nowUtc))
                {
                    return Result<LoginUserResponseDto>.Failure(IdentityErrors.AccountLocked);
                }

                if (!user.IsEmailVerified)
                {
                    return Result<LoginUserResponseDto>.Failure(IdentityErrors.Auth.VerificationRequired);
                }

                var accessToken = _accessTokenGenerator.Generate(user);

                string rawRefreshToken = _rawTokenGenerator.Generate();
                byte[] refreshTokenHash = _tokenHashProvider.Hash(rawRefreshToken);
                DateTime refreshTokenExpiresAtUtc = nowUtc.AddDays(7);

                RefreshTokenEntity refreshToken = RefreshTokenEntity.Create(
                    userId: user.UserId,
                    tokenHash: refreshTokenHash,
                    createdAt: nowUtc,
                    expiresAt: refreshTokenExpiresAtUtc,
                    createdIp: _requestContext.IpAddress,
                    userAgent: _requestContext.UserAgent,
                    correlationId: _requestContext.CorrelationId);

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    await _refreshTokenRepository.InsertAsync(
                        refreshToken,
                        cancellationToken);

                    await _userAccountRepository.UpdateLastLoginAsync(
                        user.UserId,
                        cancellationToken);

                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result<LoginUserResponseDto>.Success(new LoginUserResponseDto
                    {
                        UserId = user.UserId,
                        PublicId = user.PublicId,
                        Email = user.Email,
                        AccessToken = accessToken.AccessToken,
                        RefreshToken = rawRefreshToken,
                        AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc,
                        RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc
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
                return Result<LoginUserResponseDto>.Failure(MapPersistenceException(exception));
            }
            catch (IdentityDomainException exception)
            {
                return Result<LoginUserResponseDto>.Failure(MapDomainException(exception));
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
                "IDENTITY.REFRESH_INVALID_USER_ID" => IdentityErrors.Refresh.InvalidUserId,
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
                _ => IdentityErrors.ValidationFailed
            };
        }
    }
}