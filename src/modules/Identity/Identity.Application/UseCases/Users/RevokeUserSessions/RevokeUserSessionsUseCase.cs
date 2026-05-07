using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Contracts.Users.RevokeUserSessions;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Application.Validation.Users.RevokeUserSessions;

namespace Identity.Application.UseCases.Users.RevokeUserSessions;

public sealed class RevokeUserSessionsUseCase : IRevokeUserSessionsUseCase
{
    private const string SessionRevokeReason = "UserSessionsRevoked";

    private readonly IRequestContext _requestContext;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIdentityOutboxWriter _outboxWriter;

    public RevokeUserSessionsUseCase(
        IRequestContext requestContext,
        IUserAccountRepository userAccountRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IIdentityUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IIdentityOutboxWriter outboxWriter)
    {
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _userAccountRepository = userAccountRepository ?? throw new ArgumentNullException(nameof(userAccountRepository));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
    }

    public async Task<Result<RevokeUserSessionsResponseDto>> ExecuteAsync(
        RevokeUserSessionsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = RevokeUserSessionsValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<RevokeUserSessionsResponseDto>.Failure(validationError);
        }

        long? currentUserId = _requestContext.CurrentUserId;
        if (currentUserId is null)
        {
            return Result<RevokeUserSessionsResponseDto>.Failure(
                IdentityErrors.Auth.Unauthenticated);
        }

        if (request.UserId == currentUserId.Value)
        {
            return Result<RevokeUserSessionsResponseDto>.Failure(
                IdentityErrors.User.SelfActionDenied);
        }

        try
        {
            DateTime nowUtc = _dateTimeProvider.UtcNow;

            var user = await _userAccountRepository.GetByIdAsync(
                request.UserId,
                cancellationToken);

            if (user is null)
            {
                return Result<RevokeUserSessionsResponseDto>.Failure(
                    IdentityErrors.User.NotFound);
            }

            string? normalizedReason =
                RevokeUserSessionsValidator.Normalize(request.Reason);

            string revokedReason =
                normalizedReason ?? SessionRevokeReason;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            int revokedSessionCount;

            try
            {
                revokedSessionCount =
                    await _refreshTokenRepository.RevokeAllActiveByUserIdAsync(
                        user.UserId,
                        nowUtc,
                        revokedReason,
                        cancellationToken);

                await _outboxWriter.EnqueueUserSessionsRevokedAsync(
                    unitOfWork: _unitOfWork,
                    targetUserId: user.UserId,
                    targetUserPublicId: user.PublicId,
                    targetEmail: user.Email,
                    targetFullName: user.FullName,
                    actorUserId: currentUserId.Value,
                    reason: normalizedReason,
                    revokedSessionCount: revokedSessionCount,
                    revokedAtUtc: nowUtc,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }

            return Result<RevokeUserSessionsResponseDto>.Success(
                new RevokeUserSessionsResponseDto
                {
                    UserId = user.UserId,
                    PublicId = user.PublicId,
                    Email = user.Email,
                    RevokedSessionCount = revokedSessionCount,
                    RevokedAtUtc = nowUtc
                });
        }
        catch (PersistenceException)
        {
            return Result<RevokeUserSessionsResponseDto>.Failure(
                IdentityErrors.Session.RevokeFailed);
        }
    }
}