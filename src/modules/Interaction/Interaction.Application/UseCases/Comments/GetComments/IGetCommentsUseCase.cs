using CommercialNews.BuildingBlocks.Results;
using Interaction.Application.Contracts.Comments.Requests;
using Interaction.Application.Contracts.Comments.Responses;

namespace Interaction.Application.UseCases.Comments.GetComments;

public interface IGetCommentsUseCase
{
    Task<Result<GetCommentsResponse>> ExecuteAsync(
        GetCommentsRequest request,
        CancellationToken cancellationToken = default);
}