using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using Microsoft.Data.SqlClient;
using Reading.Application.Models.Results;
using Reading.Application.Ports.Seo;
using Reading.Infrastructure.Persistence.Exceptions;

namespace Reading.Infrastructure.Seo;

public sealed class SeoRouteResolver : ISeoRouteResolver
{
    private const string ResolveRouteProc = "[seo].[Seo_ResolveByScopeAndSlug]";
    private const string PublicScope = "public";
    private const string ArticleResourceType = "Article";

    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ReadingSqlExceptionTranslator _sqlExceptionTranslator;

    public SeoRouteResolver(
        ISqlConnectionFactory sqlConnectionFactory,
        ReadingSqlExceptionTranslator sqlExceptionTranslator)
    {
        _sqlConnectionFactory = sqlConnectionFactory
            ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));

        _sqlExceptionTranslator = sqlExceptionTranslator
            ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<ResolvedSeoRouteResult?> ResolveArticleSlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        await using SqlConnection connection =
            _sqlConnectionFactory.CreateConnection();

        try
        {
            await connection.OpenAsync(cancellationToken);

            await using SqlCommand command = connection.CreateCommand();

            command.CommandText = ResolveRouteProc;
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddRange(
            [
                new SqlParameter("@Scope", SqlDbType.VarChar, 30)
                {
                    Value = PublicScope
                },
                new SqlParameter("@Slug", SqlDbType.NVarChar, 300)
                {
                    Value = slug.Trim()
                },
                new SqlParameter("@ResourceType", SqlDbType.VarChar, 50)
                {
                    Value = ArticleResourceType
                }
            ]);

            await using SqlDataReader reader =
                await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapResolvedRoute(reader);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    private static ResolvedSeoRouteResult MapResolvedRoute(
        SqlDataReader reader)
    {
        return new ResolvedSeoRouteResult
        {
            Scope = reader.GetString(reader.GetOrdinal("Scope")),
            Slug = reader.GetString(reader.GetOrdinal("Slug")),
            ResourceType = reader.GetString(reader.GetOrdinal("ResourceType")),
            ResourcePublicId = reader.GetString(reader.GetOrdinal("ResourcePublicId")),
            CanonicalUrl = GetNullableString(reader, "CanonicalUrl"),
            IsIndexable = GetBooleanOrDefault(reader, "IsIndexable"),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            Version = GetInt32OrDefault(reader, "Version")
        };
    }

    private static string? GetNullableString(
        SqlDataReader reader,
        string columnName)
    {
        int ordinal = TryGetOrdinal(reader, columnName);

        if (ordinal < 0)
        {
            return null;
        }

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetString(ordinal);
    }

    private static bool GetBooleanOrDefault(
        SqlDataReader reader,
        string columnName)
    {
        int ordinal = TryGetOrdinal(reader, columnName);

        if (ordinal < 0 || reader.IsDBNull(ordinal))
        {
            return false;
        }

        return reader.GetBoolean(ordinal);
    }

    private static int GetInt32OrDefault(
        SqlDataReader reader,
        string columnName)
    {
        int ordinal = TryGetOrdinal(reader, columnName);

        if (ordinal < 0 || reader.IsDBNull(ordinal))
        {
            return 0;
        }

        return reader.GetInt32(ordinal);
    }

    private static int TryGetOrdinal(
        SqlDataReader reader,
        string columnName)
    {
        for (int index = 0; index < reader.FieldCount; index++)
        {
            if (string.Equals(
                    reader.GetName(index),
                    columnName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }
}