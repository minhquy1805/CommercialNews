using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.GetPermissionRoles
{
    public sealed class GetPermissionRolesUseCase : IGetPermissionRolesUseCase
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRolePermissionRepository _rolePermissionRepository;

        public GetPermissionRolesUseCase(
            IPermissionRepository permissionRepository,
            IRolePermissionRepository rolePermissionRepository)
        {
            _permissionRepository = permissionRepository;
            _rolePermissionRepository = rolePermissionRepository;
        }

        public async Task<GetPermissionRolesResponseDto> ExecuteAsync(
            GetPermissionRolesRequestDto request,
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

            var roles = await _rolePermissionRepository.GetActiveRolesByPermissionIdAsync(
                request.PermissionId,
                cancellationToken);

            return new GetPermissionRolesResponseDto
            {
                PermissionId = request.PermissionId,
                Roles = roles.Select(x => new PermissionRoleItemDto
                {
                    RoleId = x.RoleId,
                    PublicId = x.PublicId,
                    Name = x.Name,
                    NameNormalized = x.NameNormalized,
                    Description = x.Description,
                    IsSystem = x.IsSystem,
                    IsActive = x.IsActive,
                    GrantedAt = x.GrantedAt,
                    GrantedByUserId = x.GrantedByUserId
                }).ToList()
            };
        }
    }
}