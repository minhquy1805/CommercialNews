using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql;
using Content.Application.Models.QueryModels;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;
using Content.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Content.Infrastructure.Persistence.Repositories
{
    public sealed class ArticleRepository : IArticleRepository
    {
        private readonly ContentUnitOfWork _unitOfWork;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;

        public ArticleRepository(
            ContentUnitOfWork unitOfWork,
            ISqlConnectionFactory sqlConnectionFactory)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        }

        public async Task<(long ArticleId, int Version)> InsertAsync(
            Article article,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(article);

            using SqlCommand command = CreateTransactionalCommand("Content_Article_Insert");

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
                new SqlParameter("@CreatedAt", SqlDbType.DateTime2) { Value = article.CreatedAt },
                new SqlParameter("@UpdatedAt", SqlDbType.DateTime2) { Value = article.UpdatedAt },
                new SqlParameter("@CreatedByUserId", SqlDbType.BigInt) { Value = ToDbValue(article.CreatedByUserId) },
                new SqlParameter("@UpdatedByUserId", SqlDbType.BigInt) { Value = ToDbValue(article.UpdatedByUserId) },
                new SqlParameter("@IsDeleted", SqlDbType.Bit) { Value = article.IsDeleted },
                new SqlParameter("@DeletedAt", SqlDbType.DateTime2) { Value = ToDbValue(article.DeletedAt) },
                new SqlParameter("@DeletedByUserId", SqlDbType.BigInt) { Value = ToDbValue(article.DeletedByUserId) },
                new SqlParameter("@Version", SqlDbType.Int) { Value = article.Version }
            ]);

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Content_Article_Insert did not return the inserted article identity.");
            }

            long articleId = reader.GetInt64(reader.GetOrdinal("ArticleId"));
            int version = reader.GetInt32(reader.GetOrdinal("Version"));

            return (articleId, version);
        }

        public async Task<Article?> GetByIdAsync(
            long articleId,
            CancellationToken cancellationToken = default)
        {
            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateReadCommandAsync("Content_Article_SelectById", cancellationToken);

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
                    await CreateReadCommandAsync("Content_Article_SelectByPublicId", cancellationToken);

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
                    await CreateReadCommandAsync("Content_Article_SelectSkipAndTake", cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.AddRange(
                    [
                        new SqlParameter("@Page", SqlDbType.Int) { Value = query.Page },
                        new SqlParameter("@PageSize", SqlDbType.Int) { Value = query.PageSize },
                        new SqlParameter("@Status", SqlDbType.NVarChar, 30) { Value = ToDbValue(query.Status) },
                        new SqlParameter("@CategoryId", SqlDbType.BigInt) { Value = ToDbValue(query.CategoryId) },
                        new SqlParameter("@TagId", SqlDbType.BigInt) { Value = ToDbValue(query.TagId) },
                        new SqlParameter("@Sort", SqlDbType.NVarChar, 50) { Value = query.Sort }
                    ]);

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    List<ArticleListResultItem> items = [];

                    while (await reader.ReadAsync(cancellationToken))
                    {
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

                    int totalItems = 0;

                    if (await reader.NextResultAsync(cancellationToken) &&
                        await reader.ReadAsync(cancellationToken))
                    {
                        totalItems = reader.GetInt32(reader.GetOrdinal("TotalItems"));
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

            using SqlCommand command = CreateTransactionalCommand("Content_Article_Update");

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = article.ArticleId },
                new SqlParameter("@ExpectedVersion", SqlDbType.Int) { Value = expectedVersion },
                new SqlParameter("@CategoryId", SqlDbType.BigInt) { Value = ToDbValue(article.CategoryId) },
                new SqlParameter("@Title", SqlDbType.NVarChar, 300) { Value = article.Title },
                new SqlParameter("@Summary", SqlDbType.NVarChar, 2000) { Value = ToDbValue(article.Summary) },
                new SqlParameter("@Content", SqlDbType.NVarChar) { Value = article.Body },
                new SqlParameter("@Status", SqlDbType.NVarChar, 30) { Value = article.Status },
                new SqlParameter("@PublishedAt", SqlDbType.DateTime2) { Value = ToDbValue(article.PublishedAt) },
                new SqlParameter("@UnpublishedAt", SqlDbType.DateTime2) { Value = ToDbValue(article.UnpublishedAt) },
                new SqlParameter("@ArchivedAt", SqlDbType.DateTime2) { Value = ToDbValue(article.ArchivedAt) },
                new SqlParameter("@CoverMediaId", SqlDbType.BigInt) { Value = ToDbValue(article.CoverMediaId) },
                new SqlParameter("@UpdatedAt", SqlDbType.DateTime2) { Value = article.UpdatedAt },
                new SqlParameter("@UpdatedByUserId", SqlDbType.BigInt) { Value = ToDbValue(article.UpdatedByUserId) },
                new SqlParameter("@IsDeleted", SqlDbType.Bit) { Value = article.IsDeleted },
                new SqlParameter("@DeletedAt", SqlDbType.DateTime2) { Value = ToDbValue(article.DeletedAt) },
                new SqlParameter("@DeletedByUserId", SqlDbType.BigInt) { Value = ToDbValue(article.DeletedByUserId) },
                new SqlParameter("@Version", SqlDbType.Int) { Value = article.Version }
            ]);

            int affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);

            return affectedRows > 0;
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
            Article article = Article.CreateDraft(
                publicId: reader.GetString(reader.GetOrdinal("PublicId")),
                authorUserId: reader.GetInt64(reader.GetOrdinal("AuthorUserId")),
                title: reader.GetString(reader.GetOrdinal("Title")),
                body: reader.GetString(reader.GetOrdinal("Content")),
                summary: GetNullableString(reader, "Summary"),
                categoryId: GetNullableInt64(reader, "CategoryId"),
                coverMediaId: GetNullableInt64(reader, "CoverMediaId"),
                nowUtc: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                actorUserId: GetNullableInt64(reader, "CreatedByUserId"));

            article.Rehydrate(
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

            return article;
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

