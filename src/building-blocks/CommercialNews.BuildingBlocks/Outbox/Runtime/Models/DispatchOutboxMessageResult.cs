namespace CommercialNews.BuildingBlocks.Outbox.Runtime.Models;

public sealed class DispatchOutboxMessageResult
{
    public bool Succeeded { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public bool ShouldMarkDead { get; init; }
}