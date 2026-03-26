using Microsoft.OpenApi.Models;

namespace CommercialNews.Api.OpenApi
{
    public static class OpenApiRegistration
    {
        public static IServiceCollection AddHostOpenApi(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "CommercialNews API",
                    Version = "v1",
                    Description = "CommercialNews HTTP API for public and admin endpoints."
                });

                var xmlFile = $"{typeof(OpenApiRegistration).Assembly.GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                }
            });

            return services;
        }

        public static WebApplication UseHostOpenApi(this WebApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);

            if (!app.Environment.IsDevelopment())
            {
                return app;
            }

            app.UseSwagger();

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "CommercialNews API v1");
                options.RoutePrefix = "swagger";
                options.DocumentTitle = "CommercialNews API Docs";
            });

            return app;
        }
    }
}

