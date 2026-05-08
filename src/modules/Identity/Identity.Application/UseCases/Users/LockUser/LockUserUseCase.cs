using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Contracts.Users.LockUser;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Application.Validation.Users.LockUser;
using Identity.Domain.Enums;
using Identity.Domain.Exceptions;

namespace Identity.Application.UseCases.Users.LockUser;

public sealed class LockUserUseCase : ILockUserUseCase
{
    private const string SessionRevokeReason = "UserLocked";

    private readonly IRequestContext _requestContext;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIdentityOutboxWriter _outboxWriter;

    public LockUserUseCase(
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

    public async Task<Result<LockUserResponseDto>> ExecuteAsync(
        LockUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        DateTime nowUtc = _dateTimeProvider.UtcNow;

        Error? validationError = LockUserValidator.Validate(request, nowUtc);
        if (validationError is not null)
        {
            return Result<LockUserResponseDto>.Failure(validationError);
        }

        long? currentUserId = _requestContext.CurrentUserId;
        if (currentUserId is null)
        {
            return Result<LockUserResponseDto>.Failure(
                IdentityErrors.Auth.Unauthenticated);
        }

        if (request.UserId == currentUserId.Value)
        {
            return Result<LockUserResponseDto>.Failure(
                IdentityErrors.User.SelfActionDenied);
        }

        try
        {
            var user = await _userAccountRepository.GetByIdAsync(
                request.UserId,
                cancellationToken);

            if (user is null)
            {
                return Result<LockUserResponseDto>.Failure(
                    IdentityErrors.User.NotFound);
            }

            string previousStatus = user.Status;
            string newStatus = UserAccountStatuses.Locked;

            bool alreadyLockedUntilSameOrLater =
                string.Equals(
                    user.Status,
                    UserAccountStatuses.Locked,
                    StringComparison.OrdinalIgnoreCase) &&
                user.LockedUntil.HasValue &&
                user.LockedUntil.Value >= request.LockedUntilUtc;

            try
            {
                user.LockUntil(request.LockedUntilUtc, nowUtc);
            }
            catch (IdentityDomainException)
            {
                return Result<LockUserResponseDto>.Failure(
                    IdentityErrors.User.LockFailed);
            }

            if (alreadyLockedUntilSameOrLater)
            {
                return Result<LockUserResponseDto>.Success(
                    new LockUserResponseDto
                    {
                        UserId = user.UserId,
                        PublicId = user.PublicId,
                        Email = user.Email,
                        Status = user.Status,
                        LockedUntilUtc = user.LockedUntil ?? request.LockedUntilUtc,
                        Locked = false,
                        SessionsRevoked = false,
                        RevokedSessionCount = 0,
                        LockedAtUtc = nowUtc
                    });
            }

            int revokedSessionCount = 0;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                bool updated = await _userAccountRepository.LockAsync(
                    user.UserId,
                    request.LockedUntilUtc,
                    cancellationToken);

                if (!updated)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<LockUserResponseDto>.Failure(
                        IdentityErrors.User.LockFailed);
                }

                if (request.RevokeSessions)
                {
                    revokedSessionCount =
                        await _refreshTokenRepository.RevokeAllActiveByUserIdAsync(
                            user.UserId,
                            nowUtc,
                            SessionRevokeReason,
                            cancellationToken);
                }

                await _outboxWriter.EnqueueUserLockedAsync(
                    unitOfWork: _unitOfWork,
                    targetUserId: user.UserId,
                    targetUserPublicId: user.PublicId,
                    targetEmail: user.Email,
                    targetFullName: user.FullName,
                    actorUserId: currentUserId.Value,
                    reason: LockUserValidator.Normalize(request.Reason),
                    previousStatus: previousStatus,
                    newStatus: newStatus,
                    lockedUntilUtc: request.LockedUntilUtc,
                    sessionsRevoked: request.RevokeSessions,
                    revokedSessionCount: revokedSessionCount,
                    lockedAtUtc: nowUtc,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }

            return Result<LockUserResponseDto>.Success(
                new LockUserResponseDto
                {
                    UserId = user.UserId,
                    PublicId = user.PublicId,
                    Email = user.Email,
                    Status = newStatus,
                    LockedUntilUtc = request.LockedUntilUtc,
                    Locked = true,
                    SessionsRevoked = request.RevokeSessions,
                    RevokedSessionCount = revokedSessionCount,
                    LockedAtUtc = nowUtc
                });
        }
        catch (PersistenceException)
        {
            return Result<LockUserResponseDto>.Failure(
                IdentityErrors.User.LockFailed);
        }
    }
}