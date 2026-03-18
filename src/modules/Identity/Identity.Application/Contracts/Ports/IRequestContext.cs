namespace Identity.Application.Contracts.Ports
{
    public interface IRequestContext
    {
        string? IpAddress { get; }
        string? UserAgent { get; }
        string? CorrelationId { get; }
    }
}
