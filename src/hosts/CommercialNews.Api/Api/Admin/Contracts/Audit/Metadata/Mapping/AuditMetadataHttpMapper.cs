using Audit.Application.Models.Results.Metadata;
using CommercialNews.Api.Api.Admin.Contracts.Audit.Metadata.Responses;

namespace CommercialNews.Api.Api.Admin.Contracts.Audit.Metadata.Mapping;

internal static class AuditMetadataHttpMapper
{
    public static GetAuditModulesHttpResponse ToModules(
        IReadOnlyList<AuditModuleResult> result)
    {
        return new GetAuditModulesHttpResponse
        {
            Items = result
                .Select(static item => new AuditModuleHttpResponse
                {
                    SourceModule = item.SourceModule,
                    Description = item.Description
                })
                .ToArray()
        };
    }

    public static GetAuditModuleActionsHttpResponse ToActions(
        AuditModuleActionsResult result)
    {
        return new GetAuditModuleActionsHttpResponse
        {
            SourceModule = result.SourceModule,
            Items = result.Actions
        };
    }
}
