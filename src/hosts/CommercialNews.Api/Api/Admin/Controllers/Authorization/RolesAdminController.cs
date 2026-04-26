using Authorization.Application.Contracts.Roles;
using Authorization.Application.Models.QueryModels;
using Authorization.Application.UseCases.Roles.ActivateRole;
using Authorization.Application.UseCases.Roles.CreateRole;
using Authorization.Application.UseCases.Roles.DeactivateRole;
using Authorization.Application.UseCases.Roles.GetRoles;
using Authorization.Application.UseCases.Roles.UpdateRole;
using CommercialNews.Api.Api.Admin.Contracts.Authorization.Roles.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Authorization.Roles.Responses;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Authorization;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers.Authorization;

[Authorize]
[ApiController]
[Route("api/v1/admin/authz/roles")]
public sealed class RolesAdminController : ControllerBase
{
    private readonly ICreateRoleUseCase _createRoleUseCase;
    private readonly IUpdateRoleUseCase _updateRoleUseCase;
    private readonly IActivateRoleUseCase _activateRoleUseCase;
    private readonly IDeactivateRoleUseCase _deactivateRoleUseCase;
    private readonly IGetRolesUseCase _getRolesUseCase;

    public RolesAdminController(
        ICreateRoleUseCase createRoleUseCase,
        IUpdateRoleUseCase updateRoleUseCase,
        IActivateRoleUseCase activateRoleUseCase,
        IDeactivateRoleUseCase deactivateRoleUseCase,
        IGetRolesUseCase getRolesUseCase)
    {
        _createRoleUseCase = createRoleUseCase;
        _updateRoleUseCase = updateRoleUseCase;
        _activateRoleUseCase = activateRoleUseCase;
        _deactivateRoleUseCase = deactivateRoleUseCase;
        _getRolesUseCase = getRolesUseCase;
    }

    [Authorize(Policy = AuthorizationPolicies.AuthzRolesRead)]
    [HttpGet]
    public async Task<IActionResult> GetRoles(
        [FromQuery] GetRolesHttpRequest request,
        CancellationToken cancellationToken)
    {
        Result<PagedQueryResult<RoleListResultItem>> result =
            await _getRolesUseCase.ExecuteAsync(
                new GetRolesRequestDto
                {
                    Page = request.Page,
                    PageSize = request.PageSize,
                    Query = request.Query,
                    IsActive = request.IsActive
                },
                cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return Ok(MapGetRolesResponse(result.Value!));
    }

    [Authorize(Policy = AuthorizationPolicies.AuthzRolesCreate)]
    [HttpPost]
    public async Task<IActionResult> CreateRole(
        [FromBody] CreateRoleHttpRequest request,
        CancellationToken cancellationToken)
    {
        Result<CreateRoleResponseDto> result =
            await _createRoleUseCase.ExecuteAsync(
                new CreateRoleRequestDto
                {
                    Name = request.Name,
                    DisplayName = request.DisplayName,
                    Description = request.Description,
                    IsSystem = request.IsSystem
                },
                cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        var response = MapCreateRoleResponse(result.Value!);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize(Policy = AuthorizationPolicies.AuthzRolesUpdate)]
    [HttpPut("{roleId:long}")]
    public async Task<IActionResult> UpdateRole(
        [FromRoute] long roleId,
        [FromBody] UpdateRoleHttpRequest request,
        CancellationToken cancellationToken)
    {
        Result<UpdateRoleResponseDto> result =
            await _updateRoleUseCase.ExecuteAsync(
                new UpdateRoleRequestDto
                {
                    RoleId = roleId,
                    Name = request.Name,
                    DisplayName = request.DisplayName,
                    Description = request.Description
                },
                cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return Ok(MapUpdateRoleResponse(result.Value!));
    }

    [Authorize(Policy = AuthorizationPolicies.AuthzRolesActivate)]
    [HttpPost("{roleId:long}:activate")]
    public async Task<IActionResult> ActivateRole(
        [FromRoute] long roleId,
        CancellationToken cancellationToken)
    {
        Result<ActivateRoleResponseDto> result =
            await _activateRoleUseCase.ExecuteAsync(
                new ActivateRoleRequestDto
                {
                    RoleId = roleId
                },
                cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return Ok(MapActivateRoleResponse(result.Value!));
    }

    [Authorize(Policy = AuthorizationPolicies.AuthzRolesDeactivate)]
    [HttpPost("{roleId:long}:deactivate")]
    public async Task<IActionResult> DeactivateRole(
        [FromRoute] long roleId,
        CancellationToken cancellationToken)
    {
        Result<DeactivateRoleResponseDto> result =
            await _deactivateRoleUseCase.ExecuteAsync(
                new DeactivateRoleRequestDto
                {
                    RoleId = roleId
                },
                cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return Ok(MapDeactivateRoleResponse(result.Value!));
    }

    private static GetRolesHttpResponse MapGetRolesResponse(
        PagedQueryResult<RoleListResultItem> source)
    {
        return new GetRolesHttpResponse
        {
            Items = source.Items.Select(MapRoleListItemResponse).ToList(),
            Page = source.Page,
            PageSize = source.PageSize,
            TotalItems = source.TotalItems
        };
    }

    private static RoleListItemHttpResponse MapRoleListItemResponse(RoleListResultItem source)
    {
        return new RoleListItemHttpResponse
        {
            RoleId = source.RoleId,
            PublicId = source.PublicId,
            Name = source.Name,
            NameNormalized = source.NameNormalized,
            DisplayName = source.DisplayName,
            Description = source.Description,
            IsSystem = source.IsSystem,
            IsActive = source.IsActive,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            CreatedByUserId = source.CreatedByUserId,
            UpdatedByUserId = source.UpdatedByUserId
        };
    }

    private static CreateRoleHttpResponse MapCreateRoleResponse(CreateRoleResponseDto source)
    {
        return new CreateRoleHttpResponse
        {
            RoleId = source.RoleId,
            PublicId = source.PublicId,
            Name = source.Name,
            NameNormalized = source.NameNormalized,
            DisplayName = source.DisplayName,
            Description = source.Description,
            IsSystem = source.IsSystem,
            IsActive = source.IsActive,
            CreatedAt = source.CreatedAt,
            CreatedByUserId = source.CreatedByUserId
        };
    }

    private static UpdateRoleHttpResponse MapUpdateRoleResponse(UpdateRoleResponseDto source)
    {
        return new UpdateRoleHttpResponse
        {
            RoleId = source.RoleId,
            PublicId = source.PublicId,
            Name = source.Name,
            NameNormalized = source.NameNormalized,
            DisplayName = source.DisplayName,
            Description = source.Description,
            IsSystem = source.IsSystem,
            IsActive = source.IsActive,
            UpdatedAt = source.UpdatedAt,
            UpdatedByUserId = source.UpdatedByUserId
        };
    }

    private static ActivateRoleHttpResponse MapActivateRoleResponse(ActivateRoleResponseDto source)
    {
        return new ActivateRoleHttpResponse
        {
            RoleId = source.RoleId,
            IsActivated = source.IsActivated,
            WasAlreadyActivated = source.WasAlreadyActivated
        };
    }

    private static DeactivateRoleHttpResponse MapDeactivateRoleResponse(DeactivateRoleResponseDto source)
    {
        return new DeactivateRoleHttpResponse
        {
            RoleId = source.RoleId,
            IsDeactivated = source.IsDeactivated,
            WasAlreadyDeactivated = source.WasAlreadyDeactivated
        };
    }
}