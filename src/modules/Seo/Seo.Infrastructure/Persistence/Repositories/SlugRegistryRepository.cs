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

public sealed class SlugRegistryRepository : ISlugRegistryRepository
{
    private const string SlugRegistryInsertProc = "[seo].[Seo_SlugRegistry_Insert]";
    private const string SlugRegistryUpdateProc = "[seo].[Seo_SlugRegistry_Update]";
    private const string SlugRegistryActivateProc = "[seo].[Seo_SlugRegistry_Activate]";
    private const string SlugRegistryDeactivateProc = "[seo].[Seo_SlugRegistry_Deactivate]";
    private const string SlugRegistrySelectByIdProc = "[seo].[Seo_SlugRegistry_SelectById]";
    private const string SlugRegistrySelectByArticleIdProc = "[seo].[Seo_SlugRegistry_SelectByArticleId]";
    private const string SlugRegistrySelectByScopeAndSlugProc = "[seo].[Seo_SlugRegistry_SelectByScopeAndSlug]";
    private const string SlugRegistrySelectSkipAndTakeProc = "[seo].[Seo_SlugRegistry_SelectSkipAndTake]";
    private const string SlugRegistryResolveByScopeAndSlugProc = "[seo].[Seo_ResolveByScopeAndSlug]";

    private readonly SeoUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly SeoSqlExceptionTranslator _sqlExceptionTranslator;

