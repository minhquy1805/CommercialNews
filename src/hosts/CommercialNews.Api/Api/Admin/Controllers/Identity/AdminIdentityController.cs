using CommercialNews.Api.Api.Admin.Contracts.Identity.User.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Identity.User.Responses;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Authorization;
using Identity.Application.Contracts.LoginHistory.GetUserLoginHistory;
using Identity.Application.Contracts.Users.ActivateUser;
using Identity.Application.Contracts.Users.DisableUser;
using Identity.Application.Contracts.Users.GetUserDetail;
using Identity.Application.Contracts.Users.GetUserSecuritySummary;
using Identity.Application.Contracts.Users.GetUserSessions;
using Identity.Application.Contracts.Users.ListUsers;
using Identity.Application.Contracts.Users.LockUser;
using Identity.Application.Contracts.Users.MarkEmailVerified;
using Identity.Application.Contracts.Users.RevokeUserSessions;
using Identity.Application.Contracts.Users.UnlockUser;
using Identity.Application.UseCases.LoginHistory.GetUserLoginHistory;
using Identity.Application.UseCases.Users.ActivateUser;
using Identity.Application.UseCases.Users.DisableUser;
using Identity.Application.UseCases.Users.GetUserDetail;
using Identity.Application.UseCases.Users.GetUserSecuritySummary;
using Identity.Application.UseCases.Users.GetUserSessions;
using Identity.Application.UseCases.Users.ListUsers;
using Identity.Application.UseCases.Users.LockUser;
using Identity.Application.UseCases.Users.MarkEmailVerified;
using Identity.Application.UseCases.Users.RevokeUserSessions;
using Identity.Application.UseCases.Users.UnlockUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers.Identity;

