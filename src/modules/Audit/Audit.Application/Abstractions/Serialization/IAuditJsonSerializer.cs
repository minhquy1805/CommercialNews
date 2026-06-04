using System.Text.Json;

namespace Audit.Application.Abstractions.Serialization;

public interface IAuditJsonSerializer
{
    T? Deserialize<T>(
        string? json);

    string? Serialize<T>(
        T? value);

    JsonDocument? ParseDocument(
        string? json);
}