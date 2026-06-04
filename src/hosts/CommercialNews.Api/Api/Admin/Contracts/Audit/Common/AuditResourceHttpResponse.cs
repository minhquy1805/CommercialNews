namespace CommercialNews.Api.Api.Admin.Contracts.Audit.Common;

public sealed class AuditResourceHttpResponse
{
    public string Type { get; init; } = string.Empty;

    public string Id { get; init; } = string.Empty;

    public string? DisplayName { get; init; }
}
