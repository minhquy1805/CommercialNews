using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace CommercialNews.Api.Health
{
    public static class HealthEndpointRegistration
    {
        public static IServiceCollection AddHostHealthChecks(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddHealthChecks()
                .AddCheck("self-live", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { HealthTags.Live })
                .AddCheck("self-ready", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { HealthTags.Ready });

            return services;
        }

        public static IEndpointRouteBuilder MapHostHealthEndpoints(this IEndpointRouteBuilder endpoints)
        {
            ArgumentNullException.ThrowIfNull(endpoints);

            endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains(HealthTags.Live)
            });

            endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains(HealthTags.Ready)
            });

            return endpoints;
        }
    }   
}