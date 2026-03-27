namespace CommercialNews.BuildingBlocks.Abstractions.Execution
{
    public interface IRequestContext
    {
        long? CurrentUserId { get; }
        string? IpAddress { get; }
        string? UserAgent { get; }
        string? CorrelationId { get; }
    }
}

