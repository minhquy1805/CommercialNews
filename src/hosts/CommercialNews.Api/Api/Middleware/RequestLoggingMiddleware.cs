using System.Diagnostics;
using CommercialNews.BuildingBlocks.Contracts.Headers;

namespace CommercialNews.Api.Api.Middleware
{
    public sealed class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                var correlationId = GetCorrelationId(context);

                _logger.LogInformation(
                    "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms. TraceId={TraceId}, CorrelationId={CorrelationId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    context.TraceIdentifier,
                    correlationId);
            }
        }

        private static string GetCorrelationId(HttpContext context)
        {
            if (context.Items.TryGetValue(HeaderNames.CorrelationId, out var value) &&
                value is string correlationId &&
                !string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId;
            }

            return string.Empty;
        }
    }
}