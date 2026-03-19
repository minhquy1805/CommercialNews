using Microsoft.Data.SqlClient;

namespace CommercialNews.Worker.Messaging.Outbox.Sql
{
   public sealed class WorkerSqlConnectionFactory
    {
        private readonly string _connectionString;

        public WorkerSqlConnectionFactory(IConfiguration configuration)
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