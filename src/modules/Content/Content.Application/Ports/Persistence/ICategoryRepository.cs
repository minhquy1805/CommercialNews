using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Content.Application.Models.QueryModels;
using Content.Domain.Entities;

namespace Content.Application.Ports.Persistence
{
    public interface ICategoryRepository
    {
        Task<bool> ExistsByIdAsync(
            long categoryId,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsActiveByIdAsync(
            long categoryId,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsByNameNormalizedAsync(
            string nameNormalized,
            long? excludingCategoryId = null,
            CancellationToken cancellationToken = default);

        Task<Category?> GetByIdAsync(
            long categoryId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Category>> GetAllAsync(
            bool includeDeleted = false,
            CancellationToken cancellationToken = default);

        Task<PagedQueryResult<CategoryListResultItem>> GetPagedAsync(
            CategoryListQuery query,
            CancellationToken cancellationToken = default);

        Task<Category?> InsertAsync(
            Category category,
            CancellationToken cancellationToken = default);

        Task<Category?> UpdateAsync(
            Category category,
            long expectedVersion,
            CancellationToken cancellationToken = default);

        Task<Category?> SoftDeleteAsync(
            long categoryId,
            long? deletedByUserId,
            long expectedVersion,
            CancellationToken cancellationToken = default);

        Task<Category?> RestoreAsync(
            long categoryId,
            long? updatedByUserId,
            long expectedVersion,
            CancellationToken cancellationToken = default);
    }
}