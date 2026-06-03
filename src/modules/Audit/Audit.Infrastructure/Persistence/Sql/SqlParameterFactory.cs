using Microsoft.Data.SqlClient;
using System.Data;

namespace Audit.Infrastructure.Persistence.Sql;

internal static class SqlParameterFactory
{
    public static SqlParameter Char(
        string name,
        string? value,
        int size)
    {
        return new SqlParameter(name, SqlDbType.Char, size)
        {
            Value = ToDbString(value)
        };
    }

    public static SqlParameter VarChar(
        string name,
        string? value,
        int size)
    {
        return new SqlParameter(name, SqlDbType.VarChar, size)
        {
            Value = ToDbString(value)
        };
    }

    public static SqlParameter NVarChar(
        string name,
        string? value,
        int size)
    {
        return new SqlParameter(name, SqlDbType.NVarChar, size)
        {
            Value = ToDbString(value)
        };
    }

    public static SqlParameter NVarCharMax(
        string name,
        string? value)
    {
        return new SqlParameter(name, SqlDbType.NVarChar, -1)
        {
            Value = ToDbString(value)
        };
    }

    public static SqlParameter Int(
        string name,
        int? value)
    {
        return new SqlParameter(name, SqlDbType.Int)
        {
            Value = value.HasValue
                ? value.Value
                : DBNull.Value
        };
    }

    public static SqlParameter TinyInt(
        string name,
        int? value)
    {
        return new SqlParameter(name, SqlDbType.TinyInt)
        {
            Value = value.HasValue
                ? EnsureTinyInt(value.Value, name)
                : DBNull.Value
        };
    }

    public static SqlParameter BigInt(
        string name,
        long? value)
    {
        return new SqlParameter(name, SqlDbType.BigInt)
        {
            Value = value.HasValue
                ? value.Value
                : DBNull.Value
        };
    }

    public static SqlParameter Bit(
        string name,
        bool? value)
    {
        return new SqlParameter(name, SqlDbType.Bit)
        {
            Value = value.HasValue
                ? value.Value
                : DBNull.Value
        };
    }

    public static SqlParameter DateTime2(
        string name,
        DateTime? value)
    {
        return new SqlParameter(name, SqlDbType.DateTime2)
        {
            Scale = 3,
            Value = value.HasValue
                ? EnsureUtc(value.Value, name)
                : DBNull.Value
        };
    }

    public static SqlParameter OutputInt(
        string name)
    {
        return new SqlParameter(name, SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
    }

    public static SqlParameter OutputBigInt(
        string name)
    {
        return new SqlParameter(name, SqlDbType.BigInt)
        {
            Direction = ParameterDirection.Output
        };
    }

    public static SqlParameter OutputBit(
        string name)
    {
        return new SqlParameter(name, SqlDbType.Bit)
        {
            Direction = ParameterDirection.Output
        };
    }

    public static SqlParameter OutputVarChar(
        string name,
        int size)
    {
        return new SqlParameter(name, SqlDbType.VarChar, size)
        {
            Direction = ParameterDirection.Output
        };
    }

    public static SqlParameter OutputNVarChar(
        string name,
        int size)
    {
        return new SqlParameter(name, SqlDbType.NVarChar, size)
        {
            Direction = ParameterDirection.Output
        };
    }

    private static object ToDbString(
        string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? DBNull.Value
            : normalized;
    }

    private static DateTime EnsureUtc(
        DateTime value,
        string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                $"DateTime parameter '{parameterName}' must be UTC.",
                parameterName);
        }

        return value;
    }

    private static byte EnsureTinyInt(
        int value,
        string parameterName)
    {
        if (value is < byte.MinValue or > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Integer parameter '{parameterName}' must fit SQL TINYINT.");
        }

        return (byte)value;
    }
}
