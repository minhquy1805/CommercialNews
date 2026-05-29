using CommercialNews.Api.Api.Common.Headers;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;

namespace CommercialNews.Api.Api.Middleware
{
    public sealed class CorrelationIdMiddleware
    {
        private const int MaxCorrelationIdLength = 26;

        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;
        private readonly IPublicIdGenerator _publicIdGenerator;

        public CorrelationIdMiddleware(
            RequestDelegate next,
            ILogger<CorrelationIdMiddleware> logger,
            IPublicIdGenerator publicIdGenerator)
        {
            _next = next;
            _logger = logger;
            _publicIdGenerator = publicIdGenerator
                ?? throw new ArgumentNullException(nameof(publicIdGenerator));
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

        private string GetOrCreateCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(HeaderNames.CorrelationId, out var existingValue))
            {
                var candidate = existingValue.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(candidate) &&
                    candidate.Length <= MaxCorrelationIdLength)
                {
                    return candidate;
                }
            }

            return _publicIdGenerator.NewId();
        }
    }
}
