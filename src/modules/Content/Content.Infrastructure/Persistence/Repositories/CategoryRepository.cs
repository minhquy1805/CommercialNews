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
    public sealed class CategoryRepository : ICategoryRepository
    {
        private const string CategorySelectByIdProc = "[content].[Content_Category_SelectById]";
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
            return category is not null && !category.IsDeleted && category.IsActive;
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
            finally
            {
                if (ownedConnection is not null)
                {
                    await ownedConnection.DisposeAsync();
                }
            }
        }

        public async Task<PagedQueryResult<CategoryListResultItem>> SelectSkipAndTakeAsync(
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
                            Value = string.IsNullOrWhiteSpace(query.Keyword)
                                ? DBNull.Value
                                : query.Keyword.Trim()
                        });

                    command.Parameters.Add(
                        new SqlParameter("@ParentCategoryId", SqlDbType.BigInt)
                        {
                            Value = query.ParentCategoryId.HasValue
                                ? query.ParentCategoryId.Value
                                : DBNull.Value
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

                    List<CategoryListResultItem> items = new();
                    int totalItems = 0;

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        if (totalItems == 0)
                        {
                            totalItems = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                        }

                        items.Add(new CategoryListResultItem
                        {
                            CategoryId = reader.GetInt64(reader.GetOrdinal("CategoryId")),
                            PublicId = reader.GetString(reader.GetOrdinal("PublicId")),
                            ParentCategoryId = ReadNullableInt64(reader, "ParentCategoryId"),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            NameNormalized = reader.GetString(reader.GetOrdinal("NameNormalized")),
                            Description = ReadNullableString(reader, "Description"),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                            DisplayOrder = reader.GetInt32(reader.GetOrdinal("DisplayOrder")),
                            IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                            Version = reader.GetInt32(reader.GetOrdinal("Version"))
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

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(CategoryInsertProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@PublicId", SqlDbType.Char, 26) { Value = category.PublicId });

                    command.Parameters.Add(
                        new SqlParameter("@ParentCategoryId", SqlDbType.BigInt)
                        {
                            Value = category.ParentCategoryId.HasValue
                                ? category.ParentCategoryId.Value
                                : DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@Name", SqlDbType.NVarChar, 200) { Value = category.Name });

                    command.Parameters.Add(
                        new SqlParameter("@NameNormalized", SqlDbType.NVarChar, 200) { Value = category.NameNormalized });

                    command.Parameters.Add(
                        new SqlParameter("@Description", SqlDbType.NVarChar, 1000)
                        {
                            Value = category.Description is not null
                                ? category.Description
                                : DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@IsActive", SqlDbType.Bit) { Value = category.IsActive });

                    command.Parameters.Add(
                        new SqlParameter("@DisplayOrder", SqlDbType.Int) { Value = category.DisplayOrder });

                    command.Parameters.Add(
                        new SqlParameter("@CreatedByUserId", SqlDbType.BigInt)
                        {
                            Value = category.CreatedByUserId.HasValue
                                ? category.CreatedByUserId.Value
                                : DBNull.Value
                        });

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return null;
                    }

                    return MapCategory(reader);
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

        public async Task<Category?> UpdateAsync(
            Category category,
            int expectedVersion,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(category);

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(CategoryUpdateProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@CategoryId", SqlDbType.BigInt) { Value = category.CategoryId });

                    command.Parameters.Add(
                        new SqlParameter("@ParentCategoryId", SqlDbType.BigInt)
                        {
                            Value = category.ParentCategoryId.HasValue
                                ? category.ParentCategoryId.Value
                                : DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@Name", SqlDbType.NVarChar, 200) { Value = category.Name });

                    command.Parameters.Add(
                        new SqlParameter("@NameNormalized", SqlDbType.NVarChar, 200) { Value = category.NameNormalized });

                    command.Parameters.Add(
                        new SqlParameter("@Description", SqlDbType.NVarChar, 1000)
                        {
                            Value = category.Description is not null
                                ? category.Description
                                : DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@IsActive", SqlDbType.Bit) { Value = category.IsActive });

                    command.Parameters.Add(
                        new SqlParameter("@DisplayOrder", SqlDbType.Int) { Value = category.DisplayOrder });

                    command.Parameters.Add(
                        new SqlParameter("@UpdatedByUserId", SqlDbType.BigInt)
                        {
                            Value = category.UpdatedByUserId.HasValue
                                ? category.UpdatedByUserId.Value
                                : DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@ExpectedVersion", SqlDbType.Int) { Value = expectedVersion });

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

        public async Task<Category?> SoftDeleteAsync(
            long categoryId,
            long? deletedByUserId,
            int expectedVersion,
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
                    await CreateCommandAsync(CategorySoftDeleteProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@CategoryId", SqlDbType.BigInt) { Value = categoryId });

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

        public async Task<Category?> RestoreAsync(
            long categoryId,
            long? updatedByUserId,
            int expectedVersion,
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
                    await CreateCommandAsync(CategoryRestoreProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@CategoryId", SqlDbType.BigInt) { Value = categoryId });

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

                    return MapCategory(reader);
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
                parentCategoryId: ReadNullableInt64(reader, "ParentCategoryId"),
                name: reader.GetString(reader.GetOrdinal("Name")),
                nameNormalized: reader.GetString(reader.GetOrdinal("NameNormalized")),
                description: ReadNullableString(reader, "Description"),
                isActive: reader.GetBoolean(reader.GetOrdinal("IsActive")),
                displayOrder: reader.GetInt32(reader.GetOrdinal("DisplayOrder")),
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