using Authorization.Application.Contracts.Requests;
using Authorization.Application.UseCases.AssignRoleToUser;
using Authorization.Application.UseCases.GrantPermissionToRole;
using Authorization.Application.UseCases.RevokePermissionFromRole;
using Authorization.Application.UseCases.RevokeRoleFromUser;
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

        public AuthorizationAdminController(
            IAssignRoleToUserUseCase assignRoleToUserUseCase,
            IRevokeRoleFromUserUseCase revokeRoleFromUserUseCase,
            IGrantPermissionToRoleUseCase grantPermissionToRoleUseCase,
            IRevokePermissionFromRoleUseCase revokePermissionFromRoleUseCase)
        {
            _assignRoleToUserUseCase = assignRoleToUserUseCase;
            _revokeRoleFromUserUseCase = revokeRoleFromUserUseCase;
            _grantPermissionToRoleUseCase = grantPermissionToRoleUseCase;
            _revokePermissionFromRoleUseCase = revokePermissionFromRoleUseCase;
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
    }
}