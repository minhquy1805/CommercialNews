using Authorization.Application.Ports.Persistence;
using Authorization.Application.Ports.Services;
using Authorization.Infrastructure.Configuration;
using Authorization.Infrastructure.Outbox;
using Authorization.Infrastructure.Persistence.Exceptions;
using Authorization.Infrastructure.Persistence.Repositories;
using Authorization.Infrastructure.Persistence.Sql;
using Authorization.Infrastructure.Seeding;
using Authorization.Infrastructure.Services;
using CommercialNews.BuildingBlocks.Outbox.Runtime;
using CommercialNews.BuildingBlocks.Persistence.Sql.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Authorization.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthorizationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<DefaultAdminSettings>(
            configuration.GetSection(DefaultAdminSettings.SectionName));

        services.AddSingleton<AuthorizationSqlExceptionTranslator>();

        services.AddScoped<AuthorizationUnitOfWork>();
        services.AddScoped<IAuthorizationUnitOfWork>(sp =>
            sp.GetRequiredService<AuthorizationUnitOfWork>());

        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<IAuthorizationPermissionQueryRepository, AuthorizationPermissionQueryRepository>();

        services.AddScoped<IAuthorizationOutboxWriter, AuthorizationOutboxWriter>();
        services.AddScoped<IOutboxDispatcher, AuthorizationOutboxDispatcher>();

        services.AddScoped<IAuthorizationUserLookupService, AuthorizationUserLookupService>();

        services.AddScoped<RoleSeederService>();
        services.AddScoped<PermissionSeederService>();
        services.AddScoped<RolePermissionSeederService>();
        services.AddScoped<UserRoleSeederService>();

        services.AddScoped<IAuthorizationDataInitializer, AuthorizationDataInitializer>();

        return services;
    }
}