using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Media.Application.Models.Commands;
using Media.Application.Models.Queries;
using Media.Application.Models.Results;
using Media.Application.Ports.Persistence;
using Media.Domain.Entities;
using Media.Infrastructure.Persistence.Exceptions;
using Media.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Media.Infrastructure.Persistence.Repositories;

public sealed class ArticleMediaRepository : IArticleMediaRepository
{
    private const string ArticleMediaAttachProc = "[media].[Media_ArticleMedia_Attach]";
    private const string ArticleMediaDetachProc = "[media].[Media_ArticleMedia_Detach]";
    private const string ArticleMediaRestoreProc = "[media].[Media_ArticleMedia_Restore]";
    private const string ArticleMediaSetPrimaryProc = "[media].[Media_ArticleMedia_SetPrimary]";
    private const string ArticleMediaReorderByIdsProc = "[media].[Media_ArticleMedia_ReorderByIds]";

    private const string ArticleMediaSelectByIdProc = "[media].[Media_ArticleMedia_SelectById]";
    private const string ArticleMediaSelectPrimaryByArticleIdProc = "[media].[Media_ArticleMedia_SelectPrimaryByArticleId]";
    private const string ArticleMediaSelectSkipAndTakeByArticleIdProc = "[media].[Media_ArticleMedia_SelectSkipAndTakeByArticleId]";
    private const string ArticleMediaGetRecordCountByArticleIdProc = "[media].[Media_ArticleMedia_GetRecordCountByArticleId]";
    private const string ArticleMediaSelectAllByMediaIdProc = "[media].[Media_ArticleMedia_SelectAllByMediaId]";

    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly MediaSqlExceptionTranslator _sqlExceptionTranslator;

    public ArticleMediaRepository(
        IMediaUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        MediaSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<ArticleMediaAttachResult> AttachAsync(
        AttachArticleMediaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            using SqlCommand sqlCommand = CreateTransactionalCommand(ArticleMediaAttachProc);

            SqlParameter articleMediaIdParameter = new("@ArticleMediaId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };

            SqlParameter affectedRowsParameter = CreateOutputIntParameter("@AffectedRows");
            SqlParameter primaryChangedParameter = new("@PrimaryChanged", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };
            SqlParameter newVersionParameter = CreateOutputIntParameter("@NewVersion");
            SqlParameter resultCodeParameter = CreateOutputIntParameter("@ResultCode");

            sqlCommand.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = command.ArticleId },
                new SqlParameter("@MediaId", SqlDbType.BigInt) { Value = command.MediaId },
                new SqlParameter("@IsPrimary", SqlDbType.Bit) { Value = command.IsPrimary },
                new SqlParameter("@CreatedBy", SqlDbType.BigInt) { Value = ToDbValue(command.CreatedBy) },
                articleMediaIdParameter,
                affectedRowsParameter,
                primaryChangedParameter,
                newVersionParameter,
                resultCodeParameter
            ]);

            await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

