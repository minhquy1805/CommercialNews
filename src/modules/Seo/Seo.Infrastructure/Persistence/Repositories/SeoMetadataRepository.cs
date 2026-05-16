using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Microsoft.Data.SqlClient;
using Seo.Application.Models.Commands;
using Seo.Application.Models.Queries;
using Seo.Application.Models.Results;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Entities;
using Seo.Infrastructure.Persistence.Exceptions;
using Seo.Infrastructure.Persistence.Sql;

namespace Seo.Infrastructure.Persistence.Repositories;

public sealed class SeoMetadataRepository : ISeoMetadataRepository
{
    private const string SeoMetadataUpsertProc = "[seo].[Seo_SeoMetadata_Upsert]";
    private const string SeoMetadataApplyContentDefaultsProc = "[seo].[Seo_SeoMetadata_ApplyContentDefaults]";
    private const string SeoMetadataSelectByIdProc = "[seo].[Seo_SeoMetadata_SelectById]";
    private const string SeoMetadataSelectByResourceProc = "[seo].[Seo_SeoMetadata_SelectByResource]";
    private const string SeoMetadataSelectMetadataByResourceProc = "[seo].[Seo_SelectMetadataByResource]";
    private const string SeoMetadataGetArticleSeoSettingsByArticlePublicIdProc = "[seo].[Seo_SelectArticleSeoByArticlePublicId]";
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
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
        finally
        {
            if (ownedConnection is not null)
            {
                await ownedConnection.DisposeAsync();
            }
        }
    }

    public async Task<SeoMetadata?> GetByResourceAsync(
        string scope,
        string resourceType,
        string resourcePublicId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(SeoMetadataSelectByResourceProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@Scope", SqlDbType.VarChar, 30) { Value = scope },
                    new SqlParameter("@ResourceType", SqlDbType.VarChar, 50) { Value = resourceType },
                    new SqlParameter("@ResourcePublicId", SqlDbType.Char, 26) { Value = resourcePublicId }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapSeoMetadata(reader);
            }
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
        finally
        {
            if (ownedConnection is not null)
            {
                await ownedConnection.DisposeAsync();
            }
        }
    }

    public async Task<SeoMetadata?> UpsertAsync(
        SeoMetadataUpsertCommand model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(SeoMetadataUpsertProc);

            SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@Scope", SqlDbType.VarChar, 30) { Value = model.Scope },
                new SqlParameter("@ResourceType", SqlDbType.VarChar, 50) { Value = model.ResourceType },
                new SqlParameter("@ResourcePublicId", SqlDbType.Char, 26) { Value = model.ResourcePublicId },
                new SqlParameter("@Slug", SqlDbType.NVarChar, 200) { Value = ToDbValue(model.Slug) },
                new SqlParameter("@CanonicalUrl", SqlDbType.NVarChar, 500) { Value = ToDbValue(model.CanonicalUrl) },
                new SqlParameter("@MetaTitle", SqlDbType.NVarChar, 300) { Value = ToDbValue(model.MetaTitle) },
                new SqlParameter("@MetaDescription", SqlDbType.NVarChar, 500) { Value = ToDbValue(model.MetaDescription) },
                new SqlParameter("@OgTitle", SqlDbType.NVarChar, 300) { Value = ToDbValue(model.OgTitle) },
                new SqlParameter("@OgDescription", SqlDbType.NVarChar, 500) { Value = ToDbValue(model.OgDescription) },
                new SqlParameter("@OgImageUrl", SqlDbType.NVarChar, 800) { Value = ToDbValue(model.OgImageUrl) },
                new SqlParameter("@TwitterTitle", SqlDbType.NVarChar, 300) { Value = ToDbValue(model.TwitterTitle) },
                new SqlParameter("@TwitterDescription", SqlDbType.NVarChar, 500) { Value = ToDbValue(model.TwitterDescription) },
                new SqlParameter("@TwitterImageUrl", SqlDbType.NVarChar, 800) { Value = ToDbValue(model.TwitterImageUrl) },
                new SqlParameter("@Robots", SqlDbType.NVarChar, 100) { Value = ToDbValue(model.Robots) },
                new SqlParameter("@IsManualOverride", SqlDbType.Bit) { Value = model.IsManualOverride },
                new SqlParameter("@UpdatedByUserId", SqlDbType.BigInt) { Value = ToDbValue(model.UpdatedByUserId) },
                new SqlParameter("@ExpectedVersion", SqlDbType.Int) { Value = ToDbValue(model.ExpectedVersion) },
                affectedRowsParameter
            ]);

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapSeoMetadata(reader);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<SeoApplyResultModel> ApplyContentDefaultsAsync(
        ApplyContentMetadataDefaultsCommand model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(SeoMetadataApplyContentDefaultsProc);

            SqlParameter applyResultParameter = new("@ApplyResult", SqlDbType.VarChar, 30)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@Scope", SqlDbType.VarChar, 30) { Value = model.Scope },
                new SqlParameter("@ResourceType", SqlDbType.VarChar, 50) { Value = model.ResourceType },
                new SqlParameter("@ResourcePublicId", SqlDbType.Char, 26) { Value = model.ResourcePublicId },
                new SqlParameter("@Slug", SqlDbType.NVarChar, 200) { Value = ToDbValue(model.Slug) },
                new SqlParameter("@CanonicalUrl", SqlDbType.NVarChar, 500) { Value = ToDbValue(model.CanonicalUrl) },
                new SqlParameter("@MetaTitle", SqlDbType.NVarChar, 300) { Value = ToDbValue(model.MetaTitle) },
                new SqlParameter("@MetaDescription", SqlDbType.NVarChar, 500) { Value = ToDbValue(model.MetaDescription) },
                new SqlParameter("@OgTitle", SqlDbType.NVarChar, 300) { Value = ToDbValue(model.OgTitle) },
                new SqlParameter("@OgDescription", SqlDbType.NVarChar, 500) { Value = ToDbValue(model.OgDescription) },
                new SqlParameter("@OgImageUrl", SqlDbType.NVarChar, 800) { Value = ToDbValue(model.OgImageUrl) },
                new SqlParameter("@SourceAggregateVersion", SqlDbType.BigInt) { Value = model.SourceAggregateVersion },
                new SqlParameter("@LastAppliedMessageId", SqlDbType.Char, 26) { Value = model.LastAppliedMessageId },
                new SqlParameter("@LastSyncedAtUtc", SqlDbType.DateTime2) { Value = ToDbValue(model.LastSyncedAtUtc) },
                applyResultParameter
            ]);

            SeoMetadata? metadata = null;

            using (SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                if (await reader.ReadAsync(cancellationToken))
                {
                    metadata = MapSeoMetadata(reader);
                }
            }

            string applyResult = applyResultParameter.Value is DBNull or null
                ? string.Empty
                : Convert.ToString(applyResultParameter.Value) ?? string.Empty;

            return new SeoApplyResultModel
            {
                ApplyResult = applyResult,
                EntityId = metadata?.SeoId,
                Version = metadata?.Version,
                SourceAggregateVersion = metadata?.SourceAggregateVersion,
                LastAppliedMessageId = metadata?.LastAppliedMessageId,
                LastSyncedAtUtc = metadata?.LastSyncedAtUtc
            };
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<SeoMetadataResult?> SelectMetadataByResourceAsync(
        string scope,
        string resourceType,
        string resourcePublicId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(SeoMetadataSelectMetadataByResourceProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@Scope", SqlDbType.VarChar, 30) { Value = scope },
                    new SqlParameter("@ResourceType", SqlDbType.VarChar, 50) { Value = resourceType },
                    new SqlParameter("@ResourcePublicId", SqlDbType.Char, 26) { Value = resourcePublicId }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapSeoMetadataResult(reader);
            }
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
        finally
        {
            if (ownedConnection is not null)
            {
                await ownedConnection.DisposeAsync();
            }
        }
    }

    public async Task<ArticleSeoSettingsResult?> GetArticleSeoSettingsByArticlePublicIdAsync(
        string articlePublicId,
        string scope,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(SeoMetadataGetArticleSeoSettingsByArticlePublicIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticlePublicId", SqlDbType.Char, 26) { Value = articlePublicId },
                    new SqlParameter("@Scope", SqlDbType.VarChar, 30) { Value = scope }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return new ArticleSeoSettingsResult
                {
                    Scope = reader.GetString(reader.GetOrdinal("Scope")),
                    ResourceType = reader.GetString(reader.GetOrdinal("ResourceType")),
                    ResourcePublicId = reader.GetString(reader.GetOrdinal("ResourcePublicId")),
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
                    Robots = GetNullableString(reader, "Robots"),
                    IsManualOverride = GetNullableBoolean(reader, "IsManualOverride"),
                    IsIndexable = GetNullableBoolean(reader, "IsIndexable"),
                    IsActive = GetNullableBoolean(reader, "IsActive"),
                    SourceAggregateVersion = GetNullableInt64(reader, "SourceAggregateVersion"),
                    LastAppliedMessageId = GetNullableString(reader, "LastAppliedMessageId"),
                    LastSyncedAtUtc = GetNullableDateTime(reader, "LastSyncedAtUtc"),
                    Version = reader.GetInt32(reader.GetOrdinal("Version"))
                };
            }
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
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
                int skip = query.Skip < 0 ? 0 : query.Skip;
                int take = query.Take <= 0 ? 20 : query.Take;
                if (take > 200)
                {
                    take = 200;
                }

                command.Parameters.AddRange(
                [
                    new SqlParameter("@Skip", SqlDbType.Int) { Value = skip },
                    new SqlParameter("@Take", SqlDbType.Int) { Value = take },
                    new SqlParameter("@Scope", SqlDbType.VarChar, 30) { Value = ToDbValue(query.Scope) },
                    new SqlParameter("@ResourceType", SqlDbType.VarChar, 50) { Value = ToDbValue(query.ResourceType) },
                    new SqlParameter("@ResourcePublicId", SqlDbType.Char, 26) { Value = ToDbValue(query.ResourcePublicId) },
                    new SqlParameter("@IsManualOverride", SqlDbType.Bit) { Value = ToDbValue(query.IsManualOverride) },
                    new SqlParameter("@UpdatedByUserId", SqlDbType.BigInt) { Value = ToDbValue(query.UpdatedByUserId) },
                    new SqlParameter("@Keyword", SqlDbType.NVarChar, 300) { Value = ToDbValue(query.Keyword) },
                    new SqlParameter("@SortBy", SqlDbType.NVarChar, 30) { Value = query.SortBy ?? "UpdatedAtUtc" },
                    new SqlParameter("@SortDirection", SqlDbType.NVarChar, 4) { Value = query.SortDirection ?? "DESC" }
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
                        Scope = reader.GetString(reader.GetOrdinal("Scope")),
                        ResourceType = reader.GetString(reader.GetOrdinal("ResourceType")),
                        ResourcePublicId = reader.GetString(reader.GetOrdinal("ResourcePublicId")),
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
                        Robots = GetNullableString(reader, "Robots"),
                        IsManualOverride = reader.GetBoolean(reader.GetOrdinal("IsManualOverride")),
                        SourceAggregateVersion = GetNullableInt64(reader, "SourceAggregateVersion"),
                        LastAppliedMessageId = GetNullableString(reader, "LastAppliedMessageId"),
                        LastSyncedAtUtc = GetNullableDateTime(reader, "LastSyncedAtUtc"),
                        CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
                        UpdatedAtUtc = reader.GetDateTime(reader.GetOrdinal("UpdatedAtUtc")),
                        UpdatedByUserId = GetNullableInt64(reader, "UpdatedByUserId"),
                        Version = reader.GetInt32(reader.GetOrdinal("Version"))
                    });
                }

                return new PagedQueryResult<SeoMetadataListResultItem>
                {
                    Items = items,
                    Page = (skip / take) + 1,
                    PageSize = take,
                    TotalItems = totalItems
                };
            }
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
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
            scope: reader.GetString(reader.GetOrdinal("Scope")),
            resourceType: reader.GetString(reader.GetOrdinal("ResourceType")),
            resourcePublicId: reader.GetString(reader.GetOrdinal("ResourcePublicId")),
            slug: GetNullableString(reader, "Slug"),
            canonicalUrl: GetNullableString(reader, "CanonicalUrl"),
            metaTitle: GetNullableString(reader, "MetaTitle"),
            metaDescription: GetNullableString(reader, "MetaDescription"),
            ogTitle: GetNullableString(reader, "OgTitle"),
            ogDescription: GetNullableString(reader, "OgDescription"),
            ogImageUrl: GetNullableString(reader, "OgImageUrl"),
            twitterTitle: GetNullableString(reader, "TwitterTitle"),
            twitterDescription: GetNullableString(reader, "TwitterDescription"),
            twitterImageUrl: GetNullableString(reader, "TwitterImageUrl"),
            robots: GetNullableString(reader, "Robots"),
            isManualOverride: reader.GetBoolean(reader.GetOrdinal("IsManualOverride")),
            sourceAggregateVersion: GetNullableInt64(reader, "SourceAggregateVersion"),
            lastAppliedMessageId: GetNullableString(reader, "LastAppliedMessageId"),
            lastSyncedAtUtc: GetNullableDateTime(reader, "LastSyncedAtUtc"),
            version: reader.GetInt32(reader.GetOrdinal("Version")),
            createdAtUtc: reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
            updatedAtUtc: reader.GetDateTime(reader.GetOrdinal("UpdatedAtUtc")),
            updatedByUserId: GetNullableInt64(reader, "UpdatedByUserId"));
    }

    private static SeoMetadataResult MapSeoMetadataResult(SqlDataReader reader)
    {
        return new SeoMetadataResult
        {
            Scope = reader.GetString(reader.GetOrdinal("Scope")),
            ResourceType = reader.GetString(reader.GetOrdinal("ResourceType")),
            ResourcePublicId = reader.GetString(reader.GetOrdinal("ResourcePublicId")),
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
            Robots = GetNullableString(reader, "Robots"),
            IsManualOverride = GetNullableBoolean(reader, "IsManualOverride"),
            SourceAggregateVersion = GetNullableInt64(reader, "SourceAggregateVersion"),
            LastAppliedMessageId = GetNullableString(reader, "LastAppliedMessageId"),
            LastSyncedAtUtc = GetNullableDateTime(reader, "LastSyncedAtUtc"),
            Version = reader.GetInt32(reader.GetOrdinal("Version"))
        };
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

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}