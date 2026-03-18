using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Identity.Infrastructure.Persistence.Sql
{
    public sealed class IdentitySqlConnectionFactory
    {
        private readonly string _connectionString;

        public IdentitySqlConnectionFactory(IConfiguration configuration)
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
