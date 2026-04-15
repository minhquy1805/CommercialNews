using Microsoft.Data.SqlClient;

namespace CommercialNews.BuildingBlocks.Persistence.Sql.Connections;

public interface ISqlConnectionFactory
{
    SqlConnection CreateConnection();
}