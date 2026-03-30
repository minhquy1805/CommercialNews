using Content.Application.Models.QueryModels;
using Content.Domain.Entities;

namespace Content.Application.Ports.Persistence
{
    public interface ICategoryRepository
    {
        Task<bool> ExistsByIdAsync(
            long categoryId,
            CancellationToken cancellationToken = default);

        Task<Category?> GetByIdAsync(
            long categoryId,
            CancellationToken cancellationToken = default);

        Task<PagedQueryResult<CategoryListResultItem>> SelectSkipAndTakeAsync(
            CategoryListQuery query,
            CancellationToken cancellationToken = default);

        Task<Category?> InsertAsync(
            Category category,
            CancellationToken cancellationToken = default);

        Task<Category?> UpdateAsync(
            Category category,
            int expectedVersion,
            CancellationToken cancellationToken = default);

        Task<Category?> SoftDeleteAsync(
            long categoryId,
            long? deletedByUserId,
            int expectedVersion,
            CancellationToken cancellationToken = default);

        Task<Category?> RestoreAsync(
            long categoryId,
            long? updatedByUserId,
            int expectedVersion,
            CancellationToken cancellationToken = default);
    }
}