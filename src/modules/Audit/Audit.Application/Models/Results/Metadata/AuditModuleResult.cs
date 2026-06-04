namespace Audit.Application.Models.Results.Metadata;

public sealed record AuditModuleResult(
    string SourceModule,
    string Description);
