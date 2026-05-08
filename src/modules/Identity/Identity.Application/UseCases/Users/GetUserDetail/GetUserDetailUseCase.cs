using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Users.GetUserDetail;
using Identity.Application.Errors;
using Identity.Application.Models.QueryModels;
using Identity.Application.Ports.Persistence;
using Identity.Application.Validation.Users.GetUserDetail;

namespace Identity.Application.UseCases.Users.GetUserDetail;

public sealed class GetUserDetailUseCase : IGetUserDetailUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IUserAccountRepository _userAccountRepository;

    public GetUserDetailUseCase(
        IRequestContext requestContext,
        IUserAccountRepository userAccountRepository)
    {
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _userAccountRepository = userAccountRepository ?? throw new ArgumentNullException(nameof(userAccountRepository));
    }

    public async Task<Result<GetUserDetailResponseDto>> ExecuteAsync(
        GetUserDetailRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = GetUserDetailValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<GetUserDetailResponseDto>.Failure(validationError);
        }

        long? currentUserId = _requestContext.CurrentUserId;
        if (currentUserId is null)
        {
            return Result<GetUserDetailResponseDto>.Failure(IdentityErrors.Auth.Unauthenticated);
        }

        try
        {
            UserAccountDetailResult? user = await _userAccountRepository.SelectDetailByIdAsync(
                request.UserId,
                cancellationToken);

            if (user is null)
            {
                return Result<GetUserDetailResponseDto>.Failure(IdentityErrors.User.NotFound);
            }

            return Result<GetUserDetailResponseDto>.Success(MapResponse(user));
        }
        catch (PersistenceException)
        {
            return Result<GetUserDetailResponseDto>.Failure(IdentityErrors.User.QueryFailed);
        }
    }

    private static GetUserDetailResponseDto MapResponse(
        UserAccountDetailResult user)
    {
        return new GetUserDetailResponseDto
        {
            UserId = user.UserId,
            PublicId = user.PublicId,
            Email = user.Email,
            EmailNormalized = user.EmailNormalized,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            IsEmailVerified = user.IsEmailVerified,
            EmailVerifiedAt = user.EmailVerifiedAt,
            Status = user.Status,
            LockedUntil = user.LockedUntil,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt,
            Version = user.Version
        };
    }
}