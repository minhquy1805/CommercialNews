using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.ForgotPassword;

namespace Identity.Application.UseCases.ForgotPassword;

public interface IForgotPasswordUseCase
{
    Task<Result<ForgotPasswordResponseDto>> ExecuteAsync(
        ForgotPasswordRequestDto request,
        CancellationToken cancellationToken = default);
}