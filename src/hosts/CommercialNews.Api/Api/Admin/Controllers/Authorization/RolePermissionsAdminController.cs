using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.UseCases.GetPermissionRoles;
using Authorization.Application.UseCases.GetRolePermissions;
using Authorization.Application.UseCases.GrantPermissionToRole;
using Authorization.Application.UseCases.RevokePermissionFromRole;
using CommercialNews.Api.Api.Admin.Contracts.Authorization.RolePermissions.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Authorization.RolePermissions.Responses;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers.Authorization
{
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
            IRevokePermissionFromRoleUseCase revokePermissionFromRoleUseCase,
            IGetRolePermissionsUseCase getRolePermissionsUseCase,
            IGetPermissionRolesUseCase getPermissionRolesUseCase)
        {
            _grantPermissionToRoleUseCase = grantPermissionToRoleUseCase;
            _revokePermissionFromRoleUseCase = revokePermissionFromRoleUseCase;
            _getRolePermissionsUseCase = getRolePermissionsUseCase;
            _getPermissionRolesUseCase = getPermissionRolesUseCase;
        }

        [HttpPost]
        public async Task<IActionResult> GrantPermissionToRole(
            [FromRoute] long roleId,
            [FromBody] GrantPermissionToRoleHttpRequest request,
            CancellationToken cancellationToken)
        {
            Result<GrantPermissionToRoleResponseDto> result = await _grantPermissionToRoleUseCase.ExecuteAsync(
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

        [HttpGet]
        public async Task<IActionResult> GetRolePermissions(
            [FromRoute] long roleId,
            CancellationToken cancellationToken)
        {
            Result<GetRolePermissionsResponseDto> result = await _getRolePermissionsUseCase.ExecuteAsync(
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

        [HttpGet("~/api/v1/admin/authz/permissions/{permissionId:long}/roles")]
        public async Task<IActionResult> GetPermissionRoles(
            [FromRoute] long permissionId,
            CancellationToken cancellationToken)
        {
            Result<GetPermissionRolesResponseDto> result = await _getPermissionRolesUseCase.ExecuteAsync(
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
                RolePermissionId = source.RolePermissionId,
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

        private static GetRolePermissionsHttpResponse MapGetRolePermissionsResponse(GetRolePermissionsResponseDto source)
        {
            return new GetRolePermissionsHttpResponse
            {
                RoleId = source.RoleId,
                Permissions = source.Permissions
                    .Select(permission => new RolePermissionItemHttpResponse
                    {
                        PermissionId = permission.PermissionId,
                        PublicId = permission.PublicId,
                        Name = permission.Name,
                        NameNormalized = permission.NameNormalized,
                        Description = permission.Description,
                        Module = permission.Module,
                        IsSystem = permission.IsSystem,
                        IsActive = permission.IsActive,
                        GrantedAt = permission.GrantedAt,
                        GrantedByUserId = permission.GrantedByUserId
                    })
                    .ToArray()
            };
        }

        private static GetPermissionRolesHttpResponse MapGetPermissionRolesResponse(GetPermissionRolesResponseDto source)
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
}