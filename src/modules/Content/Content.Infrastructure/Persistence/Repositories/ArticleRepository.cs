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
    private const string ArticleSoftDeleteProc = "[content].[Content_Article_SoftDelete]";

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

    public async Task<(long ArticleId, long Version)> InsertAsync(
        Article article,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(article);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(ArticleInsertProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticlePublicId", SqlDbType.Char, 26) { Value = article.ArticlePublicId },
                new SqlParameter("@CategoryId", SqlDbType.BigInt) { Value = article.CategoryId },
                new SqlParameter("@AuthorUserId", SqlDbType.BigInt) { Value = article.AuthorUserId },
                new SqlParameter("@Title", SqlDbType.NVarChar, 300) { Value = article.Title },
                new SqlParameter("@Summary", SqlDbType.NVarChar, 1000) { Value = article.Summary },
                new SqlParameter("@Body", SqlDbType.NVarChar, -1) { Value = article.Body },
                new SqlParameter("@CoverMediaId", SqlDbType.BigInt) { Value = ToDbValue(article.CoverMediaId) },
                new SqlParameter("@CreatedByUserId", SqlDbType.BigInt) { Value = ToDbValue(article.CreatedByUserId) }
            ]);

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Content_Article_Insert did not return the inserted article row.");
            }

            long articleId = reader.GetInt64(reader.GetOrdinal("ArticleId"));
            long version = reader.GetInt64(reader.GetOrdinal("Version"));

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

    public async Task<Article?> GetByPublicIdAsync(
        string articlePublicId,
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
                    new SqlParameter("@ArticlePublicId", SqlDbType.Char, 26) { Value = articlePublicId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapArticle(reader);
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

                string? keyword = string.IsNullOrWhiteSpace(query.Keyword)
                    ? null
                    : query.Keyword.Trim();
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
                    new SqlParameter("@AuthorUserId", SqlDbType.BigInt) { Value = ToDbValue(query.AuthorUserId) },
                    new SqlParameter("@IsDeleted", SqlDbType.Bit) { Value = query.IsDeleted },
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
                        ArticlePublicId = reader.GetString(reader.GetOrdinal("ArticlePublicId")),
                        Title = reader.GetString(reader.GetOrdinal("Title")),
                        Summary = reader.GetString(reader.GetOrdinal("Summary")),
                        Status = reader.GetString(reader.GetOrdinal("Status")),
                        AuthorUserId = reader.GetInt64(reader.GetOrdinal("AuthorUserId")),
                        CategoryId = reader.GetInt64(reader.GetOrdinal("CategoryId")),
                        CoverMediaId = GetNullableInt64(reader, "CoverMediaId"),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        PublishedAt = GetNullableDateTime(reader, "PublishedAt"),
                        UnpublishedAt = GetNullableDateTime(reader, "UnpublishedAt"),
                        ArchivedAt = GetNullableDateTime(reader, "ArchivedAt"),
                        IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                        Version = reader.GetInt64(reader.GetOrdinal("Version"))
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

    public async Task<Article?> UpdateAsync(
        Article article,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(article);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(ArticleUpdateProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = article.ArticleId },
                new SqlParameter("@CategoryId", SqlDbType.BigInt) { Value = article.CategoryId },
                new SqlParameter("@Title", SqlDbType.NVarChar, 300) { Value = article.Title },
                new SqlParameter("@Summary", SqlDbType.NVarChar, 1000) { Value = article.Summary },
                new SqlParameter("@Body", SqlDbType.NVarChar, -1) { Value = article.Body },
                new SqlParameter("@CoverMediaId", SqlDbType.BigInt) { Value = ToDbValue(article.CoverMediaId) },
                new SqlParameter("@UpdatedByUserId", SqlDbType.BigInt) { Value = ToDbValue(article.UpdatedByUserId) },
                new SqlParameter("@ExpectedVersion", SqlDbType.BigInt) { Value = expectedVersion }
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

    public async Task<Article?> PublishAsync(
        long articleId,
        long? actorUserId,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using SqlCommand command = CreateTransactionalCommand(ArticlePublishProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                new SqlParameter("@ActorUserId", SqlDbType.BigInt) { Value = ToDbValue(actorUserId) },
                new SqlParameter("@ExpectedVersion", SqlDbType.BigInt) { Value = expectedVersion }
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
        long expectedVersion,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using SqlCommand command = CreateTransactionalCommand(ArticleUnpublishProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                new SqlParameter("@ActorUserId", SqlDbType.BigInt) { Value = ToDbValue(actorUserId) },
                new SqlParameter("@ExpectedVersion", SqlDbType.BigInt) { Value = expectedVersion },
                new SqlParameter("@Reason", SqlDbType.NVarChar, 500) { Value = reason }
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
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using SqlCommand command = CreateTransactionalCommand(ArticleArchiveProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                new SqlParameter("@ActorUserId", SqlDbType.BigInt) { Value = ToDbValue(actorUserId) },
                new SqlParameter("@ExpectedVersion", SqlDbType.BigInt) { Value = expectedVersion }
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

    public async Task<Article?> SoftDeleteAsync(
        long articleId,
        long? actorUserId,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using SqlCommand command = CreateTransactionalCommand(ArticleSoftDeleteProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                new SqlParameter("@DeletedByUserId", SqlDbType.BigInt) { Value = ToDbValue(actorUserId) },
                new SqlParameter("@ExpectedVersion", SqlDbType.BigInt) { Value = expectedVersion }
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
            articlePublicId: reader.GetString(reader.GetOrdinal("ArticlePublicId")),
            categoryId: reader.GetInt64(reader.GetOrdinal("CategoryId")),
            authorUserId: reader.GetInt64(reader.GetOrdinal("AuthorUserId")),
            title: reader.GetString(reader.GetOrdinal("Title")),
            summary: reader.GetString(reader.GetOrdinal("Summary")),
            body: reader.GetString(reader.GetOrdinal("Body")),
            status: reader.GetString(reader.GetOrdinal("Status")),
            publishedAt: GetNullableDateTime(reader, "PublishedAt"),
            unpublishedAt: GetNullableDateTime(reader, "UnpublishedAt"),
            archivedAt: GetNullableDateTime(reader, "ArchivedAt"),
            coverMediaId: GetNullableInt64(reader, "CoverMediaId"),
            createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            updatedAt: reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            createdByUserId: reader.GetInt64(reader.GetOrdinal("CreatedByUserId")),
            updatedByUserId: GetNullableInt64(reader, "UpdatedByUserId"),
            isDeleted: reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
            deletedAt: GetNullableDateTime(reader, "DeletedAt"),
            deletedByUserId: GetNullableInt64(reader, "DeletedByUserId"),
            version: reader.GetInt64(reader.GetOrdinal("Version")));
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
}