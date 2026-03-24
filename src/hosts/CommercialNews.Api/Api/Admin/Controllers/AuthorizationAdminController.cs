using Authorization.Application.Contracts.Requests;
using Authorization.Application.UseCases.AssignRoleToUser;
using CommercialNews.Api.Api.Admin.Contracts.Requests;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers
{
    [ApiController]
    [Route("api/v1/admin/authz")]
    public sealed class AuthorizationAdminController : ControllerBase
    {
        private readonly IAssignRoleToUserUseCase _assignRoleToUserUseCase;

        public AuthorizationAdminController(
            IAssignRoleToUserUseCase assignRoleToUserUseCase)
        {
            _assignRoleToUserUseCase = assignRoleToUserUseCase;
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
    }
}