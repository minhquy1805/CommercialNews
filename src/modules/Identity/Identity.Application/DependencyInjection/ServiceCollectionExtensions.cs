using Identity.Application.Configuration;
using Identity.Application.UseCases.ChangePassword;
using Identity.Application.UseCases.ForgotPassword;
using Identity.Application.UseCases.GetMyProfile;
using Identity.Application.UseCases.LoginUser;
using Identity.Application.UseCases.Logout;
using Identity.Application.UseCases.LogoutAllSessions;
using Identity.Application.UseCases.RefreshToken;
using Identity.Application.UseCases.RegisterUser;
using Identity.Application.UseCases.ResendVerificationEmail;
using Identity.Application.UseCases.ResetPassword;
using Identity.Application.UseCases.UpdateMyProfile;
using Identity.Application.UseCases.VerifyEmail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<IdentityTokenOptions>(
            configuration.GetSection(IdentityTokenOptions.SectionName));

        services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
        services.AddScoped<IVerifyEmailUseCase, VerifyEmailUseCase>();
        services.AddScoped<ILoginUserUseCase, LoginUserUseCase>();
        services.AddScoped<IRefreshTokenUseCase, RefreshTokenUseCase>();
        services.AddScoped<ILogoutUseCase, LogoutUseCase>();
        services.AddScoped<ILogoutAllSessionsUseCase, LogoutAllSessionsUseCase>();
        services.AddScoped<IForgotPasswordUseCase, ForgotPasswordUseCase>();
        services.AddScoped<IResetPasswordUseCase, ResetPasswordUseCase>();
        services.AddScoped<IChangePasswordUseCase, ChangePasswordUseCase>();
        services.AddScoped<IGetMyProfileUseCase, GetMyProfileUseCase>();
        services.AddScoped<IUpdateMyProfileUseCase, UpdateMyProfileUseCase>();
        services.AddScoped<IResendVerificationEmailUseCase, ResendVerificationEmailUseCase>();

        return services;
    }
}