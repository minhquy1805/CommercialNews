using CommercialNews.Api.Api.Admin.Contracts.Content.Tags.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Content.Tags.Responses;
using CommercialNews.Api.Api.Common.Contracts;
using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Models.QueryModels;
using Content.Application.UseCases.Tags.CreateTag;
using Content.Application.UseCases.Tags.DeleteTag;
using Content.Application.UseCases.Tags.GetTagById;
using Content.Application.UseCases.Tags.GetTags;
using Content.Application.UseCases.Tags.RestoreTag;
using Content.Application.UseCases.Tags.UpdateTag;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers.Content
{
    [ApiController]
    [Route("api/v1/admin/content/tags")]
    public sealed class TagsAdminController : ControllerBase
    {
        private const string GetTagByIdRouteName = "AdminContentTags.GetById";

        private readonly ICreateTagUseCase _createTagUseCase;
        private readonly IGetTagByIdUseCase _getTagByIdUseCase;
        private readonly IGetTagsUseCase _getTagsUseCase;
        private readonly IUpdateTagUseCase _updateTagUseCase;
        private readonly IDeleteTagUseCase _deleteTagUseCase;
        private readonly IRestoreTagUseCase _restoreTagUseCase;
        private readonly IRequestContext _requestContext;

        public TagsAdminController(
            ICreateTagUseCase createTagUseCase,
            IGetTagByIdUseCase getTagByIdUseCase,
            IGetTagsUseCase getTagsUseCase,
            IUpdateTagUseCase updateTagUseCase,
            IDeleteTagUseCase deleteTagUseCase,
            IRestoreTagUseCase restoreTagUseCase,
            IRequestContext requestContext)
        {
            _createTagUseCase = createTagUseCase;
            _getTagByIdUseCase = getTagByIdUseCase;
            _getTagsUseCase = getTagsUseCase;
            _updateTagUseCase = updateTagUseCase;
            _deleteTagUseCase = deleteTagUseCase;
            _restoreTagUseCase = restoreTagUseCase;
            _requestContext = requestContext;
        }

        [HttpPost]
        [ProducesResponseType(typeof(CreateTagResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateAsync(
            [FromBody] CreateTagRequest request,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new CreateTagRequestDto
            {
                Name = request.Name,
                Description = request.Description,
                IsActive = request.IsActive,
                ActorUserId = _requestContext.CurrentUserId
            };

            Result<CreateTagResponseDto> result =
                await _createTagUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<CreateTagResponse>.Failure(result.Error!));
            }

            var response = new CreateTagResponse
            {
                TagId = result.Value!.TagId,
                PublicId = result.Value.PublicId,
                Name = result.Value.Name,
                NameNormalized = result.Value.NameNormalized,
                Description = result.Value.Description,
                IsActive = result.Value.IsActive,
                Version = result.Value.Version,
                CreatedAt = result.Value.CreatedAt
            };

            return CreatedAtRoute(
                GetTagByIdRouteName,
                new { tagId = response.TagId },
                response);
        }

        [HttpGet("{tagId:long}", Name = GetTagByIdRouteName)]
        [ProducesResponseType(typeof(GetTagByIdResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByIdAsync(
            [FromRoute] long tagId,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new GetTagByIdRequestDto
            {
                TagId = tagId
            };

            Result<GetTagByIdResponseDto> result =
                await _getTagByIdUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<GetTagByIdResponse>.Failure(result.Error!));
            }

            var response = new GetTagByIdResponse
            {
                TagId = result.Value!.TagId,
                PublicId = result.Value.PublicId,
                Name = result.Value.Name,
                NameNormalized = result.Value.NameNormalized,
                Description = result.Value.Description,
                IsActive = result.Value.IsActive,
                IsDeleted = result.Value.IsDeleted,
                Version = result.Value.Version,
                CreatedAt = result.Value.CreatedAt,
                UpdatedAt = result.Value.UpdatedAt,
                DeletedAt = result.Value.DeletedAt
            };

            return this.ToActionResult(Result<GetTagByIdResponse>.Success(response));
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<TagListItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPagedAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] bool isDeleted = false,
            [FromQuery] string? sort = "name",
            CancellationToken cancellationToken = default)
        {
            var useCaseRequest = new TagListQuery
            {
                Page = page,
                PageSize = pageSize,
                Keyword = keyword,
                IsActive = isActive,
                IsDeleted = isDeleted,
                Sort = sort ?? "name"
            };

            Result<PagedQueryResult<TagListResultItem>> result =
                await _getTagsUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<PagedResponse<TagListItemResponse>>.Failure(result.Error!));
            }

            var value = result.Value!;

            var response = new PagedResponse<TagListItemResponse>
            {
                Items = value.Items.Select(static item => new TagListItemResponse
                {
                    TagId = item.TagId,
                    PublicId = item.PublicId,
                    Name = item.Name,
                    NameNormalized = item.NameNormalized,
                    Description = item.Description,
                    IsActive = item.IsActive,
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

            return this.ToActionResult(Result<PagedResponse<TagListItemResponse>>.Success(response));
        }

        [HttpPut("{tagId:long}")]
        [ProducesResponseType(typeof(UpdateTagResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateAsync(
            [FromRoute] long tagId,
            [FromBody] UpdateTagRequest request,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new UpdateTagRequestDto
            {
                TagId = tagId,
                Name = request.Name,
                Description = request.Description,
                IsActive = request.IsActive,
                ExpectedVersion = request.ExpectedVersion,
                ActorUserId = _requestContext.CurrentUserId
            };

            Result<UpdateTagResponseDto> result =
                await _updateTagUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<UpdateTagResponse>.Failure(result.Error!));
            }

            var response = new UpdateTagResponse
            {
                TagId = result.Value!.TagId,
                Name = result.Value.Name,
                NameNormalized = result.Value.NameNormalized,
                Description = result.Value.Description,
                IsActive = result.Value.IsActive,
                Version = result.Value.Version,
                UpdatedAt = result.Value.UpdatedAt
            };

            return this.ToActionResult(Result<UpdateTagResponse>.Success(response));
        }

        [HttpDelete("{tagId:long}")]
        [ProducesResponseType(typeof(DeleteTagResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteAsync(
            [FromRoute] long tagId,
            [FromBody] DeleteTagRequest request,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new DeleteTagRequestDto
            {
                TagId = tagId,
                ExpectedVersion = request.ExpectedVersion,
                ActorUserId = _requestContext.CurrentUserId
            };

            Result<DeleteTagResponseDto> result =
                await _deleteTagUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<DeleteTagResponse>.Failure(result.Error!));
            }

            var response = new DeleteTagResponse
            {
                TagId = result.Value!.TagId,
                IsDeleted = result.Value.IsDeleted,
                Version = result.Value.Version,
                DeletedAt = result.Value.DeletedAt
            };

            return this.ToActionResult(Result<DeleteTagResponse>.Success(response));
        }

        [HttpPost("{tagId:long}:restore")]
        [ProducesResponseType(typeof(RestoreTagResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RestoreAsync(
            [FromRoute] long tagId,
            [FromBody] RestoreTagRequest request,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new RestoreTagRequestDto
            {
                TagId = tagId,
                ExpectedVersion = request.ExpectedVersion,
                ActorUserId = _requestContext.CurrentUserId
            };

            Result<RestoreTagResponseDto> result =
                await _restoreTagUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<RestoreTagResponse>.Failure(result.Error!));
            }

            var response = new RestoreTagResponse
            {
                TagId = result.Value!.TagId,
                IsDeleted = result.Value.IsDeleted,
                Version = result.Value.Version,
                UpdatedAt = result.Value.UpdatedAt
            };

            return this.ToActionResult(Result<RestoreTagResponse>.Success(response));
        }
    }
}