    public SlugRegistryRepository(
        SeoUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        SeoSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<long> InsertAsync(
        SlugRegistry slugRegistry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slugRegistry);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(SlugRegistryInsertProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = slugRegistry.ArticleId },
                new SqlParameter("@Slug", SqlDbType.NVarChar, 200) { Value = slugRegistry.Slug },
                new SqlParameter("@Scope", SqlDbType.VarChar, 30) { Value = slugRegistry.Scope },
                new SqlParameter("@CanonicalUrl", SqlDbType.NVarChar, 500) { Value = ToDbValue(slugRegistry.CanonicalUrl) },
                new SqlParameter("@IsIndexable", SqlDbType.Bit) { Value = slugRegistry.IsIndexable },
                new SqlParameter("@IsActive", SqlDbType.Bit) { Value = slugRegistry.IsActive },
                new SqlParameter("@CreatedByUserId", SqlDbType.BigInt) { Value = ToDbValue(slugRegistry.CreatedByUserId) }
            ]);

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Seo_SlugRegistry_Insert did not return a row.");
            }

            return reader.GetInt64(reader.GetOrdinal("SlugId"));
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<SlugRegistry?> GetByIdAsync(
        long slugId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(SlugRegistrySelectByIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@SlugId", SqlDbType.BigInt) { Value = slugId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapSlugRegistry(reader);
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

    public async Task<SlugRegistry?> GetByScopeAndSlugAsync(
        string scope,
        string slug,
        bool? onlyActive = null,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(SlugRegistrySelectByScopeAndSlugProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@Scope", SqlDbType.VarChar, 30) { Value = scope },
                    new SqlParameter("@Slug", SqlDbType.NVarChar, 200) { Value = slug },
                    new SqlParameter("@OnlyActive", SqlDbType.Bit) { Value = ToDbValue(onlyActive) }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapSlugRegistry(reader);
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
        SlugRegistry slugRegistry,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slugRegistry);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(SlugRegistryUpdateProc);

            SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@SlugId", SqlDbType.BigInt) { Value = slugRegistry.SlugId },
                new SqlParameter("@Slug", SqlDbType.NVarChar, 200) { Value = slugRegistry.Slug },
                new SqlParameter("@Scope", SqlDbType.VarChar, 30) { Value = slugRegistry.Scope },
                new SqlParameter("@CanonicalUrl", SqlDbType.NVarChar, 500) { Value = ToDbValue(slugRegistry.CanonicalUrl) },
                new SqlParameter("@IsIndexable", SqlDbType.Bit) { Value = slugRegistry.IsIndexable },
                new SqlParameter("@IsActive", SqlDbType.Bit) { Value = slugRegistry.IsActive },
                new SqlParameter("@UpdatedByUserId", SqlDbType.BigInt) { Value = ToDbValue(slugRegistry.UpdatedByUserId) },
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

    public async Task<int> ActivateAsync(
        long slugId,
        long? updatedByUserId,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using SqlCommand command = CreateTransactionalCommand(SlugRegistryActivateProc);

            SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@SlugId", SqlDbType.BigInt) { Value = slugId },
                new SqlParameter("@UpdatedByUserId", SqlDbType.BigInt) { Value = ToDbValue(updatedByUserId) },
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

    public async Task<int> DeactivateAsync(
        long slugId,
        long? updatedByUserId,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using SqlCommand command = CreateTransactionalCommand(SlugRegistryDeactivateProc);

            SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@SlugId", SqlDbType.BigInt) { Value = slugId },
                new SqlParameter("@UpdatedByUserId", SqlDbType.BigInt) { Value = ToDbValue(updatedByUserId) },
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
    public async Task<IReadOnlyList<SlugRegistryListResultItem>> SelectByArticleIdAsync(
        long articleId,
        string? scope = null,
        bool? onlyActive = null,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(SlugRegistrySelectByArticleIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                    new SqlParameter("@Scope", SqlDbType.VarChar, 30) { Value = ToDbValue(scope) },
                    new SqlParameter("@OnlyActive", SqlDbType.Bit) { Value = ToDbValue(onlyActive) }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<SlugRegistryListResultItem> items = [];

                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(MapSlugRegistryListResultItem(reader));
                }

                return items;
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

    public async Task<ResolveSeoRouteResult?> ResolveByScopeAndSlugAsync(
        string scope,
        string slug,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(SlugRegistryResolveByScopeAndSlugProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@Scope", SqlDbType.VarChar, 30) { Value = scope },
                    new SqlParameter("@Slug", SqlDbType.NVarChar, 200) { Value = slug }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return new ResolveSeoRouteResult
                {
                    Scope = reader.GetString(reader.GetOrdinal("Scope")),
                    Slug = reader.GetString(reader.GetOrdinal("Slug")),
                    ResourceType = reader.GetString(reader.GetOrdinal("ResourceType")),
                    ResourceId = reader.GetInt64(reader.GetOrdinal("ResourceId")),
                    CanonicalUrl = GetNullableString(reader, "CanonicalUrl"),
                    IsIndexable = reader.GetBoolean(reader.GetOrdinal("IsIndexable")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
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

    public async Task<PagedQueryResult<SlugRegistryListResultItem>> SelectSkipAndTakeAsync(
        SlugRegistryListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(SlugRegistrySelectSkipAndTakeProc, cancellationToken);

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
                    new SqlParameter("@Scope", SqlDbType.VarChar, 30) { Value = ToDbValue(query.Scope) },
                    new SqlParameter("@IsActive", SqlDbType.Bit) { Value = ToDbValue(query.IsActive) },
                    new SqlParameter("@IsIndexable", SqlDbType.Bit) { Value = ToDbValue(query.IsIndexable) },
                    new SqlParameter("@Keyword", SqlDbType.NVarChar, 200) { Value = ToDbValue(query.Keyword) },
                    new SqlParameter("@SortBy", SqlDbType.NVarChar, 30) { Value = query.SortBy },
                    new SqlParameter("@SortDirection", SqlDbType.NVarChar, 4) { Value = query.SortDirection }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<SlugRegistryListResultItem> items = [];
                int totalItems = 0;

                while (await reader.ReadAsync(cancellationToken))
                {
                    if (totalItems == 0)
                    {
                        totalItems = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                    }

                    items.Add(MapSlugRegistryListResultItem(reader));
                }

                return new PagedQueryResult<SlugRegistryListResultItem>
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

    private static SlugRegistry MapSlugRegistry(SqlDataReader reader)
    {
        return SlugRegistry.Rehydrate(
            slugId: reader.GetInt64(reader.GetOrdinal("SlugId")),
            articleId: reader.GetInt64(reader.GetOrdinal("ArticleId")),
            slug: reader.GetString(reader.GetOrdinal("Slug")),
            scope: reader.GetString(reader.GetOrdinal("Scope")),
            canonicalUrl: GetNullableString(reader, "CanonicalUrl"),
            isIndexable: reader.GetBoolean(reader.GetOrdinal("IsIndexable")),
            isActive: reader.GetBoolean(reader.GetOrdinal("IsActive")),
            version: reader.GetInt32(reader.GetOrdinal("Version")),
            createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            createdByUserId: GetNullableInt64(reader, "CreatedByUserId"),
            updatedAt: reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            updatedByUserId: GetNullableInt64(reader, "UpdatedByUserId"));
    }

    private static SlugRegistryListResultItem MapSlugRegistryListResultItem(SqlDataReader reader)
    {
        return new SlugRegistryListResultItem
        {
            SlugId = reader.GetInt64(reader.GetOrdinal("SlugId")),
            ArticleId = reader.GetInt64(reader.GetOrdinal("ArticleId")),
            Slug = reader.GetString(reader.GetOrdinal("Slug")),
            Scope = reader.GetString(reader.GetOrdinal("Scope")),
            CanonicalUrl = GetNullableString(reader, "CanonicalUrl"),
            IsIndexable = reader.GetBoolean(reader.GetOrdinal("IsIndexable")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            CreatedByUserId = GetNullableInt64(reader, "CreatedByUserId"),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            UpdatedByUserId = GetNullableInt64(reader, "UpdatedByUserId"),
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
}