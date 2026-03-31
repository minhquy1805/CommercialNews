using CommercialNews.BuildingBlocks.Abstractions.Execution;
using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Results;
using Identity.Application.Contracts.Responses;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
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

        public async Task<Result<GetMyProfileResponseDto>> ExecuteAsync(
            CancellationToken cancellationToken = default)
        {
            long? currentUserId = _requestContext.CurrentUserId;
            if (currentUserId is null)
            {
                return Result<GetMyProfileResponseDto>.Failure(IdentityErrors.ValidationFailed);
            }

            var user = await _userAccountRepository.GetByIdAsync(
                currentUserId.Value,
                cancellationToken);

            if (user is null)
            {
                return Result<GetMyProfileResponseDto>.Failure(IdentityErrors.User.NotFound);
            }

            if (user.Status == UserAccountStatus.Inactive)
            {
                return Result<GetMyProfileResponseDto>.Failure(IdentityErrors.Auth.AccountInactive);
            }

            if (user.IsLockedAt(_dateTimeProvider.UtcNow))
            {
                return Result<GetMyProfileResponseDto>.Failure(IdentityErrors.AccountLocked);
            }

            return Result<GetMyProfileResponseDto>.Success(new GetMyProfileResponseDto
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
            });
        }
    }
}