namespace Audit.Infrastructure.Persistence.Sql;

internal static class SqlScalarValue
{
    public static int ToInt32OrDefault(
        object? value)
    {
        return value is null or DBNull
            ? 0
            : Convert.ToInt32(value);
    }

    public static int? ToNullableInt32(
        object? value)
    {
        return value is null or DBNull
            ? null
            : Convert.ToInt32(value);
    }
}