[Authorize]
[ApiController]
[Route("api/v1/admin/identity/users")]
public sealed class AdminIdentityController : ControllerBase
{
    [Authorize(Policy = AuthorizationPolicies.IdentityUsersRead)]
    [HttpGet]
    [ProducesResponseType(typeof(UserListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListUsers(
        [FromQuery] DateTime? fromCreatedAt,
        [FromQuery] DateTime? toCreatedAt,
        [FromQuery] string? status,
        [FromQuery] bool? isEmailVerified,
        [FromQuery] string? query,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromServices] IListUsersUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new ListUsersRequestDto
            {
                FromCreatedAt = fromCreatedAt,
                ToCreatedAt = toCreatedAt,
                Status = status,
                IsEmailVerified = isEmailVerified,
                Query = query,
                Page = page ?? 1,
                PageSize = pageSize ?? 20
            },
            cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(MapUserListResponse(result.Value!));
        }

        return this.ToActionResult(result);
    }

    [Authorize(Policy = AuthorizationPolicies.IdentityUsersRead)]
    [HttpGet("{userId:long}")]
    [ProducesResponseType(typeof(UserDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserDetail(
        [FromRoute] long userId,
        [FromServices] IGetUserDetailUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new GetUserDetailRequestDto
            {
                UserId = userId
            },
            cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(MapUserDetailResponse(result.Value!));
        }

        return this.ToActionResult(result);
    }

    [Authorize(Policy = AuthorizationPolicies.IdentityUsersReadSecurity)]
    [HttpGet("{userId:long}/sessions")]
    [ProducesResponseType(typeof(UserSessionListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserSessions(
        [FromRoute] long userId,
        [FromServices] IGetUserSessionsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new GetUserSessionsRequestDto
            {
                UserId = userId
            },
            cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(MapUserSessionListResponse(result.Value!));
        }

        return this.ToActionResult(result);
    }

    [Authorize(Policy = AuthorizationPolicies.IdentityUsersReadSecurity)]
    [HttpGet("{userId:long}/login-history")]
    [ProducesResponseType(typeof(UserLoginHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserLoginHistory(
        [FromRoute] long userId,
        [FromQuery] bool? succeeded,
        [FromQuery] DateTime? fromAttemptedAt,
        [FromQuery] DateTime? toAttemptedAt,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromServices] IGetUserLoginHistoryUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new GetUserLoginHistoryRequestDto
            {
                UserId = userId,
                Succeeded = succeeded,
                FromAttemptedAt = fromAttemptedAt,
                ToAttemptedAt = toAttemptedAt,
                Page = page ?? 1,
                PageSize = pageSize ?? 20
            },
            cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(MapUserLoginHistoryResponse(result.Value!));
        }

        return this.ToActionResult(result);
    }

    [Authorize(Policy = AuthorizationPolicies.IdentityUsersReadSecurity)]
    [HttpGet("{userId:long}/security-summary")]
    [ProducesResponseType(typeof(UserSecuritySummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserSecuritySummary(
        [FromRoute] long userId,
        [FromServices] IGetUserSecuritySummaryUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new GetUserSecuritySummaryRequestDto
            {
                UserId = userId
            },
            cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(MapUserSecuritySummaryResponse(result.Value!));
        }

        return this.ToActionResult(result);
    }

    [Authorize(Policy = AuthorizationPolicies.IdentityUsersManageStatus)]
    [HttpPost("{userId:long}:activate")]
    [ProducesResponseType(typeof(ActivateUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ActivateUser(
        [FromRoute] long userId,
        [FromBody] UserActionReasonRequest? request,
        [FromServices] IActivateUserUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new ActivateUserRequestDto
            {
                UserId = userId,
                Reason = request?.Reason
            },
            cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(MapActivateUserResponse(result.Value!));
        }

        return this.ToActionResult(result);
    }

    [Authorize(Policy = AuthorizationPolicies.IdentityUsersManageStatus)]
    [HttpPost("{userId:long}:disable")]
    [ProducesResponseType(typeof(DisableUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DisableUser(
        [FromRoute] long userId,
        [FromBody] DisableUserRequest? request,
        [FromServices] IDisableUserUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new DisableUserRequestDto
            {
                UserId = userId,
                Reason = request?.Reason,
                RevokeSessions = request?.RevokeSessions ?? true
            },
            cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(MapDisableUserResponse(result.Value!));
        }

        return this.ToActionResult(result);
    }

    [Authorize(Policy = AuthorizationPolicies.IdentityUsersManageSecurity)]
    [HttpPost("{userId:long}:lock")]
    [ProducesResponseType(typeof(LockUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> LockUser(
        [FromRoute] long userId,
        [FromBody] LockUserRequest request,
        [FromServices] ILockUserUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new LockUserRequestDto
            {
                UserId = userId,
                LockedUntilUtc = request.LockedUntilUtc,
                Reason = request.Reason,
                RevokeSessions = request.RevokeSessions
            },
            cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(MapLockUserResponse(result.Value!));
        }

        return this.ToActionResult(result);
    }

    [Authorize(Policy = AuthorizationPolicies.IdentityUsersManageSecurity)]
    [HttpPost("{userId:long}:unlock")]
    [ProducesResponseType(typeof(UnlockUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UnlockUser(
        [FromRoute] long userId,
        [FromBody] UserActionReasonRequest? request,
        [FromServices] IUnlockUserUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new UnlockUserRequestDto
            {
                UserId = userId,
                Reason = request?.Reason
            },
            cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(MapUnlockUserResponse(result.Value!));
        }

        return this.ToActionResult(result);
    }

    [Authorize(Policy = AuthorizationPolicies.IdentityUsersVerifyEmail)]
    [HttpPost("{userId:long}:mark-email-verified")]
    [ProducesResponseType(typeof(MarkEmailVerifiedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarkEmailVerified(
        [FromRoute] long userId,
        [FromBody] UserActionReasonRequest? request,
        [FromServices] IMarkEmailVerifiedUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new MarkEmailVerifiedRequestDto
            {
                UserId = userId,
                Reason = request?.Reason
            },
            cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(MapMarkEmailVerifiedResponse(result.Value!));
        }

        return this.ToActionResult(result);
    }

    [Authorize(Policy = AuthorizationPolicies.IdentityUsersRevokeSessions)]
    [HttpPost("{userId:long}:revoke-sessions")]
    [ProducesResponseType(typeof(RevokeUserSessionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RevokeUserSessions(
        [FromRoute] long userId,
        [FromBody] RevokeUserSessionsRequest? request,
        [FromServices] IRevokeUserSessionsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new RevokeUserSessionsRequestDto
            {
                UserId = userId,
                Reason = request?.Reason
            },
            cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(MapRevokeUserSessionsResponse(result.Value!));
        }

        return this.ToActionResult(result);
    }

    private static UserListResponse MapUserListResponse(ListUsersResponseDto source)
    {
        return new UserListResponse
        {
            Items = source.Items.Select(MapUserListItemResponse).ToArray(),
            Page = source.Page,
            PageSize = source.PageSize,
            TotalItems = source.TotalItems
        };
    }

    private static UserListItemResponse MapUserListItemResponse(UserListItemDto source)
    {
        return new UserListItemResponse
        {
            UserId = source.UserId,
            PublicId = source.PublicId,
            Email = source.Email,
            EmailNormalized = source.EmailNormalized,
            FullName = source.FullName,
            AvatarUrl = source.AvatarUrl,
            IsEmailVerified = source.IsEmailVerified,
            EmailVerifiedAt = source.EmailVerifiedAt,
            Status = source.Status,
            LockedUntil = source.LockedUntil,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            LastLoginAt = source.LastLoginAt,
            Version = source.Version
        };
    }

    private static UserDetailResponse MapUserDetailResponse(GetUserDetailResponseDto source)
    {
        return new UserDetailResponse
        {
            UserId = source.UserId,
            PublicId = source.PublicId,
            Email = source.Email,
            EmailNormalized = source.EmailNormalized,
            FullName = source.FullName,
            AvatarUrl = source.AvatarUrl,
            IsEmailVerified = source.IsEmailVerified,
            EmailVerifiedAt = source.EmailVerifiedAt,
            Status = source.Status,
            LockedUntil = source.LockedUntil,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            LastLoginAt = source.LastLoginAt,
            Version = source.Version
        };
    }

    private static UserSessionListResponse MapUserSessionListResponse(GetUserSessionsResponseDto source)
    {
        return new UserSessionListResponse
        {
            UserId = source.UserId,
            Items = source.Items
                .Select(item => new UserSessionItemResponse
                {
                    RefreshTokenId = item.RefreshTokenId,
                    UserId = item.UserId,
                    CreatedAt = item.CreatedAt,
                    ExpiresAt = item.ExpiresAt,
                    RevokedAt = item.RevokedAt,
                    RevokedReason = item.RevokedReason,
                    CreatedIp = item.CreatedIp,
                    UserAgent = item.UserAgent,
                    CorrelationId = item.CorrelationId,
                    IsRevoked = item.IsRevoked,
                    IsExpired = item.IsExpired,
                    IsActive = item.IsActive
                })
                .ToArray()
        };
    }

    private static UserLoginHistoryResponse MapUserLoginHistoryResponse(
        GetUserLoginHistoryResponseDto source)
    {
        return new UserLoginHistoryResponse
        {
            UserId = source.UserId,
            Items = source.Items
                .Select(item => new UserLoginHistoryItemResponse
                {
                    LoginId = item.LoginId,
                    UserId = item.UserId,
                    Succeeded = item.Succeeded,
                    FailureReason = item.FailureReason,
                    AttemptedAt = item.AttemptedAt,
                    IpAddress = item.IpAddress,
                    UserAgent = item.UserAgent,
                    CorrelationId = item.CorrelationId
                })
                .ToArray(),
            Page = source.Page,
            PageSize = source.PageSize,
            TotalItems = source.TotalItems
        };
    }

    private static UserSecuritySummaryResponse MapUserSecuritySummaryResponse(
        GetUserSecuritySummaryResponseDto source)
    {
        return new UserSecuritySummaryResponse
        {
            UserId = source.UserId,
            PublicId = source.PublicId,
            Email = source.Email,
            FullName = source.FullName,
            IsEmailVerified = source.IsEmailVerified,
            Status = source.Status,
            LockedUntil = source.LockedUntil,
            LastLoginAt = source.LastLoginAt,
            TotalSessionCount = source.TotalSessionCount,
            ActiveSessionCount = source.ActiveSessionCount,
            RevokedSessionCount = source.RevokedSessionCount,
            ExpiredSessionCount = source.ExpiredSessionCount,
            LoginSuccessCount = source.LoginSuccessCount,
            LoginFailureCount = source.LoginFailureCount,
            FailedLoginCountLast7Days = source.FailedLoginCountLast7Days,
            RecentFailedLoginAt = source.RecentFailedLoginAt,
            LastPasswordResetRequestedAt = source.LastPasswordResetRequestedAt,
            PasswordResetTokenCount = source.PasswordResetTokenCount,
            ActivePasswordResetTokenCount = source.ActivePasswordResetTokenCount
        };
    }

    private static ActivateUserResponse MapActivateUserResponse(ActivateUserResponseDto source)
    {
        return new ActivateUserResponse
        {
            UserId = source.UserId,
            PublicId = source.PublicId,
            Email = source.Email,
            Status = source.Status,
            Activated = source.Activated,
            ActivatedAtUtc = source.ActivatedAtUtc
        };
    }

    private static DisableUserResponse MapDisableUserResponse(DisableUserResponseDto source)
    {
        return new DisableUserResponse
        {
            UserId = source.UserId,
            PublicId = source.PublicId,
            Email = source.Email,
            Status = source.Status,
            Disabled = source.Disabled,
            SessionsRevoked = source.SessionsRevoked,
            RevokedSessionCount = source.RevokedSessionCount,
            DisabledAtUtc = source.DisabledAtUtc
        };
    }

    private static LockUserResponse MapLockUserResponse(LockUserResponseDto source)
    {
        return new LockUserResponse
        {
            UserId = source.UserId,
            PublicId = source.PublicId,
            Email = source.Email,
            Status = source.Status,
            LockedUntilUtc = source.LockedUntilUtc,
            Locked = source.Locked,
            SessionsRevoked = source.SessionsRevoked,
            RevokedSessionCount = source.RevokedSessionCount,
            LockedAtUtc = source.LockedAtUtc
        };
    }

    private static UnlockUserResponse MapUnlockUserResponse(UnlockUserResponseDto source)
    {
        return new UnlockUserResponse
        {
            UserId = source.UserId,
            PublicId = source.PublicId,
            Email = source.Email,
            Status = source.Status,
            Unlocked = source.Unlocked,
            UnlockedAtUtc = source.UnlockedAtUtc
        };
    }

    private static MarkEmailVerifiedResponse MapMarkEmailVerifiedResponse(
        MarkEmailVerifiedResponseDto source)
    {
        return new MarkEmailVerifiedResponse
        {
            UserId = source.UserId,
            PublicId = source.PublicId,
            Email = source.Email,
            IsEmailVerified = source.IsEmailVerified,
            WasAlreadyVerified = source.WasAlreadyVerified,
            Status = source.Status,
            MarkedVerifiedAtUtc = source.MarkedVerifiedAtUtc
        };
    }

    private static RevokeUserSessionsResponse MapRevokeUserSessionsResponse(
        RevokeUserSessionsResponseDto source)
    {
        return new RevokeUserSessionsResponse
        {
            UserId = source.UserId,
            PublicId = source.PublicId,
            Email = source.Email,
            RevokedSessionCount = source.RevokedSessionCount,
            RevokedAtUtc = source.RevokedAtUtc
        };
    }
}
