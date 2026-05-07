using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Contracts.Users.MarkEmailVerified;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Application.Validation.Users.MarkEmailVerified;
using Identity.Domain.Enums;
using Identity.Domain.Exceptions;

namespace Identity.Application.UseCases.Users.MarkEmailVerified;

public sealed class MarkEmailVerifiedUseCase : IMarkEmailVerifiedUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIdentityOutboxWriter _outboxWriter;

    public MarkEmailVerifiedUseCase(
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

    public async Task<Result<MarkEmailVerifiedResponseDto>> ExecuteAsync(
        MarkEmailVerifiedRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = MarkEmailVerifiedValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<MarkEmailVerifiedResponseDto>.Failure(validationError);
        }

        long? currentUserId = _requestContext.CurrentUserId;
        if (currentUserId is null)
        {
            return Result<MarkEmailVerifiedResponseDto>.Failure(
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
                return Result<MarkEmailVerifiedResponseDto>.Failure(
                    IdentityErrors.User.NotFound);
            }

            string previousStatus = user.Status;
            bool wasAlreadyVerified = user.IsEmailVerified;

            string newStatus =
                string.Equals(user.Status, UserAccountStatuses.Unverified, StringComparison.OrdinalIgnoreCase)
                    ? UserAccountStatuses.Active
                    : user.Status;

            try
            {
                user.MarkEmailVerified(nowUtc);
            }
            catch (IdentityDomainException)
            {
                return Result<MarkEmailVerifiedResponseDto>.Failure(
                    IdentityErrors.User.MarkEmailVerifiedFailed);
            }

            if (wasAlreadyVerified)
            {
                return Result<MarkEmailVerifiedResponseDto>.Success(
                    new MarkEmailVerifiedResponseDto
                    {
                        UserId = user.UserId,
                        PublicId = user.PublicId,
                        Email = user.Email,
                        IsEmailVerified = true,
                        WasAlreadyVerified = true,
                        Status = user.Status,
                        MarkedVerifiedAtUtc = nowUtc
                    });
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                bool updated = await _userAccountRepository.MarkEmailVerifiedAsync(
                    user.UserId,
                    cancellationToken);

                if (!updated)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<MarkEmailVerifiedResponseDto>.Failure(
                        IdentityErrors.User.MarkEmailVerifiedFailed);
                }

                await _outboxWriter.EnqueueEmailMarkedVerifiedAsync(
                    unitOfWork: _unitOfWork,
                    targetUserId: user.UserId,
                    targetUserPublicId: user.PublicId,
                    targetEmail: user.Email,
                    targetFullName: user.FullName,
                    actorUserId: currentUserId.Value,
                    reason: MarkEmailVerifiedValidator.Normalize(request.Reason),
                    wasAlreadyVerified: wasAlreadyVerified,
                    previousStatus: previousStatus,
                    newStatus: newStatus,
                    markedVerifiedAtUtc: nowUtc,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }

            return Result<MarkEmailVerifiedResponseDto>.Success(
                new MarkEmailVerifiedResponseDto
                {
                    UserId = user.UserId,
                    PublicId = user.PublicId,
                    Email = user.Email,
                    IsEmailVerified = true,
                    WasAlreadyVerified = false,
                    Status = newStatus,
                    MarkedVerifiedAtUtc = nowUtc
                });
        }
        catch (PersistenceException)
        {
            return Result<MarkEmailVerifiedResponseDto>.Failure(
                IdentityErrors.User.MarkEmailVerifiedFailed);
        }
    }
}