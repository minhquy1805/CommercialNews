using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.ResendVerificationEmail;

namespace Identity.Application.UseCases.ResendVerificationEmail;

public interface IResendVerificationEmailUseCase
{
    Task<Result<ResendVerificationEmailResponseDto>> ExecuteAsync(
        ResendVerificationEmailRequestDto request,
        CancellationToken cancellationToken = default);
}