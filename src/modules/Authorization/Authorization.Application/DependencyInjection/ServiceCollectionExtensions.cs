using Authorization.Application.UseCases.Permissions.ActivatePermission;
using Authorization.Application.UseCases.Permissions.CreatePermission;
using Authorization.Application.UseCases.Permissions.DeactivatePermission;
using Authorization.Application.UseCases.Permissions.GetPermissions;
using Authorization.Application.UseCases.Permissions.UpdatePermission;
using Authorization.Application.UseCases.Queries.GetUserEffectivePermissions;
using Authorization.Application.UseCases.RolePermissions.GetPermissionRoles;
using Authorization.Application.UseCases.RolePermissions.GetRolePermissions;
using Authorization.Application.UseCases.RolePermissions.GrantPermissionToRole;
using Authorization.Application.UseCases.RolePermissions.RevokePermissionFromRole;
using Authorization.Application.UseCases.Roles.ActivateRole;
using Authorization.Application.UseCases.Roles.CreateRole;
using Authorization.Application.UseCases.Roles.DeactivateRole;
using Authorization.Application.UseCases.Roles.GetRoles;
using Authorization.Application.UseCases.Roles.UpdateRole;
using Authorization.Application.UseCases.UserRoles.AssignRoleToUser;
using Authorization.Application.UseCases.UserRoles.GetUserRoles;
using Authorization.Application.UseCases.UserRoles.RevokeRoleFromUser;
using Microsoft.Extensions.DependencyInjection;

namespace Authorization.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthorizationApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICreateRoleUseCase, CreateRoleUseCase>();
        services.AddScoped<IUpdateRoleUseCase, UpdateRoleUseCase>();
        services.AddScoped<IActivateRoleUseCase, ActivateRoleUseCase>();
        services.AddScoped<IDeactivateRoleUseCase, DeactivateRoleUseCase>();
        services.AddScoped<IGetRolesUseCase, GetRolesUseCase>();

        services.AddScoped<ICreatePermissionUseCase, CreatePermissionUseCase>();
        services.AddScoped<IUpdatePermissionUseCase, UpdatePermissionUseCase>();
        services.AddScoped<IActivatePermissionUseCase, ActivatePermissionUseCase>();
        services.AddScoped<IDeactivatePermissionUseCase, DeactivatePermissionUseCase>();
        services.AddScoped<IGetPermissionsUseCase, GetPermissionsUseCase>();

        services.AddScoped<IAssignRoleToUserUseCase, AssignRoleToUserUseCase>();
        services.AddScoped<IRevokeRoleFromUserUseCase, RevokeRoleFromUserUseCase>();
        services.AddScoped<IGetUserRolesUseCase, GetUserRolesUseCase>();

        services.AddScoped<IGrantPermissionToRoleUseCase, GrantPermissionToRoleUseCase>();
        services.AddScoped<IRevokePermissionFromRoleUseCase, RevokePermissionFromRoleUseCase>();
        services.AddScoped<IGetRolePermissionsUseCase, GetRolePermissionsUseCase>();
        services.AddScoped<IGetPermissionRolesUseCase, GetPermissionRolesUseCase>();

        services.AddScoped<IGetUserEffectivePermissionsUseCase, GetUserEffectivePermissionsUseCase>();

        return services;
    }
}