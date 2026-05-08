using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Configuration;
using Identity.Application.Contracts.LoginUser;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Application.Ports.Services.Models;
using Identity.Application.Validation.LoginUser;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LoginHistoryEntity = Identity.Domain.Entities.LoginHistory;
using RefreshTokenEntity = Identity.Domain.Entities.RefreshToken;

namespace Identity.Application.UseCases.LoginUser;

public sealed class LoginUserUseCase : ILoginUserUseCase
{
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
    private readonly ILogger<LoginUserUseCase> _logger;
    private readonly IdentityTokenOptions _tokenOptions;

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
        IIdentityUnitOfWork unitOfWork,
        ILogger<LoginUserUseCase> logger,
        IOptions<IdentityTokenOptions> tokenOptions)
    {
        _userAccountRepository = userAccountRepository ?? throw new ArgumentNullException(nameof(userAccountRepository));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _loginHistoryRepository = loginHistoryRepository ?? throw new ArgumentNullException(nameof(loginHistoryRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _rawTokenGenerator = rawTokenGenerator ?? throw new ArgumentNullException(nameof(rawTokenGenerator));
        _tokenHashProvider = tokenHashProvider ?? throw new ArgumentNullException(nameof(tokenHashProvider));
        _accessTokenGenerator = accessTokenGenerator ?? throw new ArgumentNullException(nameof(accessTokenGenerator));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenOptions = tokenOptions?.Value ?? throw new ArgumentNullException(nameof(tokenOptions));
    }

    public async Task<Result<LoginUserResponseDto>> ExecuteAsync(
        LoginUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = LoginUserValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<LoginUserResponseDto>.Failure(validationError);
        }

        try
        {
            string normalizedEmail = NormalizeEmail(request.Email);
            DateTime nowUtc = _dateTimeProvider.UtcNow;

            UserAccount? user = await _userAccountRepository.GetByEmailNormalizedAsync(
                normalizedEmail,
                cancellationToken);

            if (user is null)
            {
                await TryRecordLoginFailureAsync(
                    user: null,
                    normalizedEmail: normalizedEmail,
                    failureReason: LoginFailureReasons.InvalidCredentials,
                    attemptedAtUtc: nowUtc,
                    cancellationToken: cancellationToken);

                return Result<LoginUserResponseDto>.Failure(IdentityErrors.InvalidCredentials);
            }

            if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                await TryRecordLoginFailureAsync(
                    user: user,
                    normalizedEmail: normalizedEmail,
                    failureReason: LoginFailureReasons.InvalidCredentials,
                    attemptedAtUtc: nowUtc,
                    cancellationToken: cancellationToken);

                return Result<LoginUserResponseDto>.Failure(IdentityErrors.InvalidCredentials);
            }

            if (user.IsLockedAt(nowUtc))
            {
                await TryRecordLoginFailureAsync(
                    user: user,
                    normalizedEmail: normalizedEmail,
                    failureReason: LoginFailureReasons.AccountLocked,
                    attemptedAtUtc: nowUtc,
                    cancellationToken: cancellationToken);

                return Result<LoginUserResponseDto>.Failure(IdentityErrors.Auth.AccountLocked);
            }

            if (string.Equals(user.Status, UserAccountStatuses.Disabled, StringComparison.OrdinalIgnoreCase))
            {
                await TryRecordLoginFailureAsync(
                    user: user,
                    normalizedEmail: normalizedEmail,
                    failureReason: LoginFailureReasons.AccountDisabled,
                    attemptedAtUtc: nowUtc,
                    cancellationToken: cancellationToken);

                return Result<LoginUserResponseDto>.Failure(IdentityErrors.Auth.AccountDisabled);
            }

            if (!user.IsEmailVerified)
            {
                await TryRecordLoginFailureAsync(
                    user: user,
                    normalizedEmail: normalizedEmail,
                    failureReason: LoginFailureReasons.AccountUnverified,
                    attemptedAtUtc: nowUtc,
                    cancellationToken: cancellationToken);

                return Result<LoginUserResponseDto>.Failure(IdentityErrors.Auth.VerificationRequired);
            }

            AccessTokenResult accessToken = _accessTokenGenerator.Generate(user);

            string rawRefreshToken = _rawTokenGenerator.Generate();
            byte[] refreshTokenHash = _tokenHashProvider.Hash(rawRefreshToken);
            DateTime refreshTokenExpiresAtUtc = nowUtc.AddDays(_tokenOptions.RefreshTokenLifetimeDays);

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
                    nowUtc,
                    cancellationToken);

                await RecordLoginSuccessAsync(
                    user,
                    normalizedEmail,
                    nowUtc,
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
        catch (IdentityDomainException)
        {
            return Result<LoginUserResponseDto>.Failure(IdentityErrors.ValidationFailed);
        }
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private async Task RecordLoginFailureAsync(
        UserAccount? user,
        string normalizedEmail,
        string failureReason,
        DateTime attemptedAtUtc,
        CancellationToken cancellationToken)
    {
        LoginHistoryEntity loginHistory = LoginHistoryEntity.CreateFailure(
            userId: user?.UserId,
            emailNormalizedAttempted: normalizedEmail,
            failureReason: failureReason,
            attemptedAt: attemptedAtUtc,
            ipAddress: _requestContext.IpAddress,
            userAgent: _requestContext.UserAgent,
            correlationId: _requestContext.CorrelationId);

        await _loginHistoryRepository.InsertAsync(
            loginHistory,
            cancellationToken);
    }

    private async Task TryRecordLoginFailureAsync(
        UserAccount? user,
        string normalizedEmail,
        string failureReason,
        DateTime attemptedAtUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            await RecordLoginFailureAsync(
                user,
                normalizedEmail,
                failureReason,
                attemptedAtUtc,
                cancellationToken);
        }
        catch (PersistenceException exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to record failed-login history. UserId: {UserId}; FailureReason: {FailureReason}; AttemptedAtUtc: {AttemptedAtUtc}; CorrelationId: {CorrelationId}",
                user?.UserId,
                failureReason,
                attemptedAtUtc,
                _requestContext.CorrelationId);
        }
    }

    private async Task RecordLoginSuccessAsync(
        UserAccount user,
        string normalizedEmail,
        DateTime attemptedAtUtc,
        CancellationToken cancellationToken)
    {
        LoginHistoryEntity loginHistory = LoginHistoryEntity.CreateSuccess(
            userId: user.UserId,
            emailNormalizedAttempted: normalizedEmail,
            attemptedAt: attemptedAtUtc,
            ipAddress: _requestContext.IpAddress,
            userAgent: _requestContext.UserAgent,
            correlationId: _requestContext.CorrelationId);

        await _loginHistoryRepository.InsertAsync(
            loginHistory,
            cancellationToken);
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            _ => IdentityErrors.ValidationFailed
        };
    }
}
