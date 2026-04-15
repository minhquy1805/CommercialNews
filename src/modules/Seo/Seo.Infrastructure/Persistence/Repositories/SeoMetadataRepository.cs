using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Microsoft.Data.SqlClient;
using Seo.Application.Models.QueryModels;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Entities;
using Seo.Infrastructure.Persistence.Exceptions;
using Seo.Infrastructure.Persistence.Sql;

namespace Seo.Infrastructure.Persistence.Repositories;

public sealed class SeoMetadataRepository : ISeoMetadataRepository
{
    private const string SeoMetadataInsertProc = "[seo].[Seo_SeoMetadata_Insert]";
    private const string SeoMetadataUpdateProc = "[seo].[Seo_SeoMetadata_Update]";
    private const string SeoMetadataSelectByIdProc = "[seo].[Seo_SeoMetadata_SelectById]";
    private const string SeoMetadataSelectByArticleIdProc = "[seo].[Seo_SeoMetadata_SelectByArticleId]";
    private const string SeoMetadataSelectMetadataByArticleIdProc = "[seo].[Seo_SelectMetadataByArticleId]";
    private const string SeoMetadataGetArticleSeoSettingsByArticleIdProc = "[seo].[Seo_SelectArticleSeoByArticleId]";
    private const string SeoMetadataSelectSkipAndTakeProc = "[seo].[Seo_SeoMetadata_SelectSkipAndTake]";

    private readonly SeoUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly SeoSqlExceptionTranslator _sqlExceptionTranslator;

