using Authorization.Application.Contracts.Ports;
using Authorization.Application.UseCases.AssignRoleToUser;
using Authorization.Application.UseCases.CheckUserHasPermission;
using Authorization.Application.UseCases.CreatePermission;
using Authorization.Application.UseCases.CreateRole;
using Authorization.Application.UseCases.DeactivatePermission;
using Authorization.Application.UseCases.DeactivateRole;
using Authorization.Application.UseCases.GetPermissionRoles;
using Authorization.Application.UseCases.GetRolePermissions;
using Authorization.Application.UseCases.GetRoleUsers;
using Authorization.Application.UseCases.GetUserEffectivePermissions;
using Authorization.Application.UseCases.GetUserRoles;
using Authorization.Application.UseCases.GrantPermissionToRole;
using Authorization.Application.UseCases.RevokePermissionFromRole;
using Authorization.Application.UseCases.RevokeRoleFromUser;
using Authorization.Application.UseCases.UpdatePermission;
using Authorization.Application.UseCases.UpdateRole;
using Authorization.Infrastructure.Persistence.Sql;
using Authorization.Infrastructure.Persistence.Sql.Repositories;
using Authorization.Infrastructure.Requesting;
using Authorization.Infrastructure.Security;
using Authorization.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Authorization.Infrastructure.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthorizationInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<AuthorizationSqlConnectionFactory>();
            services.AddScoped<AuthorizationUnitOfWork>();

            services.AddScoped<IAuthorizationUnitOfWork>(sp =>
                sp.GetRequiredService<AuthorizationUnitOfWork>());

            services.AddHttpContextAccessor();

            services.AddScoped<IRequestContext, HttpRequestContext>();

            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();

            services.AddScoped<IAuthorizationUserLookupService, AuthorizationUserLookupService>();

            services.AddScoped<IAssignRoleToUserUseCase, AssignRoleToUserUseCase>();

            services.AddScoped<IRevokeRoleFromUserUseCase, RevokeRoleFromUserUseCase>();
            services.AddScoped<IGrantPermissionToRoleUseCase, GrantPermissionToRoleUseCase>();
            services.AddScoped<IRevokePermissionFromRoleUseCase, RevokePermissionFromRoleUseCase>();

            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();

            services.AddScoped<ICreateRoleUseCase, CreateRoleUseCase>();
            services.AddScoped<ICreatePermissionUseCase, CreatePermissionUseCase>();

            services.AddSingleton<IPublicIdGenerator, AuthorizationPublicIdGenerator>();

            services.AddScoped<IUpdateRoleUseCase, UpdateRoleUseCase>();
            services.AddScoped<IUpdatePermissionUseCase, UpdatePermissionUseCase>();

            services.AddScoped<IDeactivateRoleUseCase, DeactivateRoleUseCase>();
            services.AddScoped<IDeactivatePermissionUseCase, DeactivatePermissionUseCase>();

            services.AddScoped<IGetUserRolesUseCase, GetUserRolesUseCase>();
            services.AddScoped<IGetRoleUsersUseCase, GetRoleUsersUseCase>();

            services.AddScoped<IGetRolePermissionsUseCase, GetRolePermissionsUseCase>();
            services.AddScoped<IGetPermissionRolesUseCase, GetPermissionRolesUseCase>();

            services.AddScoped<IAuthorizationPermissionQueryRepository, AuthorizationPermissionQueryRepository>();
            services.AddScoped<IGetUserEffectivePermissionsUseCase, GetUserEffectivePermissionsUseCase>();
            services.AddScoped<ICheckUserHasPermissionUseCase, CheckUserHasPermissionUseCase>();

            return services;
        }
    }
}