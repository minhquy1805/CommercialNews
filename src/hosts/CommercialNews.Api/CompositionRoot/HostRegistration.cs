using CommercialNews.Api.Api.Common.RequestContext;
using CommercialNews.Api.Health;
using CommercialNews.Api.OpenApi;
using CommercialNews.BuildingBlocks.Abstractions.Execution;

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

            services.AddHttpContextAccessor();
            services.AddScoped<IRequestContext, HttpRequestContext>();
            
            services.AddRouting();
            services.AddControllers();

            services.AddHostHealthChecks();
            services.AddHostOpenApi();

            return services;
        }
    }
}