using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Api.Public.Identity.Contracts.Recovery.Requests;
using CommercialNews.Api.Api.Public.Identity.Contracts.Recovery.Responses;
using CommercialNews.Api.Api.Public.Identity.Contracts.Session.Requests;
using CommercialNews.Api.Api.Public.Identity.Contracts.Session.Responses;
using CommercialNews.Api.Api.Public.Identity.Contracts.User.Requests;
using CommercialNews.Api.Api.Public.Identity.Contracts.User.Responses;
using Identity.Application.Contracts.ChangePassword;
using Identity.Application.Contracts.ForgotPassword;
using Identity.Application.Contracts.LoginHistory.GetMyLoginHistory;
using Identity.Application.Contracts.LoginUser;
using Identity.Application.Contracts.Logout;
using Identity.Application.Contracts.RefreshToken;
using Identity.Application.Contracts.RegisterUser;
using Identity.Application.Contracts.ResendVerificationEmail;
using Identity.Application.Contracts.ResetPassword;
using Identity.Application.Contracts.UpdateMyAvatar;
using Identity.Application.Contracts.UpdateMyProfile;
using Identity.Application.Contracts.VerifyEmail;
using Identity.Application.UseCases.ChangePassword;
using Identity.Application.UseCases.ForgotPassword;
using Identity.Application.UseCases.GetMyProfile;
using Identity.Application.UseCases.LoginHistory.GetMyLoginHistory;
using Identity.Application.UseCases.LoginUser;
using Identity.Application.UseCases.Logout;
using Identity.Application.UseCases.LogoutAllSessions;
using Identity.Application.UseCases.RefreshToken;
using Identity.Application.UseCases.RegisterUser;
using Identity.Application.UseCases.ResendVerificationEmail;
using Identity.Application.UseCases.ResetPassword;
using Identity.Application.UseCases.UpdateMyAvatar;
using Identity.Application.UseCases.UpdateMyProfile;
using Identity.Application.UseCases.VerifyEmail;
using Microsoft.AspNetCore.Authorization;
using CommercialNews.Api.Api.Common.Auth;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Public.Controllers.Identity;

[ApiController]
[Route("api/v1/auth")]
public sealed class IdentityController : ControllerBase
{

    private readonly IHostEnvironment _environment;

