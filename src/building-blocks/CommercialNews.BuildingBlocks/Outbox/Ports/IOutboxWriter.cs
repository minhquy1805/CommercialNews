using CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;
using CommercialNews.BuildingBlocks.Persistence.Sql.Transactions;

namespace CommercialNews.BuildingBlocks.Outbox.Ports;

public interface IOutboxWriter
{
    Task<long> WriteAsync(
        ISqlUnitOfWork unitOfWork,
        OutboxWriteRequest request,
        CancellationToken cancellationToken = default);
}