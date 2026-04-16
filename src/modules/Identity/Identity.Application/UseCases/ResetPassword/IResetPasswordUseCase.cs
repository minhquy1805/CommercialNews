using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.ResetPassword;

namespace Identity.Application.UseCases.ResetPassword;

public interface IResetPasswordUseCase
{
    Task<Result<ResetPasswordResponseDto>> ExecuteAsync(
        ResetPasswordRequestDto request,
        CancellationToken cancellationToken = default);
}