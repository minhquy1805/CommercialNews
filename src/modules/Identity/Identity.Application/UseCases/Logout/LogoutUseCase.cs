using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Contracts.Logout;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Application.Validation.Logout;
using Identity.Domain.Enums;

namespace Identity.Application.UseCases.Logout;

public sealed class LogoutUseCase : ILogoutUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly ITokenHashProvider _tokenHashProvider;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LogoutUseCase(
        IRequestContext requestContext,
        ITokenHashProvider tokenHashProvider,
        IRefreshTokenRepository refreshTokenRepository,
        IIdentityUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _tokenHashProvider = tokenHashProvider ?? throw new ArgumentNullException(nameof(tokenHashProvider));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<LogoutResponseDto>> ExecuteAsync(
        LogoutRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = LogoutValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<LogoutResponseDto>.Failure(validationError);
        }

        long? currentUserId = _requestContext.CurrentUserId;
        if (currentUserId is null)
        {
            return Result<LogoutResponseDto>.Failure(IdentityErrors.Logout.NotAuthenticated);
        }

        try
        {
            byte[] tokenHash = _tokenHashProvider.Hash(request.RefreshToken.Trim());

            var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(
                tokenHash,
                cancellationToken);

            if (refreshToken is null)
            {
                return Result<LogoutResponseDto>.Failure(IdentityErrors.Refresh.TokenNotFound);
            }

            if (refreshToken.UserId != currentUserId.Value)
            {
                return Result<LogoutResponseDto>.Failure(IdentityErrors.Logout.Forbidden);
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;

            if (refreshToken.IsRevoked || refreshToken.IsExpired(nowUtc))
            {
                return Result<LogoutResponseDto>.Success(new LogoutResponseDto
                {
                    UserId = currentUserId.Value,
                    LoggedOut = true
                });
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                bool revoked = await _refreshTokenRepository.RevokeAsync(
                    refreshTokenId: refreshToken.RefreshTokenId,
                    revokedAtUtc: nowUtc,
                    revokedReason: RefreshTokenRevokedReasons.Logout,
                    replacedByTokenHash: null,
                    cancellationToken: cancellationToken);

                if (!revoked)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<LogoutResponseDto>.Failure(
                        IdentityErrors.Logout.Failed);
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
            _ => IdentityErrors.Logout.Failed
        };
    }
}