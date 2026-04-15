
using CommercialNews.Api.Api.Common.Headers;

namespace CommercialNews.Api.Api.Middleware
{
    public sealed class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = GetOrCreateCorrelationId(context);

            context.Items[HeaderNames.CorrelationId] = correlationId;
            context.Response.Headers[HeaderNames.CorrelationId] = correlationId;

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["TraceId"] = context.TraceIdentifier
            }))
            {
                await _next(context);
            }
        }

        private static string GetOrCreateCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(HeaderNames.CorrelationId, out var existingValue))
            {
                var candidate = existingValue.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    return candidate;
                }
            }

            return Guid.NewGuid().ToString("N");
        }
    }
}