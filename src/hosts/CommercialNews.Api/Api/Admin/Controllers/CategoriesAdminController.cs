using CommercialNews.Api.Api.Admin.Contracts.Content.Categories.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Content.Categories.Responses;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.BuildingBlocks.Abstractions.Execution;
using CommercialNews.BuildingBlocks.Contracts.Common;
using CommercialNews.BuildingBlocks.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Models.QueryModels;
using Content.Application.UseCases.Categories.CreateCategory;
using Content.Application.UseCases.Categories.DeleteCategory;
using Content.Application.UseCases.Categories.GetCategories;
using Content.Application.UseCases.Categories.GetCategoryById;
using Content.Application.UseCases.Categories.RestoreCategory;
using Content.Application.UseCases.Categories.UpdateCategory;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers
{
    [ApiController]
    [Route("api/v1/admin/content/categories")]
    public sealed class CategoriesAdminController : ControllerBase
    {
        private const string GetCategoryByIdRouteName = "AdminContentCategories.GetById";

        private readonly ICreateCategoryUseCase _createCategoryUseCase;
        private readonly IGetCategoryByIdUseCase _getCategoryByIdUseCase;
        private readonly IGetCategoriesUseCase _getCategoriesUseCase;
        private readonly IUpdateCategoryUseCase _updateCategoryUseCase;
        private readonly IDeleteCategoryUseCase _deleteCategoryUseCase;
        private readonly IRestoreCategoryUseCase _restoreCategoryUseCase;
        private readonly IRequestContext _requestContext;

        public CategoriesAdminController(
            ICreateCategoryUseCase createCategoryUseCase,
            IGetCategoryByIdUseCase getCategoryByIdUseCase,
            IGetCategoriesUseCase getCategoriesUseCase,
            IUpdateCategoryUseCase updateCategoryUseCase,
            IDeleteCategoryUseCase deleteCategoryUseCase,
            IRestoreCategoryUseCase restoreCategoryUseCase,
            IRequestContext requestContext)
        {
            _createCategoryUseCase = createCategoryUseCase;
            _getCategoryByIdUseCase = getCategoryByIdUseCase;
            _getCategoriesUseCase = getCategoriesUseCase;
            _updateCategoryUseCase = updateCategoryUseCase;
            _deleteCategoryUseCase = deleteCategoryUseCase;
            _restoreCategoryUseCase = restoreCategoryUseCase;
            _requestContext = requestContext;
        }

        [HttpPost]
        [ProducesResponseType(typeof(CreateCategoryResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateAsync(
            [FromBody] CreateCategoryRequest request,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new CreateCategoryRequestDto
            {
                ParentCategoryId = request.ParentCategoryId,
                Name = request.Name,
                Description = request.Description,
                IsActive = request.IsActive,
                DisplayOrder = request.DisplayOrder,
                ActorUserId = _requestContext.CurrentUserId
            };

            Result<CreateCategoryResponseDto> result =
                await _createCategoryUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<CreateCategoryResponse>.Failure(result.Error!));
            }

            var response = new CreateCategoryResponse
            {
                CategoryId = result.Value!.CategoryId,
                PublicId = result.Value.PublicId,
                ParentCategoryId = result.Value.ParentCategoryId,
                Name = result.Value.Name,
                NameNormalized = result.Value.NameNormalized,
                Description = result.Value.Description,
                IsActive = result.Value.IsActive,
                DisplayOrder = result.Value.DisplayOrder,
                Version = result.Value.Version,
                CreatedAt = result.Value.CreatedAt
            };

            return CreatedAtRoute(
                GetCategoryByIdRouteName,
                new { categoryId = response.CategoryId },
                response);
        }

        [HttpGet("{categoryId:long}", Name = GetCategoryByIdRouteName)]
        [ProducesResponseType(typeof(GetCategoryByIdResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByIdAsync(
            [FromRoute] long categoryId,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new GetCategoryByIdRequestDto
            {
                CategoryId = categoryId
            };

            Result<GetCategoryByIdResponseDto> result =
                await _getCategoryByIdUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<GetCategoryByIdResponse>.Failure(result.Error!));
            }

            var response = new GetCategoryByIdResponse
            {
                CategoryId = result.Value!.CategoryId,
                PublicId = result.Value.PublicId,
                ParentCategoryId = result.Value.ParentCategoryId,
                Name = result.Value.Name,
                NameNormalized = result.Value.NameNormalized,
                Description = result.Value.Description,
                IsActive = result.Value.IsActive,
                DisplayOrder = result.Value.DisplayOrder,
                IsDeleted = result.Value.IsDeleted,
                Version = result.Value.Version,
                CreatedAt = result.Value.CreatedAt,
                UpdatedAt = result.Value.UpdatedAt,
                DeletedAt = result.Value.DeletedAt
            };

            return this.ToActionResult(Result<GetCategoryByIdResponse>.Success(response));
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<CategoryListItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPagedAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] long? parentCategoryId = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] bool isDeleted = false,
            [FromQuery] string? sort = "displayOrder",
            CancellationToken cancellationToken = default)
        {
            var useCaseRequest = new CategoryListQuery
            {
                Page = page,
                PageSize = pageSize,
                Keyword = keyword,
                ParentCategoryId = parentCategoryId,
                IsActive = isActive,
                IsDeleted = isDeleted,
                Sort = sort ?? "displayOrder"
            };

            Result<PagedQueryResult<CategoryListResultItem>> result =
                await _getCategoriesUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<PagedResponse<CategoryListItemResponse>>.Failure(result.Error!));
            }

            var value = result.Value!;

            var response = new PagedResponse<CategoryListItemResponse>
            {
                Items = value.Items.Select(static item => new CategoryListItemResponse
                {
                    CategoryId = item.CategoryId,
                    PublicId = item.PublicId,
                    ParentCategoryId = item.ParentCategoryId,
                    Name = item.Name,
                    NameNormalized = item.NameNormalized,
                    Description = item.Description,
                    IsActive = item.IsActive,
                    DisplayOrder = item.DisplayOrder,
                    IsDeleted = item.IsDeleted,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    Version = item.Version
                }).ToArray(),
                PageInfo = new PageInfo
                {
                    Page = value.Page,
                    PageSize = value.PageSize,
                    TotalItems = value.TotalItems,
                    TotalPages = value.PageSize <= 0
                        ? 0
                        : (int)Math.Ceiling((double)value.TotalItems / value.PageSize)
                }
            };

            return this.ToActionResult(Result<PagedResponse<CategoryListItemResponse>>.Success(response));
        }

        [HttpPut("{categoryId:long}")]
        [ProducesResponseType(typeof(UpdateCategoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateAsync(
            [FromRoute] long categoryId,
            [FromBody] UpdateCategoryRequest request,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new UpdateCategoryRequestDto
            {
                CategoryId = categoryId,
                ParentCategoryId = request.ParentCategoryId,
                Name = request.Name,
                Description = request.Description,
                IsActive = request.IsActive,
                DisplayOrder = request.DisplayOrder,
                ExpectedVersion = request.ExpectedVersion,
                ActorUserId = _requestContext.CurrentUserId
            };

            Result<UpdateCategoryResponseDto> result =
                await _updateCategoryUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<UpdateCategoryResponse>.Failure(result.Error!));
            }

            var response = new UpdateCategoryResponse
            {
                CategoryId = result.Value!.CategoryId,
                ParentCategoryId = result.Value.ParentCategoryId,
                Name = result.Value.Name,
                NameNormalized = result.Value.NameNormalized,
                Description = result.Value.Description,
                IsActive = result.Value.IsActive,
                DisplayOrder = result.Value.DisplayOrder,
                Version = result.Value.Version,
                UpdatedAt = result.Value.UpdatedAt
            };

            return this.ToActionResult(Result<UpdateCategoryResponse>.Success(response));
        }

        [HttpDelete("{categoryId:long}")]
        [ProducesResponseType(typeof(DeleteCategoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteAsync(
            [FromRoute] long categoryId,
            [FromBody] DeleteCategoryRequest request,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new DeleteCategoryRequestDto
            {
                CategoryId = categoryId,
                ExpectedVersion = request.ExpectedVersion,
                ActorUserId = _requestContext.CurrentUserId
            };

            Result<DeleteCategoryResponseDto> result =
                await _deleteCategoryUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<DeleteCategoryResponse>.Failure(result.Error!));
            }

            var response = new DeleteCategoryResponse
            {
                CategoryId = result.Value!.CategoryId,
                IsDeleted = result.Value.IsDeleted,
                Version = result.Value.Version,
                DeletedAt = result.Value.DeletedAt
            };

            return this.ToActionResult(Result<DeleteCategoryResponse>.Success(response));
        }

        [HttpPost("{categoryId:long}:restore")]
        [ProducesResponseType(typeof(RestoreCategoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RestoreAsync(
            [FromRoute] long categoryId,
            [FromBody] RestoreCategoryRequest request,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new RestoreCategoryRequestDto
            {
                CategoryId = categoryId,
                ExpectedVersion = request.ExpectedVersion,
                ActorUserId = _requestContext.CurrentUserId
            };

            Result<RestoreCategoryResponseDto> result =
                await _restoreCategoryUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<RestoreCategoryResponse>.Failure(result.Error!));
            }

            var response = new RestoreCategoryResponse
            {
                CategoryId = result.Value!.CategoryId,
                IsDeleted = result.Value.IsDeleted,
                Version = result.Value.Version,
                UpdatedAt = result.Value.UpdatedAt
            };

            return this.ToActionResult(Result<RestoreCategoryResponse>.Success(response));
        }
    }
}