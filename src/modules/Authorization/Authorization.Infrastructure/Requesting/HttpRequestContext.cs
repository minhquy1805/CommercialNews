using System.Security.Claims;
using Authorization.Application.Contracts.Ports;
using Microsoft.AspNetCore.Http;

namespace Authorization.Infrastructure.Requesting
{
    public sealed class HttpRequestContext : IRequestContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpRequestContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public long? CurrentUserId
        {
            get
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext is null)
                {
                    return null;
                }

                var claimValue = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (long.TryParse(claimValue, out var claimUserId))
                {
                    return claimUserId;
                }

                var headerValue = httpContext.Request.Headers["X-User-Id"].ToString();
                if (long.TryParse(headerValue, out var headerUserId))
                {
                    return headerUserId;
                }

                return null;
            }
        }

        public string? IpAddress
            => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        public string? UserAgent
            => _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();

        public string? CorrelationId
            => _httpContextAccessor.HttpContext?.TraceIdentifier;
    }
}