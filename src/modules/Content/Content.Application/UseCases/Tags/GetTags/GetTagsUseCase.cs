using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Models.QueryModels;
using Content.Application.Ports.Persistence;

namespace Content.Application.UseCases.Tags.GetTags
{
    public sealed class GetTagsUseCase : IGetTagsUseCase
    {
        private readonly ITagRepository _tagRepository;

        public GetTagsUseCase(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public async Task<Result<PagedQueryResult<TagListResultItem>>> ExecuteAsync(
            TagListQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            int safePage = query.Page <= 0 ? 1 : query.Page;
            int safePageSize = query.PageSize <= 0 ? 20 : query.PageSize;

            if (safePageSize > 200)
            {
                safePageSize = 200;
            }

            var safeQuery = new TagListQuery
            {
                Page = safePage,
                PageSize = safePageSize,
                Keyword = string.IsNullOrWhiteSpace(query.Keyword)
                    ? null
                    : query.Keyword.Trim(),
                IsActive = query.IsActive,
                IsDeleted = query.IsDeleted,
                Sort = string.IsNullOrWhiteSpace(query.Sort)
                    ? "name"
                    : query.Sort.Trim()
            };

            var result = await _tagRepository.SelectSkipAndTakeAsync(
                safeQuery,
                cancellationToken);

            return Result<PagedQueryResult<TagListResultItem>>.Success(result);
        }
    }
}