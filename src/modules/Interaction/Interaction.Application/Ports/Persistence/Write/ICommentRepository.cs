using Interaction.Domain.Entities;

namespace Interaction.Application.Ports.Persistence.Write;

public interface ICommentRepository
{
    Task<long> InsertAsync(
        Comment comment,
        CancellationToken cancellationToken = default);

    Task<Comment?> GetByIdAsync(
        long commentId,
        CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(
        Comment comment,
        CancellationToken cancellationToken = default);

    Task<int> SoftDeleteAsync(
        long commentId,
        long? deletedByUserId,
        long? expectedUserId = null,
        CancellationToken cancellationToken = default);
}