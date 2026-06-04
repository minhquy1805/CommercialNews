using Audit.Domain.Constants.Common;
using Audit.Domain.Exceptions;

namespace Audit.Domain.ValueObjects.Common;

public sealed record AuditAggregateRef
{
    public string? AggregateType { get; }
    public string? AggregateId { get; }
    public string? AggregatePublicId { get; }
    public int? AggregateVersion { get; }

    private AuditAggregateRef(
        string? aggregateType,
        string? aggregateId,
        string? aggregatePublicId,
        int? aggregateVersion)
    {
        AggregateType = aggregateType;
        AggregateId = aggregateId;
        AggregatePublicId = aggregatePublicId;
        AggregateVersion = aggregateVersion;
    }

    public static AuditAggregateRef Create(
        string? aggregateType,
        string? aggregateId,
        string? aggregatePublicId,
        int? aggregateVersion)
    {
        var normalizedAggregateType = NormalizeOptional(aggregateType);
        if (normalizedAggregateType is not null &&
            normalizedAggregateType.Length > AuditConstants.MaxAggregateTypeLength)
        {
            throw AuditDomainException.AggregateTypeTooLong();
        }

        var normalizedAggregateId = NormalizeOptional(aggregateId);
        if (normalizedAggregateId is not null &&
            normalizedAggregateId.Length > AuditConstants.MaxAggregateIdLength)
        {
            throw AuditDomainException.AggregateIdTooLong();
        }

        var normalizedAggregatePublicId = NormalizeOptional(aggregatePublicId);
        if (normalizedAggregatePublicId is not null &&
            normalizedAggregatePublicId.Length != AuditConstants.PublicIdLength)
        {
            throw AuditDomainException.AggregatePublicIdInvalidLength();
        }

        if (aggregateVersion is not null &&
            aggregateVersion < AuditConstants.MinVersion)
        {
            throw AuditDomainException.AggregateVersionInvalid();
        }

        return new AuditAggregateRef(
            normalizedAggregateType,
            normalizedAggregateId,
            normalizedAggregatePublicId,
            aggregateVersion);
    }

    public static AuditAggregateRef Empty()
    {
        return new AuditAggregateRef(
            aggregateType: null,
            aggregateId: null,
            aggregatePublicId: null,
            aggregateVersion: null);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }
}