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

public sealed class SlugRegistryRepository : ISlugRegistryRepository
{
    private const string SlugRegistryUpsertProc = "[seo].[Seo_SlugRegistry_Upsert]";
    private const string SlugRegistryApplyContentVisibilityProc = "[seo].[Seo_SlugRegistry_ApplyContentVisibility]";
    private const string SlugRegistryDeactivateByResourceProc = "[seo].[Seo_SlugRegistry_DeactivateByResource]";
    private const string SlugRegistrySelectByIdProc = "[seo].[Seo_SlugRegistry_SelectById]";
    private const string SlugRegistrySelectByResourceProc = "[seo].[Seo_SlugRegistry_SelectByResource]";
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

    public async Task<SlugRegistry?> GetByResourceAsync(
        string scope,
        string resourceType,
        string resourcePublicId,
        bool? onlyActive = null,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(SlugRegistrySelectByResourceProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@Scope", SqlDbType.VarChar, 30) { Value = scope },
                    new SqlParameter("@ResourceType", SqlDbType.VarChar, 50) { Value = resourceType },
                    new SqlParameter("@ResourcePublicId", SqlDbType.Char, 26) { Value = resourcePublicId },
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

    public async Task<SlugRegistry?> UpsertAsync(
        SlugRegistryUpsertCommand model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(SlugRegistryUpsertProc);

            SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@Scope", SqlDbType.VarChar, 30) { Value = model.Scope },
                new SqlParameter("@Slug", SqlDbType.NVarChar, 200) { Value = model.Slug },
                new SqlParameter("@ResourceType", SqlDbType.VarChar, 50) { Value = model.ResourceType },
                new SqlParameter("@ResourcePublicId", SqlDbType.Char, 26) { Value = model.ResourcePublicId },
                new SqlParameter("@CanonicalUrl", SqlDbType.NVarChar, 500) { Value = ToDbValue(model.CanonicalUrl) },
                new SqlParameter("@IsIndexable", SqlDbType.Bit) { Value = model.IsIndexable },
                new SqlParameter("@IsActive", SqlDbType.Bit) { Value = model.IsActive },
                new SqlParameter("@ActorUserId", SqlDbType.BigInt) { Value = ToDbValue(model.ActorUserId) },
                new SqlParameter("@ExpectedVersion", SqlDbType.Int) { Value = ToDbValue(model.ExpectedVersion) },
                affectedRowsParameter
            ]);

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapSlugRegistry(reader);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<SeoApplyResultModel> ApplyContentVisibilityAsync(
        ApplyContentVisibilityCommand model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(SlugRegistryApplyContentVisibilityProc);

            SqlParameter applyResultParameter = new("@ApplyResult", SqlDbType.VarChar, 30)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@Scope", SqlDbType.VarChar, 30) { Value = model.Scope },
                new SqlParameter("@Slug", SqlDbType.NVarChar, 200) { Value = ToDbValue(model.Slug) },
                new SqlParameter("@ResourceType", SqlDbType.VarChar, 50) { Value = model.ResourceType },
                new SqlParameter("@ResourcePublicId", SqlDbType.Char, 26) { Value = model.ResourcePublicId },
                new SqlParameter("@CanonicalUrl", SqlDbType.NVarChar, 500) { Value = ToDbValue(model.CanonicalUrl) },
                new SqlParameter("@IsIndexable", SqlDbType.Bit) { Value = model.IsIndexable },
                new SqlParameter("@IsActive", SqlDbType.Bit) { Value = model.IsActive },
                new SqlParameter("@SourceAggregateVersion", SqlDbType.BigInt) { Value = model.SourceAggregateVersion },
                new SqlParameter("@LastAppliedMessageId", SqlDbType.Char, 26) { Value = model.LastAppliedMessageId },
                new SqlParameter("@LastSyncedAtUtc", SqlDbType.DateTime2) { Value = ToDbValue(model.LastSyncedAtUtc) },
                applyResultParameter
            ]);

            SlugRegistry? slugRegistry = null;

            using (SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                if (await reader.ReadAsync(cancellationToken))
                {
                    slugRegistry = MapSlugRegistry(reader);
                }
            }

            string applyResult = applyResultParameter.Value is DBNull or null
                ? string.Empty
                : Convert.ToString(applyResultParameter.Value) ?? string.Empty;

