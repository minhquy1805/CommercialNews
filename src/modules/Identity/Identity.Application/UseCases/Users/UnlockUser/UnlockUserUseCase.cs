using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Contracts.Users.UnlockUser;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Application.Validation.Users.UnlockUser;
using Identity.Domain.Enums;
using Identity.Domain.Exceptions;

namespace Identity.Application.UseCases.Users.UnlockUser;

public sealed class UnlockUserUseCase : IUnlockUserUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIdentityOutboxWriter _outboxWriter;

    public UnlockUserUseCase(
        IRequestContext requestContext,
        IUserAccountRepository userAccountRepository,
        IIdentityUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IIdentityOutboxWriter outboxWriter)
    {
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _userAccountRepository = userAccountRepository ?? throw new ArgumentNullException(nameof(userAccountRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
    }

    public async Task<Result<UnlockUserResponseDto>> ExecuteAsync(
        UnlockUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = UnlockUserValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<UnlockUserResponseDto>.Failure(validationError);
        }

        long? currentUserId = _requestContext.CurrentUserId;
        if (currentUserId is null)
        {
            return Result<UnlockUserResponseDto>.Failure(
                IdentityErrors.Auth.Unauthenticated);
        }

        if (request.UserId == currentUserId.Value)
        {
            return Result<UnlockUserResponseDto>.Failure(
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
                return Result<UnlockUserResponseDto>.Failure(
                    IdentityErrors.User.NotFound);
            }

            string previousStatus = user.Status;
            string newStatus = UserAccountStatuses.Active;

            bool alreadyUnlocked =
                !string.Equals(
                    user.Status,
                    UserAccountStatuses.Locked,
                    StringComparison.OrdinalIgnoreCase) &&
                user.LockedUntil is null;

            try
            {
                user.Unlock(nowUtc);
            }
            catch (IdentityDomainException)
            {
                return Result<UnlockUserResponseDto>.Failure(
                    IdentityErrors.User.UnlockFailed);
            }

            if (alreadyUnlocked)
            {
                return Result<UnlockUserResponseDto>.Success(
                    new UnlockUserResponseDto
                    {
                        UserId = user.UserId,
                        PublicId = user.PublicId,
                        Email = user.Email,
                        Status = user.Status,
                        Unlocked = false,
                        UnlockedAtUtc = nowUtc
                    });
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                bool updated = await _userAccountRepository.UnlockAsync(
                    user.UserId,
                    cancellationToken);

                if (!updated)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<UnlockUserResponseDto>.Failure(
                        IdentityErrors.User.UnlockFailed);
                }

                await _outboxWriter.EnqueueUserUnlockedAsync(
                    unitOfWork: _unitOfWork,
                    targetUserId: user.UserId,
                    targetUserPublicId: user.PublicId,
                    targetEmail: user.Email,
                    targetFullName: user.FullName,
                    actorUserId: currentUserId.Value,
                    reason: UnlockUserValidator.Normalize(request.Reason),
                    previousStatus: previousStatus,
                    newStatus: newStatus,
                    unlockedAtUtc: nowUtc,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }

            return Result<UnlockUserResponseDto>.Success(
                new UnlockUserResponseDto
                {
                    UserId = user.UserId,
                    PublicId = user.PublicId,
                    Email = user.Email,
                    Status = newStatus,
                    Unlocked = true,
                    UnlockedAtUtc = nowUtc
                });
        }
        catch (PersistenceException)
        {
            return Result<UnlockUserResponseDto>.Failure(
                IdentityErrors.User.UnlockFailed);
        }
    }
}