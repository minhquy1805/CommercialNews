using Audit.Application.Services.Normalization;

namespace Audit.Application.Abstractions.Normalization;

public interface IAuditEventNormalizer
{
    bool CanHandle(string eventType);

    AuditNormalizedEvent Normalize(
        AuditEventNormalizationContext context);
}