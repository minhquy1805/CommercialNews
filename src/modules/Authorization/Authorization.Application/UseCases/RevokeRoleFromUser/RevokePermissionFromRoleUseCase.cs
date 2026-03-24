using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.RevokePermissionFromRole
{
    public sealed class RevokePermissionFromRoleUseCase : IRevokePermissionFromRoleUseCase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly IRequestContext _requestContext;

        public RevokePermissionFromRoleUseCase(
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

        public async Task<RevokePermissionFromRoleResponseDto> ExecuteAsync(
            RevokePermissionFromRoleRequestDto request,
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

            var permission = await _permissionRepository.GetByIdAsync(
                request.PermissionId,
                cancellationToken);

            if (permission is null)
            {
                throw new InvalidOperationException($"Permission with id {request.PermissionId} was not found.");
            }

            var existingGrant = await _rolePermissionRepository.GetActiveByRoleIdAndPermissionIdAsync(
                request.RoleId,
                request.PermissionId,
                cancellationToken);

            if (existingGrant is null)
            {
                return new RevokePermissionFromRoleResponseDto
                {
                    RoleId = request.RoleId,
                    PermissionId = request.PermissionId,
                    IsRevoked = true,
                    WasAlreadyRevoked = true
                };
            }

            await _rolePermissionRepository.RevokeAsync(
                request.RoleId,
                request.PermissionId,
                _requestContext.CurrentUserId,
                cancellationToken);

            return new RevokePermissionFromRoleResponseDto
            {
                RoleId = request.RoleId,
                PermissionId = request.PermissionId,
                IsRevoked = true,
                WasAlreadyRevoked = false
            };
        }
    }
}