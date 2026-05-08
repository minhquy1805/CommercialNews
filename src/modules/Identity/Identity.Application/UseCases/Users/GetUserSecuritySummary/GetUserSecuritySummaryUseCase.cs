using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Contracts.Users.GetUserSecuritySummary;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Validation.Users.GetUserSecuritySummary;
using LoginHistoryEntity = Identity.Domain.Entities.LoginHistory;
using PasswordResetTokenEntity = Identity.Domain.Entities.PasswordResetToken;
using RefreshTokenEntity = Identity.Domain.Entities.RefreshToken;

namespace Identity.Application.UseCases.Users.GetUserSecuritySummary;

public sealed class GetUserSecuritySummaryUseCase : IGetUserSecuritySummaryUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILoginHistoryRepository _loginHistoryRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetUserSecuritySummaryUseCase(
        IRequestContext requestContext,
        IUserAccountRepository userAccountRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ILoginHistoryRepository loginHistoryRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _userAccountRepository = userAccountRepository ?? throw new ArgumentNullException(nameof(userAccountRepository));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _loginHistoryRepository = loginHistoryRepository ?? throw new ArgumentNullException(nameof(loginHistoryRepository));
        _passwordResetTokenRepository = passwordResetTokenRepository ?? throw new ArgumentNullException(nameof(passwordResetTokenRepository));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<GetUserSecuritySummaryResponseDto>> ExecuteAsync(
        GetUserSecuritySummaryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = GetUserSecuritySummaryValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<GetUserSecuritySummaryResponseDto>.Failure(validationError);
        }

        long? currentUserId = _requestContext.CurrentUserId;
        if (currentUserId is null)
        {
            return Result<GetUserSecuritySummaryResponseDto>.Failure(
                IdentityErrors.Auth.Unauthenticated);
        }

        try
        {
            DateTime nowUtc = _dateTimeProvider.UtcNow;

            var user = await _userAccountRepository.GetByIdAsync(
                request.UserId,
                cancellationToken);

            if (user is null)
            {
                return Result<GetUserSecuritySummaryResponseDto>.Failure(
                    IdentityErrors.User.NotFound);
            }

            IReadOnlyList<RefreshTokenEntity> refreshTokens =
                await _refreshTokenRepository.GetByUserIdAsync(
                    request.UserId,
                    cancellationToken);

            IReadOnlyList<LoginHistoryEntity> loginHistories =
                await _loginHistoryRepository.GetByUserIdAsync(
                    request.UserId,
                    cancellationToken);

            IReadOnlyList<PasswordResetTokenEntity> passwordResetTokens =
                await _passwordResetTokenRepository.GetByUserIdAsync(
                    request.UserId,
                    cancellationToken);

            DateTime last7DaysUtc = nowUtc.AddDays(-7);

            int activeSessionCount = refreshTokens.Count(token =>
                token.IsActiveAt(nowUtc));

            int revokedSessionCount = refreshTokens.Count(token =>
                token.IsRevoked);

            int expiredSessionCount = refreshTokens.Count(token =>
                token.IsExpired(nowUtc));

            int loginSuccessCount = loginHistories.Count(history =>
                history.Succeeded);

            int loginFailureCount = loginHistories.Count(history =>
                !history.Succeeded);

            int failedLoginCountLast7Days = loginHistories.Count(history =>
                !history.Succeeded &&
                history.AttemptedAt >= last7DaysUtc);

            DateTime? recentFailedLoginAt = loginHistories
                .Where(history => !history.Succeeded)
                .OrderByDescending(history => history.AttemptedAt)
                .Select(history => (DateTime?)history.AttemptedAt)
                .FirstOrDefault();

            DateTime? lastPasswordResetRequestedAt = passwordResetTokens
                .OrderByDescending(token => token.CreatedAt)
                .Select(token => (DateTime?)token.CreatedAt)
                .FirstOrDefault();

            int activePasswordResetTokenCount = passwordResetTokens.Count(token =>
                token.CanBeUsed(nowUtc));

            return Result<GetUserSecuritySummaryResponseDto>.Success(
                new GetUserSecuritySummaryResponseDto
                {
                    UserId = user.UserId,
                    PublicId = user.PublicId,
                    Email = user.Email,
                    FullName = user.FullName,
                    IsEmailVerified = user.IsEmailVerified,
                    Status = user.Status,
                    LockedUntil = user.LockedUntil,
                    LastLoginAt = user.LastLoginAt,
                    TotalSessionCount = refreshTokens.Count,
                    ActiveSessionCount = activeSessionCount,
                    RevokedSessionCount = revokedSessionCount,
                    ExpiredSessionCount = expiredSessionCount,
                    LoginSuccessCount = loginSuccessCount,
                    LoginFailureCount = loginFailureCount,
                    FailedLoginCountLast7Days = failedLoginCountLast7Days,
                    RecentFailedLoginAt = recentFailedLoginAt,
                    LastPasswordResetRequestedAt = lastPasswordResetRequestedAt,
                    PasswordResetTokenCount = passwordResetTokens.Count,
                    ActivePasswordResetTokenCount = activePasswordResetTokenCount
                });
        }
        catch (PersistenceException)
        {
            return Result<GetUserSecuritySummaryResponseDto>.Failure(
                IdentityErrors.User.QueryFailed);
        }
    }
}