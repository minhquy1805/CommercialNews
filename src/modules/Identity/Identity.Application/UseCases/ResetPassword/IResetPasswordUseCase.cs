using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.ResetPassword
{
    public interface IResetPasswordUseCase
    {
        Task<Result<ResetPasswordResponseDto>> ExecuteAsync(
            ResetPasswordRequestDto request,
            CancellationToken cancellationToken = default);
    }
}