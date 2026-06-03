namespace Audit.Application.Services.Redaction;

public sealed record AuditRedactionInput(
    string SourceModule,
    string EventType,
    string? PayloadJson,
    string? HeadersJson,
    string? MetadataJson,
    string? BeforeJson,
    string? AfterJson,
    string? ChangesJson);