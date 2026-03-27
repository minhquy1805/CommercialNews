using CommercialNews.Api.Api.Common.RequestContext;
using CommercialNews.Api.Health;
using CommercialNews.Api.OpenApi;
using CommercialNews.BuildingBlocks.Abstractions.Execution;
using CommercialNews.BuildingBlocks.Abstractions.Identifiers;
using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.DependencyInjection;
using CommercialNews.BuildingBlocks.Identifiers;
using CommercialNews.BuildingBlocks.Time;

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

            services.AddBuildingBlocksSql(configuration);

            services.AddHttpContextAccessor();
            services.AddScoped<IRequestContext, HttpRequestContext>();

            services.AddSingleton<IPublicIdGenerator, UlidPublicIdGenerator>();
            services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

            services.AddRouting();
            services.AddControllers();

            services.AddHostHealthChecks();
            services.AddHostOpenApi();

            return services;
        }
    }
}