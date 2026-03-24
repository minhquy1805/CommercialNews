using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.Helpers;

namespace Authorization.Application.UseCases.UpdatePermission
{
    public sealed class UpdatePermissionUseCase : IUpdatePermissionUseCase
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRequestContext _requestContext;

        public UpdatePermissionUseCase(
            IPermissionRepository permissionRepository,
            IRequestContext requestContext)
        {
            _permissionRepository = permissionRepository;
            _requestContext = requestContext;
        }

        public async Task<UpdatePermissionResponseDto> ExecuteAsync(
            UpdatePermissionRequestDto request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.PermissionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request.PermissionId), "PermissionId must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Permission name is required.", nameof(request.Name));
            }

            var permission = await _permissionRepository.GetByIdAsync(
                request.PermissionId,
                cancellationToken);

            if (permission is null)
            {
                throw new InvalidOperationException($"Permission with id {request.PermissionId} was not found.");
            }

            var normalizedName = AuthorizationNameNormalizer.Normalize(request.Name);

            var existingPermission = await _permissionRepository.GetByNameNormalizedAsync(
                normalizedName,
                cancellationToken);

            if (existingPermission is not null && existingPermission.PermissionId != permission.PermissionId)
            {
                throw new InvalidOperationException(
                    $"Another permission with normalized name '{normalizedName}' already exists.");
            }

            var now = DateTime.UtcNow;
            var actorUserId = _requestContext.CurrentUserId;

            permission.Rename(
                request.Name.Trim(),
                normalizedName,
                now,
                actorUserId);

            permission.ChangeDescription(
                request.Description,
                now,
                actorUserId);

            permission.ChangeModule(
                request.Module,
                now,
                actorUserId);

            var updatedPermission = await _permissionRepository.UpdateAsync(
                permission,
                cancellationToken);

            return new UpdatePermissionResponseDto
            {
                PermissionId = updatedPermission.PermissionId,
                PublicId = updatedPermission.PublicId,
                Name = updatedPermission.Name,
                NameNormalized = updatedPermission.NameNormalized,
                Description = updatedPermission.Description,
                Module = updatedPermission.Module,
                IsSystem = updatedPermission.IsSystem,
                IsActive = updatedPermission.IsActive,
                UpdatedAt = updatedPermission.UpdatedAt,
                UpdatedByUserId = updatedPermission.UpdatedByUserId
            };
        }
    }
}