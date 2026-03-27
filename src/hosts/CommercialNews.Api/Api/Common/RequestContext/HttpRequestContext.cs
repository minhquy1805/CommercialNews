using CommercialNews.BuildingBlocks.Abstractions.Execution;

namespace CommercialNews.Api.Api.Common.RequestContext
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
                string? value = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

                return long.TryParse(value, out long userId) ? userId : null;
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