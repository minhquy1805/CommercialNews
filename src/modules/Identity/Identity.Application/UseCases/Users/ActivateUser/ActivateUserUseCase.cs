using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Contracts.Users.ActivateUser;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Application.Validation.Users.ActivateUser;
using Identity.Domain.Enums;
using Identity.Domain.Exceptions;

namespace Identity.Application.UseCases.Users.ActivateUser;

public sealed class ActivateUserUseCase : IActivateUserUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIdentityOutboxWriter _outboxWriter;

    public ActivateUserUseCase(
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

    public async Task<Result<ActivateUserResponseDto>> ExecuteAsync(
        ActivateUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = ActivateUserValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<ActivateUserResponseDto>.Failure(validationError);
        }

        long? currentUserId = _requestContext.CurrentUserId;
        if (currentUserId is null)
        {
            return Result<ActivateUserResponseDto>.Failure(
                IdentityErrors.Auth.Unauthenticated);
        }

        try
        {
            DateTime nowUtc = _dateTimeProvider.UtcNow;

            var user = await _userAccountRepository.GetByIdAsync(
                request.UserId,
                cancellationToken);

            if (user is null)
            {
                return Result<ActivateUserResponseDto>.Failure(
                    IdentityErrors.User.NotFound);
            }

            string previousStatus = user.Status;
            string newStatus = UserAccountStatuses.Active;

            bool alreadyActive =
                string.Equals(
                    user.Status,
                    UserAccountStatuses.Active,
                    StringComparison.OrdinalIgnoreCase) &&
                user.LockedUntil is null;

            try
            {
                user.Activate(nowUtc);
            }
            catch (IdentityDomainException exception)
                when (string.Equals(
                    exception.Code,
                    "IDENTITY.USER_CANNOT_ACTIVATE_UNVERIFIED",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Result<ActivateUserResponseDto>.Failure(
                    IdentityErrors.User.CannotActivateUnverified);
            }
            catch (IdentityDomainException)
            {
                return Result<ActivateUserResponseDto>.Failure(
                    IdentityErrors.User.ActivateFailed);
            }

            if (alreadyActive)
            {
                return Result<ActivateUserResponseDto>.Success(
                    new ActivateUserResponseDto
                    {
                        UserId = user.UserId,
                        PublicId = user.PublicId,
                        Email = user.Email,
                        Status = user.Status,
                        Activated = false,
                        ActivatedAtUtc = nowUtc
                    });
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                bool updated = await _userAccountRepository.ActivateAsync(
                    user.UserId,
                    cancellationToken);

                if (!updated)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<ActivateUserResponseDto>.Failure(
                        IdentityErrors.User.ActivateFailed);
                }

                await _outboxWriter.EnqueueUserActivatedAsync(
                    unitOfWork: _unitOfWork,
                    targetUserId: user.UserId,
                    targetUserPublicId: user.PublicId,
                    targetEmail: user.Email,
                    targetFullName: user.FullName,
                    actorUserId: currentUserId.Value,
                    reason: ActivateUserValidator.Normalize(request.Reason),
                    previousStatus: previousStatus,
                    newStatus: newStatus,
                    activatedAtUtc: nowUtc,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }

            return Result<ActivateUserResponseDto>.Success(
                new ActivateUserResponseDto
                {
                    UserId = user.UserId,
                    PublicId = user.PublicId,
                    Email = user.Email,
                    Status = newStatus,
                    Activated = true,
                    ActivatedAtUtc = nowUtc
                });
        }
        catch (PersistenceException)
        {
            return Result<ActivateUserResponseDto>.Failure(
                IdentityErrors.User.ActivateFailed);
        }
    }
}