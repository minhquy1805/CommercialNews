using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Contracts.LogoutAllSessions;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Domain.Enums;

namespace Identity.Application.UseCases.LogoutAllSessions;

public sealed class LogoutAllSessionsUseCase : ILogoutAllSessionsUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LogoutAllSessionsUseCase(
        IRequestContext requestContext,
        IUserAccountRepository userAccountRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IIdentityUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _userAccountRepository = userAccountRepository ?? throw new ArgumentNullException(nameof(userAccountRepository));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<LogoutAllSessionsResponseDto>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        long? currentUserId = _requestContext.CurrentUserId;
        if (currentUserId is null)
        {
            return Result<LogoutAllSessionsResponseDto>.Failure(
                IdentityErrors.LogoutAllSessions.NotAuthenticated);
        }

        try
        {
            DateTime nowUtc = _dateTimeProvider.UtcNow;

            var user = await _userAccountRepository.GetByIdAsync(
                currentUserId.Value,
                cancellationToken);

            if (user is null)
            {
                return Result<LogoutAllSessionsResponseDto>.Failure(
                    IdentityErrors.User.NotFound);
            }

            if (string.Equals(user.Status, UserAccountStatuses.Disabled, StringComparison.OrdinalIgnoreCase))
            {
                return Result<LogoutAllSessionsResponseDto>.Failure(
                    IdentityErrors.Auth.AccountDisabled);
            }

            if (user.IsLockedAt(nowUtc))
            {
                return Result<LogoutAllSessionsResponseDto>.Failure(
                    IdentityErrors.Auth.AccountLocked);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                await _refreshTokenRepository.RevokeAllActiveByUserIdAsync(
                    userId: user.UserId,
                    revokedAtUtc: nowUtc,
                    revokedReason: RefreshTokenRevokedReasons.LogoutAll,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<LogoutAllSessionsResponseDto>.Success(new LogoutAllSessionsResponseDto
                {
                    UserId = user.UserId,
                    LoggedOutAllSessions = true
                });
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException)
        {
            return Result<LogoutAllSessionsResponseDto>.Failure(
                IdentityErrors.LogoutAllSessions.Failed);
        }
    }
}