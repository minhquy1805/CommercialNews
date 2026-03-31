using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Infrastructure.Persistence.Exceptions;
using Identity.Infrastructure.Persistence.Repositories;
using Identity.Infrastructure.Persistence.Sql;
using Identity.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddScoped<IdentityUnitOfWork>();
            services.AddScoped<IIdentityUnitOfWork>(sp => sp.GetRequiredService<IdentityUnitOfWork>());

            services.AddSingleton<IdentitySqlExceptionTranslator>();

            services.AddScoped<IUserAccountRepository, UserAccountRepository>();
            services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
            services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<ILoginHistoryRepository, LoginHistoryRepository>();

            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<IRawTokenGenerator, RawTokenGenerator>();
            services.AddSingleton<ITokenHashProvider, TokenHashProvider>();
            services.AddSingleton<IAccessTokenGenerator, AccessTokenGenerator>();

            return services;
        }
    }
}