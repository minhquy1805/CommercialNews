namespace Audit.Application.Models.Results.Metadata;

public sealed record AuditModuleActionsResult(
    string SourceModule,
    IReadOnlyList<string> Actions);
