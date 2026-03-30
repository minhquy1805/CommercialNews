using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql;
using Content.Application.Models.QueryModels;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;
using Content.Infrastructure.Persistence.Exceptions;
using Content.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Content.Infrastructure.Persistence.Repositories
{
    public sealed class ArticleRepository : IArticleRepository
    {
        private const string ArticleInsertProc = "[content].[Content_Article_Insert]";
        private const string ArticleSelectByIdProc = "[content].[Content_Article_SelectById]";
        private const string ArticleSelectByPublicIdProc = "[content].[Content_Article_SelectByPublicId]";
        private const string ArticleSelectSkipAndTakeProc = "[content].[Content_Article_SelectSkipAndTake]";
        private const string ArticleUpdateProc = "[content].[Content_Article_Update]";

        private const string ArticlePublishProc = "[content].[Content_Article_Publish]";
        private const string ArticleUnpublishProc = "[content].[Content_Article_Unpublish]";
        private const string ArticleArchiveProc = "[content].[Content_Article_Archive]";
        private const string ArticleRestoreProc = "[content].[Content_Article_Restore]";
        private const string ArticleDeleteProc = "[content].[Content_Article_Delete]";

        private readonly ContentUnitOfWork _unitOfWork;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;
        private readonly ContentSqlExceptionTranslator _sqlExceptionTranslator;

        public ArticleRepository(
            ContentUnitOfWork unitOfWork,
            ISqlConnectionFactory sqlConnectionFactory,
            ContentSqlExceptionTranslator sqlExceptionTranslator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
            _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
        }

        public async Task<(long ArticleId, int Version)> InsertAsync(
            Article article,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(article);

            try
            {
                using SqlCommand command = CreateTransactionalCommand(ArticleInsertProc);

                command.Parameters.AddRange(
                [
                    new SqlParameter("@PublicId", SqlDbType.Char, 26) { Value = article.PublicId },
                    new SqlParameter("@CategoryId", SqlDbType.BigInt) { Value = ToDbValue(article.CategoryId) },
                    new SqlParameter("@AuthorUserId", SqlDbType.BigInt) { Value = article.AuthorUserId },
                    new SqlParameter("@Title", SqlDbType.NVarChar, 300) { Value = article.Title },
                    new SqlParameter("@Summary", SqlDbType.NVarChar, 2000) { Value = ToDbValue(article.Summary) },
                    new SqlParameter("@Content", SqlDbType.NVarChar) { Value = article.Body },
                    new SqlParameter("@Status", SqlDbType.NVarChar, 30) { Value = article.Status },
                    new SqlParameter("@PublishedAt", SqlDbType.DateTime2) { Value = ToDbValue(article.PublishedAt) },
                    new SqlParameter("@UnpublishedAt", SqlDbType.DateTime2) { Value = ToDbValue(article.UnpublishedAt) },
                    new SqlParameter("@ArchivedAt", SqlDbType.DateTime2) { Value = ToDbValue(article.ArchivedAt) },
                    new SqlParameter("@CoverMediaId", SqlDbType.BigInt) { Value = ToDbValue(article.CoverMediaId) },
                    new SqlParameter("@CreatedByUserId", SqlDbType.BigInt) { Value = ToDbValue(article.CreatedByUserId) }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    throw new InvalidOperationException("Content_Article_Insert did not return the inserted article row.");
                }

                long articleId = reader.GetInt64(reader.GetOrdinal("ArticleId"));
                int version = reader.GetInt32(reader.GetOrdinal("Version"));

                return (articleId, version);
            }
            catch (SqlException exception)
            {
                throw _sqlExceptionTranslator.Translate(exception);
            }
        }

        public async Task<Article?> GetByIdAsync(
            long articleId,
            CancellationToken cancellationToken = default)
        {
            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateReadCommandAsync(ArticleSelectByIdProc, cancellationToken);

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

                    return MapArticle(reader);
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

        public async Task<Article?> GetByPublicIdAsync(
            string publicId,
            CancellationToken cancellationToken = default)
        {
            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateReadCommandAsync(ArticleSelectByPublicIdProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@PublicId", SqlDbType.Char, 26) { Value = publicId });

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return null;
                    }

                    return MapArticle(reader);
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

        public async Task<PagedQueryResult<ArticleListResultItem>> GetPagedAsync(
            ArticleListQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateReadCommandAsync(ArticleSelectSkipAndTakeProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    int skip = (query.Page - 1) * query.PageSize;
                    int take = query.PageSize;

                    string? keyword = null;
                    string? sortBy = "UpdatedAt";
                    string? sortDirection = "DESC";

                    if (!string.IsNullOrWhiteSpace(query.Sort))
                    {
                        string raw = query.Sort.Trim();

                        if (raw.StartsWith("-"))
                        {
                            sortDirection = "DESC";
                            raw = raw[1..];
                        }
                        else
                        {
                            sortDirection = "ASC";
                        }

                        sortBy = raw switch
                        {
                            "createdAt" => "CreatedAt",
                            "updatedAt" => "UpdatedAt",
                            "publishedAt" => "PublishedAt",
                            "title" => "Title",
                            _ => "UpdatedAt"
                        };
                    }

                    command.Parameters.AddRange(
                    [
                        new SqlParameter("@Skip", SqlDbType.Int) { Value = skip },
                        new SqlParameter("@Take", SqlDbType.Int) { Value = take },
                        new SqlParameter("@Keyword", SqlDbType.NVarChar, 300) { Value = ToDbValue(keyword) },
                        new SqlParameter("@Status", SqlDbType.NVarChar, 30) { Value = ToDbValue(query.Status) },
                        new SqlParameter("@CategoryId", SqlDbType.BigInt) { Value = ToDbValue(query.CategoryId) },
                        new SqlParameter("@AuthorUserId", SqlDbType.BigInt) { Value = DBNull.Value },
                        new SqlParameter("@IsDeleted", SqlDbType.Bit) { Value = false },
                        new SqlParameter("@SortBy", SqlDbType.NVarChar, 30) { Value = sortBy! },
                        new SqlParameter("@SortDirection", SqlDbType.NVarChar, 4) { Value = sortDirection! }
                    ]);

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    List<ArticleListResultItem> items = [];
                    int totalItems = 0;

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        if (totalItems == 0 && !reader.IsDBNull(reader.GetOrdinal("TotalCount")))
                        {
                            totalItems = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                        }

                        items.Add(new ArticleListResultItem
                        {
                            ArticleId = reader.GetInt64(reader.GetOrdinal("ArticleId")),
                            PublicId = reader.GetString(reader.GetOrdinal("PublicId")),
                            Title = reader.GetString(reader.GetOrdinal("Title")),
                            Summary = GetNullableString(reader, "Summary"),
                            Status = reader.GetString(reader.GetOrdinal("Status")),
                            AuthorUserId = reader.GetInt64(reader.GetOrdinal("AuthorUserId")),
                            CategoryId = GetNullableInt64(reader, "CategoryId"),
                            CoverMediaId = GetNullableInt64(reader, "CoverMediaId"),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                            PublishedAt = GetNullableDateTime(reader, "PublishedAt"),
                            Version = reader.GetInt32(reader.GetOrdinal("Version"))
                        });
                    }

                    return new PagedQueryResult<ArticleListResultItem>
                    {
                        Items = items,
                        Page = query.Page,
                        PageSize = query.PageSize,
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

        public async Task<bool> UpdateAsync(
            Article article,
            int expectedVersion,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(article);

            try
            {
                using SqlCommand command = CreateTransactionalCommand(ArticleUpdateProc);

                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = article.ArticleId },
                    new SqlParameter("@CategoryId", SqlDbType.BigInt) { Value = ToDbValue(article.CategoryId) },
                    new SqlParameter("@Title", SqlDbType.NVarChar, 300) { Value = article.Title },
                    new SqlParameter("@Summary", SqlDbType.NVarChar, 2000) { Value = ToDbValue(article.Summary) },
                    new SqlParameter("@Content", SqlDbType.NVarChar) { Value = article.Body },
                    new SqlParameter("@CoverMediaId", SqlDbType.BigInt) { Value = ToDbValue(article.CoverMediaId) },
                    new SqlParameter("@UpdatedByUserId", SqlDbType.BigInt) { Value = ToDbValue(article.UpdatedByUserId) },
                    new SqlParameter("@ExpectedVersion", SqlDbType.Int) { Value = expectedVersion }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                return await reader.ReadAsync(cancellationToken);
            }
            catch (SqlException exception)
            {
                throw _sqlExceptionTranslator.Translate(exception);
            }
        }

        public async Task<Article?> PublishAsync(
            long articleId,
            long? actorUserId,
            int expectedVersion,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using SqlCommand command = CreateTransactionalCommand(ArticlePublishProc);

                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                    new SqlParameter("@ActorUserId", SqlDbType.BigInt) { Value = ToDbValue(actorUserId) },
                    new SqlParameter("@ExpectedVersion", SqlDbType.Int) { Value = expectedVersion }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapArticle(reader);
            }
            catch (SqlException exception)
            {
                throw _sqlExceptionTranslator.Translate(exception);
            }
        }

        public async Task<Article?> UnpublishAsync(
            long articleId,
            long? actorUserId,
            int expectedVersion,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using SqlCommand command = CreateTransactionalCommand(ArticleUnpublishProc);

                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                    new SqlParameter("@ActorUserId", SqlDbType.BigInt) { Value = ToDbValue(actorUserId) },
                    new SqlParameter("@ExpectedVersion", SqlDbType.Int) { Value = expectedVersion }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapArticle(reader);
            }
            catch (SqlException exception)
            {
                throw _sqlExceptionTranslator.Translate(exception);
            }
        }

        public async Task<Article?> ArchiveAsync(
            long articleId,
            long? actorUserId,
            int expectedVersion,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using SqlCommand command = CreateTransactionalCommand(ArticleArchiveProc);

                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                    new SqlParameter("@ActorUserId", SqlDbType.BigInt) { Value = ToDbValue(actorUserId) },
                    new SqlParameter("@ExpectedVersion", SqlDbType.Int) { Value = expectedVersion }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapArticle(reader);
            }
            catch (SqlException exception)
            {
                throw _sqlExceptionTranslator.Translate(exception);
            }
        }

        public async Task<Article?> RestoreAsync(
            long articleId,
            long? actorUserId,
            int expectedVersion,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using SqlCommand command = CreateTransactionalCommand(ArticleRestoreProc);

                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                    new SqlParameter("@ActorUserId", SqlDbType.BigInt) { Value = ToDbValue(actorUserId) },
                    new SqlParameter("@ExpectedVersion", SqlDbType.Int) { Value = expectedVersion }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapArticle(reader);
            }
            catch (SqlException exception)
            {
                throw _sqlExceptionTranslator.Translate(exception);
            }
        }

        public async Task<Article?> DeleteAsync(
            long articleId,
            long? actorUserId,
            int expectedVersion,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using SqlCommand command = CreateTransactionalCommand(ArticleDeleteProc);

                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                    new SqlParameter("@DeletedByUserId", SqlDbType.BigInt) { Value = ToDbValue(actorUserId) },
                    new SqlParameter("@ExpectedVersion", SqlDbType.Int) { Value = expectedVersion }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapArticle(reader);
            }
            catch (SqlException exception)
            {
                throw _sqlExceptionTranslator.Translate(exception);
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

        private static Article MapArticle(SqlDataReader reader)
        {
            return Article.Rehydrate(
                articleId: reader.GetInt64(reader.GetOrdinal("ArticleId")),
                publicId: reader.GetString(reader.GetOrdinal("PublicId")),
                title: reader.GetString(reader.GetOrdinal("Title")),
                summary: GetNullableString(reader, "Summary"),
                body: reader.GetString(reader.GetOrdinal("Content")),
                status: reader.GetString(reader.GetOrdinal("Status")),
                authorUserId: reader.GetInt64(reader.GetOrdinal("AuthorUserId")),
                categoryId: GetNullableInt64(reader, "CategoryId"),
                coverMediaId: GetNullableInt64(reader, "CoverMediaId"),
                createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                updatedAt: reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                publishedAt: GetNullableDateTime(reader, "PublishedAt"),
                unpublishedAt: GetNullableDateTime(reader, "UnpublishedAt"),
                archivedAt: GetNullableDateTime(reader, "ArchivedAt"),
                createdByUserId: GetNullableInt64(reader, "CreatedByUserId"),
                updatedByUserId: GetNullableInt64(reader, "UpdatedByUserId"),
                isDeleted: reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                deletedAt: GetNullableDateTime(reader, "DeletedAt"),
                deletedByUserId: GetNullableInt64(reader, "DeletedByUserId"),
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

        private static DateTime? GetNullableDateTime(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
        }
    }
}