namespace CommercialNews.Api.Api.Admin.Contracts.Audit.Common;

public sealed class AuditAggregateHttpResponse
{
    public string? Type { get; init; }

    public string? Id { get; init; }

    public string? PublicId { get; init; }

    public int? Version { get; init; }
}
