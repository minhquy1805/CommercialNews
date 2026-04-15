using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.ChangePassword
{
    public interface IChangePasswordUseCase
    {
        Task<Result<ChangePasswordResponseDto>> ExecuteAsync(
            ChangePasswordRequestDto request,
            CancellationToken cancellationToken = default);
    }
}