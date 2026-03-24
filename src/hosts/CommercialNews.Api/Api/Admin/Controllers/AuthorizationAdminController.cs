using Authorization.Application.Contracts.Requests;
using Authorization.Application.UseCases.AssignRoleToUser;
using Authorization.Application.UseCases.CheckUserHasPermission;
using Authorization.Application.UseCases.CreatePermission;
using Authorization.Application.UseCases.CreateRole;
using Authorization.Application.UseCases.DeactivatePermission;
using Authorization.Application.UseCases.DeactivateRole;
using Authorization.Application.UseCases.GetPermissionRoles;
using Authorization.Application.UseCases.GetRolePermissions;
using Authorization.Application.UseCases.GetRoleUsers;
using Authorization.Application.UseCases.GetUserEffectivePermissions;
using Authorization.Application.UseCases.GetUserRoles;
using Authorization.Application.UseCases.GrantPermissionToRole;
using Authorization.Application.UseCases.RevokePermissionFromRole;
using Authorization.Application.UseCases.RevokeRoleFromUser;
using Authorization.Application.UseCases.UpdatePermission;
using Authorization.Application.UseCases.UpdateRole;
using CommercialNews.Api.Api.Admin.Contracts.Requests;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers
{
    [ApiController]
    [Route("api/v1/admin/authz")]
    public sealed class AuthorizationAdminController : ControllerBase
    {
        private readonly IAssignRoleToUserUseCase _assignRoleToUserUseCase;
        private readonly IRevokeRoleFromUserUseCase _revokeRoleFromUserUseCase;
        private readonly IGrantPermissionToRoleUseCase _grantPermissionToRoleUseCase;
        private readonly IRevokePermissionFromRoleUseCase _revokePermissionFromRoleUseCase;
        private readonly ICreateRoleUseCase _createRoleUseCase;
        private readonly ICreatePermissionUseCase _createPermissionUseCase;
        private readonly IUpdateRoleUseCase _updateRoleUseCase;
        private readonly IUpdatePermissionUseCase _updatePermissionUseCase;
        private readonly IDeactivateRoleUseCase _deactivateRoleUseCase;
        private readonly IDeactivatePermissionUseCase _deactivatePermissionUseCase;
        private readonly IGetUserRolesUseCase _getUserRolesUseCase;
        private readonly IGetRoleUsersUseCase _getRoleUsersUseCase;
        private readonly IGetRolePermissionsUseCase _getRolePermissionsUseCase;
        private readonly IGetPermissionRolesUseCase _getPermissionRolesUseCase;
        private readonly IGetUserEffectivePermissionsUseCase _getUserEffectivePermissionsUseCase;
        private readonly ICheckUserHasPermissionUseCase _checkUserHasPermissionUseCase;

        public AuthorizationAdminController(
            IAssignRoleToUserUseCase assignRoleToUserUseCase,
            IRevokeRoleFromUserUseCase revokeRoleFromUserUseCase,
            IGrantPermissionToRoleUseCase grantPermissionToRoleUseCase,
            IRevokePermissionFromRoleUseCase revokePermissionFromRoleUseCase,
            ICreateRoleUseCase createRoleUseCase,
            ICreatePermissionUseCase createPermissionUseCase,
            IUpdateRoleUseCase updateRoleUseCase,
            IUpdatePermissionUseCase updatePermissionUseCase,
            IDeactivateRoleUseCase deactivateRoleUseCase,
            IDeactivatePermissionUseCase deactivatePermissionUseCase,
            IGetUserRolesUseCase getUserRolesUseCase,
            IGetRoleUsersUseCase getRoleUsersUseCase,
            IGetRolePermissionsUseCase getRolePermissionsUseCase,
            IGetPermissionRolesUseCase getPermissionRolesUseCase,
            IGetUserEffectivePermissionsUseCase getUserEffectivePermissionsUseCase,
            ICheckUserHasPermissionUseCase checkUserHasPermissionUseCase)
        {
            _assignRoleToUserUseCase = assignRoleToUserUseCase;
            _revokeRoleFromUserUseCase = revokeRoleFromUserUseCase;
            _grantPermissionToRoleUseCase = grantPermissionToRoleUseCase;
            _revokePermissionFromRoleUseCase = revokePermissionFromRoleUseCase;
            _createRoleUseCase = createRoleUseCase;
            _createPermissionUseCase = createPermissionUseCase;
            _updateRoleUseCase = updateRoleUseCase;
            _updatePermissionUseCase = updatePermissionUseCase;
            _deactivateRoleUseCase = deactivateRoleUseCase;
            _deactivatePermissionUseCase = deactivatePermissionUseCase;
            _getUserRolesUseCase = getUserRolesUseCase;
            _getRoleUsersUseCase = getRoleUsersUseCase;
            _getRolePermissionsUseCase = getRolePermissionsUseCase;
            _getPermissionRolesUseCase = getPermissionRolesUseCase;
            _getUserEffectivePermissionsUseCase = getUserEffectivePermissionsUseCase;
            _checkUserHasPermissionUseCase = checkUserHasPermissionUseCase;
        }

        [HttpPost("roles")]
        public async Task<IActionResult> CreateRole(
            [FromBody] CreateRoleHttpRequest request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest("Request body is required.");
            }

            var response = await _createRoleUseCase.ExecuteAsync(
                new CreateRoleRequestDto
                {
                    Name = request.Name,
                    Description = request.Description,
                    IsSystem = request.IsSystem
                },
                cancellationToken);

            return Ok(response);
        }

        [HttpPost("permissions")]
        public async Task<IActionResult> CreatePermission(
            [FromBody] CreatePermissionHttpRequest request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest("Request body is required.");
            }

            var response = await _createPermissionUseCase.ExecuteAsync(
                new CreatePermissionRequestDto
                {
                    Name = request.Name,
                    Description = request.Description,
                    Module = request.Module,
                    IsSystem = request.IsSystem
                },
                cancellationToken);

            return Ok(response);
        }

        [HttpPost("users/{userId:long}/roles")]
        public async Task<IActionResult> AssignRoleToUser(
            [FromRoute] long userId,
            [FromBody] AssignRoleToUserHttpRequest request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest("Request body is required.");
            }

            var response = await _assignRoleToUserUseCase.ExecuteAsync(
                new AssignRoleToUserRequestDto
                {
                    UserId = userId,
                    RoleId = request.RoleId
                },
                cancellationToken);

            return Ok(response);
        }

        [HttpPost("users/{userId:long}/roles/{roleId:long}:revoke")]
        public async Task<IActionResult> RevokeRoleFromUser(
            [FromRoute] long userId,
            [FromRoute] long roleId,
            CancellationToken cancellationToken)
        {
            var response = await _revokeRoleFromUserUseCase.ExecuteAsync(
                new RevokeRoleFromUserRequestDto
                {
                    UserId = userId,
                    RoleId = roleId
                },
                cancellationToken);

            return Ok(response);
        }

        [HttpPost("roles/{roleId:long}/permissions")]
        public async Task<IActionResult> GrantPermissionToRole(
            [FromRoute] long roleId,
            [FromBody] GrantPermissionToRoleHttpRequest request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest("Request body is required.");
            }

            var response = await _grantPermissionToRoleUseCase.ExecuteAsync(
                new GrantPermissionToRoleRequestDto
                {
                    RoleId = roleId,
                    PermissionId = request.PermissionId
                },
                cancellationToken);

            return Ok(response);
        }

        [HttpPost("roles/{roleId:long}/permissions/{permissionId:long}:revoke")]
        public async Task<IActionResult> RevokePermissionFromRole(
            [FromRoute] long roleId,
            [FromRoute] long permissionId,
            CancellationToken cancellationToken)
        {
            var response = await _revokePermissionFromRoleUseCase.ExecuteAsync(
                new RevokePermissionFromRoleRequestDto
                {
                    RoleId = roleId,
                    PermissionId = permissionId
                },
                cancellationToken);

            return Ok(response);
        }

        [HttpPut("roles/{roleId:long}")]
        public async Task<IActionResult> UpdateRole(
            [FromRoute] long roleId,
            [FromBody] UpdateRoleHttpRequest request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest("Request body is required.");
            }

            var response = await _updateRoleUseCase.ExecuteAsync(
                new UpdateRoleRequestDto
                {
                    RoleId = roleId,
                    Name = request.Name,
                    Description = request.Description
                },
                cancellationToken);

            return Ok(response);
        }

        [HttpPut("permissions/{permissionId:long}")]
        public async Task<IActionResult> UpdatePermission(
            [FromRoute] long permissionId,
            [FromBody] UpdatePermissionHttpRequest request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest("Request body is required.");
            }

            var response = await _updatePermissionUseCase.ExecuteAsync(
                new UpdatePermissionRequestDto
                {
                    PermissionId = permissionId,
                    Name = request.Name,
                    Description = request.Description,
                    Module = request.Module
                },
                cancellationToken);

            return Ok(response);
        }

        [HttpPost("roles/{roleId:long}:deactivate")]
        public async Task<IActionResult> DeactivateRole(
            [FromRoute] long roleId,
            CancellationToken cancellationToken)
        {
            var response = await _deactivateRoleUseCase.ExecuteAsync(
                new DeactivateRoleRequestDto
                {
                    RoleId = roleId
                },
                cancellationToken);

            return Ok(response);
        }

        [HttpPost("permissions/{permissionId:long}:deactivate")]
        public async Task<IActionResult> DeactivatePermission(
            [FromRoute] long permissionId,
            CancellationToken cancellationToken)
        {
            var response = await _deactivatePermissionUseCase.ExecuteAsync(
                new DeactivatePermissionRequestDto
                {
                    PermissionId = permissionId
                },
                cancellationToken);

            return Ok(response);
        }

        [HttpGet("users/{userId:long}/roles")]
        public async Task<IActionResult> GetUserRoles(
            [FromRoute] long userId,
            CancellationToken cancellationToken)
        {
            var response = await _getUserRolesUseCase.ExecuteAsync(
                new GetUserRolesRequestDto
                {
                    UserId = userId
                },
                cancellationToken);

            return Ok(response);
        }

        [HttpGet("roles/{roleId:long}/users")]
        public async Task<IActionResult> GetRoleUsers(
            [FromRoute] long roleId,
            CancellationToken cancellationToken)
        {
            var response = await _getRoleUsersUseCase.ExecuteAsync(
                new GetRoleUsersRequestDto
                {
                    RoleId = roleId
                },
                cancellationToken);

            return Ok(response);
        }

        [HttpGet("roles/{roleId:long}/permissions")]
        public async Task<IActionResult> GetRolePermissions(
            [FromRoute] long roleId,
            CancellationToken cancellationToken)
        {
            var response = await _getRolePermissionsUseCase.ExecuteAsync(
                new GetRolePermissionsRequestDto
                {
                    RoleId = roleId
                },
                cancellationToken);

            return Ok(response);
        }

        [HttpGet("permissions/{permissionId:long}/roles")]
        public async Task<IActionResult> GetPermissionRoles(
            [FromRoute] long permissionId,
            CancellationToken cancellationToken)
        {
            var response = await _getPermissionRolesUseCase.ExecuteAsync(
                new GetPermissionRolesRequestDto
                {
                    PermissionId = permissionId
                },
                cancellationToken);

            return Ok(response);
        }

        [HttpGet("users/{userId:long}/effective-permissions")]
        public async Task<IActionResult> GetUserEffectivePermissions(
            [FromRoute] long userId,
            CancellationToken cancellationToken)
        {
            var response = await _getUserEffectivePermissionsUseCase.ExecuteAsync(
                new GetUserEffectivePermissionsRequestDto
                {
                    UserId = userId
                },
                cancellationToken);

            return Ok(response);
        }

        [HttpPost("users/{userId:long}:check-permission")]
        public async Task<IActionResult> CheckUserHasPermission(
            [FromRoute] long userId,
            [FromBody] CheckUserHasPermissionHttpRequest request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest("Request body is required.");
            }

            var response = await _checkUserHasPermissionUseCase.ExecuteAsync(
                new CheckUserHasPermissionRequestDto
                {
                    UserId = userId,
                    PermissionName = request.PermissionName
                },
                cancellationToken);

            return Ok(response);
        }
    }
}