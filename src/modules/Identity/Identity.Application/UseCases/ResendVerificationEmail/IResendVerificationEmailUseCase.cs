using Identity.Application.Contracts.Dtos;

namespace Identity.Application.UseCases.ResendVerificationEmail
{
    public interface IResendVerificationEmailUseCase
    {
        Task<ResendVerificationEmailResponseDto> ExecuteAsync(
            ResendVerificationEmailRequestDto request,
            CancellationToken cancellationToken);
    }
}