    public IdentityController(IHostEnvironment environment)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        [FromServices] IRegisterUserUseCase useCase,
        CancellationToken cancellationToken)
    {
        var applicationRequest = new RegisterUserRequestDto
        {
            Email = request.Email,
            Password = request.Password,
            FullName = request.FullName
        };

        var result = await useCase.ExecuteAsync(applicationRequest, cancellationToken);

        if (result.IsSuccess)
        {
            var value = result.Value!;
            return Ok(new RegisterResponse
            {
                UserId = value.UserId,
                PublicId = value.PublicId,
                Email = value.Email,
                RequiresEmailVerification = value.RequiresEmailVerification
            });
        }

        return this.ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(VerifyEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        [FromServices] IVerifyEmailUseCase useCase,
        CancellationToken cancellationToken)
    {
        var applicationRequest = new VerifyEmailRequestDto
        {
            Token = request.Token
        };

        var result = await useCase.ExecuteAsync(applicationRequest, cancellationToken);

        if (result.IsSuccess)
        {
            var value = result.Value!;
            return Ok(new VerifyEmailResponse
            {
                UserId = value.UserId,
                Verified = value.Verified
            });
        }

        return this.ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] ILoginUserUseCase useCase,
        CancellationToken cancellationToken)
    {
        var applicationRequest = new LoginUserRequestDto
        {
            Email = request.Email,
            Password = request.Password,
            RememberMe = request.RememberMe
        };

        var result = await useCase.ExecuteAsync(applicationRequest, cancellationToken);

        if (result.IsSuccess)
        {
            var value = result.Value!;

            if (!string.IsNullOrWhiteSpace(value.RefreshToken) &&
                value.RefreshTokenExpiresAtUtc.HasValue)
            {
                Response.SetRefreshTokenCookie(
                    value.RefreshToken,
                    value.RefreshTokenExpiresAtUtc.Value,
                    _environment.IsProduction());
            }
            else
            {
                Response.ClearRefreshTokenCookie(_environment.IsProduction());
            }

            return Ok(new LoginResponse
            {
                UserId = value.UserId,
                PublicId = value.PublicId,
                Email = value.Email,
                AccessToken = value.AccessToken,
                AccessTokenExpiresAtUtc = value.AccessTokenExpiresAtUtc
            });
        }

        return this.ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        [FromServices] IForgotPasswordUseCase useCase,
        CancellationToken cancellationToken)
    {
        var applicationRequest = new ForgotPasswordRequestDto
        {
            Email = request.Email
        };

        var result = await useCase.ExecuteAsync(applicationRequest, cancellationToken);

        if (result.IsSuccess)
        {
            var value = result.Value!;
            return Ok(new ForgotPasswordResponse
            {
                Requested = value.Requested,
                Message = value.Message
            });
        }

        return this.ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        [FromServices] IResetPasswordUseCase useCase,
        CancellationToken cancellationToken)
    {
        var applicationRequest = new ResetPasswordRequestDto
        {
            Token = request.Token,
            NewPassword = request.NewPassword
        };

        var result = await useCase.ExecuteAsync(applicationRequest, cancellationToken);

        if (result.IsSuccess)
        {
            var value = result.Value!;
            return Ok(new ResetPasswordResponse
            {
                UserId = value.UserId,
                PasswordReset = value.PasswordReset
            });
        }

        return this.ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RefreshToken(
        [FromServices] IRefreshTokenUseCase useCase,
        CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[AuthCookieNames.RefreshToken];

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            Response.ClearRefreshTokenCookie(_environment.IsProduction());
            return Unauthorized();
        }

        var applicationRequest = new RefreshTokenRequestDto
        {
            RefreshToken = refreshToken
        };

        var result = await useCase.ExecuteAsync(applicationRequest, cancellationToken);

        if (result.IsSuccess)
        {
            var value = result.Value!;

            Response.SetRefreshTokenCookie(
                value.RefreshToken,
                value.RefreshTokenExpiresAtUtc,
                _environment.IsProduction());

            return Ok(new RefreshTokenResponse
            {
                UserId = value.UserId,
                AccessToken = value.AccessToken,
                AccessTokenExpiresAtUtc = value.AccessTokenExpiresAtUtc
            });
        }

        Response.ClearRefreshTokenCookie(_environment.IsProduction());

        return this.ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpPost("resend-verification")]
    [ProducesResponseType(typeof(ResendVerificationEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendVerificationEmail(
        [FromBody] ResendVerificationEmailRequest request,
        [FromServices] IResendVerificationEmailUseCase useCase,
        CancellationToken cancellationToken)
    {
        var applicationRequest = new ResendVerificationEmailRequestDto
        {
            Email = request.Email
        };

        var result = await useCase.ExecuteAsync(applicationRequest, cancellationToken);

        if (result.IsSuccess)
        {
            var value = result.Value!;
            return Ok(new ResendVerificationEmailResponse
            {
                Requested = value.Requested,
                Message = value.Message
            });
        }

        return this.ToActionResult(result);
    }
    
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ChangePasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        [FromServices] IChangePasswordUseCase useCase,
        CancellationToken cancellationToken)
    {
        var applicationRequest = new ChangePasswordRequestDto
        {
            CurrentPassword = request.CurrentPassword,
            NewPassword = request.NewPassword
        };

        var result = await useCase.ExecuteAsync(applicationRequest, cancellationToken);

        if (result.IsSuccess)
        {
            var value = result.Value!;
            return Ok(new ChangePasswordResponse
            {
                UserId = value.UserId,
                PasswordChanged = value.PasswordChanged
            });
        }

        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(LogoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Logout(
        [FromServices] ILogoutUseCase useCase,
        CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[AuthCookieNames.RefreshToken];

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            Response.ClearRefreshTokenCookie(_environment.IsProduction());

            return Ok(new LogoutResponse
            {
                LoggedOut = true
            });
        }

        var applicationRequest = new LogoutRequestDto
        {
            RefreshToken = refreshToken
        };

        var result = await useCase.ExecuteAsync(applicationRequest, cancellationToken);

        Response.ClearRefreshTokenCookie(_environment.IsProduction());

        if (result.IsSuccess)
        {
            var value = result.Value!;

            return Ok(new LogoutResponse
            {
                UserId = value.UserId,
                LoggedOut = value.LoggedOut
            });
        }

        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(GetMyProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(
        [FromServices] IGetMyProfileUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(cancellationToken);

        if (result.IsSuccess)
        {
            var value = result.Value!;
            return Ok(new GetMyProfileResponse
            {
                UserId = value.UserId,
                PublicId = value.PublicId,
                Email = value.Email,
                FullName = value.FullName,
                AvatarUrl = value.AvatarUrl,
                IsEmailVerified = value.IsEmailVerified,
                Status = value.Status,
                CreatedAt = value.CreatedAt,
                UpdatedAt = value.UpdatedAt,
                LastLoginAt = value.LastLoginAt
            });
        }

        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpGet("me/login-history")]
    [ProducesResponseType(typeof(GetMyLoginHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyLoginHistory(
        [FromQuery] bool? succeeded,
        [FromQuery] DateTime? fromAttemptedAt,
        [FromQuery] DateTime? toAttemptedAt,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromServices] IGetMyLoginHistoryUseCase useCase,
        CancellationToken cancellationToken)
    {
        var applicationRequest = new GetMyLoginHistoryRequestDto
        {
            Succeeded = succeeded,
            FromAttemptedAt = fromAttemptedAt,
            ToAttemptedAt = toAttemptedAt,
            Page = page ?? 1,
            PageSize = pageSize ?? 20
        };

        var result = await useCase.ExecuteAsync(applicationRequest, cancellationToken);

        if (result.IsSuccess)
        {
            var value = result.Value!;
            return Ok(new GetMyLoginHistoryResponse
            {
                Items = value.Items
                    .Select(item => new LoginHistoryItemResponse
                    {
                        LoginId = item.LoginId,
                        Succeeded = item.Succeeded,
                        FailureReason = item.FailureReason,
                        AttemptedAt = item.AttemptedAt,
                        IpAddress = item.IpAddress,
                        UserAgent = item.UserAgent,
                        CorrelationId = item.CorrelationId
                    })
                    .ToArray(),
                Page = value.Page,
                PageSize = value.PageSize,
                TotalItems = value.TotalItems
            });
        }

        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpPut("me")]
    [ProducesResponseType(typeof(UpdateMyProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateMyProfileRequest request,
        [FromServices] IUpdateMyProfileUseCase useCase,
        CancellationToken cancellationToken)
    {
        var applicationRequest = new UpdateMyProfileRequestDto
        {
            FullName = request.FullName
        };

        var result = await useCase.ExecuteAsync(applicationRequest, cancellationToken);

        if (result.IsSuccess)
        {
            var value = result.Value!;
            return Ok(new UpdateMyProfileResponse
            {
                UserId = value.UserId,
                PublicId = value.PublicId,
                Email = value.Email,
                FullName = value.FullName,
                AvatarUrl = value.AvatarUrl,
                IsEmailVerified = value.IsEmailVerified,
                Status = value.Status,
                UpdatedAt = value.UpdatedAt
            });
        }

        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpPut("me/avatar")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UpdateMyAvatarResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyAvatar(
        [FromForm] UpdateMyAvatarRequest request,
        [FromServices] IUpdateMyAvatarUseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.File is null)
        {
            var missingFileResult = await useCase.ExecuteAsync(
                new UpdateMyAvatarRequestDto(),
                cancellationToken);

            return this.ToActionResult(missingFileResult);
        }

        await using Stream stream = request.File.OpenReadStream();

        var applicationRequest = new UpdateMyAvatarRequestDto
        {
            Content = stream,
            OriginalFileName = request.File.FileName,
            ContentType = request.File.ContentType,
            Length = request.File.Length
        };

        var result = await useCase.ExecuteAsync(applicationRequest, cancellationToken);

        if (result.IsSuccess)
        {
            var value = result.Value!;
            return Ok(new UpdateMyAvatarResponse
            {
                UserId = value.UserId,
                PublicId = value.PublicId,
                Email = value.Email,
                FullName = value.FullName,
                AvatarUrl = value.AvatarUrl,
                IsEmailVerified = value.IsEmailVerified,
                Status = value.Status,
                UpdatedAt = value.UpdatedAt
            });
        }

        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpPost("logout-all-sessions")]
    [ProducesResponseType(typeof(LogoutAllSessionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LogoutAllSessions(
        [FromServices] ILogoutAllSessionsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(cancellationToken);

        if (result.IsSuccess)
        {
            var value = result.Value!;

            Response.ClearRefreshTokenCookie(_environment.IsProduction());

            return Ok(new LogoutAllSessionsResponse
            {
                UserId = value.UserId,
                LoggedOutAllSessions = value.LoggedOutAllSessions
            });
        }

        return this.ToActionResult(result);
    }
}
