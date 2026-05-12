using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Content.Application.Models.QueryModels;
using Content.Domain.Entities;

namespace Content.Application.Ports.Persistence
{
    public interface ITagRepository
    {
        Task<bool> ExistsByIdAsync(
            long tagId,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsActiveByIdAsync(
            long tagId,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsByNameNormalizedAsync(
            string nameNormalized,
            long? excludingTagId = null,
            CancellationToken cancellationToken = default);

        Task<Tag?> GetByIdAsync(
            long tagId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Tag>> GetAllAsync(
            bool includeDeleted = false,
            CancellationToken cancellationToken = default);

        Task<PagedQueryResult<TagListResultItem>> GetPagedAsync(
            TagListQuery query,
            CancellationToken cancellationToken = default);

        Task<Tag?> InsertAsync(
            Tag tag,
            CancellationToken cancellationToken = default);

        Task<Tag?> UpdateAsync(
            Tag tag,
            long expectedVersion,
            CancellationToken cancellationToken = default);

        Task<Tag?> SoftDeleteAsync(
            long tagId,
            long? deletedByUserId,
            long expectedVersion,
            CancellationToken cancellationToken = default);

        Task<Tag?> RestoreAsync(
            long tagId,
            long? updatedByUserId,
            long expectedVersion,
            CancellationToken cancellationToken = default);
    }
}