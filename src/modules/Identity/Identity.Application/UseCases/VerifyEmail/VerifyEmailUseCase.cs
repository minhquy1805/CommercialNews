using Identity.Application.Contracts.Dtos;
using Identity.Application.Contracts.Ports;

namespace Identity.Application.UseCases.VerifyEmail
{
    public sealed class VerifyEmailUseCase : IVerifyEmailUseCase
    {
        private readonly ITokenHashProvider _tokenHashProvider;
        private readonly IIdentityVerificationRepository _identityVerificationRepository;

        public VerifyEmailUseCase(
            ITokenHashProvider tokenHashProvider,
            IIdentityVerificationRepository identityVerificationRepository)
        {
            _tokenHashProvider = tokenHashProvider;
            _identityVerificationRepository = identityVerificationRepository;
        }

        public async Task<VerifyEmailResponseDto> ExecuteAsync(
           VerifyEmailRequestDto request,
           CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            var tokenHash = _tokenHashProvider.Hash(request.Token);
            var userId = await _identityVerificationRepository.VerifyEmailByTokenHashAsync(
                tokenHash,
                cancellationToken);

            return new VerifyEmailResponseDto
            {
                UserId = userId,
                Verified = true
            };
        }

        private static void ValidateRequest(VerifyEmailRequestDto request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Token))
            {
                throw new ArgumentException("Token is required.", nameof(request.Token));
            }
        }
    }
}
