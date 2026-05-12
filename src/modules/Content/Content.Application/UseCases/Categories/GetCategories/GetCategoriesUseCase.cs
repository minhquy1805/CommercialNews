using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Models.QueryModels;
using Content.Application.Ports.Persistence;

namespace Content.Application.UseCases.Categories.GetCategories;

public sealed class GetCategoriesUseCase : IGetCategoriesUseCase
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly ICategoryRepository _categoryRepository;

    public GetCategoriesUseCase(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
    }

    public async Task<Result<PagedQueryResult<CategoryListResultItem>>> ExecuteAsync(
        CategoryListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var normalizedQuery = new CategoryListQuery
        {
            Page = query.Page <= 0 ? DefaultPage : query.Page,
            PageSize = query.PageSize <= 0
                ? DefaultPageSize
                : Math.Min(query.PageSize, MaxPageSize),
            Keyword = string.IsNullOrWhiteSpace(query.Keyword)
                ? null
                : query.Keyword.Trim(),
            ParentCategoryId = query.ParentCategoryId,
            IsActive = query.IsActive,
            IsDeleted = query.IsDeleted,
            Sort = string.IsNullOrWhiteSpace(query.Sort)
                ? "displayOrder"
                : query.Sort.Trim()
        };

        var result = await _categoryRepository.GetPagedAsync(
            normalizedQuery,
            cancellationToken);

        return Result<PagedQueryResult<CategoryListResultItem>>.Success(result);
    }
}