            return new ArticleMediaAttachResult(
                ResultCode: GetOutputInt32OrDefault(resultCodeParameter),
                ArticleMediaId: GetOutputNullableInt64(articleMediaIdParameter),
                AffectedRows: GetOutputInt32OrDefault(affectedRowsParameter),
                PrimaryChanged: GetOutputBooleanOrDefault(primaryChangedParameter),
                NewVersion: GetOutputNullableInt32(newVersionParameter));
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<ArticleMediaDetachResult> DetachAsync(
        DetachArticleMediaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            using SqlCommand sqlCommand = CreateTransactionalCommand(ArticleMediaDetachProc);

            SqlParameter affectedRowsParameter = CreateOutputIntParameter("@AffectedRows");
            SqlParameter primaryClearedParameter = new("@PrimaryCleared", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };
            SqlParameter newVersionParameter = CreateOutputIntParameter("@NewVersion");
            SqlParameter resultCodeParameter = CreateOutputIntParameter("@ResultCode");

            sqlCommand.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = command.ArticleId },
                new SqlParameter("@MediaId", SqlDbType.BigInt) { Value = command.MediaId },
                new SqlParameter("@DeletedBy", SqlDbType.BigInt) { Value = ToDbValue(command.DeletedBy) },
                affectedRowsParameter,
                primaryClearedParameter,
                newVersionParameter,
                resultCodeParameter
            ]);

            await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

            return new ArticleMediaDetachResult(
                ResultCode: GetOutputInt32OrDefault(resultCodeParameter),
                AffectedRows: GetOutputInt32OrDefault(affectedRowsParameter),
                PrimaryCleared: GetOutputBooleanOrDefault(primaryClearedParameter),
                NewVersion: GetOutputNullableInt32(newVersionParameter));
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<ArticleMediaMutationResult> RestoreAsync(
        RestoreArticleMediaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            using SqlCommand sqlCommand = CreateTransactionalCommand(ArticleMediaRestoreProc);

            SqlParameter affectedRowsParameter = CreateOutputIntParameter("@AffectedRows");
            SqlParameter newVersionParameter = CreateOutputIntParameter("@NewVersion");
            SqlParameter resultCodeParameter = CreateOutputIntParameter("@ResultCode");

            sqlCommand.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = command.ArticleId },
                new SqlParameter("@MediaId", SqlDbType.BigInt) { Value = command.MediaId },
                new SqlParameter("@RestoredBy", SqlDbType.BigInt) { Value = ToDbValue(command.RestoredBy) },
                affectedRowsParameter,
                newVersionParameter,
                resultCodeParameter
            ]);

            await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

            return new ArticleMediaMutationResult(
                ResultCode: GetOutputInt32OrDefault(resultCodeParameter),
                AffectedRows: GetOutputInt32OrDefault(affectedRowsParameter),
                NewVersion: GetOutputNullableInt32(newVersionParameter));
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<ArticleMediaMutationResult> SetPrimaryAsync(
        SetPrimaryArticleMediaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            using SqlCommand sqlCommand = CreateTransactionalCommand(ArticleMediaSetPrimaryProc);

            SqlParameter affectedRowsParameter = CreateOutputIntParameter("@AffectedRows");
            SqlParameter newVersionParameter = CreateOutputIntParameter("@NewVersion");
            SqlParameter resultCodeParameter = CreateOutputIntParameter("@ResultCode");

            sqlCommand.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = command.ArticleId },
                new SqlParameter("@MediaId", SqlDbType.BigInt) { Value = command.MediaId },
                new SqlParameter("@ExpectedVersion", SqlDbType.Int) { Value = ToDbValue(command.ExpectedVersion) },
                new SqlParameter("@UpdatedBy", SqlDbType.BigInt) { Value = ToDbValue(command.UpdatedBy) },
                affectedRowsParameter,
                newVersionParameter,
                resultCodeParameter
            ]);

            await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

            return new ArticleMediaMutationResult(
                ResultCode: GetOutputInt32OrDefault(resultCodeParameter),
                AffectedRows: GetOutputInt32OrDefault(affectedRowsParameter),
                NewVersion: GetOutputNullableInt32(newVersionParameter));
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<ArticleMediaMutationResult> ReorderByIdsAsync(
        ReorderArticleMediaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            using SqlCommand sqlCommand = CreateTransactionalCommand(ArticleMediaReorderByIdsProc);

            SqlParameter affectedRowsParameter = CreateOutputIntParameter("@AffectedRows");
            SqlParameter newVersionParameter = CreateOutputIntParameter("@NewVersion");
            SqlParameter resultCodeParameter = CreateOutputIntParameter("@ResultCode");

            SqlParameter ordersParameter = new("@Orders", SqlDbType.Structured)
            {
                TypeName = "media.MediaOrderListType",
                Value = BuildMediaOrderDataTable(command.Orders)
            };

            sqlCommand.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = command.ArticleId },
                new SqlParameter("@ExpectedVersion", SqlDbType.Int) { Value = ToDbValue(command.ExpectedVersion) },
                new SqlParameter("@UpdatedBy", SqlDbType.BigInt) { Value = ToDbValue(command.UpdatedBy) },
                ordersParameter,
                affectedRowsParameter,
                newVersionParameter,
                resultCodeParameter
            ]);

            await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

            return new ArticleMediaMutationResult(
                ResultCode: GetOutputInt32OrDefault(resultCodeParameter),
                AffectedRows: GetOutputInt32OrDefault(affectedRowsParameter),
                NewVersion: GetOutputNullableInt32(newVersionParameter));
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<ArticleMedia?> GetByIdAsync(
        long articleMediaId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(ArticleMediaSelectByIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@ArticleMediaId", SqlDbType.BigInt) { Value = articleMediaId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapArticleMedia(reader);
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

    public async Task<ArticleMediaListResultItem?> GetPrimaryByArticleIdAsync(
        long articleId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(ArticleMediaSelectPrimaryByArticleIdProc, cancellationToken);

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

                return MapArticleMediaListResultItem(reader);
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

    public async Task<PagedQueryResult<ArticleMediaListResultItem>> SelectByArticleIdAsync(
        ArticleMediaListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SqlConnection? ownedConnection = null;

        try
        {
            int page = query.Page <= 0 ? 1 : query.Page;
            int pageSize = query.PageSize <= 0 ? 20 : query.PageSize;
            int skip = (page - 1) * pageSize;
            int take = pageSize;

            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(ArticleMediaSelectSkipAndTakeByArticleIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = query.ArticleId },
                    new SqlParameter("@Skip", SqlDbType.Int) { Value = skip },
                    new SqlParameter("@Take", SqlDbType.Int) { Value = take },
                    new SqlParameter("@IncludeDeleted", SqlDbType.Bit) { Value = query.IncludeDeleted },
                    new SqlParameter("@SortBy", SqlDbType.NVarChar, 50) { Value = query.SortBy },
                    new SqlParameter("@SortDirection", SqlDbType.NVarChar, 4) { Value = query.SortDirection }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<ArticleMediaListResultItem> items = [];

                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(MapArticleMediaListResultItem(reader));
                }

                int totalItems = await GetRecordCountByArticleIdInternalAsync(
                    query.ArticleId,
                    query.IncludeDeleted,
                    cancellationToken);

                return new PagedQueryResult<ArticleMediaListResultItem>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
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

    public async Task<IReadOnlyList<ArticleMediaUsageResultItem>> SelectByMediaIdAsync(
        long mediaId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(ArticleMediaSelectAllByMediaIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@MediaId", SqlDbType.BigInt) { Value = mediaId },
                    new SqlParameter("@IncludeDeleted", SqlDbType.Bit) { Value = includeDeleted }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<ArticleMediaUsageResultItem> items = [];

                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(MapArticleMediaUsageResultItem(reader));
                }

                return items;
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

    private async Task<int> GetRecordCountByArticleIdInternalAsync(
        long articleId,
        bool includeDeleted,
        CancellationToken cancellationToken)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(ArticleMediaGetRecordCountByArticleIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                    new SqlParameter("@IncludeDeleted", SqlDbType.Bit) { Value = includeDeleted }
                ]);

                object? scalar = await command.ExecuteScalarAsync(cancellationToken);

                return scalar is null || scalar is DBNull
                    ? 0
                    : Convert.ToInt32(scalar);
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

    private static DataTable BuildMediaOrderDataTable(
        IReadOnlyList<ArticleMediaOrderItem> orders)
    {
        DataTable table = new();
        table.Columns.Add("MediaId", typeof(long));
        table.Columns.Add("SortOrder", typeof(int));

        foreach (ArticleMediaOrderItem order in orders)
        {
            table.Rows.Add(order.MediaId, order.SortOrder);
        }

        return table;
    }

    private static ArticleMedia MapArticleMedia(SqlDataReader reader)
    {
        return ArticleMedia.Rehydrate(
            articleMediaId: reader.GetInt64(reader.GetOrdinal("ArticleMediaId")),
            articleId: reader.GetInt64(reader.GetOrdinal("ArticleId")),
            mediaId: reader.GetInt64(reader.GetOrdinal("MediaId")),
            sortOrder: reader.GetInt32(reader.GetOrdinal("SortOrder")),
            isPrimary: reader.GetBoolean(reader.GetOrdinal("IsPrimary")),
            altTextOverride: GetNullableString(reader, "AltTextOverride"),
            caption: GetNullableString(reader, "Caption"),
            version: reader.GetInt32(reader.GetOrdinal("Version")),
            createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            createdBy: GetNullableInt64(reader, "CreatedBy"),
            updatedAt: reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            updatedBy: GetNullableInt64(reader, "UpdatedBy"),
            isDeleted: reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
            deletedAt: GetNullableDateTime(reader, "DeletedAt"),
            deletedBy: GetNullableInt64(reader, "DeletedBy"));
    }

    private static ArticleMediaListResultItem MapArticleMediaListResultItem(SqlDataReader reader)
    {
        return new ArticleMediaListResultItem
        {
            ArticleMediaId = reader.GetInt64(reader.GetOrdinal("ArticleMediaId")),
            ArticleId = reader.GetInt64(reader.GetOrdinal("ArticleId")),
            AttachmentSetVersion = reader.GetInt32(reader.GetOrdinal("AttachmentSetVersion")),
            MediaId = reader.GetInt64(reader.GetOrdinal("MediaId")),
            PublicId = reader.GetString(reader.GetOrdinal("PublicId")),
            StorageProvider = reader.GetString(reader.GetOrdinal("StorageProvider")),
            Url = reader.GetString(reader.GetOrdinal("Url")),
            StoragePath = GetNullableString(reader, "StoragePath"),
            FileName = GetNullableString(reader, "FileName"),
            MediaType = reader.GetString(reader.GetOrdinal("MediaType")),
            MimeType = GetNullableString(reader, "MimeType"),
            FileSizeBytes = GetNullableInt64(reader, "FileSizeBytes"),
            Width = GetNullableInt32(reader, "Width"),
            Height = GetNullableInt32(reader, "Height"),
            DurationSeconds = GetNullableInt32(reader, "DurationSeconds"),
            DefaultAltText = GetNullableString(reader, "DefaultAltText"),
            MediaIsDeleted = HasColumn(reader, "MediaIsDeleted") && !reader.IsDBNull(reader.GetOrdinal("MediaIsDeleted"))
                ? reader.GetBoolean(reader.GetOrdinal("MediaIsDeleted"))
                : false,
            AltTextOverride = GetNullableString(reader, "AltTextOverride"),
            Caption = GetNullableString(reader, "Caption"),
            SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
            IsPrimary = reader.GetBoolean(reader.GetOrdinal("IsPrimary")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            CreatedBy = GetNullableInt64(reader, "CreatedBy"),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            UpdatedBy = GetNullableInt64(reader, "UpdatedBy"),
            Version = reader.GetInt32(reader.GetOrdinal("Version")),
            IsDeleted = HasColumn(reader, "IsDeleted") && !reader.IsDBNull(reader.GetOrdinal("IsDeleted"))
                ? reader.GetBoolean(reader.GetOrdinal("IsDeleted"))
                : false,
            DeletedAt = HasColumn(reader, "DeletedAt")
                ? GetNullableDateTime(reader, "DeletedAt")
                : null,
            DeletedBy = HasColumn(reader, "DeletedBy")
                ? GetNullableInt64(reader, "DeletedBy")
                : null
        };
    }

    private static ArticleMediaUsageResultItem MapArticleMediaUsageResultItem(SqlDataReader reader)
    {
        return new ArticleMediaUsageResultItem
        {
            ArticleMediaId = reader.GetInt64(reader.GetOrdinal("ArticleMediaId")),
            ArticleId = reader.GetInt64(reader.GetOrdinal("ArticleId")),
            AttachmentSetVersion = reader.GetInt32(reader.GetOrdinal("AttachmentSetVersion")),
            MediaId = reader.GetInt64(reader.GetOrdinal("MediaId")),
            SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
            IsPrimary = reader.GetBoolean(reader.GetOrdinal("IsPrimary")),
            AltTextOverride = GetNullableString(reader, "AltTextOverride"),
            Caption = GetNullableString(reader, "Caption"),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            CreatedBy = GetNullableInt64(reader, "CreatedBy"),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            UpdatedBy = GetNullableInt64(reader, "UpdatedBy"),
            Version = reader.GetInt32(reader.GetOrdinal("Version")),
            IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
            DeletedAt = GetNullableDateTime(reader, "DeletedAt"),
            DeletedBy = GetNullableInt64(reader, "DeletedBy")
        };
    }

    private static SqlParameter CreateOutputIntParameter(string name)
    {
        return new SqlParameter(name, SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
    }

    private static int GetOutputInt32OrDefault(SqlParameter parameter)
    {
        return parameter.Value is null or DBNull
            ? 0
            : Convert.ToInt32(parameter.Value);
    }

    private static int? GetOutputNullableInt32(SqlParameter parameter)
    {
        return parameter.Value is null or DBNull
            ? null
            : Convert.ToInt32(parameter.Value);
    }

    private static long? GetOutputNullableInt64(SqlParameter parameter)
    {
        return parameter.Value is null or DBNull
            ? null
            : Convert.ToInt64(parameter.Value);
    }

    private static bool GetOutputBooleanOrDefault(SqlParameter parameter)
    {
        return parameter.Value is not null
            && parameter.Value is not DBNull
            && Convert.ToBoolean(parameter.Value);
    }

    private static object ToDbValue(object? value)
    {
        return value ?? DBNull.Value;
    }

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

    private static int? GetNullableInt32(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }

    private static bool HasColumn(SqlDataReader reader, string columnName)
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}