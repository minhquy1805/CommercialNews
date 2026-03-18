using Identity.Application.Contracts.Ports;
using Identity.Application.UseCases.ForgotPassword;
using Identity.Application.UseCases.LoginUser;
using Identity.Application.UseCases.RegisterUser;
using Identity.Application.UseCases.VerifyEmail;
using Identity.Infrastructure.Messaging;
using Identity.Infrastructure.Persistence.Sql;
using Identity.Infrastructure.Persistence.Sql.Repositories;
using Identity.Infrastructure.Requesting;
using Identity.Infrastructure.Security;
using Identity.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IdentitySqlConnectionFactory>();
            services.AddScoped<IdentityUnitOfWork>();

            services.AddScoped<IIdentityUnitOfWork>(sp => sp.GetRequiredService<IdentityUnitOfWork>());

            services.AddScoped<IUserAccountRepository, UserAccountRepository>();
            services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();

            services.AddScoped<IIdentityVerificationRepository, IdentityVerificationRepository>();
            services.AddScoped<IVerifyEmailUseCase, VerifyEmailUseCase>();

            services.AddHttpContextAccessor();

            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<ILoginHistoryRepository, LoginHistoryRepository>();
            services.AddScoped<IRequestContext, HttpRequestContext>();

            services.AddSingleton<IAccessTokenGenerator, AccessTokenGenerator>();

            services.AddScoped<ILoginUserUseCase, LoginUserUseCase>();

            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<IPublicIdGenerator, PublicIdGenerator>();
            services.AddSingleton<IRawTokenGenerator, RawTokenGenerator>();
            services.AddSingleton<ITokenHashProvider, TokenHashProvider>();
            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

            services.AddScoped<IIdentityEmailSender, IdentityEmailSender>();

            services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();

            services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
            services.AddScoped<IForgotPasswordUseCase, ForgotPasswordUseCase>();

            return services;
        }
    }
}
