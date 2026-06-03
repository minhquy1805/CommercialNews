using Audit.Application.Services.Normalization;

namespace Audit.Application.Abstractions.Normalization;

public interface IAuditEventNormalizer
{
    string SourceModule { get; }

    bool CanHandle(
        string eventType);

    AuditNormalizedEvent Normalize(
        AuditEventNormalizationContext context);
}