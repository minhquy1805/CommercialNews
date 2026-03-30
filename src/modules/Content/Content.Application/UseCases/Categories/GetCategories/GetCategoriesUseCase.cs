using CommercialNews.BuildingBlocks.Results;
using Content.Application.Models.QueryModels;
using Content.Application.Ports.Persistence;

namespace Content.Application.UseCases.Categories.GetCategories
{
    public sealed class GetCategoriesUseCase : IGetCategoriesUseCase
    {
        private readonly ICategoryRepository _categoryRepository;

        public GetCategoriesUseCase(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Result<PagedQueryResult<CategoryListResultItem>>> ExecuteAsync(
            CategoryListQuery query,
            CancellationToken cancellationToken = default)
        {
            var normalizedQuery = new CategoryListQuery
            {
                Page = query.Page <= 0 ? 1 : query.Page,
                PageSize = query.PageSize <= 0 ? 20 : query.PageSize,
                Keyword = string.IsNullOrWhiteSpace(query.Keyword) ? null : query.Keyword.Trim(),
                ParentCategoryId = query.ParentCategoryId,
                IsActive = query.IsActive,
                IsDeleted = query.IsDeleted,
                Sort = string.IsNullOrWhiteSpace(query.Sort) ? "displayOrder" : query.Sort.Trim()
            };

            var result = await _categoryRepository.SelectSkipAndTakeAsync(
                normalizedQuery,
                cancellationToken);

            return Result<PagedQueryResult<CategoryListResultItem>>.Success(result);
        }
    }
}