using System.Security.Claims;
using System.Text;
using CommercialNews.Api.Api.Common.RequestContext;
using CommercialNews.Api.Authorization;
using CommercialNews.Api.Health;
using CommercialNews.Api.OpenApi;
using CommercialNews.BuildingBlocks.DependencyInjection;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace CommercialNews.Api.CompositionRoot
{
    public static class HostRegistration
    {
        public static IServiceCollection AddHostServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddBuildingBlocks(configuration);

            services.Configure<JwtSettings>(
                configuration.GetSection(JwtSettings.SectionName));

            var jwtSettings = configuration
                .GetSection(JwtSettings.SectionName)
                .Get<JwtSettings>()
                ?? throw new InvalidOperationException("JWT settings are missing.");

            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = false;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.Issuer,

                        ValidateAudience = true,
                        ValidAudience = jwtSettings.Audience,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingKey,

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1),

                        NameClaimType = ClaimTypes.NameIdentifier,
                        RoleClaimType = ClaimTypes.Role
                    };
                });

            services.AddApiAuthorizationPolicies();

            services.AddHttpContextAccessor();
            services.AddScoped<IRequestContext, HttpRequestContext>();

            services.AddRouting();

            services.AddCors(options =>
            {
                options.AddPolicy("FrontendCors", policy =>
                {
                    policy
                        .WithOrigins("http://localhost:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            services.AddControllers();

            services.AddHostHealthChecks();
            services.AddHostOpenApi();

            return services;
        }
    }
}