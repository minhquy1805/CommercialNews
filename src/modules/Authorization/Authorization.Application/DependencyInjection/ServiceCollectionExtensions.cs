using Authorization.Application.UseCases.ActivatePermission;
using Authorization.Application.UseCases.ActivateRole;
using Authorization.Application.UseCases.AssignRoleToUser;
using Authorization.Application.UseCases.CreatePermission;
using Authorization.Application.UseCases.CreateRole;
using Authorization.Application.UseCases.DeactivatePermission;
using Authorization.Application.UseCases.DeactivateRole;
using Authorization.Application.UseCases.GetPermissionRoles;
using Authorization.Application.UseCases.GetRolePermissions;
using Authorization.Application.UseCases.GetUserEffectivePermissions;
using Authorization.Application.UseCases.GetUserRoles;
using Authorization.Application.UseCases.GrantPermissionToRole;
using Authorization.Application.UseCases.RevokePermissionFromRole;
using Authorization.Application.UseCases.RevokeRoleFromUser;
using Authorization.Application.UseCases.UpdatePermission;
using Authorization.Application.UseCases.UpdateRole;
using Microsoft.Extensions.DependencyInjection;

namespace Authorization.Application.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthorizationApplication(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddScoped<ICreateRoleUseCase, CreateRoleUseCase>();
            services.AddScoped<IUpdateRoleUseCase, UpdateRoleUseCase>();
            services.AddScoped<IActivateRoleUseCase, ActivateRoleUseCase>();
            services.AddScoped<IDeactivateRoleUseCase, DeactivateRoleUseCase>();

            services.AddScoped<ICreatePermissionUseCase, CreatePermissionUseCase>();
            services.AddScoped<IUpdatePermissionUseCase, UpdatePermissionUseCase>();
            services.AddScoped<IActivatePermissionUseCase, ActivatePermissionUseCase>();
            services.AddScoped<IDeactivatePermissionUseCase, DeactivatePermissionUseCase>();

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
}