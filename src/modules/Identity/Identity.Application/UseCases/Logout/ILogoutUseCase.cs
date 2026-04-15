using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.Logout
{
    public interface ILogoutUseCase
    {
        Task<Result<LogoutResponseDto>> ExecuteAsync(
            LogoutRequestDto request,
            CancellationToken cancellationToken = default);
    }
}