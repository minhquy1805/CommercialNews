using Identity.Application.Contracts.Ports;
using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.Logout
{
    public sealed class LogoutUseCase : ILogoutUseCase
    {
        private readonly IRequestContext _requestContext;
        private readonly ITokenHashProvider _tokenHashProvider;
        private readonly IRefreshTokenLookupService _refreshTokenLookupService;
        private readonly IRefreshTokenRevocationService _refreshTokenRevocationService;
        private readonly IIdentityUnitOfWork _unitOfWork;

        public LogoutUseCase(
            IRequestContext requestContext,
            ITokenHashProvider tokenHashProvider,
            IRefreshTokenLookupService refreshTokenLookupService,
            IRefreshTokenRevocationService refreshTokenRevocationService,
            IIdentityUnitOfWork unitOfWork)
        {
            _requestContext = requestContext;
            _tokenHashProvider = tokenHashProvider;
            _refreshTokenLookupService = refreshTokenLookupService;
            _refreshTokenRevocationService = refreshTokenRevocationService;
            _unitOfWork = unitOfWork;
        }

        public async Task<LogoutResponseDto> ExecuteAsync(
            LogoutRequestDto request,
            CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            var currentUserId = _requestContext.CurrentUserId;
            if (currentUserId is null)
            {
                throw new InvalidOperationException("Current user is not available.");
            }

            var tokenHash = _tokenHashProvider.Hash(request.RefreshToken);

            var refreshToken = await _refreshTokenLookupService.GetByTokenHashAsync(
                tokenHash,
                cancellationToken);

            if (refreshToken is null)
            {
                throw new InvalidOperationException("Refresh token not found.");
            }

            if (refreshToken.UserId != currentUserId.Value)
            {
                throw new InvalidOperationException("Refresh token does not belong to current user.");
            }

            if (refreshToken.RevokedAt is not null)
            {
                throw new InvalidOperationException("Refresh token is already revoked.");
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                await _refreshTokenRevocationService.RevokeAsync(
                    refreshToken.RefreshTokenId,
                    "LoggedOut",
                    replacedByTokenHash: null,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }

            return new LogoutResponseDto
            {
                UserId = currentUserId.Value,
                LoggedOut = true
            };
        }

        private static void ValidateRequest(LogoutRequestDto request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                throw new ArgumentException("Refresh token is required.", nameof(request.RefreshToken));
            }
        }
    }   
}