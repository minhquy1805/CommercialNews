using Identity.Application.Contracts.Ports;
using Microsoft.AspNetCore.Http;

namespace Identity.Infrastructure.Requesting
{
    public sealed class HttpRequestContext : IRequestContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpRequestContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? IpAddress
            => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        public string? UserAgent
            => _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();

        public string? CorrelationId
            => _httpContextAccessor.HttpContext?.TraceIdentifier;
    }
}
