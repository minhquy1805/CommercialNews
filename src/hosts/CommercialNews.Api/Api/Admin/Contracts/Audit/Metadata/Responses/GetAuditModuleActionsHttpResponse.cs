namespace CommercialNews.Api.Api.Admin.Contracts.Audit.Metadata.Responses;

public sealed class GetAuditModuleActionsHttpResponse
{
    public string SourceModule { get; init; } = string.Empty;

    public IReadOnlyList<string> Items { get; init; } = Array.Empty<string>();
}
