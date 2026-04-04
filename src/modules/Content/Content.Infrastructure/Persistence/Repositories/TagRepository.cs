using System.Data;
using CommercialNews.BuildingBlocks.Contracts.Common;
using CommercialNews.BuildingBlocks.Persistence.Sql;
using Content.Application.Models.QueryModels;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;
using Content.Infrastructure.Persistence.Exceptions;
using Content.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Content.Infrastructure.Persistence.Repositories
{
    public sealed class TagRepository : ITagRepository
    {
        private const string TagSelectByIdProc = "[content].[Content_Tag_SelectById]";
        private const string TagSelectSkipAndTakeProc = "[content].[Content_Tag_SelectSkipAndTake]";
        private const string TagInsertProc = "[content].[Content_Tag_Insert]";
        private const string TagUpdateProc = "[content].[Content_Tag_Update]";
        private const string TagSoftDeleteProc = "[content].[Content_Tag_SoftDelete]";
        private const string TagRestoreProc = "[content].[Content_Tag_Restore]";

        private readonly ContentUnitOfWork _unitOfWork;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;
        private readonly ContentSqlExceptionTranslator _sqlExceptionTranslator;

        public TagRepository(
            ContentUnitOfWork unitOfWork,
            ISqlConnectionFactory sqlConnectionFactory,
            ContentSqlExceptionTranslator sqlExceptionTranslator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
            _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
        }

        public async Task<Tag?> GetByIdAsync(
            long tagId,
            CancellationToken cancellationToken = default)
        {
            if (tagId <= 0)
            {
                return null;
            }

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(TagSelectByIdProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@TagId", SqlDbType.BigInt) { Value = tagId });

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return null;
                    }

                    return MapTag(reader);
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

        public async Task<PagedQueryResult<TagListResultItem>> SelectSkipAndTakeAsync(
            TagListQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(TagSelectSkipAndTakeProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    int safePage = query.Page <= 0 ? 1 : query.Page;
                    int safePageSize = query.PageSize <= 0 ? 20 : query.PageSize;
                    int skip = (safePage - 1) * safePageSize;

                    (string sortBy, string sortDirection) = ParseSort(query.Sort);

                    command.Parameters.Add(
                        new SqlParameter("@Skip", SqlDbType.Int) { Value = skip });

                    command.Parameters.Add(
                        new SqlParameter("@Take", SqlDbType.Int) { Value = safePageSize });

                    command.Parameters.Add(
                        new SqlParameter("@Keyword", SqlDbType.NVarChar, 150)
                        {
                            Value = string.IsNullOrWhiteSpace(query.Keyword)
                                ? DBNull.Value
                                : query.Keyword.Trim()
                        });

                    command.Parameters.Add(
                        new SqlParameter("@IsActive", SqlDbType.Bit)
                        {
                            Value = query.IsActive.HasValue
                                ? query.IsActive.Value
                                : DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@IsDeleted", SqlDbType.Bit) { Value = query.IsDeleted });

                    command.Parameters.Add(
                        new SqlParameter("@SortBy", SqlDbType.NVarChar, 30) { Value = sortBy });

                    command.Parameters.Add(
                        new SqlParameter("@SortDirection", SqlDbType.NVarChar, 4) { Value = sortDirection });

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    List<TagListResultItem> items = new();
                    int totalItems = 0;

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        if (totalItems == 0 && !reader.IsDBNull(reader.GetOrdinal("TotalCount")))
                        {
                            totalItems = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                        }

                        items.Add(new TagListResultItem
                        {
                            TagId = reader.GetInt64(reader.GetOrdinal("TagId")),
                            PublicId = reader.GetString(reader.GetOrdinal("PublicId")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            NameNormalized = reader.GetString(reader.GetOrdinal("NameNormalized")),
                            Description = ReadNullableString(reader, "Description"),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                            IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                            Version = reader.GetInt32(reader.GetOrdinal("Version"))
                        });
                    }

                    return new PagedQueryResult<TagListResultItem>
                    {
                        Items = items,
                        Page = safePage,
                        PageSize = safePageSize,
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

        public async Task<Tag?> InsertAsync(
            Tag tag,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(tag);

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(TagInsertProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@PublicId", SqlDbType.Char, 26) { Value = tag.PublicId });

                    command.Parameters.Add(
                        new SqlParameter("@Name", SqlDbType.NVarChar, 150) { Value = tag.Name });

                    command.Parameters.Add(
                        new SqlParameter("@NameNormalized", SqlDbType.NVarChar, 150) { Value = tag.NameNormalized });

                    command.Parameters.Add(
                        new SqlParameter("@Description", SqlDbType.NVarChar, 500)
                        {
                            Value = tag.Description is not null
                                ? tag.Description
                                : DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@IsActive", SqlDbType.Bit) { Value = tag.IsActive });

                    command.Parameters.Add(
                        new SqlParameter("@CreatedByUserId", SqlDbType.BigInt)
                        {
                            Value = tag.CreatedByUserId.HasValue
                                ? tag.CreatedByUserId.Value
                                : DBNull.Value
                        });

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return null;
                    }

                    return MapTag(reader);
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

        public async Task<Tag?> UpdateAsync(
            Tag tag,
            int expectedVersion,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(tag);

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(TagUpdateProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@TagId", SqlDbType.BigInt) { Value = tag.TagId });

                    command.Parameters.Add(
                        new SqlParameter("@Name", SqlDbType.NVarChar, 150) { Value = tag.Name });

                    command.Parameters.Add(
                        new SqlParameter("@NameNormalized", SqlDbType.NVarChar, 150) { Value = tag.NameNormalized });

                    command.Parameters.Add(
                        new SqlParameter("@Description", SqlDbType.NVarChar, 500)
                        {
                            Value = tag.Description is not null
                                ? tag.Description
                                : DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@IsActive", SqlDbType.Bit) { Value = tag.IsActive });

                    command.Parameters.Add(
                        new SqlParameter("@UpdatedByUserId", SqlDbType.BigInt)
                        {
                            Value = tag.UpdatedByUserId.HasValue
                                ? tag.UpdatedByUserId.Value
                                : DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@ExpectedVersion", SqlDbType.Int) { Value = expectedVersion });

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return null;
                    }

                    return MapTag(reader);
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

        public async Task<Tag?> SoftDeleteAsync(
            long tagId,
            long? deletedByUserId,
            int expectedVersion,
            CancellationToken cancellationToken = default)
        {
            if (tagId <= 0)
            {
                return null;
            }

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(TagSoftDeleteProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@TagId", SqlDbType.BigInt) { Value = tagId });

                    command.Parameters.Add(
                        new SqlParameter("@DeletedByUserId", SqlDbType.BigInt)
                        {
                            Value = deletedByUserId.HasValue
                                ? deletedByUserId.Value
                                : DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@ExpectedVersion", SqlDbType.Int) { Value = expectedVersion });

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return null;
                    }

                    return MapTag(reader);
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

        public async Task<Tag?> RestoreAsync(
            long tagId,
            long? updatedByUserId,
            int expectedVersion,
            CancellationToken cancellationToken = default)
        {
            if (tagId <= 0)
            {
                return null;
            }

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(TagRestoreProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@TagId", SqlDbType.BigInt) { Value = tagId });

                    command.Parameters.Add(
                        new SqlParameter("@UpdatedByUserId", SqlDbType.BigInt)
                        {
                            Value = updatedByUserId.HasValue
                                ? updatedByUserId.Value
                                : DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@ExpectedVersion", SqlDbType.Int) { Value = expectedVersion });

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return null;
                    }

                    return MapTag(reader);
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

        private async Task<(SqlCommand Command, SqlConnection? OwnedConnection)> CreateCommandAsync(
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

        private static Tag MapTag(SqlDataReader reader)
        {
            return Tag.Rehydrate(
                tagId: reader.GetInt64(reader.GetOrdinal("TagId")),
                publicId: reader.GetString(reader.GetOrdinal("PublicId")),
                name: reader.GetString(reader.GetOrdinal("Name")),
                nameNormalized: reader.GetString(reader.GetOrdinal("NameNormalized")),
                description: ReadNullableString(reader, "Description"),
                isActive: reader.GetBoolean(reader.GetOrdinal("IsActive")),
                createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                updatedAt: reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                createdByUserId: ReadNullableInt64(reader, "CreatedByUserId"),
                updatedByUserId: ReadNullableInt64(reader, "UpdatedByUserId"),
                isDeleted: reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                deletedAt: ReadNullableDateTime(reader, "DeletedAt"),
                deletedByUserId: ReadNullableInt64(reader, "DeletedByUserId"),
                version: reader.GetInt32(reader.GetOrdinal("Version")));
        }

        private static (string SortBy, string SortDirection) ParseSort(string? sort)
        {
            if (string.IsNullOrWhiteSpace(sort))
            {
                return ("Name", "ASC");
            }

            return sort.Trim() switch
            {
                "name" => ("Name", "ASC"),
                "-name" => ("Name", "DESC"),
                "createdAt" => ("CreatedAt", "ASC"),
                "-createdAt" => ("CreatedAt", "DESC"),
                "updatedAt" => ("UpdatedAt", "ASC"),
                "-updatedAt" => ("UpdatedAt", "DESC"),
                _ => ("Name", "ASC")
            };
        }

        private static long? ReadNullableInt64(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
        }

        private static DateTime? ReadNullableDateTime(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
        }

        private static string? ReadNullableString(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
    }
}