namespace CommercialNews.Api.Api.Admin.Contracts.Audit.Metadata.Responses;

public sealed class AuditModuleHttpResponse
{
    public string SourceModule { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}
