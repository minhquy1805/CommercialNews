using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.ResendVerificationEmail
{
    public interface IResendVerificationEmailUseCase
    {
        Task<ResendVerificationEmailResponseDto> ExecuteAsync(
            ResendVerificationEmailRequestDto request,
            CancellationToken cancellationToken);
    }
}

