using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.VerifyEmail;

namespace Identity.Application.UseCases.VerifyEmail;

public interface IVerifyEmailUseCase
{
    Task<Result<VerifyEmailResponseDto>> ExecuteAsync(
        VerifyEmailRequestDto request,
        CancellationToken cancellationToken = default);
}