    public SeoMetadataRepository(
        SeoUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        SeoSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<long> InsertAsync(
        SeoMetadata seoMetadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(seoMetadata);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(SeoMetadataInsertProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = seoMetadata.ArticleId },
                new SqlParameter("@CanonicalUrl", SqlDbType.NVarChar, 500) { Value = ToDbValue(seoMetadata.CanonicalUrl) },
                new SqlParameter("@MetaTitle", SqlDbType.NVarChar, 300) { Value = ToDbValue(seoMetadata.MetaTitle) },
                new SqlParameter("@MetaDescription", SqlDbType.NVarChar, 500) { Value = ToDbValue(seoMetadata.MetaDescription) },
                new SqlParameter("@OgTitle", SqlDbType.NVarChar, 300) { Value = ToDbValue(seoMetadata.OgTitle) },
                new SqlParameter("@OgDescription", SqlDbType.NVarChar, 500) { Value = ToDbValue(seoMetadata.OgDescription) },
                new SqlParameter("@OgImageUrl", SqlDbType.NVarChar, 800) { Value = ToDbValue(seoMetadata.OgImageUrl) },
                new SqlParameter("@TwitterTitle", SqlDbType.NVarChar, 300) { Value = ToDbValue(seoMetadata.TwitterTitle) },
                new SqlParameter("@TwitterDescription", SqlDbType.NVarChar, 500) { Value = ToDbValue(seoMetadata.TwitterDescription) },
                new SqlParameter("@TwitterImageUrl", SqlDbType.NVarChar, 800) { Value = ToDbValue(seoMetadata.TwitterImageUrl) },
                new SqlParameter("@UpdatedByUserId", SqlDbType.BigInt) { Value = ToDbValue(seoMetadata.UpdatedByUserId) }
            ]);

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Seo_SeoMetadata_Insert did not return a row.");
            }

            return reader.GetInt64(reader.GetOrdinal("SeoId"));
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<SeoMetadata?> GetByIdAsync(
        long seoId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(SeoMetadataSelectByIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@SeoId", SqlDbType.BigInt) { Value = seoId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapSeoMetadata(reader);
            }
        }
        finally
        {
            if (ownedConnection is not null)
            {
                await ownedConnection.DisposeAsync();
            }
        }
    }

    public async Task<SeoMetadata?> GetByArticleIdAsync(
        long articleId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(SeoMetadataSelectByArticleIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapSeoMetadata(reader);
            }
        }
        finally
        {
            if (ownedConnection is not null)
            {
                await ownedConnection.DisposeAsync();
            }
        }
    }

    public async Task<int> UpdateAsync(
        SeoMetadata seoMetadata,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(seoMetadata);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(SeoMetadataUpdateProc);

            SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@SeoId", SqlDbType.BigInt) { Value = seoMetadata.SeoId },
                new SqlParameter("@CanonicalUrl", SqlDbType.NVarChar, 500) { Value = ToDbValue(seoMetadata.CanonicalUrl) },
                new SqlParameter("@MetaTitle", SqlDbType.NVarChar, 300) { Value = ToDbValue(seoMetadata.MetaTitle) },
                new SqlParameter("@MetaDescription", SqlDbType.NVarChar, 500) { Value = ToDbValue(seoMetadata.MetaDescription) },
                new SqlParameter("@OgTitle", SqlDbType.NVarChar, 300) { Value = ToDbValue(seoMetadata.OgTitle) },
                new SqlParameter("@OgDescription", SqlDbType.NVarChar, 500) { Value = ToDbValue(seoMetadata.OgDescription) },
                new SqlParameter("@OgImageUrl", SqlDbType.NVarChar, 800) { Value = ToDbValue(seoMetadata.OgImageUrl) },
                new SqlParameter("@TwitterTitle", SqlDbType.NVarChar, 300) { Value = ToDbValue(seoMetadata.TwitterTitle) },
                new SqlParameter("@TwitterDescription", SqlDbType.NVarChar, 500) { Value = ToDbValue(seoMetadata.TwitterDescription) },
                new SqlParameter("@TwitterImageUrl", SqlDbType.NVarChar, 800) { Value = ToDbValue(seoMetadata.TwitterImageUrl) },
                new SqlParameter("@UpdatedByUserId", SqlDbType.BigInt) { Value = ToDbValue(seoMetadata.UpdatedByUserId) },
                new SqlParameter("@ExpectedVersion", SqlDbType.Int) { Value = expectedVersion },
                affectedRowsParameter
            ]);

            await command.ExecuteNonQueryAsync(cancellationToken);

            return affectedRowsParameter.Value is DBNull
                ? 0
                : Convert.ToInt32(affectedRowsParameter.Value);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<SeoMetadataResult?> SelectMetadataByArticleIdAsync(
        long articleId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(SeoMetadataSelectMetadataByArticleIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return new SeoMetadataResult
                {
                    ResourceType = reader.GetString(reader.GetOrdinal("ResourceType")),
                    ResourceId = reader.GetInt64(reader.GetOrdinal("ResourceId")),
                    Slug = GetNullableString(reader, "Slug"),
                    CanonicalUrl = GetNullableString(reader, "CanonicalUrl"),
                    MetaTitle = GetNullableString(reader, "MetaTitle"),
                    MetaDescription = GetNullableString(reader, "MetaDescription"),
                    OgTitle = GetNullableString(reader, "OgTitle"),
                    OgDescription = GetNullableString(reader, "OgDescription"),
                    OgImageUrl = GetNullableString(reader, "OgImageUrl"),
                    Version = reader.GetInt32(reader.GetOrdinal("Version"))
                };
            }
        }
        finally
        {
            if (ownedConnection is not null)
            {
                await ownedConnection.DisposeAsync();
            }
        }
    }

    public async Task<ArticleSeoSettingsResult?> GetArticleSeoSettingsByArticleIdAsync(
        long articleId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(SeoMetadataGetArticleSeoSettingsByArticleIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return new ArticleSeoSettingsResult
                {
                    ArticleId = reader.GetInt64(reader.GetOrdinal("ArticleId")),
                    Scope = GetNullableString(reader, "Scope"),
                    Slug = GetNullableString(reader, "Slug"),
                    CanonicalUrl = GetNullableString(reader, "CanonicalUrl"),
                    MetaTitle = GetNullableString(reader, "MetaTitle"),
                    MetaDescription = GetNullableString(reader, "MetaDescription"),
                    OgTitle = GetNullableString(reader, "OgTitle"),
                    OgDescription = GetNullableString(reader, "OgDescription"),
                    OgImageUrl = GetNullableString(reader, "OgImageUrl"),
                    TwitterTitle = GetNullableString(reader, "TwitterTitle"),
                    TwitterDescription = GetNullableString(reader, "TwitterDescription"),
                    TwitterImageUrl = GetNullableString(reader, "TwitterImageUrl"),
                    IsIndexable = GetNullableBoolean(reader, "IsIndexable"),
                    IsActive = GetNullableBoolean(reader, "IsActive"),
                    Version = reader.GetInt32(reader.GetOrdinal("Version"))
                };
            }
        }
        finally
        {
            if (ownedConnection is not null)
            {
                await ownedConnection.DisposeAsync();
            }
        }
    }

    public async Task<PagedQueryResult<SeoMetadataListResultItem>> SelectSkipAndTakeAsync(
        SeoMetadataListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(SeoMetadataSelectSkipAndTakeProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                int page = query.Page <= 0 ? 1 : query.Page;
                int pageSize = query.PageSize <= 0 ? 20 : query.PageSize;

                int skip = (page - 1) * pageSize;
                int take = pageSize;

                command.Parameters.AddRange(
                [
                    new SqlParameter("@Skip", SqlDbType.Int) { Value = skip },
                    new SqlParameter("@Take", SqlDbType.Int) { Value = take },
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = ToDbValue(query.ArticleId) },
                    new SqlParameter("@UpdatedByUserId", SqlDbType.BigInt) { Value = ToDbValue(query.UpdatedByUserId) },
                    new SqlParameter("@Keyword", SqlDbType.NVarChar, 300) { Value = ToDbValue(query.Keyword) },
                    new SqlParameter("@SortBy", SqlDbType.NVarChar, 30) { Value = query.SortBy },
                    new SqlParameter("@SortDirection", SqlDbType.NVarChar, 4) { Value = query.SortDirection }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<SeoMetadataListResultItem> items = [];
                int totalItems = 0;

                while (await reader.ReadAsync(cancellationToken))
                {
                    if (totalItems == 0)
                    {
                        totalItems = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                    }

                    items.Add(new SeoMetadataListResultItem
                    {
                        SeoId = reader.GetInt64(reader.GetOrdinal("SeoId")),
                        ArticleId = reader.GetInt64(reader.GetOrdinal("ArticleId")),
                        CanonicalUrl = GetNullableString(reader, "CanonicalUrl"),
                        MetaTitle = GetNullableString(reader, "MetaTitle"),
                        MetaDescription = GetNullableString(reader, "MetaDescription"),
                        OgTitle = GetNullableString(reader, "OgTitle"),
                        OgDescription = GetNullableString(reader, "OgDescription"),
                        OgImageUrl = GetNullableString(reader, "OgImageUrl"),
                        TwitterTitle = GetNullableString(reader, "TwitterTitle"),
                        TwitterDescription = GetNullableString(reader, "TwitterDescription"),
                        TwitterImageUrl = GetNullableString(reader, "TwitterImageUrl"),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        UpdatedByUserId = GetNullableInt64(reader, "UpdatedByUserId"),
                        Version = reader.GetInt32(reader.GetOrdinal("Version"))
                    });
                }

                return new PagedQueryResult<SeoMetadataListResultItem>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalItems
                };
            }
        }
        finally
        {
            if (ownedConnection is not null)
            {
                await ownedConnection.DisposeAsync();
            }
        }
    }

    private SqlCommand CreateTransactionalCommand(string storedProcedureName)
    {
        SqlCommand command = _unitOfWork.Connection.CreateCommand();
        command.Transaction = _unitOfWork.Transaction;
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;
        return command;
    }

    private async Task<(SqlCommand Command, SqlConnection? OwnedConnection)> CreateReadCommandAsync(
        string storedProcedureName,
        CancellationToken cancellationToken)
    {
        if (_unitOfWork.HasActiveConnection)
        {
            SqlCommand ambientCommand = _unitOfWork.Connection.CreateCommand();
            ambientCommand.Transaction = _unitOfWork.HasActiveTransaction
                ? _unitOfWork.Transaction
                : null;
            ambientCommand.CommandText = storedProcedureName;
            ambientCommand.CommandType = CommandType.StoredProcedure;

            return (ambientCommand, null);
        }

        SqlConnection ownedConnection = _sqlConnectionFactory.CreateConnection();
        await ownedConnection.OpenAsync(cancellationToken);

        SqlCommand command = ownedConnection.CreateCommand();
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        return (command, ownedConnection);
    }

    private static SeoMetadata MapSeoMetadata(SqlDataReader reader)
    {
        return SeoMetadata.Rehydrate(
            seoId: reader.GetInt64(reader.GetOrdinal("SeoId")),
            articleId: reader.GetInt64(reader.GetOrdinal("ArticleId")),
            canonicalUrl: GetNullableString(reader, "CanonicalUrl"),
            metaTitle: GetNullableString(reader, "MetaTitle"),
            metaDescription: GetNullableString(reader, "MetaDescription"),
            ogTitle: GetNullableString(reader, "OgTitle"),
            ogDescription: GetNullableString(reader, "OgDescription"),
            ogImageUrl: GetNullableString(reader, "OgImageUrl"),
            twitterTitle: GetNullableString(reader, "TwitterTitle"),
            twitterDescription: GetNullableString(reader, "TwitterDescription"),
            twitterImageUrl: GetNullableString(reader, "TwitterImageUrl"),
            version: reader.GetInt32(reader.GetOrdinal("Version")),
            updatedAt: reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            updatedByUserId: GetNullableInt64(reader, "UpdatedByUserId"));
    }

    private static object ToDbValue(object? value) => value ?? DBNull.Value;

    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static long? GetNullableInt64(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }

    private static bool? GetNullableBoolean(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
    }
}