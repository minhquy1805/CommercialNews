using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Contracts.ChangePassword;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Application.Validation.ChangePassword;
using Identity.Domain.Enums;

namespace Identity.Application.UseCases.ChangePassword;

public sealed class ChangePasswordUseCase : IChangePasswordUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIdentityOutboxWriter _outboxWriter;

    public ChangePasswordUseCase(
        IRequestContext requestContext,
        IUserAccountRepository userAccountRepository,
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokenRepository,
        IIdentityUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IIdentityOutboxWriter outboxWriter)
    {
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _userAccountRepository = userAccountRepository ?? throw new ArgumentNullException(nameof(userAccountRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
    }

    public async Task<Result<ChangePasswordResponseDto>> ExecuteAsync(
        ChangePasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = ChangePasswordValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<ChangePasswordResponseDto>.Failure(validationError);
        }

        long? currentUserId = _requestContext.CurrentUserId;
        if (currentUserId is null)
        {
            return Result<ChangePasswordResponseDto>.Failure(
                IdentityErrors.Auth.Unauthenticated);
        }

        try
        {
            DateTime nowUtc = _dateTimeProvider.UtcNow;

            var user = await _userAccountRepository.GetByIdAsync(
                currentUserId.Value,
                cancellationToken);

            if (user is null)
            {
                return Result<ChangePasswordResponseDto>.Failure(
                    IdentityErrors.User.NotFound);
            }

            if (!user.IsEmailVerified)
            {
                return Result<ChangePasswordResponseDto>.Failure(
                    IdentityErrors.Auth.VerificationRequired);
            }

            if (string.Equals(user.Status, UserAccountStatuses.Disabled, StringComparison.OrdinalIgnoreCase))
            {
                return Result<ChangePasswordResponseDto>.Failure(
                    IdentityErrors.Auth.AccountDisabled);
            }

            if (user.IsLockedAt(nowUtc))
            {
                return Result<ChangePasswordResponseDto>.Failure(
                    IdentityErrors.Auth.AccountLocked);
            }

            if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return Result<ChangePasswordResponseDto>.Failure(
                    IdentityErrors.InvalidCredentials);
            }

            string newPasswordHash = _passwordHasher.Hash(request.NewPassword);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                bool updated = await _userAccountRepository.UpdatePasswordAsync(
                    user.UserId,
                    newPasswordHash,
                    cancellationToken);

                if (!updated)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<ChangePasswordResponseDto>.Failure(
                        IdentityErrors.User.ChangePasswordFailed);
                }

                await _refreshTokenRepository.RevokeAllActiveByUserIdAsync(
                    user.UserId,
                    nowUtc,
                    PasswordChangedReasons.ChangedByUser,
                    cancellationToken);

                await _outboxWriter.EnqueuePasswordChangedAsync(
                    unitOfWork: _unitOfWork,
                    userId: user.UserId,
                    userPublicId: user.PublicId,
                    email: user.Email,
                    fullName: user.FullName,
                    reason: PasswordChangedReasons.ChangedByUser,
                    occurredAtUtc: nowUtc,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<ChangePasswordResponseDto>.Success(new ChangePasswordResponseDto
                {
                    UserId = user.UserId,
                    PasswordChanged = true
                });
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException)
        {
            return Result<ChangePasswordResponseDto>.Failure(
                IdentityErrors.User.ChangePasswordFailed);
        }
    }
}