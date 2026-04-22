using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Contracts.Outbox.Responses;

namespace Notifications.Application.UseCases.Outbox.GetOutboxMessageByMessageId;

/// <summary>
/// Returns a single outbox message by technical message id for admin troubleshooting.
/// This is a read-only use case and does not open a transaction.
/// </summary>
public interface IGetOutboxMessageByMessageIdUseCase
{
    Task<Result<GetOutboxMessageByIdResponse>> ExecuteAsync(
        GetOutboxMessageByMessageIdRequest request,
        CancellationToken cancellationToken = default);
}