using Microsoft.Data.SqlClient;

namespace Audit.Infrastructure.Persistence.Sql;

internal static class SqlParameterExtensions
{
    public static long GetRequiredInt64Value(
        this SqlParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        if (parameter.Value is null or DBNull)
        {
            throw new InvalidOperationException(
                $"Required SQL parameter '{parameter.ParameterName}' is null.");
        }

        return Convert.ToInt64(parameter.Value);
    }

    public static bool GetRequiredBooleanValue(
        this SqlParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        if (parameter.Value is null or DBNull)
        {
            throw new InvalidOperationException(
                $"Required SQL parameter '{parameter.ParameterName}' is null.");
        }

        return Convert.ToBoolean(parameter.Value);
    }

    public static string GetRequiredStringValue(
        this SqlParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        if (parameter.Value is null or DBNull)
        {
            throw new InvalidOperationException(
                $"Required SQL parameter '{parameter.ParameterName}' is null.");
        }

        return Convert.ToString(parameter.Value)
            ?? throw new InvalidOperationException(
                $"Required SQL parameter '{parameter.ParameterName}' cannot be converted to string.");
    }
}
