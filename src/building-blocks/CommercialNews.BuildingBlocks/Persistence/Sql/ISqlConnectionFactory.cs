using Microsoft.Data.SqlClient;

namespace CommercialNews.BuildingBlocks.Persistence.Sql
{
   public interface ISqlConnectionFactory
    {
        SqlConnection CreateConnection();
    }
}