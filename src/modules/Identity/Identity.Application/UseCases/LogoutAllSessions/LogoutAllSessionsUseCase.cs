using Identity.Application.Contracts.Dtos;
using Identity.Application.Contracts.Ports;
using Identity.Domain.Enums;

namespace Identity.Application.UseCases.LogoutAllSessions
{
    public sealed class LogoutAllSessionsUseCase : ILogoutAllSessionsUseCase
    {
        private readonly IRequestContext _requestContext;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IRefreshTokenRevocationService _refreshTokenRevocationService;
        private readonly IIdentityUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;

        public LogoutAllSessionsUseCase(
            IRequestContext requestContext,
            IUserAccountRepository userAccountRepository,
            IRefreshTokenRevocationService refreshTokenRevocationService,
            IIdentityUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider)
        {
            _requestContext = requestContext;
            _userAccountRepository = userAccountRepository;
            _refreshTokenRevocationService = refreshTokenRevocationService;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<LogoutAllSessionsResponseDto> ExecuteAsync(
            CancellationToken cancellationToken)
        {
            var currentUserId = _requestContext.CurrentUserId;
            if (currentUserId is null)
            {
                throw new InvalidOperationException("Current user is not available.");
            }

            var user = await _userAccountRepository.GetByIdAsync(currentUserId.Value, cancellationToken);
            if (user is null)
            {
                throw new InvalidOperationException("User not found.");
            }

            if (user.Status == UserAccountStatus.Inactive)
            {
                throw new InvalidOperationException("Account is inactive.");
            }

            if (user.IsLockedAt(_dateTimeProvider.UtcNow))
            {
                throw new InvalidOperationException("Account is locked.");
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                await _refreshTokenRevocationService.RevokeAllActiveByUserIdAsync(
                    user.UserId,
                    "LoggedOutAllSessions",
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }

            return new LogoutAllSessionsResponseDto
            {
                UserId = user.UserId,
                LoggedOutAllSessions = true
            };
        }
    }
}