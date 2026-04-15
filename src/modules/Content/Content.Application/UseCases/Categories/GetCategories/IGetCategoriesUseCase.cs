using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Models.QueryModels;

namespace Content.Application.UseCases.Categories.GetCategories
{
    public interface IGetCategoriesUseCase
    {
        Task<Result<PagedQueryResult<CategoryListResultItem>>> ExecuteAsync(
            CategoryListQuery query,
            CancellationToken cancellationToken = default);
    }
}