using Microsoft.Data.SqlClient;

namespace Audit.Infrastructure.Persistence.Sql;

internal static class SqlDataReaderExtensions
{
    public static string? GetNullableString(
        this SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetString(ordinal);
    }

    public static string GetRequiredString(
        this SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        if (reader.IsDBNull(ordinal))
        {
            throw new InvalidOperationException(
                $"Required column '{columnName}' is null.");
        }

        return reader.GetString(ordinal);
    }

    public static int? GetNullableInt32(
        this SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetInt32(ordinal);
    }

    public static int? GetNullableInt32FromNumber(
        this SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : Convert.ToInt32(reader.GetValue(ordinal));
    }

    public static int GetRequiredInt32(
        this SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        if (reader.IsDBNull(ordinal))
        {
            throw new InvalidOperationException(
                $"Required column '{columnName}' is null.");
        }

        return reader.GetInt32(ordinal);
    }

    public static int GetRequiredInt32FromNumber(
        this SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        if (reader.IsDBNull(ordinal))
        {
            throw new InvalidOperationException(
                $"Required column '{columnName}' is null.");
        }

        return Convert.ToInt32(reader.GetValue(ordinal));
    }

    public static long? GetNullableInt64(
        this SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetInt64(ordinal);
    }

    public static long GetRequiredInt64(
        this SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        if (reader.IsDBNull(ordinal))
        {
            throw new InvalidOperationException(
                $"Required column '{columnName}' is null.");
        }

        return reader.GetInt64(ordinal);
    }

    public static DateTime? GetNullableDateTime(
        this SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Utc);
    }

    public static DateTime GetRequiredDateTime(
        this SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        if (reader.IsDBNull(ordinal))
        {
            throw new InvalidOperationException(
                $"Required column '{columnName}' is null.");
        }

        return DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Utc);
    }

    public static bool GetRequiredBoolean(
        this SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        if (reader.IsDBNull(ordinal))
        {
            throw new InvalidOperationException(
                $"Required column '{columnName}' is null.");
        }

        return reader.GetBoolean(ordinal);
    }
}
