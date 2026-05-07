using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Contracts.Users.DisableUser;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Application.Validation.Users.DisableUser;
using Identity.Domain.Enums;
using Identity.Domain.Exceptions;

namespace Identity.Application.UseCases.Users.DisableUser;

public sealed class DisableUserUseCase : IDisableUserUseCase
{
    private const string SessionRevokeReason = "UserDisabled";

    private readonly IRequestContext _requestContext;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIdentityOutboxWriter _outboxWriter;

    public DisableUserUseCase(
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

    public async Task<Result<DisableUserResponseDto>> ExecuteAsync(
        DisableUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = DisableUserValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<DisableUserResponseDto>.Failure(validationError);
        }

        long? currentUserId = _requestContext.CurrentUserId;
        if (currentUserId is null)
        {
            return Result<DisableUserResponseDto>.Failure(
                IdentityErrors.Auth.Unauthenticated);
        }

        if (request.UserId == currentUserId.Value)
        {
            return Result<DisableUserResponseDto>.Failure(
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
                return Result<DisableUserResponseDto>.Failure(
                    IdentityErrors.User.NotFound);
            }

            string previousStatus = user.Status;
            string newStatus = UserAccountStatuses.Disabled;

            bool alreadyDisabled = string.Equals(
                user.Status,
                UserAccountStatuses.Disabled,
                StringComparison.OrdinalIgnoreCase);

            try
            {
                user.Disable(nowUtc);
            }
            catch (IdentityDomainException)
            {
                return Result<DisableUserResponseDto>.Failure(
                    IdentityErrors.User.DisableFailed);
            }

            if (alreadyDisabled)
            {
                return Result<DisableUserResponseDto>.Success(
                    new DisableUserResponseDto
                    {
                        UserId = user.UserId,
                        PublicId = user.PublicId,
                        Email = user.Email,
                        Status = user.Status,
                        Disabled = false,
                        SessionsRevoked = false,
                        RevokedSessionCount = 0,
                        DisabledAtUtc = nowUtc
                    });
            }

            int revokedSessionCount = 0;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                bool updated = await _userAccountRepository.DisableAsync(
                    user.UserId,
                    cancellationToken);

                if (!updated)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<DisableUserResponseDto>.Failure(
                        IdentityErrors.User.DisableFailed);
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

                await _outboxWriter.EnqueueUserDisabledAsync(
                    unitOfWork: _unitOfWork,
                    targetUserId: user.UserId,
                    targetUserPublicId: user.PublicId,
                    targetEmail: user.Email,
                    targetFullName: user.FullName,
                    actorUserId: currentUserId.Value,
                    reason: DisableUserValidator.Normalize(request.Reason),
                    previousStatus: previousStatus,
                    newStatus: newStatus,
                    sessionsRevoked: request.RevokeSessions,
                    revokedSessionCount: revokedSessionCount,
                    disabledAtUtc: nowUtc,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }

            return Result<DisableUserResponseDto>.Success(
                new DisableUserResponseDto
                {
                    UserId = user.UserId,
                    PublicId = user.PublicId,
                    Email = user.Email,
                    Status = newStatus,
                    Disabled = true,
                    SessionsRevoked = request.RevokeSessions,
                    RevokedSessionCount = revokedSessionCount,
                    DisabledAtUtc = nowUtc
                });
        }
        catch (PersistenceException)
        {
            return Result<DisableUserResponseDto>.Failure(
                IdentityErrors.User.DisableFailed);
        }
    }
}