            return new SeoApplyResultModel
            {
                ApplyResult = applyResult,
                EntityId = slugRegistry?.SlugId,
                Version = slugRegistry?.Version,
                SourceAggregateVersion = slugRegistry?.SourceAggregateVersion,
                LastAppliedMessageId = slugRegistry?.LastAppliedMessageId,
                LastSyncedAtUtc = slugRegistry?.LastSyncedAtUtc
            };
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<SlugRegistry?> DeactivateByResourceAsync(
        SlugRegistryDeactivateByResourceCommand model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(SlugRegistryDeactivateByResourceProc);

            SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@Scope", SqlDbType.VarChar, 30) { Value = model.Scope },
                new SqlParameter("@ResourceType", SqlDbType.VarChar, 50) { Value = model.ResourceType },
                new SqlParameter("@ResourcePublicId", SqlDbType.Char, 26) { Value = model.ResourcePublicId },
                new SqlParameter("@ActorUserId", SqlDbType.BigInt) { Value = ToDbValue(model.ActorUserId) },
                new SqlParameter("@ExpectedVersion", SqlDbType.Int) { Value = ToDbValue(model.ExpectedVersion) },
                affectedRowsParameter
            ]);

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapSlugRegistry(reader);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<ResolvedSlugRouteResult?> ResolveByScopeAndSlugAsync(
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

                return new ResolvedSlugRouteResult
                {
                    Scope = reader.GetString(reader.GetOrdinal("Scope")),
                    Slug = reader.GetString(reader.GetOrdinal("Slug")),
                    ResourceType = reader.GetString(reader.GetOrdinal("ResourceType")),
                    ResourcePublicId = reader.GetString(reader.GetOrdinal("ResourcePublicId")),
                    CanonicalUrl = GetNullableString(reader, "CanonicalUrl"),
                    IsIndexable = reader.GetBoolean(reader.GetOrdinal("IsIndexable")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    SourceAggregateVersion = GetNullableInt64(reader, "SourceAggregateVersion"),
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
                    new SqlParameter("@IsActive", SqlDbType.Bit) { Value = ToDbValue(query.IsActive) },
                    new SqlParameter("@IsIndexable", SqlDbType.Bit) { Value = ToDbValue(query.IsIndexable) },
                    new SqlParameter("@Keyword", SqlDbType.NVarChar, 200) { Value = ToDbValue(query.Keyword) },
                    new SqlParameter("@SortBy", SqlDbType.NVarChar, 30) { Value = query.SortBy ?? "UpdatedAtUtc" },
                    new SqlParameter("@SortDirection", SqlDbType.NVarChar, 4) { Value = query.SortDirection ?? "DESC" }
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

    private static SlugRegistry MapSlugRegistry(SqlDataReader reader)
    {
        return SlugRegistry.Rehydrate(
            slugId: reader.GetInt64(reader.GetOrdinal("SlugId")),
            scope: reader.GetString(reader.GetOrdinal("Scope")),
            slug: reader.GetString(reader.GetOrdinal("Slug")),
            resourceType: reader.GetString(reader.GetOrdinal("ResourceType")),
            resourcePublicId: reader.GetString(reader.GetOrdinal("ResourcePublicId")),
            canonicalUrl: GetNullableString(reader, "CanonicalUrl"),
            isIndexable: reader.GetBoolean(reader.GetOrdinal("IsIndexable")),
            isActive: reader.GetBoolean(reader.GetOrdinal("IsActive")),
            sourceAggregateVersion: GetNullableInt64(reader, "SourceAggregateVersion"),
            lastAppliedMessageId: GetNullableString(reader, "LastAppliedMessageId"),
            lastSyncedAtUtc: GetNullableDateTime(reader, "LastSyncedAtUtc"),
            version: reader.GetInt32(reader.GetOrdinal("Version")),
            createdAtUtc: reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
            createdByUserId: GetNullableInt64(reader, "CreatedByUserId"),
            updatedAtUtc: reader.GetDateTime(reader.GetOrdinal("UpdatedAtUtc")),
            updatedByUserId: GetNullableInt64(reader, "UpdatedByUserId"));
    }

    private static SlugRegistryListResultItem MapSlugRegistryListResultItem(SqlDataReader reader)
    {
        return new SlugRegistryListResultItem
        {
            SlugId = reader.GetInt64(reader.GetOrdinal("SlugId")),
            Scope = reader.GetString(reader.GetOrdinal("Scope")),
            Slug = reader.GetString(reader.GetOrdinal("Slug")),
            ResourceType = reader.GetString(reader.GetOrdinal("ResourceType")),
            ResourcePublicId = reader.GetString(reader.GetOrdinal("ResourcePublicId")),
            CanonicalUrl = GetNullableString(reader, "CanonicalUrl"),
            IsIndexable = reader.GetBoolean(reader.GetOrdinal("IsIndexable")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            SourceAggregateVersion = GetNullableInt64(reader, "SourceAggregateVersion"),
            LastAppliedMessageId = GetNullableString(reader, "LastAppliedMessageId"),
            LastSyncedAtUtc = GetNullableDateTime(reader, "LastSyncedAtUtc"),
            Version = reader.GetInt32(reader.GetOrdinal("Version")),
            CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
            CreatedByUserId = GetNullableInt64(reader, "CreatedByUserId"),
            UpdatedAtUtc = reader.GetDateTime(reader.GetOrdinal("UpdatedAtUtc")),
            UpdatedByUserId = GetNullableInt64(reader, "UpdatedByUserId")
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

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}