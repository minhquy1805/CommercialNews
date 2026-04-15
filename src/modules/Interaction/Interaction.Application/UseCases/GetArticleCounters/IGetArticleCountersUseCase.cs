using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Counters.Requests;
using Interaction.Application.Contracts.Counters.Responses;

namespace Interaction.Application.UseCases.GetArticleCounters;

public interface IGetArticleCountersUseCase
{
    Task<Result<GetArticleCountersResponse>> ExecuteAsync(
        GetArticleCountersRequest request,
        CancellationToken cancellationToken = default);
}