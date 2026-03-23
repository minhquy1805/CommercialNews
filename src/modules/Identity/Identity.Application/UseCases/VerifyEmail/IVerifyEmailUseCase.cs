using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.VerifyEmail
{
    public interface IVerifyEmailUseCase
    {
        Task<VerifyEmailResponseDto> ExecuteAsync(
            VerifyEmailRequestDto request,
            CancellationToken cancellationToken);
    }
}
