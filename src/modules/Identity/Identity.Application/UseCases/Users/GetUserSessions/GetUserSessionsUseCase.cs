using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Contracts.Users.GetUserSessions;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Validation.Users.GetUserSessions;
using RefreshTokenEntity = Identity.Domain.Entities.RefreshToken;

namespace Identity.Application.UseCases.Users.GetUserSessions;

public sealed class GetUserSessionsUseCase : IGetUserSessionsUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetUserSessionsUseCase(
        IRequestContext requestContext,
        IUserAccountRepository userAccountRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _userAccountRepository = userAccountRepository ?? throw new ArgumentNullException(nameof(userAccountRepository));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<GetUserSessionsResponseDto>> ExecuteAsync(
        GetUserSessionsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = GetUserSessionsValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<GetUserSessionsResponseDto>.Failure(validationError);
        }

        long? currentUserId = _requestContext.CurrentUserId;
        if (currentUserId is null)
        {
            return Result<GetUserSessionsResponseDto>.Failure(IdentityErrors.Auth.Unauthenticated);
        }

        try
        {
            var user = await _userAccountRepository.GetByIdAsync(
                request.UserId,
                cancellationToken);

            if (user is null)
            {
                return Result<GetUserSessionsResponseDto>.Failure(IdentityErrors.User.NotFound);
            }

            IReadOnlyList<RefreshTokenEntity> sessions =
                await _refreshTokenRepository.GetByUserIdAsync(
                    request.UserId,
                    cancellationToken);

            DateTime nowUtc = _dateTimeProvider.UtcNow;

            return Result<GetUserSessionsResponseDto>.Success(
                new GetUserSessionsResponseDto
                {
                    UserId = request.UserId,
                    Items = sessions
                        .Select(session => MapItem(session, nowUtc))
                        .ToArray()
                });
        }
        catch (PersistenceException)
        {
            return Result<GetUserSessionsResponseDto>.Failure(
                IdentityErrors.Session.QueryFailed);
        }
    }

    private static UserSessionItemDto MapItem(
        RefreshTokenEntity session,
        DateTime nowUtc)
    {
        return new UserSessionItemDto
        {
            RefreshTokenId = session.RefreshTokenId,
            UserId = session.UserId,
            CreatedAt = session.CreatedAt,
            ExpiresAt = session.ExpiresAt,
            RevokedAt = session.RevokedAt,
            RevokedReason = session.RevokedReason,
            CreatedIp = session.CreatedIp,
            UserAgent = session.UserAgent,
            CorrelationId = session.CorrelationId,
            IsRevoked = session.IsRevoked,
            IsExpired = session.IsExpired(nowUtc),
            IsActive = session.IsActiveAt(nowUtc)
        };
    }
}