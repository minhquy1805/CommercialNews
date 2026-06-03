using Audit.Application.Abstractions.Normalization;

namespace Audit.Application.Services.Normalization;

public sealed class AuditEventNormalizerRegistry : IAuditEventNormalizerRegistry
{
    private readonly IReadOnlyList<IAuditEventNormalizer> _normalizers;

    public AuditEventNormalizerRegistry(
        IEnumerable<IAuditEventNormalizer> normalizers)
    {
        ArgumentNullException.ThrowIfNull(normalizers);

        _normalizers = normalizers.ToArray();
    }

    public IAuditEventNormalizer? Resolve(string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            return null;
        }

        return _normalizers.FirstOrDefault(
            normalizer => normalizer.CanHandle(eventType));
    }
}