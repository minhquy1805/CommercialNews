using Identity.Application.Contracts.Dtos;
using Identity.Application.Contracts.Ports;
using Identity.Domain.Enums;

namespace Identity.Application.UseCases.ChangePassword
{
    public sealed class ChangePasswordUseCase : IChangePasswordUseCase
    {
        private readonly IRequestContext _requestContext;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUserPasswordService _userPasswordService;
        private readonly IRefreshTokenRevocationService _refreshTokenRevocationService;
        private readonly IIdentityUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;

        public ChangePasswordUseCase(
            IRequestContext requestContext,
            IUserAccountRepository userAccountRepository,
            IPasswordHasher passwordHasher,
            IUserPasswordService userPasswordService,
            IRefreshTokenRevocationService refreshTokenRevocationService,
            IIdentityUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider)
        {
            _requestContext = requestContext;
            _userAccountRepository = userAccountRepository;
            _passwordHasher = passwordHasher;
            _userPasswordService = userPasswordService;
            _refreshTokenRevocationService = refreshTokenRevocationService;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<ChangePasswordResponseDto> ExecuteAsync(
            ChangePasswordRequestDto request,
            CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            var currentUserId = _requestContext.CurrentUserId;
            if (currentUserId is null)
            {
                throw new InvalidOperationException("Current user is not available.");
            }

            var user = await _userAccountRepository.GetByIdAsync(currentUserId.Value, cancellationToken);
            if (user is null)
            {
                throw new InvalidOperationException("User not found.");
            }

            if (!user.IsEmailVerified)
            {
                throw new InvalidOperationException("Email is not verified.");
            }

            if (user.Status == UserAccountStatus.Inactive)
            {
                throw new InvalidOperationException("Account is inactive.");
            }

            if (user.IsLockedAt(_dateTimeProvider.UtcNow))
            {
                throw new InvalidOperationException("Account is locked.");
            }

            if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            {
                throw new InvalidOperationException("Current password is incorrect.");
            }

            if (request.CurrentPassword == request.NewPassword)
            {
                throw new InvalidOperationException("New password must be different from current password.");
            }

            var newPasswordHash = _passwordHasher.Hash(request.NewPassword);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                await _userPasswordService.UpdatePasswordAsync(
                    user.UserId,
                    newPasswordHash,
                    cancellationToken);

                await _refreshTokenRevocationService.RevokeAllActiveByUserIdAsync(
                    user.UserId,
                    "PasswordChanged",
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }

            return new ChangePasswordResponseDto
            {
                UserId = user.UserId,
                PasswordChanged = true
            };
        }

        private static void ValidateRequest(ChangePasswordRequestDto request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            {
                throw new ArgumentException("Current password is required.", nameof(request.CurrentPassword));
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                throw new ArgumentException("New password is required.", nameof(request.NewPassword));
            }

            if (request.NewPassword.Length < 8)
            {
                throw new ArgumentException("New password must be at least 8 characters long.", nameof(request.NewPassword));
            }
        }
    }
}

