using CommercialNews.Api.Api.Middleware;
using CommercialNews.Api.Health;

namespace CommercialNews.Api.CompositionRoot
{
    public static class PipelineRegistration
    {
        public static WebApplication UseHostPipeline(this WebApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);

            app.UseApiExceptionHandling();
            app.UseApiCorrelationId();
            app.UseApiRequestLogging();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHostHealthEndpoints();

            return app;
        }
    }
}