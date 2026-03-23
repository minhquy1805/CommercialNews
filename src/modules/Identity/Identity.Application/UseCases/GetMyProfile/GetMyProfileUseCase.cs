using Identity.Application.Contracts.Dtos;
using Identity.Application.Contracts.Ports;
using Identity.Domain.Enums;

namespace Identity.Application.UseCases.GetMyProfile
{
    public sealed class GetMyProfileUseCase : IGetMyProfileUseCase
    {
        private readonly IRequestContext _requestContext;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IDateTimeProvider _dateTimeProvider;

        public GetMyProfileUseCase(
            IRequestContext requestContext,
            IUserAccountRepository userAccountRepository,
            IDateTimeProvider dateTimeProvider)
        {
            _requestContext = requestContext;
            _userAccountRepository = userAccountRepository;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<GetMyProfileResponseDto> ExecuteAsync(
            CancellationToken cancellationToken)
        {
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

            return new GetMyProfileResponseDto
            {
                UserId = user.UserId,
                PublicId = user.PublicId,
                Email = user.Email,
                FullName = user.FullName,
                AvatarUrl = user.AvatarUrl,
                IsEmailVerified = user.IsEmailVerified,
                Status = user.Status.ToString(),
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
    }
}