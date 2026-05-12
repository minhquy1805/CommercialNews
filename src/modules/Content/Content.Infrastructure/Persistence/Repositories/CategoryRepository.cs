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

public sealed class CategoryRepository : ICategoryRepository
{
    private const string CategorySelectByIdProc = "[content].[Content_Category_SelectById]";
    private const string CategorySelectAllProc = "[content].[Content_Category_SelectAll]";
    private const string CategorySelectSkipAndTakeProc = "[content].[Content_Category_SelectSkipAndTake]";
    private const string CategoryInsertProc = "[content].[Content_Category_Insert]";
    private const string CategoryUpdateProc = "[content].[Content_Category_Update]";
    private const string CategorySoftDeleteProc = "[content].[Content_Category_SoftDelete]";
    private const string CategoryRestoreProc = "[content].[Content_Category_Restore]";
    private readonly ContentSqlExceptionTranslator _sqlExceptionTranslator;

    private readonly ContentUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public CategoryRepository(
        ContentUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        ContentSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<bool> ExistsByIdAsync(
        long categoryId,
        CancellationToken cancellationToken = default)
    {
        if (categoryId <= 0)
        {
            return false;
        }

        var category = await GetByIdAsync(categoryId, cancellationToken);
        return category is not null && !category.IsDeleted;
    }

    public async Task<bool> ExistsActiveByIdAsync(
        long categoryId,
        CancellationToken cancellationToken = default)
    {
        if (categoryId <= 0)
        {
            return false;
        }

        var category = await GetByIdAsync(categoryId, cancellationToken);
        return category is not null && category.CanBeUsedByArticle;
    }

    public async Task<bool> ExistsByNameNormalizedAsync(
        string nameNormalized,
        long? excludingCategoryId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nameNormalized))
        {
            return false;
        }

        string normalized = nameNormalized.Trim();
        IReadOnlyList<Category> categories = await GetAllAsync(
            includeDeleted: true,
            cancellationToken);

