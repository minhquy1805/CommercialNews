using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Media.Application.Models.QueryModels;
using Media.Application.Ports.Persistence;
using Media.Domain.Entities;
using Media.Infrastructure.Persistence.Exceptions;
using Media.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Media.Infrastructure.Persistence.Repositories
{
    public sealed class MediaAssetRepository : IMediaAssetRepository
    {
        private const string MediaAssetInsertProc = "[media].[Media_MediaAsset_Insert]";
        private const string MediaAssetSelectByIdProc = "[media].[Media_MediaAsset_SelectById]";
        private const string MediaAssetSelectByPublicIdProc = "[media].[Media_MediaAsset_SelectByPublicId]";
        private const string MediaAssetSoftDeleteProc = "[media].[Media_MediaAsset_SoftDelete]";
        private const string MediaAssetRestoreProc = "[media].[Media_MediaAsset_Restore]";
        private const string MediaAssetGetRecordCountProc = "[media].[Media_MediaAsset_GetRecordCount]";
        private const string MediaAssetSelectSkipAndTakeProc = "[media].[Media_MediaAsset_SelectSkipAndTake]";

        private readonly MediaUnitOfWork _unitOfWork;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;
        private readonly MediaSqlExceptionTranslator _sqlExceptionTranslator;

        public MediaAssetRepository(
            MediaUnitOfWork unitOfWork,
            ISqlConnectionFactory sqlConnectionFactory,
            MediaSqlExceptionTranslator sqlExceptionTranslator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
            _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
        }

        public async Task<long> InsertAsync(
            MediaAsset mediaAsset,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(mediaAsset);

            try
            {
                using SqlCommand command = CreateTransactionalCommand(MediaAssetInsertProc);

                SqlParameter mediaIdParameter = new("@MediaId", SqlDbType.BigInt)
                {
                    Direction = ParameterDirection.Output
                };

                command.Parameters.AddRange(
                [
                    new SqlParameter("@PublicId", SqlDbType.VarChar, 26) { Value = mediaAsset.PublicId },
                    new SqlParameter("@StorageProvider", SqlDbType.VarChar, 30) { Value = mediaAsset.StorageProvider },
                    new SqlParameter("@Url", SqlDbType.NVarChar, 800) { Value = mediaAsset.Url },
                    new SqlParameter("@StoragePath", SqlDbType.NVarChar, 800) { Value = ToDbValue(mediaAsset.StoragePath) },
                    new SqlParameter("@FileName", SqlDbType.NVarChar, 255) { Value = ToDbValue(mediaAsset.FileName) },
                    new SqlParameter("@MediaType", SqlDbType.VarChar, 20) { Value = mediaAsset.MediaType },
                    new SqlParameter("@MimeType", SqlDbType.NVarChar, 100) { Value = ToDbValue(mediaAsset.MimeType) },
                    new SqlParameter("@FileSizeBytes", SqlDbType.BigInt) { Value = ToDbValue(mediaAsset.FileSizeBytes) },
                    new SqlParameter("@Width", SqlDbType.Int) { Value = ToDbValue(mediaAsset.Width) },
                    new SqlParameter("@Height", SqlDbType.Int) { Value = ToDbValue(mediaAsset.Height) },
                    new SqlParameter("@DurationSeconds", SqlDbType.Int) { Value = ToDbValue(mediaAsset.DurationSeconds) },
                    new SqlParameter("@AltText", SqlDbType.NVarChar, 300) { Value = ToDbValue(mediaAsset.AltText) },
                    new SqlParameter("@MetadataJson", SqlDbType.NVarChar) { Value = ToDbValue(mediaAsset.MetadataJson) },
                    new SqlParameter("@ContentHash", SqlDbType.VarBinary, 32) { Value = ToDbValue(mediaAsset.ContentHash) },
                    new SqlParameter("@CreatedBy", SqlDbType.BigInt) { Value = ToDbValue(mediaAsset.CreatedByUserId) },
                    mediaIdParameter
                ]);

                await command.ExecuteNonQueryAsync(cancellationToken);

                return mediaIdParameter.Value is DBNull
                    ? throw new InvalidOperationException("Media_MediaAsset_Insert did not return MediaId.")
                    : Convert.ToInt64(mediaIdParameter.Value);
            }
            catch (SqlException exception)
            {
                throw _sqlExceptionTranslator.Translate(exception);
            }
        }

        public async Task<MediaAsset?> GetByIdAsync(
            long mediaId,
            CancellationToken cancellationToken = default)
        {
            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateReadCommandAsync(MediaAssetSelectByIdProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@MediaId", SqlDbType.BigInt) { Value = mediaId });

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return null;
                    }

                    return MapMediaAsset(reader);
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

        public async Task<MediaAsset?> GetByPublicIdAsync(
            string publicId,
            CancellationToken cancellationToken = default)
        {
            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateReadCommandAsync(MediaAssetSelectByPublicIdProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@PublicId", SqlDbType.VarChar, 26) { Value = publicId });

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return null;
                    }

                    return MapMediaAsset(reader);
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

        public async Task<int> SoftDeleteAsync(
            long mediaId,
            long? deletedByUserId,
            DateTime? restoreUntil,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using SqlCommand command = CreateTransactionalCommand(MediaAssetSoftDeleteProc);

                SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };

                command.Parameters.AddRange(
                [
                    new SqlParameter("@MediaId", SqlDbType.BigInt) { Value = mediaId },
                    new SqlParameter("@DeletedBy", SqlDbType.BigInt) { Value = ToDbValue(deletedByUserId) },
                    new SqlParameter("@RestoreUntil", SqlDbType.DateTime2) { Value = ToDbValue(restoreUntil) },
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
            long mediaId,
            long? restoredByUserId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using SqlCommand command = CreateTransactionalCommand(MediaAssetRestoreProc);

                SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };

                command.Parameters.AddRange(
                [
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

        public async Task<PagedQueryResult<MediaAssetListResultItem>> SelectSkipAndTakeAsync(
            MediaAssetListQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateReadCommandAsync(MediaAssetSelectSkipAndTakeProc, cancellationToken);

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
                        new SqlParameter("@IsDeleted", SqlDbType.Bit) { Value = ToDbValue(query.IsDeleted) },
                        new SqlParameter("@MediaType", SqlDbType.VarChar, 20) { Value = ToDbValue(query.MediaType) },
                        new SqlParameter("@SortBy", SqlDbType.NVarChar, 50) { Value = query.SortBy },
                        new SqlParameter("@SortDirection", SqlDbType.NVarChar, 4) { Value = query.SortDirection }
                    ]);

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    List<MediaAssetListResultItem> items = [];

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        items.Add(new MediaAssetListResultItem
                        {
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
                            AltText = GetNullableString(reader, "AltText"),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                            IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                            Version = reader.GetInt32(reader.GetOrdinal("Version"))
                        });
                    }

                    int totalItems = await GetRecordCountInternalAsync(
                        query.IsDeleted,
                        query.MediaType,
                        cancellationToken);

                    return new PagedQueryResult<MediaAssetListResultItem>
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

        private async Task<int> GetRecordCountInternalAsync(
            bool? isDeleted,
            string? mediaType,
            CancellationToken cancellationToken)
        {
            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateReadCommandAsync(MediaAssetGetRecordCountProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.AddRange(
                    [
                        new SqlParameter("@IsDeleted", SqlDbType.Bit) { Value = ToDbValue(isDeleted) },
                        new SqlParameter("@MediaType", SqlDbType.VarChar, 20) { Value = ToDbValue(mediaType) }
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

        private static MediaAsset MapMediaAsset(SqlDataReader reader)
        {
            return MediaAsset.Rehydrate(
                mediaId: reader.GetInt64(reader.GetOrdinal("MediaId")),
                publicId: reader.GetString(reader.GetOrdinal("PublicId")),
                storageProvider: reader.GetString(reader.GetOrdinal("StorageProvider")),
                url: reader.GetString(reader.GetOrdinal("Url")),
                storagePath: GetNullableString(reader, "StoragePath"),
                fileName: GetNullableString(reader, "FileName"),
                mediaType: reader.GetString(reader.GetOrdinal("MediaType")),
                mimeType: GetNullableString(reader, "MimeType"),
                fileSizeBytes: GetNullableInt64(reader, "FileSizeBytes"),
                width: GetNullableInt32(reader, "Width"),
                height: GetNullableInt32(reader, "Height"),
                durationSeconds: GetNullableInt32(reader, "DurationSeconds"),
                altText: GetNullableString(reader, "AltText"),
                metadataJson: GetNullableString(reader, "MetadataJson"),
                contentHash: GetNullableBytes(reader, "ContentHash"),
                createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                updatedAt: reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                createdByUserId: GetNullableInt64(reader, "CreatedBy"),
                updatedByUserId: GetNullableInt64(reader, "UpdatedBy"),
                isDeleted: reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                deletedAt: GetNullableDateTime(reader, "DeletedAt"),
                deletedByUserId: GetNullableInt64(reader, "DeletedBy"),
                restoreUntil: GetNullableDateTime(reader, "RestoreUntil"),
                version: reader.GetInt32(reader.GetOrdinal("Version")));
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

        private static byte[]? GetNullableBytes(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);

            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            return (byte[])reader.GetValue(ordinal);
        }
    }
}