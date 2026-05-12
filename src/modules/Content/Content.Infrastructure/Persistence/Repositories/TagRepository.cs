using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Content.Application.Models.QueryModels;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;
using Content.Infrastructure.Persistence.Exceptions;
using Content.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Content.Infrastructure.Persistence.Repositories;

public sealed class TagRepository : ITagRepository
{
    private const string TagSelectByIdProc = "[content].[Content_Tag_SelectById]";
    private const string TagSelectAllProc = "[content].[Content_Tag_SelectAll]";
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

    public async Task<bool> ExistsByIdAsync(
        long tagId,
        CancellationToken cancellationToken = default)
    {
        if (tagId <= 0)
        {
            return false;
        }

        var tag = await GetByIdAsync(tagId, cancellationToken);
        return tag is not null && !tag.IsDeleted;
    }

    public async Task<bool> ExistsActiveByIdAsync(
        long tagId,
        CancellationToken cancellationToken = default)
    {
        if (tagId <= 0)
        {
            return false;
        }

        var tag = await GetByIdAsync(tagId, cancellationToken);
        return tag is not null && tag.CanBeAttachedToArticle;
    }

    public async Task<bool> ExistsByNameNormalizedAsync(
        string nameNormalized,
        long? excludingTagId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nameNormalized))
        {
            return false;
        }

        string normalized = nameNormalized.Trim();
        IReadOnlyList<Tag> tags = await GetAllAsync(
            includeDeleted: true,
            cancellationToken);

        return tags.Any(tag =>
            string.Equals(tag.NameNormalized, normalized, StringComparison.OrdinalIgnoreCase) &&
            (!excludingTagId.HasValue || tag.TagId != excludingTagId.Value));
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

    public async Task<IReadOnlyList<Tag>> GetAllAsync(
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(TagSelectAllProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@IncludeDeleted", SqlDbType.Bit) { Value = includeDeleted });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<Tag> tags = [];

                while (await reader.ReadAsync(cancellationToken))
                {
                    tags.Add(MapTag(reader));
                }

                return tags;
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

    public async Task<PagedQueryResult<TagListResultItem>> GetPagedAsync(
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
                        Value = ToDbValue(string.IsNullOrWhiteSpace(query.Keyword)
                            ? null
                            : query.Keyword.Trim())
                    });

                command.Parameters.Add(
                    new SqlParameter("@IsActive", SqlDbType.Bit)
                    {
                        Value = ToDbValue(query.IsActive)
                    });

                command.Parameters.Add(
                    new SqlParameter("@IsDeleted", SqlDbType.Bit) { Value = query.IsDeleted });

                command.Parameters.Add(
                    new SqlParameter("@SortBy", SqlDbType.NVarChar, 30) { Value = sortBy });

                command.Parameters.Add(
                    new SqlParameter("@SortDirection", SqlDbType.NVarChar, 4) { Value = sortDirection });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<TagListResultItem> items = [];
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
                        Description = GetNullableString(reader, "Description"),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        Version = reader.GetInt64(reader.GetOrdinal("Version"))
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

    public async Task<Tag?> InsertAsync(
        Tag tag,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tag);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(TagInsertProc);

            command.Parameters.Add(
                new SqlParameter("@PublicId", SqlDbType.Char, 26) { Value = tag.PublicId });

            command.Parameters.Add(
                new SqlParameter("@Name", SqlDbType.NVarChar, 150) { Value = tag.Name });

            command.Parameters.Add(
                new SqlParameter("@NameNormalized", SqlDbType.NVarChar, 150) { Value = tag.NameNormalized });

            command.Parameters.Add(
                new SqlParameter("@Description", SqlDbType.NVarChar, 500)
                {
                    Value = ToDbValue(tag.Description)
                });

            command.Parameters.Add(
                new SqlParameter("@IsActive", SqlDbType.Bit) { Value = tag.IsActive });

            command.Parameters.Add(
                new SqlParameter("@CreatedByUserId", SqlDbType.BigInt)
                {
                    Value = ToDbValue(tag.CreatedByUserId)
                });

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapTag(reader);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<Tag?> UpdateAsync(
        Tag tag,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tag);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(TagUpdateProc);

            command.Parameters.Add(
                new SqlParameter("@TagId", SqlDbType.BigInt) { Value = tag.TagId });

            command.Parameters.Add(
                new SqlParameter("@Name", SqlDbType.NVarChar, 150) { Value = tag.Name });

            command.Parameters.Add(
                new SqlParameter("@NameNormalized", SqlDbType.NVarChar, 150) { Value = tag.NameNormalized });

            command.Parameters.Add(
                new SqlParameter("@Description", SqlDbType.NVarChar, 500)
                {
                    Value = ToDbValue(tag.Description)
                });

            command.Parameters.Add(
                new SqlParameter("@IsActive", SqlDbType.Bit) { Value = tag.IsActive });

            command.Parameters.Add(
                new SqlParameter("@UpdatedByUserId", SqlDbType.BigInt)
                {
                    Value = ToDbValue(tag.UpdatedByUserId)
                });

            command.Parameters.Add(
                new SqlParameter("@ExpectedVersion", SqlDbType.BigInt) { Value = expectedVersion });

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapTag(reader);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<Tag?> SoftDeleteAsync(
        long tagId,
        long? deletedByUserId,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        if (tagId <= 0)
        {
            return null;
        }

        try
        {
            using SqlCommand command = CreateTransactionalCommand(TagSoftDeleteProc);

            command.Parameters.Add(
                new SqlParameter("@TagId", SqlDbType.BigInt) { Value = tagId });

            command.Parameters.Add(
                new SqlParameter("@DeletedByUserId", SqlDbType.BigInt)
                {
                    Value = ToDbValue(deletedByUserId)
                });

            command.Parameters.Add(
                new SqlParameter("@ExpectedVersion", SqlDbType.BigInt) { Value = expectedVersion });

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapTag(reader);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<Tag?> RestoreAsync(
        long tagId,
        long? updatedByUserId,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        if (tagId <= 0)
        {
            return null;
        }

        try
        {
            using SqlCommand command = CreateTransactionalCommand(TagRestoreProc);

            command.Parameters.Add(
                new SqlParameter("@TagId", SqlDbType.BigInt) { Value = tagId });

            command.Parameters.Add(
                new SqlParameter("@UpdatedByUserId", SqlDbType.BigInt)
                {
                    Value = ToDbValue(updatedByUserId)
                });

            command.Parameters.Add(
                new SqlParameter("@ExpectedVersion", SqlDbType.BigInt) { Value = expectedVersion });

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapTag(reader);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    private SqlCommand CreateTransactionalCommand(string storedProcedureName)
    {
        if (!_unitOfWork.HasActiveConnection || !_unitOfWork.HasActiveTransaction)
        {
            throw new InvalidOperationException(
                "Content write operation requires an active SQL transaction.");
        }

        SqlCommand command = _unitOfWork.Connection.CreateCommand();
        command.Transaction = _unitOfWork.Transaction;
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;
        return command;
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
            description: GetNullableString(reader, "Description"),
            isActive: reader.GetBoolean(reader.GetOrdinal("IsActive")),
            createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            updatedAt: reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            createdByUserId: GetNullableInt64(reader, "CreatedByUserId"),
            updatedByUserId: GetNullableInt64(reader, "UpdatedByUserId"),
            isDeleted: reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
            deletedAt: GetNullableDateTime(reader, "DeletedAt"),
            deletedByUserId: GetNullableInt64(reader, "DeletedByUserId"),
            version: reader.GetInt64(reader.GetOrdinal("Version")));
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

    private static object ToDbValue(object? value) => value ?? DBNull.Value;

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

    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