        return categories.Any(category =>
            string.Equals(category.NameNormalized, normalized, StringComparison.OrdinalIgnoreCase) &&
            (!excludingCategoryId.HasValue || category.CategoryId != excludingCategoryId.Value));
    }

    public async Task<Category?> GetByIdAsync(
        long categoryId,
        CancellationToken cancellationToken = default)
    {
        if (categoryId <= 0)
        {
            return null;
        }

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(CategorySelectByIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@CategoryId", SqlDbType.BigInt) { Value = categoryId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapCategory(reader);
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

    public async Task<IReadOnlyList<Category>> GetAllAsync(
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(CategorySelectAllProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@IncludeDeleted", SqlDbType.Bit) { Value = includeDeleted });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<Category> categories = [];

                while (await reader.ReadAsync(cancellationToken))
                {
                    categories.Add(MapCategory(reader));
                }

                return categories;
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

    public async Task<PagedQueryResult<CategoryListResultItem>> GetPagedAsync(
        CategoryListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(CategorySelectSkipAndTakeProc, cancellationToken);

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
                    new SqlParameter("@Keyword", SqlDbType.NVarChar, 200)
                    {
                        Value = ToDbValue(string.IsNullOrWhiteSpace(query.Keyword)
                            ? null
                            : query.Keyword.Trim())
                    });

                command.Parameters.Add(
                    new SqlParameter("@ParentCategoryId", SqlDbType.BigInt)
                    {
                        Value = ToDbValue(query.ParentCategoryId)
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

                List<CategoryListResultItem> items = [];
                int totalItems = 0;

                while (await reader.ReadAsync(cancellationToken))
                {
                    if (totalItems == 0 && !reader.IsDBNull(reader.GetOrdinal("TotalCount")))
                    {
                        totalItems = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                    }

                    items.Add(new CategoryListResultItem
                    {
                        CategoryId = reader.GetInt64(reader.GetOrdinal("CategoryId")),
                        PublicId = reader.GetString(reader.GetOrdinal("PublicId")),
                        ParentCategoryId = GetNullableInt64(reader, "ParentCategoryId"),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        NameNormalized = reader.GetString(reader.GetOrdinal("NameNormalized")),
                        Description = GetNullableString(reader, "Description"),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        DisplayOrder = reader.GetInt32(reader.GetOrdinal("DisplayOrder")),
                        IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        Version = reader.GetInt64(reader.GetOrdinal("Version"))
                    });
                }

                return new PagedQueryResult<CategoryListResultItem>
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

    public async Task<Category?> InsertAsync(
        Category category,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(category);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(CategoryInsertProc);

            command.Parameters.Add(
                new SqlParameter("@PublicId", SqlDbType.Char, 26) { Value = category.PublicId });

            command.Parameters.Add(
                new SqlParameter("@ParentCategoryId", SqlDbType.BigInt)
                {
                    Value = ToDbValue(category.ParentCategoryId)
                });

            command.Parameters.Add(
                new SqlParameter("@Name", SqlDbType.NVarChar, 200) { Value = category.Name });

            command.Parameters.Add(
                new SqlParameter("@NameNormalized", SqlDbType.NVarChar, 200) { Value = category.NameNormalized });

            command.Parameters.Add(
                new SqlParameter("@Description", SqlDbType.NVarChar, 1000)
                {
                    Value = ToDbValue(category.Description)
                });

            command.Parameters.Add(
                new SqlParameter("@IsActive", SqlDbType.Bit) { Value = category.IsActive });

            command.Parameters.Add(
                new SqlParameter("@DisplayOrder", SqlDbType.Int) { Value = category.DisplayOrder });

            command.Parameters.Add(
                new SqlParameter("@CreatedByUserId", SqlDbType.BigInt)
                {
                    Value = ToDbValue(category.CreatedByUserId)
                });

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapCategory(reader);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<Category?> UpdateAsync(
        Category category,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(category);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(CategoryUpdateProc);

            command.Parameters.Add(
                new SqlParameter("@CategoryId", SqlDbType.BigInt) { Value = category.CategoryId });

            command.Parameters.Add(
                new SqlParameter("@ParentCategoryId", SqlDbType.BigInt)
                {
                    Value = ToDbValue(category.ParentCategoryId)
                });

            command.Parameters.Add(
                new SqlParameter("@Name", SqlDbType.NVarChar, 200) { Value = category.Name });

            command.Parameters.Add(
                new SqlParameter("@NameNormalized", SqlDbType.NVarChar, 200) { Value = category.NameNormalized });

            command.Parameters.Add(
                new SqlParameter("@Description", SqlDbType.NVarChar, 1000)
                {
                    Value = ToDbValue(category.Description)
                });

            command.Parameters.Add(
                new SqlParameter("@IsActive", SqlDbType.Bit) { Value = category.IsActive });

            command.Parameters.Add(
                new SqlParameter("@DisplayOrder", SqlDbType.Int) { Value = category.DisplayOrder });

            command.Parameters.Add(
                new SqlParameter("@UpdatedByUserId", SqlDbType.BigInt)
                {
                    Value = ToDbValue(category.UpdatedByUserId)
                });

            command.Parameters.Add(
                new SqlParameter("@ExpectedVersion", SqlDbType.BigInt) { Value = expectedVersion });

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapCategory(reader);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<Category?> SoftDeleteAsync(
        long categoryId,
        long? deletedByUserId,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        if (categoryId <= 0)
        {
            return null;
        }

        try
        {
            using SqlCommand command = CreateTransactionalCommand(CategorySoftDeleteProc);

            command.Parameters.Add(
                new SqlParameter("@CategoryId", SqlDbType.BigInt) { Value = categoryId });

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

            return MapCategory(reader);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<Category?> RestoreAsync(
        long categoryId,
        long? updatedByUserId,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        if (categoryId <= 0)
        {
            return null;
        }

        try
        {
            using SqlCommand command = CreateTransactionalCommand(CategoryRestoreProc);

            command.Parameters.Add(
                new SqlParameter("@CategoryId", SqlDbType.BigInt) { Value = categoryId });

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

            return MapCategory(reader);
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

    private static Category MapCategory(SqlDataReader reader)
    {
        return Category.Rehydrate(
            categoryId: reader.GetInt64(reader.GetOrdinal("CategoryId")),
            publicId: reader.GetString(reader.GetOrdinal("PublicId")),
            parentCategoryId: GetNullableInt64(reader, "ParentCategoryId"),
            name: reader.GetString(reader.GetOrdinal("Name")),
            nameNormalized: reader.GetString(reader.GetOrdinal("NameNormalized")),
            description: GetNullableString(reader, "Description"),
            isActive: reader.GetBoolean(reader.GetOrdinal("IsActive")),
            displayOrder: reader.GetInt32(reader.GetOrdinal("DisplayOrder")),
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
            return ("DisplayOrder", "ASC");
        }

        return sort.Trim() switch
        {
            "displayOrder" => ("DisplayOrder", "ASC"),
            "-displayOrder" => ("DisplayOrder", "DESC"),
            "name" => ("Name", "ASC"),
            "-name" => ("Name", "DESC"),
            "createdAt" => ("CreatedAt", "ASC"),
            "-createdAt" => ("CreatedAt", "DESC"),
            "updatedAt" => ("UpdatedAt", "ASC"),
            "-updatedAt" => ("UpdatedAt", "DESC"),
            _ => ("DisplayOrder", "ASC")
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