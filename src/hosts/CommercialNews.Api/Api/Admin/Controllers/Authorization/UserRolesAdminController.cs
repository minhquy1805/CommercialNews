using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.UseCases.AssignRoleToUser;
using Authorization.Application.UseCases.GetUserEffectivePermissions;
using Authorization.Application.UseCases.GetUserRoles;
using Authorization.Application.UseCases.RevokeRoleFromUser;
using CommercialNews.Api.Api.Admin.Contracts.Authorization.UserRoles.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Authorization.UserRoles.Responses;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers.Authorization
{
    [ApiController]
    [Route("api/v1/admin/authz/users/{userId:long}/roles")]
    public sealed class UserRolesAdminController : ControllerBase
    {
        private readonly IAssignRoleToUserUseCase _assignRoleToUserUseCase;
        private readonly IRevokeRoleFromUserUseCase _revokeRoleFromUserUseCase;
        private readonly IGetUserRolesUseCase _getUserRolesUseCase;
        private readonly IGetUserEffectivePermissionsUseCase _getUserEffectivePermissionsUseCase;

        public UserRolesAdminController(
            IAssignRoleToUserUseCase assignRoleToUserUseCase,
            IRevokeRoleFromUserUseCase revokeRoleFromUserUseCase,
            IGetUserRolesUseCase getUserRolesUseCase,
            IGetUserEffectivePermissionsUseCase getUserEffectivePermissionsUseCase)
        {
            _assignRoleToUserUseCase = assignRoleToUserUseCase;
            _revokeRoleFromUserUseCase = revokeRoleFromUserUseCase;
            _getUserRolesUseCase = getUserRolesUseCase;
            _getUserEffectivePermissionsUseCase = getUserEffectivePermissionsUseCase;
        }

        [HttpPost]
        public async Task<IActionResult> AssignRoleToUser(
            [FromRoute] long userId,
            [FromBody] AssignRoleToUserHttpRequest request,
            CancellationToken cancellationToken)
        {
            Result<AssignRoleToUserResponseDto> result = await _assignRoleToUserUseCase.ExecuteAsync(
                new AssignRoleToUserRequestDto
                {
                    UserId = userId,
                    RoleId = request.RoleId
                },
                cancellationToken);

            if (!result.IsSuccess)
            {
                return this.ToActionResult(result);
            }

            return Ok(MapAssignRoleToUserResponse(result.Value!));
        }

        [HttpDelete("{roleId:long}")]
        public async Task<IActionResult> RevokeRoleFromUser(
            [FromRoute] long userId,
            [FromRoute] long roleId,
            CancellationToken cancellationToken)
        {
            Result<RevokeRoleFromUserResponseDto> result = await _revokeRoleFromUserUseCase.ExecuteAsync(
                new RevokeRoleFromUserRequestDto
                {
                    UserId = userId,
                    RoleId = roleId
                },
                cancellationToken);

            if (!result.IsSuccess)
            {
                return this.ToActionResult(result);
            }

            return Ok(MapRevokeRoleFromUserResponse(result.Value!));
        }

        [HttpGet]
        public async Task<IActionResult> GetUserRoles(
            [FromRoute] long userId,
            CancellationToken cancellationToken)
        {
            Result<GetUserRolesResponseDto> result = await _getUserRolesUseCase.ExecuteAsync(
                new GetUserRolesRequestDto
                {
                    UserId = userId
                },
                cancellationToken);

            if (!result.IsSuccess)
            {
                return this.ToActionResult(result);
            }

            return Ok(MapGetUserRolesResponse(result.Value!));
        }

        [HttpGet("~/api/v1/admin/authz/users/{userId:long}/effective-permissions")]
        public async Task<IActionResult> GetUserEffectivePermissions(
            [FromRoute] long userId,
            CancellationToken cancellationToken)
        {
            Result<GetUserEffectivePermissionsResponseDto> result =
                await _getUserEffectivePermissionsUseCase.ExecuteAsync(
                    new GetUserEffectivePermissionsRequestDto
                    {
                        UserId = userId
                    },
                    cancellationToken);

            if (!result.IsSuccess)
            {
                return this.ToActionResult(result);
            }

            return Ok(MapGetUserEffectivePermissionsResponse(result.Value!));
        }

        private static AssignRoleToUserHttpResponse MapAssignRoleToUserResponse(AssignRoleToUserResponseDto source)
        {
            return new AssignRoleToUserHttpResponse
            {
                UserRoleId = source.UserRoleId,
                UserId = source.UserId,
                RoleId = source.RoleId,
                IsAssigned = source.IsAssigned,
                WasAlreadyAssigned = source.WasAlreadyAssigned
            };
        }

        private static RevokeRoleFromUserHttpResponse MapRevokeRoleFromUserResponse(RevokeRoleFromUserResponseDto source)
        {
            return new RevokeRoleFromUserHttpResponse
            {
                UserId = source.UserId,
                RoleId = source.RoleId,
                IsRevoked = source.IsRevoked,
                WasAlreadyRevoked = source.WasAlreadyRevoked
            };
        }

        private static GetUserRolesHttpResponse MapGetUserRolesResponse(GetUserRolesResponseDto source)
        {
            return new GetUserRolesHttpResponse
            {
                UserId = source.UserId,
                Roles = source.Roles
                    .Select(role => new UserRoleItemHttpResponse
                    {
                        RoleId = role.RoleId,
                        PublicId = role.PublicId,
                        Name = role.Name,
                        NameNormalized = role.NameNormalized,
                        Description = role.Description,
                        IsSystem = role.IsSystem,
                        IsActive = role.IsActive,
                        AssignedAt = role.AssignedAt,
                        AssignedByUserId = role.AssignedByUserId
                    })
                    .ToArray()
            };
        }

        private static GetUserEffectivePermissionsHttpResponse MapGetUserEffectivePermissionsResponse(
            GetUserEffectivePermissionsResponseDto source)
        {
            return new GetUserEffectivePermissionsHttpResponse
            {
                UserId = source.UserId,
                Permissions = source.Permissions
                    .Select(permission => new EffectivePermissionItemHttpResponse
                    {
                        PermissionId = permission.PermissionId,
                        PublicId = permission.PublicId,
                        Name = permission.Name,
                        NameNormalized = permission.NameNormalized,
                        Description = permission.Description,
                        Module = permission.Module,
                        IsSystem = permission.IsSystem,
                        IsActive = permission.IsActive
                    })
                    .ToArray()
            };
        }
    }
}