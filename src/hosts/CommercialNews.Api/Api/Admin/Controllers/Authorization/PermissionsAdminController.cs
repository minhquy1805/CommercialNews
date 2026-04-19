using Authorization.Application.Contracts.Permissions;
using Authorization.Application.Models.QueryModels;
using Authorization.Application.UseCases.Permissions.ActivatePermission;
using Authorization.Application.UseCases.Permissions.CreatePermission;
using Authorization.Application.UseCases.Permissions.DeactivatePermission;
using Authorization.Application.UseCases.Permissions.GetPermissions;
using Authorization.Application.UseCases.Permissions.UpdatePermission;
using CommercialNews.Api.Api.Admin.Contracts.Authorization.Permissions.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Authorization.Permissions.Responses;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers.Authorization;

[ApiController]
[Route("api/v1/admin/authz/permissions")]
public sealed class PermissionsAdminController : ControllerBase
{
    private readonly ICreatePermissionUseCase _createPermissionUseCase;
    private readonly IUpdatePermissionUseCase _updatePermissionUseCase;
    private readonly IActivatePermissionUseCase _activatePermissionUseCase;
    private readonly IDeactivatePermissionUseCase _deactivatePermissionUseCase;
    private readonly IGetPermissionsUseCase _getPermissionsUseCase;

    public PermissionsAdminController(
        ICreatePermissionUseCase createPermissionUseCase,
        IUpdatePermissionUseCase updatePermissionUseCase,
        IActivatePermissionUseCase activatePermissionUseCase,
        IDeactivatePermissionUseCase deactivatePermissionUseCase,
        IGetPermissionsUseCase getPermissionsUseCase)
    {
        _createPermissionUseCase = createPermissionUseCase;
        _updatePermissionUseCase = updatePermissionUseCase;
        _activatePermissionUseCase = activatePermissionUseCase;
        _deactivatePermissionUseCase = deactivatePermissionUseCase;
        _getPermissionsUseCase = getPermissionsUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> GetPermissions(
        [FromQuery] GetPermissionsHttpRequest request,
        CancellationToken cancellationToken)
    {
        Result<PagedQueryResult<PermissionListResultItem>> result =
            await _getPermissionsUseCase.ExecuteAsync(
                new GetPermissionsRequestDto
                {
                    Page = request.Page,
                    PageSize = request.PageSize,
                    Query = request.Query,
                    Module = request.Module,
                    Action = request.Action,
                    IsActive = request.IsActive
                },
                cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return Ok(MapGetPermissionsResponse(result.Value!));
    }

    [HttpPost]
    public async Task<IActionResult> CreatePermission(
        [FromBody] CreatePermissionHttpRequest request,
        CancellationToken cancellationToken)
    {
        Result<CreatePermissionResponseDto> result =
            await _createPermissionUseCase.ExecuteAsync(
                new CreatePermissionRequestDto
                {
                    Key = request.Key,
                    Module = request.Module,
                    Action = request.Action,
                    Description = request.Description,
                    IsSystem = request.IsSystem
                },
                cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        var response = MapCreatePermissionResponse(result.Value!);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{permissionId:long}")]
    public async Task<IActionResult> UpdatePermission(
        [FromRoute] long permissionId,
        [FromBody] UpdatePermissionHttpRequest request,
        CancellationToken cancellationToken)
    {
        Result<UpdatePermissionResponseDto> result =
            await _updatePermissionUseCase.ExecuteAsync(
                new UpdatePermissionRequestDto
                {
                    PermissionId = permissionId,
                    Key = request.Key,
                    Module = request.Module,
                    Action = request.Action,
                    Description = request.Description
                },
                cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return Ok(MapUpdatePermissionResponse(result.Value!));
    }

    [HttpPost("{permissionId:long}:activate")]
    public async Task<IActionResult> ActivatePermission(
        [FromRoute] long permissionId,
        CancellationToken cancellationToken)
    {
        Result<ActivatePermissionResponseDto> result =
            await _activatePermissionUseCase.ExecuteAsync(
                new ActivatePermissionRequestDto
                {
                    PermissionId = permissionId
                },
                cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return Ok(MapActivatePermissionResponse(result.Value!));
    }

    [HttpPost("{permissionId:long}:deactivate")]
    public async Task<IActionResult> DeactivatePermission(
        [FromRoute] long permissionId,
        CancellationToken cancellationToken)
    {
        Result<DeactivatePermissionResponseDto> result =
            await _deactivatePermissionUseCase.ExecuteAsync(
                new DeactivatePermissionRequestDto
                {
                    PermissionId = permissionId
                },
                cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return Ok(MapDeactivatePermissionResponse(result.Value!));
    }

    private static GetPermissionsHttpResponse MapGetPermissionsResponse(
        PagedQueryResult<PermissionListResultItem> source)
    {
        return new GetPermissionsHttpResponse
        {
            Items = source.Items.Select(MapPermissionListItemResponse).ToList(),
            Page = source.Page,
            PageSize = source.PageSize,
            TotalItems = source.TotalItems
        };
    }

    private static PermissionListItemHttpResponse MapPermissionListItemResponse(
        PermissionListResultItem source)
    {
        return new PermissionListItemHttpResponse
        {
            PermissionId = source.PermissionId,
            PublicId = source.PublicId,
            Key = source.Key,
            KeyNormalized = source.KeyNormalized,
            Description = source.Description,
            Module = source.Module,
            Action = source.Action,
            IsSystem = source.IsSystem,
            IsActive = source.IsActive,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            CreatedByUserId = source.CreatedByUserId,
            UpdatedByUserId = source.UpdatedByUserId
        };
    }

    private static CreatePermissionHttpResponse MapCreatePermissionResponse(
        CreatePermissionResponseDto source)
    {
        return new CreatePermissionHttpResponse
        {
            PermissionId = source.PermissionId,
            PublicId = source.PublicId,
            Key = source.Key,
            KeyNormalized = source.KeyNormalized,
            Description = source.Description,
            Module = source.Module,
            Action = source.Action,
            IsSystem = source.IsSystem,
            IsActive = source.IsActive,
            CreatedAt = source.CreatedAt,
            CreatedByUserId = source.CreatedByUserId
        };
    }

    private static UpdatePermissionHttpResponse MapUpdatePermissionResponse(
        UpdatePermissionResponseDto source)
    {
        return new UpdatePermissionHttpResponse
        {
            PermissionId = source.PermissionId,
            PublicId = source.PublicId,
            Key = source.Key,
            KeyNormalized = source.KeyNormalized,
            Description = source.Description,
            Module = source.Module,
            Action = source.Action,
            IsSystem = source.IsSystem,
            IsActive = source.IsActive,
            UpdatedAt = source.UpdatedAt,
            UpdatedByUserId = source.UpdatedByUserId
        };
    }

    private static ActivatePermissionHttpResponse MapActivatePermissionResponse(
        ActivatePermissionResponseDto source)
    {
        return new ActivatePermissionHttpResponse
        {
            PermissionId = source.PermissionId,
            IsActivated = source.IsActivated,
            WasAlreadyActivated = source.WasAlreadyActivated
        };
    }

    private static DeactivatePermissionHttpResponse MapDeactivatePermissionResponse(
        DeactivatePermissionResponseDto source)
    {
        return new DeactivatePermissionHttpResponse
        {
            PermissionId = source.PermissionId,
            IsDeactivated = source.IsDeactivated,
            WasAlreadyDeactivated = source.WasAlreadyDeactivated
        };
    }
}