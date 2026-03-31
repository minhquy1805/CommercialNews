using CommercialNews.BuildingBlocks.Results;
using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.ForgotPassword
{
    public interface IForgotPasswordUseCase
    {
        Task<Result<ForgotPasswordResponseDto>> ExecuteAsync(
            ForgotPasswordRequestDto request,
            CancellationToken cancellationToken = default);
    }
}