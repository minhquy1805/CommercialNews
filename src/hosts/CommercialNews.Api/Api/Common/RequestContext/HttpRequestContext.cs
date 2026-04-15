using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using System.Security.Claims;

namespace CommercialNews.Api.Api.Common.RequestContext
{
    public sealed class HttpRequestContext : IRequestContext
    {
        private const string UserIdHeaderName = "X-User-Id";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpRequestContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor
                ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public long? CurrentUserId
        {
            get
            {
                HttpContext? httpContext = _httpContextAccessor.HttpContext;
                if (httpContext is null)
                {
                    return null;
                }

                string? claimValue =
                    httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.User?.FindFirst("sub")?.Value;

                if (long.TryParse(claimValue, out long claimUserId))
                {
                    return claimUserId;
                }

                string? headerValue = httpContext.Request.Headers[UserIdHeaderName].ToString();

                return long.TryParse(headerValue, out long headerUserId)
                    ? headerUserId
                    : null;
            }
        }

        public string? IpAddress =>
            _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

        public string? UserAgent =>
            _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString();

        public string? CorrelationId =>
            _httpContextAccessor.HttpContext?.TraceIdentifier;
    }
}