using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Content.Application.Models.QueryModels;
using Content.Domain.Entities;

namespace Content.Application.Ports.Persistence
{
    public interface ITagRepository
    {
        Task<Tag?> GetByIdAsync(
            long tagId,
            CancellationToken cancellationToken = default);

        Task<PagedQueryResult<TagListResultItem>> SelectSkipAndTakeAsync(
            TagListQuery query,
            CancellationToken cancellationToken = default);

        Task<Tag?> InsertAsync(
            Tag tag,
            CancellationToken cancellationToken = default);

        Task<Tag?> UpdateAsync(
            Tag tag,
            int expectedVersion,
            CancellationToken cancellationToken = default);

        Task<Tag?> SoftDeleteAsync(
            long tagId,
            long? deletedByUserId,
            int expectedVersion,
            CancellationToken cancellationToken = default);

        Task<Tag?> RestoreAsync(
            long tagId,
            long? updatedByUserId,
            int expectedVersion,
            CancellationToken cancellationToken = default);
    }
}