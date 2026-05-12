using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Models.QueryModels;
using Content.Application.Ports.Persistence;

namespace Content.Application.UseCases.Tags.GetTags;

public sealed class GetTagsUseCase : IGetTagsUseCase
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly ITagRepository _tagRepository;

    public GetTagsUseCase(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
    }

    public async Task<Result<PagedQueryResult<TagListResultItem>>> ExecuteAsync(
        TagListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var safeQuery = new TagListQuery
        {
            Page = query.Page <= 0 ? DefaultPage : query.Page,
            PageSize = query.PageSize <= 0
                ? DefaultPageSize
                : Math.Min(query.PageSize, MaxPageSize),
            Keyword = string.IsNullOrWhiteSpace(query.Keyword)
                ? null
                : query.Keyword.Trim(),
            IsActive = query.IsActive,
            IsDeleted = query.IsDeleted,
            Sort = string.IsNullOrWhiteSpace(query.Sort)
                ? "name"
                : query.Sort.Trim()
        };

        var result = await _tagRepository.GetPagedAsync(
            safeQuery,
            cancellationToken);

        return Result<PagedQueryResult<TagListResultItem>>.Success(result);
    }
}