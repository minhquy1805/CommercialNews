using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.DeactivatePermission
{
    public sealed class DeactivatePermissionUseCase : IDeactivatePermissionUseCase
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRequestContext _requestContext;

        public DeactivatePermissionUseCase(
            IPermissionRepository permissionRepository,
            IRequestContext requestContext)
        {
            _permissionRepository = permissionRepository;
            _requestContext = requestContext;
        }

        public async Task<DeactivatePermissionResponseDto> ExecuteAsync(
            DeactivatePermissionRequestDto request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.PermissionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request.PermissionId), "PermissionId must be greater than zero.");
            }

            var permission = await _permissionRepository.GetByIdAsync(
                request.PermissionId,
                cancellationToken);

            if (permission is null)
            {
                throw new InvalidOperationException($"Permission with id {request.PermissionId} was not found.");
            }

            if (!permission.IsActive)
            {
                return new DeactivatePermissionResponseDto
                {
                    PermissionId = request.PermissionId,
                    IsDeactivated = true,
                    WasAlreadyDeactivated = true
                };
            }

            permission.Deactivate(
                DateTime.UtcNow,
                _requestContext.CurrentUserId);

            await _permissionRepository.UpdateAsync(permission, cancellationToken);

            return new DeactivatePermissionResponseDto
            {
                PermissionId = request.PermissionId,
                IsDeactivated = true,
                WasAlreadyDeactivated = false
            };
        }
    }
}