using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.UseCases.ActivatePermission;
using Authorization.Application.UseCases.CreatePermission;
using Authorization.Application.UseCases.DeactivatePermission;
using Authorization.Application.UseCases.UpdatePermission;
using CommercialNews.Api.Api.Admin.Contracts.Authorization.Permissions.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Authorization.Permissions.Responses;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.BuildingBlocks.Results;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers.Authorization
{
    [ApiController]
    [Route("api/v1/admin/authz/permissions")]
    public sealed class PermissionsAdminController : ControllerBase
    {
        private readonly ICreatePermissionUseCase _createPermissionUseCase;
        private readonly IUpdatePermissionUseCase _updatePermissionUseCase;
        private readonly IActivatePermissionUseCase _activatePermissionUseCase;
        private readonly IDeactivatePermissionUseCase _deactivatePermissionUseCase;

        public PermissionsAdminController(
            ICreatePermissionUseCase createPermissionUseCase,
            IUpdatePermissionUseCase updatePermissionUseCase,
            IActivatePermissionUseCase activatePermissionUseCase,
            IDeactivatePermissionUseCase deactivatePermissionUseCase)
        {
            _createPermissionUseCase = createPermissionUseCase;
            _updatePermissionUseCase = updatePermissionUseCase;
            _activatePermissionUseCase = activatePermissionUseCase;
            _deactivatePermissionUseCase = deactivatePermissionUseCase;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePermission(
            [FromBody] CreatePermissionHttpRequest request,
            CancellationToken cancellationToken)
        {
            Result<CreatePermissionResponseDto> result = await _createPermissionUseCase.ExecuteAsync(
                new CreatePermissionRequestDto
                {
                    Name = request.Name,
                    Description = request.Description,
                    Module = request.Module,
                    IsSystem = request.IsSystem
                },
                cancellationToken);

            if (!result.IsSuccess)
            {
                return this.ToActionResult(result);
            }

            CreatePermissionHttpResponse response = MapCreatePermissionResponse(result.Value!);
            return StatusCode(StatusCodes.Status201Created, response);
        }

        [HttpPut("{permissionId:long}")]
        public async Task<IActionResult> UpdatePermission(
            [FromRoute] long permissionId,
            [FromBody] UpdatePermissionHttpRequest request,
            CancellationToken cancellationToken)
        {
            Result<UpdatePermissionResponseDto> result = await _updatePermissionUseCase.ExecuteAsync(
                new UpdatePermissionRequestDto
                {
                    PermissionId = permissionId,
                    Name = request.Name,
                    Description = request.Description,
                    Module = request.Module
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
            Result<ActivatePermissionResponseDto> result = await _activatePermissionUseCase.ExecuteAsync(
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
            Result<DeactivatePermissionResponseDto> result = await _deactivatePermissionUseCase.ExecuteAsync(
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

        private static CreatePermissionHttpResponse MapCreatePermissionResponse(CreatePermissionResponseDto source)
        {
            return new CreatePermissionHttpResponse
            {
                PermissionId = source.PermissionId,
                PublicId = source.PublicId,
                Name = source.Name,
                NameNormalized = source.NameNormalized,
                Description = source.Description,
                Module = source.Module,
                IsSystem = source.IsSystem,
                IsActive = source.IsActive,
                CreatedAt = source.CreatedAt,
                CreatedByUserId = source.CreatedByUserId
            };
        }

        private static UpdatePermissionHttpResponse MapUpdatePermissionResponse(UpdatePermissionResponseDto source)
        {
            return new UpdatePermissionHttpResponse
            {
                PermissionId = source.PermissionId,
                PublicId = source.PublicId,
                Name = source.Name,
                NameNormalized = source.NameNormalized,
                Description = source.Description,
                Module = source.Module,
                IsSystem = source.IsSystem,
                IsActive = source.IsActive,
                UpdatedAt = source.UpdatedAt,
                UpdatedByUserId = source.UpdatedByUserId
            };
        }

        private static ActivatePermissionHttpResponse MapActivatePermissionResponse(ActivatePermissionResponseDto source)
        {
            return new ActivatePermissionHttpResponse
            {
                PermissionId = source.PermissionId,
                IsActivated = source.IsActivated,
                WasAlreadyActivated = source.WasAlreadyActivated
            };
        }

        private static DeactivatePermissionHttpResponse MapDeactivatePermissionResponse(DeactivatePermissionResponseDto source)
        {
            return new DeactivatePermissionHttpResponse
            {
                PermissionId = source.PermissionId,
                IsDeactivated = source.IsDeactivated,
                WasAlreadyDeactivated = source.WasAlreadyDeactivated
            };
        }
    }
}