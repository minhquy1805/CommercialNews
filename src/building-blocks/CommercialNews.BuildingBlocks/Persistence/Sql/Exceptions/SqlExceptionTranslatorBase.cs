using Microsoft.Data.SqlClient;

namespace CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;

public abstract class SqlExceptionTranslatorBase : ISqlExceptionTranslator
{
    public abstract Exception Translate(SqlException exception);
}