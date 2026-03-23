using Identity.Application.Contracts.Dtos;
using Identity.Application.UseCases.ChangePassword;
using Identity.Application.UseCases.ForgotPassword;
using Identity.Application.UseCases.GetMyProfile;
using Identity.Application.UseCases.LoginUser;
using Identity.Application.UseCases.Logout;
using Identity.Application.UseCases.RefreshToken;
using Identity.Application.UseCases.RegisterUser;
using Identity.Application.UseCases.ResendVerificationEmail;
using Identity.Application.UseCases.ResetPassword;
using Identity.Application.UseCases.UpdateMyProfile;
using Identity.Application.UseCases.VerifyEmail;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Public.Controllers
{
    [ApiController]
    [Route("api/v1/identity")]
    public sealed class IdentityController : ControllerBase
    {
        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterUserResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(
            [FromBody] RegisterUserRequestDto request,
            [FromServices] IRegisterUserUseCase useCase,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await useCase.ExecuteAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("verify-email")]
        [ProducesResponseType(typeof(VerifyEmailResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> VerifyEmail(
            [FromBody] VerifyEmailRequestDto request,
            [FromServices] IVerifyEmailUseCase useCase,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await useCase.ExecuteAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginUserResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Login(
            [FromBody] LoginUserRequestDto request,
            [FromServices] ILoginUserUseCase useCase,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await useCase.ExecuteAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("forgot-password")]
        [ProducesResponseType(typeof(ForgotPasswordResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] ForgotPasswordRequestDto request,
            [FromServices] IForgotPasswordUseCase useCase,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await useCase.ExecuteAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(ResetPasswordResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordRequestDto request,
            [FromServices] IResetPasswordUseCase useCase,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await useCase.ExecuteAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(RefreshTokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RefreshToken(
            [FromBody] RefreshTokenRequestDto request,
            [FromServices] IRefreshTokenUseCase useCase,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await useCase.ExecuteAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("resend-verification-email")]
        [ProducesResponseType(typeof(ResendVerificationEmailResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ResendVerificationEmail(
            [FromBody] ResendVerificationEmailRequestDto request,
            [FromServices] IResendVerificationEmailUseCase useCase,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await useCase.ExecuteAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("change-password")]
        [ProducesResponseType(typeof(ChangePasswordResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordRequestDto request,
            [FromServices] IChangePasswordUseCase useCase,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await useCase.ExecuteAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("logout")]
        [ProducesResponseType(typeof(LogoutResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Logout(
            [FromBody] LogoutRequestDto request,
            [FromServices] ILogoutUseCase useCase,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await useCase.ExecuteAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpGet("me")]
        [ProducesResponseType(typeof(GetMyProfileResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> GetMyProfile(
            [FromServices] IGetMyProfileUseCase useCase,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await useCase.ExecuteAsync(cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPut("me")]
        [ProducesResponseType(typeof(UpdateMyProfileResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateMyProfile(
            [FromBody] UpdateMyProfileRequestDto request,
            [FromServices] IUpdateMyProfileUseCase useCase,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await useCase.ExecuteAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }
    }
}
