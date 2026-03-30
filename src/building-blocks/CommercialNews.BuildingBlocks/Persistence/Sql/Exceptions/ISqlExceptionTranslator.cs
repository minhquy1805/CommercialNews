using Microsoft.Data.SqlClient;

namespace CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions
{
    public interface ISqlExceptionTranslator
    {
        Exception Translate(SqlException exception);
    }
}