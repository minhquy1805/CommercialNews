using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Contracts.GetMyProfile;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Domain.Enums;

namespace Identity.Application.UseCases.GetMyProfile;

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
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _userAccountRepository = userAccountRepository ?? throw new ArgumentNullException(nameof(userAccountRepository));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
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

        if (string.Equals(user.Status, UserAccountStatuses.Disabled, StringComparison.OrdinalIgnoreCase))
        {
            return Result<GetMyProfileResponseDto>.Failure(IdentityErrors.Auth.AccountDisabled);
        }

        if (user.IsLockedAt(_dateTimeProvider.UtcNow))
        {
            return Result<GetMyProfileResponseDto>.Failure(IdentityErrors.Auth.AccountLocked);
        }

        return Result<GetMyProfileResponseDto>.Success(new GetMyProfileResponseDto
        {
            UserId = user.UserId,
            PublicId = user.PublicId,
            Email = user.Email,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            IsEmailVerified = user.IsEmailVerified,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt
        });
    }
}