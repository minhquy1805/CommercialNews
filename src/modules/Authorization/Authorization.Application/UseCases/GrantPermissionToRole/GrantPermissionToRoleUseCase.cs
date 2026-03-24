using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Domain.Entities;

namespace Authorization.Application.UseCases.GrantPermissionToRole
{
    public sealed class GrantPermissionToRoleUseCase : IGrantPermissionToRoleUseCase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly IRequestContext _requestContext;

        public GrantPermissionToRoleUseCase(
            IRoleRepository roleRepository,
            IPermissionRepository permissionRepository,
            IRolePermissionRepository rolePermissionRepository,
            IRequestContext requestContext)
        {
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _requestContext = requestContext;
        }

        public async Task<GrantPermissionToRoleResponseDto> ExecuteAsync(
            GrantPermissionToRoleRequestDto request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.RoleId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request.RoleId), "RoleId must be greater than zero.");
            }

            if (request.PermissionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request.PermissionId), "PermissionId must be greater than zero.");
            }

            var role = await _roleRepository.GetByIdAsync(
                request.RoleId,
                cancellationToken);

            if (role is null)
            {
                throw new InvalidOperationException($"Role with id {request.RoleId} was not found.");
            }

            if (!role.IsActive)
            {
                throw new InvalidOperationException($"Role with id {request.RoleId} is inactive.");
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
                throw new InvalidOperationException($"Permission with id {request.PermissionId} is inactive.");
            }

            var existingGrant = await _rolePermissionRepository.GetActiveByRoleIdAndPermissionIdAsync(
                request.RoleId,
                request.PermissionId,
                cancellationToken);

            if (existingGrant is not null)
            {
                return new GrantPermissionToRoleResponseDto
                {
                    RolePermissionId = existingGrant.RolePermissionId,
                    RoleId = existingGrant.RoleId,
                    PermissionId = existingGrant.PermissionId,
                    IsGranted = true,
                    WasAlreadyGranted = true
                };
            }

            var newGrant = RolePermission.CreateNew(
                roleId: request.RoleId,
                permissionId: request.PermissionId,
                grantedAt: DateTime.UtcNow,
                grantedByUserId: _requestContext.CurrentUserId);

            var grantedRolePermission = await _rolePermissionRepository.InsertAsync(
                newGrant,
                cancellationToken);

            return new GrantPermissionToRoleResponseDto
            {
                RolePermissionId = grantedRolePermission.RolePermissionId,
                RoleId = grantedRolePermission.RoleId,
                PermissionId = grantedRolePermission.PermissionId,
                IsGranted = true,
                WasAlreadyGranted = false
            };
        }
    }
}