using System.Text.Json;
using Audit.Application.Abstractions.Serialization;

namespace Audit.Infrastructure.Serialization;

public sealed class AuditJsonSerializer : IAuditJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public T? Deserialize<T>(
        string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(
            json,
            Options);
    }

    public string? Serialize<T>(
        T? value)
    {
        if (value is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(
            value,
            Options);
    }

    public JsonDocument? ParseDocument(
        string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonDocument.Parse(json);
    }
}