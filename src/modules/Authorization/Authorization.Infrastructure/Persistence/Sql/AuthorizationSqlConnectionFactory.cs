using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Authorization.Infrastructure.Persistence.Sql
{
    public sealed class AuthorizationSqlConnectionFactory
    {
        private readonly string _connectionString;

        public AuthorizationSqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CommercialNews")
                ?? throw new InvalidOperationException("Connection string 'CommercialNews' was not found.");
        }

        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}