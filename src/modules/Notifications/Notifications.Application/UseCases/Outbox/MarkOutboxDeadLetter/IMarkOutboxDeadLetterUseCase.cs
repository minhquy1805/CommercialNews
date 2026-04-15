using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Contracts.Outbox.Responses;

namespace Notifications.Application.UseCases.Outbox.MarkOutboxDeadLetter;

public interface IMarkOutboxDeadLetterUseCase
{
    Task<Result<MarkOutboxDeadLetterResponse>> ExecuteAsync(
        MarkOutboxDeadLetterRequest request,
        CancellationToken cancellationToken = default);
}