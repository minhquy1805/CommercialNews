namespace CommercialNews.Api.Api.Admin.Contracts.Audit.Metadata.Responses;

public sealed class GetAuditModulesHttpResponse
{
    public IReadOnlyList<AuditModuleHttpResponse> Items { get; init; } =
        Array.Empty<AuditModuleHttpResponse>();
}
