using System.Data;
using CommercialNews.BuildingBlocks.Contracts.Common;
using CommercialNews.BuildingBlocks.Persistence.Sql;
using Media.Application.Models.QueryModels;
using Media.Application.Ports.Persistence;
using Media.Domain.Entities;
using Media.Infrastructure.Persistence.Exceptions;
using Media.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Media.Infrastructure.Persistence.Repositories
{
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

        private readonly MediaUnitOfWork _unitOfWork;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;
        private readonly MediaSqlExceptionTranslator _sqlExceptionTranslator;

        public ArticleMediaRepository(
            MediaUnitOfWork unitOfWork,
            ISqlConnectionFactory sqlConnectionFactory,
            MediaSqlExceptionTranslator sqlExceptionTranslator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
            _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
        }

        public async Task<(long? ArticleMediaId, int AffectedRows)> AttachAsync(
            long articleId,
            long mediaId,
            bool isPrimary,
            long? createdByUserId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using SqlCommand command = CreateTransactionalCommand(ArticleMediaAttachProc);

                SqlParameter articleMediaIdParameter = new("@ArticleMediaId", SqlDbType.BigInt)
                {
                    Direction = ParameterDirection.Output
                };

                SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };

                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                    new SqlParameter("@MediaId", SqlDbType.BigInt) { Value = mediaId },
                    new SqlParameter("@IsPrimary", SqlDbType.Bit) { Value = isPrimary },
                    new SqlParameter("@CreatedBy", SqlDbType.BigInt) { Value = ToDbValue(createdByUserId) },
                    articleMediaIdParameter,
                    affectedRowsParameter
                ]);

                await command.ExecuteNonQueryAsync(cancellationToken);

                long? articleMediaId = articleMediaIdParameter.Value is DBNull
                    ? null
                    : Convert.ToInt64(articleMediaIdParameter.Value);

                int affectedRows = affectedRowsParameter.Value is DBNull
                    ? 0
                    : Convert.ToInt32(affectedRowsParameter.Value);

                return (articleMediaId, affectedRows);
            }
            catch (SqlException exception)
            {
                throw _sqlExceptionTranslator.Translate(exception);
            }
        }

        public async Task<int> DetachAsync(
            long articleId,
            long mediaId,
            long? deletedByUserId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using SqlCommand command = CreateTransactionalCommand(ArticleMediaDetachProc);

                SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };

                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                    new SqlParameter("@MediaId", SqlDbType.BigInt) { Value = mediaId },
                    new SqlParameter("@DeletedBy", SqlDbType.BigInt) { Value = ToDbValue(deletedByUserId) },
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

        public async Task<int> RestoreAsync(
            long articleId,
            long mediaId,
            long? restoredByUserId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using SqlCommand command = CreateTransactionalCommand(ArticleMediaRestoreProc);

                SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };

                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                    new SqlParameter("@MediaId", SqlDbType.BigInt) { Value = mediaId },
                    new SqlParameter("@RestoredBy", SqlDbType.BigInt) { Value = ToDbValue(restoredByUserId) },
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

        public async Task<int> SetPrimaryAsync(
            long articleId,
            long mediaId,
            long? updatedByUserId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using SqlCommand command = CreateTransactionalCommand(ArticleMediaSetPrimaryProc);

                SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };

                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                    new SqlParameter("@MediaId", SqlDbType.BigInt) { Value = mediaId },
                    new SqlParameter("@UpdatedBy", SqlDbType.BigInt) { Value = ToDbValue(updatedByUserId) },
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

        public async Task<int> ReorderByIdsAsync(
            long articleId,
            IReadOnlyList<(long MediaId, int SortOrder)> orders,
            long? updatedByUserId,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(orders);

            try
            {
                using SqlCommand command = CreateTransactionalCommand(ArticleMediaReorderByIdsProc);

                SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };

                SqlParameter ordersParameter = new("@Orders", SqlDbType.Structured)
                {
                    TypeName = "[media].[MediaOrderListType]",
                    Value = BuildMediaOrderDataTable(orders)
                };

                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                    new SqlParameter("@UpdatedBy", SqlDbType.BigInt) { Value = ToDbValue(updatedByUserId) },
                    ordersParameter,
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
            finally
            {
                if (ownedConnection is not null)
                {
                    await ownedConnection.DisposeAsync();
                }
            }
        }

        public async Task<IReadOnlyList<ArticleMediaListResultItem>> SelectByMediaIdAsync(
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

                    List<ArticleMediaListResultItem> items = [];

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        items.Add(MapArticleMediaListResultItem(reader));
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
            IReadOnlyList<(long MediaId, int SortOrder)> orders)
        {
            DataTable table = new();
            table.Columns.Add("MediaId", typeof(long));
            table.Columns.Add("SortOrder", typeof(int));

            foreach ((long mediaId, int sortOrder) in orders)
            {
                table.Rows.Add(mediaId, sortOrder);
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
                createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                updatedAt: reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                createdByUserId: GetNullableInt64(reader, "CreatedBy"),
                updatedByUserId: GetNullableInt64(reader, "UpdatedBy"),
                isDeleted: reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                deletedAt: GetNullableDateTime(reader, "DeletedAt"),
                deletedByUserId: GetNullableInt64(reader, "DeletedBy"),
                version: reader.GetInt32(reader.GetOrdinal("Version")));
        }

        private static ArticleMediaListResultItem MapArticleMediaListResultItem(SqlDataReader reader)
        {
            return new ArticleMediaListResultItem
            {
                ArticleMediaId = reader.GetInt64(reader.GetOrdinal("ArticleMediaId")),
                ArticleId = reader.GetInt64(reader.GetOrdinal("ArticleId")),
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
                AltTextOverride = GetNullableString(reader, "AltTextOverride"),
                Caption = GetNullableString(reader, "Caption"),
                SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
                IsPrimary = reader.GetBoolean(reader.GetOrdinal("IsPrimary")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
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
    }
}