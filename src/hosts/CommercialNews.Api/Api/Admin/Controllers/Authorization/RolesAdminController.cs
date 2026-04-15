using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.UseCases.ActivateRole;
using Authorization.Application.UseCases.CreateRole;
using Authorization.Application.UseCases.DeactivateRole;
using Authorization.Application.UseCases.UpdateRole;
using CommercialNews.Api.Api.Admin.Contracts.Authorization.Roles.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Authorization.Roles.Responses;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers.Authorization
{
    [ApiController]
    [Route("api/v1/admin/authz/roles")]
    public sealed class RolesAdminController : ControllerBase
    {
        private readonly ICreateRoleUseCase _createRoleUseCase;
        private readonly IUpdateRoleUseCase _updateRoleUseCase;
        private readonly IActivateRoleUseCase _activateRoleUseCase;
        private readonly IDeactivateRoleUseCase _deactivateRoleUseCase;

        public RolesAdminController(
            ICreateRoleUseCase createRoleUseCase,
            IUpdateRoleUseCase updateRoleUseCase,
            IActivateRoleUseCase activateRoleUseCase,
            IDeactivateRoleUseCase deactivateRoleUseCase)
        {
            _createRoleUseCase = createRoleUseCase;
            _updateRoleUseCase = updateRoleUseCase;
            _activateRoleUseCase = activateRoleUseCase;
            _deactivateRoleUseCase = deactivateRoleUseCase;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole(
            [FromBody] CreateRoleHttpRequest request,
            CancellationToken cancellationToken)
        {
            Result<CreateRoleResponseDto> result = await _createRoleUseCase.ExecuteAsync(
                new CreateRoleRequestDto
                {
                    Name = request.Name,
                    Description = request.Description,
                    IsSystem = request.IsSystem
                },
                cancellationToken);

            if (!result.IsSuccess)
            {
                return this.ToActionResult(result);
            }

            CreateRoleHttpResponse response = MapCreateRoleResponse(result.Value!);
            return StatusCode(StatusCodes.Status201Created, response);
        }

        [HttpPut("{roleId:long}")]
        public async Task<IActionResult> UpdateRole(
            [FromRoute] long roleId,
            [FromBody] UpdateRoleHttpRequest request,
            CancellationToken cancellationToken)
        {
            Result<UpdateRoleResponseDto> result = await _updateRoleUseCase.ExecuteAsync(
                new UpdateRoleRequestDto
                {
                    RoleId = roleId,
                    Name = request.Name,
                    Description = request.Description
                },
                cancellationToken);

            if (!result.IsSuccess)
            {
                return this.ToActionResult(result);
            }

            return Ok(MapUpdateRoleResponse(result.Value!));
        }

        [HttpPost("{roleId:long}:activate")]
        public async Task<IActionResult> ActivateRole(
            [FromRoute] long roleId,
            CancellationToken cancellationToken)
        {
            Result<ActivateRoleResponseDto> result = await _activateRoleUseCase.ExecuteAsync(
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

        [HttpPost("{roleId:long}:deactivate")]
        public async Task<IActionResult> DeactivateRole(
            [FromRoute] long roleId,
            CancellationToken cancellationToken)
        {
            Result<DeactivateRoleResponseDto> result = await _deactivateRoleUseCase.ExecuteAsync(
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

        private static CreateRoleHttpResponse MapCreateRoleResponse(CreateRoleResponseDto source)
        {
            return new CreateRoleHttpResponse
            {
                RoleId = source.RoleId,
                PublicId = source.PublicId,
                Name = source.Name,
                NameNormalized = source.NameNormalized,
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
}