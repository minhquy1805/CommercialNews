using Authorization.Application.Ports.Persistence;
using Authorization.Application.Ports.Services;
using Authorization.Infrastructure.Persistence.Exceptions;
using Authorization.Infrastructure.Persistence.Repositories;
using Authorization.Infrastructure.Persistence.Sql;
using Authorization.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Authorization.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthorizationInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<AuthorizationSqlExceptionTranslator>();

        services.AddScoped<AuthorizationUnitOfWork>();
        services.AddScoped<IAuthorizationUnitOfWork>(sp =>
            sp.GetRequiredService<AuthorizationUnitOfWork>());

        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<IAuthorizationPermissionQueryRepository, AuthorizationPermissionQueryRepository>();

        services.AddScoped<IAuthorizationUserLookupService, AuthorizationUserLookupService>();

        return services;
    }
}