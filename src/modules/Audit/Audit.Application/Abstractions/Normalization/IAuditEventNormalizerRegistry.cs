namespace Audit.Application.Abstractions.Normalization;

public interface IAuditEventNormalizerRegistry
{
    IAuditEventNormalizer? Resolve(string eventType);
}