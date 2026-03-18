using Identity.Application.Contracts.Dtos;

namespace Identity.Application.UseCases.VerifyEmail
{
    public interface IVerifyEmailUseCase
    {
        Task<VerifyEmailResponseDto> ExecuteAsync(
            VerifyEmailRequestDto request,
            CancellationToken cancellationToken);
    }
}
