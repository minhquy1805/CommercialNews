using Identity.Application.Contracts.Dtos;
using Identity.Application.Contracts.Ports;

namespace Identity.Application.UseCases.ResetPassword
{
    public sealed class ResetPasswordUseCase : IResetPasswordUseCase
    {
        private readonly ITokenHashProvider _tokenHashProvider;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IPasswordResetService _passwordResetService;

        public ResetPasswordUseCase(
            ITokenHashProvider tokenHashProvider,
            IPasswordHasher passwordHasher,
            IPasswordResetService passwordResetService)
        {
            _tokenHashProvider = tokenHashProvider;
            _passwordHasher = passwordHasher;
            _passwordResetService = passwordResetService;
        }

        public async Task<ResetPasswordResponseDto> ExecuteAsync(
            ResetPasswordRequestDto request,
            CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            var tokenHash = _tokenHashProvider.Hash(request.Token);
            var newPasswordHash = _passwordHasher.Hash(request.NewPassword);

            var userId = await _passwordResetService.ResetPasswordByTokenHashAsync(
                tokenHash,
                newPasswordHash,
                cancellationToken);

            return new ResetPasswordResponseDto
            {
                UserId = userId,
                PasswordReset = true
            };
        }

        private static void ValidateRequest(ResetPasswordRequestDto request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Token))
            {
                throw new ArgumentException("Token is required.", nameof(request.Token));
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