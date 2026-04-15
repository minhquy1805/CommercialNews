using CommercialNews.BuildingBlocks.Persistence.Sql.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace CommercialNews.BuildingBlocks.Persistence.Sql.Connections;

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IOptions<SqlOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _connectionString = options.Value.ConnectionString;

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Sql:ConnectionString is not configured.");
        }
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}