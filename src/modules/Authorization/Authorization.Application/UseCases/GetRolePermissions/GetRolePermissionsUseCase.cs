using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.GetRolePermissions
{
    public sealed class GetRolePermissionsUseCase : IGetRolePermissionsUseCase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IRolePermissionRepository _rolePermissionRepository;

        public GetRolePermissionsUseCase(
            IRoleRepository roleRepository,
            IRolePermissionRepository rolePermissionRepository)
        {
            _roleRepository = roleRepository;
            _rolePermissionRepository = rolePermissionRepository;
        }

        public async Task<GetRolePermissionsResponseDto> ExecuteAsync(
            GetRolePermissionsRequestDto request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.RoleId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request.RoleId), "RoleId must be greater than zero.");
            }

            var role = await _roleRepository.GetByIdAsync(
                request.RoleId,
                cancellationToken);

            if (role is null)
            {
                throw new InvalidOperationException($"Role with id {request.RoleId} was not found.");
            }

            var permissions = await _rolePermissionRepository.GetActivePermissionsByRoleIdAsync(
                request.RoleId,
                cancellationToken);

            return new GetRolePermissionsResponseDto
            {
                RoleId = request.RoleId,
                Permissions = permissions.Select(x => new RolePermissionItemDto
                {
                    PermissionId = x.PermissionId,
                    PublicId = x.PublicId,
                    Name = x.Name,
                    NameNormalized = x.NameNormalized,
                    Description = x.Description,
                    Module = x.Module,
                    IsSystem = x.IsSystem,
                    IsActive = x.IsActive,
                    GrantedAt = x.GrantedAt,
                    GrantedByUserId = x.GrantedByUserId
                }).ToList()
            };
        }
    }
}