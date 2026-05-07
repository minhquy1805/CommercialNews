using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Users.ListUsers;
using Identity.Application.Errors;
using Identity.Application.Models.QueryModels;
using Identity.Application.Ports.Persistence;
using Identity.Application.Validation.Users.ListUsers;

namespace Identity.Application.UseCases.Users.ListUsers;

public sealed class ListUsersUseCase : IListUsersUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IUserAccountRepository _userAccountRepository;

    public ListUsersUseCase(
        IRequestContext requestContext,
        IUserAccountRepository userAccountRepository)
    {
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _userAccountRepository = userAccountRepository ?? throw new ArgumentNullException(nameof(userAccountRepository));
    }

    public async Task<Result<ListUsersResponseDto>> ExecuteAsync(
        ListUsersRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = ListUsersValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<ListUsersResponseDto>.Failure(validationError);
        }

        long? currentUserId = _requestContext.CurrentUserId;
        if (currentUserId is null)
        {
            return Result<ListUsersResponseDto>.Failure(IdentityErrors.Auth.Unauthenticated);
        }

        int page = ListUsersValidator.NormalizePage(request.Page);
        int pageSize = ListUsersValidator.NormalizePageSize(request.PageSize);

        var query = new UserAccountListQuery
        {
            FromCreatedAt = request.FromCreatedAt,
            ToCreatedAt = request.ToCreatedAt,
            Status = ListUsersValidator.Normalize(request.Status),
            IsEmailVerified = request.IsEmailVerified,
            Query = ListUsersValidator.Normalize(request.Query),
            Page = page,
            PageSize = pageSize
        };

        try
        {
            var result = await _userAccountRepository.SelectSkipAndTakeAsync(
                query,
                cancellationToken);

            return Result<ListUsersResponseDto>.Success(
                new ListUsersResponseDto
                {
                    Items = result.Items
                        .Select(MapItem)
                        .ToArray(),
                    Page = result.Page,
                    PageSize = result.PageSize,
                    TotalItems = result.TotalItems
                });
        }
        catch (PersistenceException)
        {
            return Result<ListUsersResponseDto>.Failure(
                IdentityErrors.User.QueryFailed);
        }
    }

    private static UserListItemDto MapItem(
        UserAccountListResultItem item)
    {
        return new UserListItemDto
        {
            UserId = item.UserId,
            PublicId = item.PublicId,
            Email = item.Email,
            EmailNormalized = item.EmailNormalized,
            FullName = item.FullName,
            AvatarUrl = item.AvatarUrl,
            IsEmailVerified = item.IsEmailVerified,
            EmailVerifiedAt = item.EmailVerifiedAt,
            Status = item.Status,
            LockedUntil = item.LockedUntil,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            LastLoginAt = item.LastLoginAt,
            Version = item.Version
        };
    }
}