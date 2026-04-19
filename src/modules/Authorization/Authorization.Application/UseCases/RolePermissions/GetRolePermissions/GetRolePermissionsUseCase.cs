using Authorization.Application.Contracts.RolePermissions;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using Authorization.Application.Validation.RolePermissions;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.RolePermissions.GetRolePermissions;

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

    public async Task<Result<GetRolePermissionsResponseDto>> ExecuteAsync(
        GetRolePermissionsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = GetRolePermissionsValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<GetRolePermissionsResponseDto>.Failure(validationError);
        }

        try
        {
            var role = await _roleRepository.GetByIdAsync(
                request.RoleId,
                cancellationToken);

            if (role is null)
            {
                return Result<GetRolePermissionsResponseDto>.Failure(
                    AuthorizationErrors.Role.NotFound);
            }

            var permissions = await _rolePermissionRepository.GetPermissionsByRoleIdAsync(
                request.RoleId,
                cancellationToken);

            return Result<GetRolePermissionsResponseDto>.Success(
                new GetRolePermissionsResponseDto
                {
                    RoleId = request.RoleId,
                    Permissions = permissions.Select(x => new RolePermissionItemDto
                    {
                        PermissionId = x.PermissionId,
                        PublicId = x.PublicId,
                        Key = x.Key,
                        KeyNormalized = x.KeyNormalized,
                        Description = x.Description,
                        Module = x.Module,
                        Action = x.Action,
                        IsSystem = x.IsSystem,
                        IsActive = x.IsActive,
                        GrantedAt = x.GrantedAt,
                        GrantedByUserId = x.GrantedByUserId
                    }).ToList()
                });
        }
        catch (PersistenceException exception)
        {
            return Result<GetRolePermissionsResponseDto>.Failure(
                MapPersistenceException(exception));
        }
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "AUTHORIZATION.ROLE_NOT_FOUND" =>
                AuthorizationErrors.Role.NotFound,

            _ => AuthorizationErrors.UnexpectedError
        };
    }
}