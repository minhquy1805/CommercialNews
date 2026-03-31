using CommercialNews.BuildingBlocks.Abstractions.Execution;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;

namespace Identity.Application.UseCases.Logout
{
    public sealed class LogoutUseCase : ILogoutUseCase
    {
        private readonly IRequestContext _requestContext;
        private readonly ITokenHashProvider _tokenHashProvider;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IIdentityUnitOfWork _unitOfWork;

        public LogoutUseCase(
            IRequestContext requestContext,
            ITokenHashProvider tokenHashProvider,
            IRefreshTokenRepository refreshTokenRepository,
            IIdentityUnitOfWork unitOfWork)
        {
            _requestContext = requestContext;
            _tokenHashProvider = tokenHashProvider;
            _refreshTokenRepository = refreshTokenRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<LogoutResponseDto>> ExecuteAsync(
            LogoutRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return Result<LogoutResponseDto>.Failure(IdentityErrors.ValidationFailed);
            }

            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Result<LogoutResponseDto>.Failure(IdentityErrors.Refresh.TokenHashRequired);
            }

            long? currentUserId = _requestContext.CurrentUserId;
            if (currentUserId is null)
            {
                return Result<LogoutResponseDto>.Failure(IdentityErrors.Auth.LogoutFailed);
            }

            try
            {
                byte[] tokenHash = _tokenHashProvider.Hash(request.RefreshToken);

                var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(
                    tokenHash,
                    cancellationToken);

                if (refreshToken is null)
                {
                    return Result<LogoutResponseDto>.Failure(IdentityErrors.Refresh.TokenNotFound);
                }

                if (refreshToken.UserId != currentUserId.Value)
                {
                    return Result<LogoutResponseDto>.Failure(IdentityErrors.Auth.LogoutFailed);
                }

                if (refreshToken.RevokedAt is not null)
                {
                    return Result<LogoutResponseDto>.Failure(IdentityErrors.Refresh.TokenRevoked);
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    bool revoked = await _refreshTokenRepository.RevokeAsync(
                        refreshTokenId: refreshToken.RefreshTokenId,
                        revokedReason: "LoggedOut",
                        replacedByTokenHash: null,
                        cancellationToken: cancellationToken);

                    if (!revoked)
                    {
                        await _unitOfWork.RollbackAsync(cancellationToken);
                        return Result<LogoutResponseDto>.Failure(IdentityErrors.Auth.LogoutFailed);
                    }

                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result<LogoutResponseDto>.Success(new LogoutResponseDto
                    {
                        UserId = currentUserId.Value,
                        LoggedOut = true
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
                return Result<LogoutResponseDto>.Failure(MapPersistenceException(exception));
            }
        }

        private static Error MapPersistenceException(PersistenceException exception)
        {
            return exception.Code switch
            {
                "IDENTITY.REFRESH_TOKEN_NOT_FOUND" => IdentityErrors.Refresh.TokenNotFound,
                "IDENTITY.REFRESH_TOKEN_REVOKED" => IdentityErrors.Refresh.TokenRevoked,
                _ => IdentityErrors.Auth.LogoutFailed
            };
        }
    }
}