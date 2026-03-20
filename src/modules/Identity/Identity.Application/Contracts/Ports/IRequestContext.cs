namespace Identity.Application.Contracts.Ports
{
    public interface IRequestContext
    {
        long? CurrentUserId { get; }
        string? IpAddress { get; }
        string? UserAgent { get; }
        string? CorrelationId { get; }
    }
}
