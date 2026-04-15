using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

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

        public async Task<Result<GetPermissionRolesResponseDto>> ExecuteAsync(
            GetPermissionRolesRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.PermissionId <= 0)
            {
                return Result<GetPermissionRolesResponseDto>.Failure(
                    AuthorizationErrors.Permission.InvalidPermissionId);
            }

            try
            {
                var permission = await _permissionRepository.GetByIdAsync(
                    request.PermissionId,
                    cancellationToken);

                if (permission is null)
                {
                    return Result<GetPermissionRolesResponseDto>.Failure(
                        AuthorizationErrors.Permission.NotFound);
                }

                var roles = await _rolePermissionRepository.GetActiveRolesByPermissionIdAsync(
                    request.PermissionId,
                    cancellationToken);

                return Result<GetPermissionRolesResponseDto>.Success(
                    new GetPermissionRolesResponseDto
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
                    });
            }
            catch (PersistenceException exception)
            {
                return Result<GetPermissionRolesResponseDto>.Failure(
                    MapPersistenceException(exception));
            }
        }

        private static Error MapPersistenceException(PersistenceException exception)
        {
            return exception.Code switch
            {
                "AUTHORIZATION.PERMISSION_NOT_FOUND" =>
                    AuthorizationErrors.Permission.NotFound,

                _ => AuthorizationErrors.ValidationFailed
            };
        }
    }
}