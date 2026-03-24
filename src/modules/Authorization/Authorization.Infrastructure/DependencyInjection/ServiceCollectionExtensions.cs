using Authorization.Application.Contracts.Ports;
using Authorization.Application.UseCases.AssignRoleToUser;
using Authorization.Application.UseCases.GrantPermissionToRole;
using Authorization.Application.UseCases.RevokePermissionFromRole;
using Authorization.Application.UseCases.RevokeRoleFromUser;
using Authorization.Infrastructure.Persistence.Sql;
using Authorization.Infrastructure.Persistence.Sql.Repositories;
using Authorization.Infrastructure.Requesting;
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

            return services;
        }
    }
}