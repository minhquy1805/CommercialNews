using Interaction.Domain.Entities;

namespace Interaction.Application.Ports.Persistence.Write;

public interface IArticleViewEventRepository
{
    Task<long> InsertAsync(
        ArticleViewEvent articleViewEvent,
        CancellationToken cancellationToken = default);

    Task<int> DeleteBeforeDateAsync(
        DateTime deleteBeforeUtc,
        CancellationToken cancellationToken = default);
}