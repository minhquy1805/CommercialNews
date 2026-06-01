using Audit.Domain.Constants.Common;
using Audit.Domain.Exceptions;

namespace Audit.Domain.ValueObjects.Evidence;

public sealed record AuditRequestContext
{
    public string? IpAddress { get; }
    public string? UserAgent { get; }

    private AuditRequestContext(
        string? ipAddress,
        string? userAgent)
    {
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    public static AuditRequestContext Create(
        string? ipAddress,
        string? userAgent)
    {
        var normalizedIpAddress = NormalizeOptional(ipAddress);
        if (normalizedIpAddress is not null &&
            normalizedIpAddress.Length > AuditConstants.MaxIpAddressLength)
        {
            throw AuditDomainException.IpAddressTooLong();
        }

        var normalizedUserAgent = NormalizeOptional(userAgent);
        if (normalizedUserAgent is not null &&
            normalizedUserAgent.Length > AuditConstants.MaxUserAgentLength)
        {
            throw AuditDomainException.UserAgentTooLong();
        }

        return new AuditRequestContext(
            normalizedIpAddress,
            normalizedUserAgent);
    }

    public static AuditRequestContext Empty()
    {
        return new AuditRequestContext(
            ipAddress: null,
            userAgent: null);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }
}