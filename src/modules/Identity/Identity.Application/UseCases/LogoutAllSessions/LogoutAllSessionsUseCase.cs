using CommercialNews.BuildingBlocks.Abstractions.Execution;
using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Identity.Application.Contracts.Responses;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Domain.Enums;

namespace Identity.Application.UseCases.LogoutAllSessions
{
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
            _requestContext = requestContext;
            _userAccountRepository = userAccountRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<LogoutAllSessionsResponseDto>> ExecuteAsync(
            CancellationToken cancellationToken = default)
        {
            long? currentUserId = _requestContext.CurrentUserId;
            if (currentUserId is null)
            {
                return Result<LogoutAllSessionsResponseDto>.Failure(IdentityErrors.Auth.LogoutFailed);
            }

            try
            {
                var user = await _userAccountRepository.GetByIdAsync(
                    currentUserId.Value,
                    cancellationToken);

                if (user is null)
                {
                    return Result<LogoutAllSessionsResponseDto>.Failure(IdentityErrors.User.NotFound);
                }

                if (user.Status == UserAccountStatus.Inactive)
                {
                    return Result<LogoutAllSessionsResponseDto>.Failure(IdentityErrors.Auth.AccountInactive);
                }

                if (user.IsLockedAt(_dateTimeProvider.UtcNow))
                {
                    return Result<LogoutAllSessionsResponseDto>.Failure(IdentityErrors.AccountLocked);
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    await _refreshTokenRepository.RevokeAllActiveByUserIdAsync(
                        userId: user.UserId,
                        revokedReason: "LoggedOutAllSessions",
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
            catch (PersistenceException exception)
            {
                return Result<LogoutAllSessionsResponseDto>.Failure(MapPersistenceException(exception));
            }
        }

        private static Error MapPersistenceException(PersistenceException exception)
        {
            return exception.Code switch
            {
                _ => IdentityErrors.Auth.LogoutFailed
            };
        }
    }
}