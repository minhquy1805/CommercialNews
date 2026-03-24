using Authorization.Application.Contracts.Ports;
using Authorization.Application.UseCases.AssignRoleToUser;
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

            return services;
        }
    }
}