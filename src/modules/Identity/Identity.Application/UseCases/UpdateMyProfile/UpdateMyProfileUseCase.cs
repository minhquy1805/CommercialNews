using Identity.Application.Contracts.Dtos;
using Identity.Application.Contracts.Ports;
using Identity.Domain.Enums;

namespace Identity.Application.UseCases.UpdateMyProfile
{
    public sealed class UpdateMyProfileUseCase : IUpdateMyProfileUseCase
    {
        private readonly IRequestContext _requestContext;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IUserProfileService _userProfileService;
        private readonly IIdentityUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;

        public UpdateMyProfileUseCase(
            IRequestContext requestContext,
            IUserAccountRepository userAccountRepository,
            IUserProfileService userProfileService,
            IIdentityUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider)
        {
            _requestContext = requestContext;
            _userAccountRepository = userAccountRepository;
            _userProfileService = userProfileService;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<UpdateMyProfileResponseDto> ExecuteAsync(
            UpdateMyProfileRequestDto request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

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

            if (user.Status == UserAccountStatus.Inactive)
            {
                throw new InvalidOperationException("Account is inactive.");
            }

            if (user.IsLockedAt(_dateTimeProvider.UtcNow))
            {
                throw new InvalidOperationException("Account is locked.");
            }

            var normalizedFullName = NormalizeOptional(request.FullName, 200);
            var normalizedAvatarUrl = NormalizeOptional(request.AvatarUrl, 800);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                await _userProfileService.UpdateProfileAsync(
                    user.UserId,
                    normalizedFullName,
                    normalizedAvatarUrl,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }

            var updatedUser = await _userAccountRepository.GetByIdAsync(user.UserId, cancellationToken);
            if (updatedUser is null)
            {
                throw new InvalidOperationException("Updated user could not be loaded.");
            }

            return new UpdateMyProfileResponseDto
            {
                UserId = updatedUser.UserId,
                PublicId = updatedUser.PublicId,
                Email = updatedUser.Email,
                FullName = updatedUser.FullName,
                AvatarUrl = updatedUser.AvatarUrl,
                IsEmailVerified = updatedUser.IsEmailVerified,
                Status = updatedUser.Status.ToString(),
                UpdatedAt = updatedUser.UpdatedAt
            };
        }

        private static string? NormalizeOptional(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();

            if (trimmed.Length > maxLength)
            {
                throw new ArgumentException($"Value must not exceed {maxLength} characters.");
            }

            return trimmed;
        }
    }
}