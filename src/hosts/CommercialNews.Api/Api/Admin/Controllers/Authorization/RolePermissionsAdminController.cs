using Authorization.Application.Contracts.RolePermissions;
using Authorization.Application.UseCases.RolePermissions.GetPermissionRoles;
using Authorization.Application.UseCases.RolePermissions.GetRolePermissions;
using Authorization.Application.UseCases.RolePermissions.GrantPermissionToRole;
using Authorization.Application.UseCases.RolePermissions.RevokePermissionFromRole;
using CommercialNews.Api.Api.Admin.Contracts.Authorization.RolePermissions.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Authorization.RolePermissions.Responses;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Authorization;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers.Authorization;

[Authorize]
[ApiController]
[Route("api/v1/admin/authz/roles/{roleId:long}/permissions")]
public sealed class RolePermissionsAdminController : ControllerBase
{
    private readonly IGrantPermissionToRoleUseCase _grantPermissionToRoleUseCase;
    private readonly IRevokePermissionFromRoleUseCase _revokePermissionFromRoleUseCase;
    private readonly IGetRolePermissionsUseCase _getRolePermissionsUseCase;
    private readonly IGetPermissionRolesUseCase _getPermissionRolesUseCase;

    public RolePermissionsAdminController(
        IGrantPermissionToRoleUseCase grantPermissionToRoleUseCase,
        IRevokePermissionFromRoleUseCase revokePermissionToRoleUseCase,
        IGetRolePermissionsUseCase getRolePermissionsUseCase,
        IGetPermissionRolesUseCase getPermissionRolesUseCase)
    {
        _grantPermissionToRoleUseCase = grantPermissionToRoleUseCase;
        _revokePermissionFromRoleUseCase = revokePermissionToRoleUseCase;
        _getRolePermissionsUseCase = getRolePermissionsUseCase;
        _getPermissionRolesUseCase = getPermissionRolesUseCase;
    }

    [Authorize(Policy = AuthorizationPolicies.AuthzRolePermissionsGrant)]
    [HttpPost]
    public async Task<IActionResult> GrantPermissionToRole(
        [FromRoute] long roleId,
        [FromBody] GrantPermissionToRoleHttpRequest request,
        CancellationToken cancellationToken)
    {
        Result<GrantPermissionToRoleResponseDto> result =
            await _grantPermissionToRoleUseCase.ExecuteAsync(
                new GrantPermissionToRoleRequestDto
                {
                    RoleId = roleId,
                    PermissionId = request.PermissionId
                },
                cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return Ok(MapGrantPermissionToRoleResponse(result.Value!));
    }

    [Authorize(Policy = AuthorizationPolicies.AuthzRolePermissionsRevoke)]
    [HttpDelete("{permissionId:long}")]
    public async Task<IActionResult> RevokePermissionFromRole(
        [FromRoute] long roleId,
        [FromRoute] long permissionId,
        CancellationToken cancellationToken)
    {
        Result<RevokePermissionFromRoleResponseDto> result =
            await _revokePermissionFromRoleUseCase.ExecuteAsync(
                new RevokePermissionFromRoleRequestDto
                {
                    RoleId = roleId,
                    PermissionId = permissionId
                },
                cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return Ok(MapRevokePermissionFromRoleResponse(result.Value!));
    }

    [Authorize(Policy = AuthorizationPolicies.AuthzRolePermissionsRead)]
    [HttpGet]
    public async Task<IActionResult> GetRolePermissions(
        [FromRoute] long roleId,
        CancellationToken cancellationToken)
    {
        Result<GetRolePermissionsResponseDto> result =
            await _getRolePermissionsUseCase.ExecuteAsync(
                new GetRolePermissionsRequestDto
                {
                    RoleId = roleId
                },
                cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return Ok(MapGetRolePermissionsResponse(result.Value!));
    }

    [Authorize(Policy = AuthorizationPolicies.AuthzRolePermissionsRead)]
    [HttpGet("~/api/v1/admin/authz/permissions/{permissionId:long}/roles")]
    public async Task<IActionResult> GetPermissionRoles(
        [FromRoute] long permissionId,
        CancellationToken cancellationToken)
    {
        Result<GetPermissionRolesResponseDto> result =
            await _getPermissionRolesUseCase.ExecuteAsync(
                new GetPermissionRolesRequestDto
                {
                    PermissionId = permissionId
                },
                cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return Ok(MapGetPermissionRolesResponse(result.Value!));
    }

    private static GrantPermissionToRoleHttpResponse MapGrantPermissionToRoleResponse(
        GrantPermissionToRoleResponseDto source)
    {
        return new GrantPermissionToRoleHttpResponse
        {
            RoleId = source.RoleId,
            PermissionId = source.PermissionId,
            IsGranted = source.IsGranted,
            WasAlreadyGranted = source.WasAlreadyGranted
        };
    }

    private static RevokePermissionFromRoleHttpResponse MapRevokePermissionFromRoleResponse(
        RevokePermissionFromRoleResponseDto source)
    {
        return new RevokePermissionFromRoleHttpResponse
        {
            RoleId = source.RoleId,
            PermissionId = source.PermissionId,
            IsRevoked = source.IsRevoked,
            WasAlreadyRevoked = source.WasAlreadyRevoked
        };
    }

    private static GetRolePermissionsHttpResponse MapGetRolePermissionsResponse(
        GetRolePermissionsResponseDto source)
    {
        return new GetRolePermissionsHttpResponse
        {
            RoleId = source.RoleId,
            Permissions = source.Permissions
                .Select(permission => new RolePermissionItemHttpResponse
                {
                    PermissionId = permission.PermissionId,
                    PublicId = permission.PublicId,
                    Key = permission.Key,
                    KeyNormalized = permission.KeyNormalized,
                    Description = permission.Description,
                    Module = permission.Module,
                    Action = permission.Action,
                    IsSystem = permission.IsSystem,
                    IsActive = permission.IsActive,
                    GrantedAt = permission.GrantedAt,
                    GrantedByUserId = permission.GrantedByUserId
                })
                .ToArray()
        };
    }

    private static GetPermissionRolesHttpResponse MapGetPermissionRolesResponse(
        GetPermissionRolesResponseDto source)
    {
        return new GetPermissionRolesHttpResponse
        {
            PermissionId = source.PermissionId,
            Roles = source.Roles
                .Select(role => new PermissionRoleItemHttpResponse
                {
                    RoleId = role.RoleId,
                    PublicId = role.PublicId,
                    Name = role.Name,
                    NameNormalized = role.NameNormalized,
                    DisplayName = role.DisplayName,
                    Description = role.Description,
                    IsSystem = role.IsSystem,
                    IsActive = role.IsActive,
                    GrantedAt = role.GrantedAt,
                    GrantedByUserId = role.GrantedByUserId
                })
                .ToArray()
        };
    }
}