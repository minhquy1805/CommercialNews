using CommercialNews.BuildingBlocks.Results;
using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.RegisterUser
{
    public interface IRegisterUserUseCase
    {
        Task<Result<RegisterUserResponseDto>> ExecuteAsync(
            RegisterUserRequestDto request,
            CancellationToken cancellationToken = default);
    }
}