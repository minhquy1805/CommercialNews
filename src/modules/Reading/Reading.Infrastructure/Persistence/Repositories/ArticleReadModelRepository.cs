using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using Microsoft.Data.SqlClient;
using Reading.Application.Models.Commands;
using Reading.Application.Models.Queries;
using Reading.Application.Models.Results;
using Reading.Application.Ports.Persistence;
using Reading.Infrastructure.Persistence.Exceptions;
using Reading.Infrastructure.Persistence.Sql;

namespace Reading.Infrastructure.Persistence.Repositories;

public sealed class ArticleReadModelRepository : IArticleReadModelRepository
{
    private const string SelectByPublicIdProc = "[reading].[Reading_ArticleReadModel_SelectByPublicId]";
    private const string SelectSkipAndTakeProc = "[reading].[Reading_ArticleReadModel_SelectSkipAndTake]";
    private const string SearchProc = "[reading].[Reading_ArticleReadModel_Search]";
    private const string SelectRelatedProc = "[reading].[Reading_ArticleReadModel_SelectRelated]";
    private const string UpsertFromContentProc = "[reading].[Reading_ArticleReadModel_UpsertFromContent]";
    private const string MarkNotPublicProc = "[reading].[Reading_ArticleReadModel_MarkNotPublic]";

    private readonly ReadingUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ReadingSqlExceptionTranslator _sqlExceptionTranslator;

    public ArticleReadModelRepository(
        ReadingUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        ReadingSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<ArticleDetailResult?> SelectByPublicIdAsync(
        GetArticleByPublicIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(SelectByPublicIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@ArticlePublicId", SqlDbType.Char, 26)
                    {
                        Value = query.ArticlePublicId
                    });

                using SqlDataReader reader =
                    await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                ArticleDetailCore detailCore = MapArticleDetailCore(reader);

                List<ArticleTagResult> tags = [];
                List<ArticleMediaResult> media = [];

                if (await reader.NextResultAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        tags.Add(MapArticleTag(reader));
                    }
                }

                if (await reader.NextResultAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        media.Add(MapArticleMedia(reader));
                    }
                }

                return ToArticleDetailResult(
                    detailCore,
                    tags,
                    media);
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

    public async Task<PagedReadingResult<ArticleListItemResult>> SelectSkipAndTakeAsync(
        GetArticlesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(SelectSkipAndTakeProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                AddListParameters(command, query);

                using SqlDataReader reader =
                    await command.ExecuteReaderAsync(cancellationToken);

                List<ArticleListItemResult> items = [];
                long totalItems = 0;

                while (await reader.ReadAsync(cancellationToken))
                {
                    if (totalItems == 0)
                    {
                        totalItems = GetInt64OrInt32(reader, "TotalCount");
                    }

                    items.Add(MapArticleListItem(reader));
                }

                return new PagedReadingResult<ArticleListItemResult>
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

    public async Task<PagedReadingResult<ArticleListItemResult>> SearchAsync(
        SearchArticlesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(SearchProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                int page = query.Page < 1 ? 1 : query.Page;
                int pageSize = NormalizePageSize(query.PageSize);
                int skip = (page - 1) * pageSize;

                command.Parameters.AddRange(
                [
                    new SqlParameter("@Keyword", SqlDbType.NVarChar, 300)
                    {
                        Value = query.Query
                    },
                    new SqlParameter("@Skip", SqlDbType.Int)
                    {
                        Value = skip
                    },
                    new SqlParameter("@Take", SqlDbType.Int)
                    {
                        Value = pageSize
                    },
                    new SqlParameter("@SortBy", SqlDbType.NVarChar, 30)
                    {
                        Value = GetSortBy(query.Sort)
                    },
                    new SqlParameter("@SortDirection", SqlDbType.NVarChar, 4)
                    {
                        Value = GetSortDirection(query.Sort)
                    }
                ]);

                using SqlDataReader reader =
                    await command.ExecuteReaderAsync(cancellationToken);

                List<ArticleListItemResult> items = [];
                long totalItems = 0;

                while (await reader.ReadAsync(cancellationToken))
                {
                    if (totalItems == 0)
                    {
                        totalItems = GetInt64OrInt32(reader, "TotalCount");
                    }

                    items.Add(MapArticleListItem(reader));
                }

                return new PagedReadingResult<ArticleListItemResult>
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

    public async Task<IReadOnlyList<ArticleListItemResult>> SelectRelatedAsync(
        GetRelatedArticlesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(SelectRelatedProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticlePublicId", SqlDbType.Char, 26)
                    {
                        Value = query.ArticlePublicId
                    },
                    new SqlParameter("@Limit", SqlDbType.Int)
                    {
                        Value = query.Limit
                    }
                ]);

                using SqlDataReader reader =
                    await command.ExecuteReaderAsync(cancellationToken);

                List<ArticleListItemResult> items = [];

                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(MapArticleListItem(reader));
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

    public async Task<ArticleProjectionApplyResult> UpsertFromContentAsync(
        ApplyContentArticleProjectionCommand commandModel,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(commandModel);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(UpsertFromContentProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt)
                {
                    Value = commandModel.ArticleId
                },
                new SqlParameter("@ArticlePublicId", SqlDbType.Char, 26)
                {
                    Value = commandModel.ArticlePublicId
                },
                new SqlParameter("@Title", SqlDbType.NVarChar, 300)
                {
                    Value = commandModel.Title
                },
                new SqlParameter("@Summary", SqlDbType.NVarChar, 2000)
                {
                    Value = commandModel.Summary
                },
                new SqlParameter("@Body", SqlDbType.NVarChar)
                {
                    Value = commandModel.Body
                },
                new SqlParameter("@CategoryId", SqlDbType.BigInt)
                {
                    Value = ToDbValue(commandModel.CategoryId)
                },
                new SqlParameter("@CategoryName", SqlDbType.NVarChar, 200)
                {
                    Value = ToDbValue(commandModel.CategoryName)
                },
                new SqlParameter("@AuthorUserId", SqlDbType.BigInt)
                {
                    Value = ToDbValue(commandModel.AuthorUserId)
                },
                new SqlParameter("@AuthorDisplayName", SqlDbType.NVarChar, 200)
                {
                    Value = ToDbValue(commandModel.AuthorDisplayName)
                },
                new SqlParameter("@CoverMediaId", SqlDbType.BigInt)
                {
                    Value = ToDbValue(commandModel.CoverMediaId)
                },
                new SqlParameter("@Status", SqlDbType.VarChar, 30)
                {
                    Value = commandModel.Status
                },
                new SqlParameter("@IsPublic", SqlDbType.Bit)
                {
                    Value = commandModel.IsPublic
                },
                new SqlParameter("@PublishedAtUtc", SqlDbType.DateTime2)
                {
                    Value = ToDbValue(commandModel.PublishedAtUtc)
                },
                new SqlParameter("@UpdatedAtUtc", SqlDbType.DateTime2)
                {
                    Value = commandModel.UpdatedAtUtc
                },
                new SqlParameter("@SourceVersion", SqlDbType.BigInt)
                {
                    Value = commandModel.SourceVersion
                },
                new SqlParameter("@MessageId", SqlDbType.Char, 26)
                {
                    Value = ToDbValue(commandModel.MessageId)
                },
                new SqlParameter("@SourceOccurredAtUtc", SqlDbType.DateTime2)
                {
                    Value = ToDbValue(commandModel.SourceOccurredAtUtc)
                }
            ]);

            using SqlDataReader reader =
                await command.ExecuteReaderAsync(cancellationToken);

            return await ReadProjectionApplyResultAsync(
                reader,
                commandModel.SourceVersion,
                cancellationToken);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<ArticleProjectionApplyResult> MarkNotPublicAsync(
        MarkArticleProjectionNotPublicCommand commandModel,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(commandModel);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(MarkNotPublicProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt)
                {
                    Value = commandModel.ArticleId
                },
                new SqlParameter("@Status", SqlDbType.VarChar, 30)
                {
                    Value = commandModel.Status
                },
                new SqlParameter("@SourceVersion", SqlDbType.BigInt)
                {
                    Value = commandModel.SourceVersion
                },
                new SqlParameter("@MessageId", SqlDbType.Char, 26)
                {
                    Value = ToDbValue(commandModel.MessageId)
                },
                new SqlParameter("@SourceOccurredAtUtc", SqlDbType.DateTime2)
                {
                    Value = ToDbValue(commandModel.SourceOccurredAtUtc)
                }
            ]);

            using SqlDataReader reader =
                await command.ExecuteReaderAsync(cancellationToken);

            return await ReadProjectionApplyResultAsync(
                reader,
                commandModel.SourceVersion,
                cancellationToken);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    private void AddListParameters(
        SqlCommand command,
        GetArticlesQuery query)
    {
        int page = query.Page < 1 ? 1 : query.Page;
        int pageSize = NormalizePageSize(query.PageSize);
        int skip = (page - 1) * pageSize;

        command.Parameters.AddRange(
        [
            new SqlParameter("@Skip", SqlDbType.Int)
            {
                Value = skip
            },
            new SqlParameter("@Take", SqlDbType.Int)
            {
                Value = pageSize
            },
            new SqlParameter("@CategoryId", SqlDbType.BigInt)
            {
                Value = ToDbValue(query.CategoryId)
            },
            new SqlParameter("@TagId", SqlDbType.BigInt)
            {
                Value = ToDbValue(query.TagId)
            },
            new SqlParameter("@Keyword", SqlDbType.NVarChar, 300)
            {
                Value = ToDbValue(query.Keyword)
            },
            new SqlParameter("@SortBy", SqlDbType.NVarChar, 30)
            {
                Value = GetSortBy(query.Sort)
            },
            new SqlParameter("@SortDirection", SqlDbType.NVarChar, 4)
            {
                Value = GetSortDirection(query.Sort)
            }
        ]);
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

    private static ArticleDetailCore MapArticleDetailCore(SqlDataReader reader)
    {
        return new ArticleDetailCore
        {
            ArticleId = reader.GetInt64(reader.GetOrdinal("ArticleId")),
            ArticlePublicId = reader.GetString(reader.GetOrdinal("ArticlePublicId")),
            Slug = GetNullableString(reader, "Slug"),
            Title = reader.GetString(reader.GetOrdinal("Title")),
            Summary = reader.GetString(reader.GetOrdinal("Summary")),
            Body = reader.GetString(reader.GetOrdinal("Body")),

            CategoryId = GetNullableInt64(reader, "CategoryId"),
            CategoryName = GetNullableString(reader, "CategoryName"),

            AuthorUserId = GetNullableInt64(reader, "AuthorUserId"),
            AuthorDisplayName = GetNullableString(reader, "AuthorDisplayName"),

            CoverMediaId = GetNullableInt64(reader, "CoverMediaId"),
            CoverMediaUrl = GetNullableString(reader, "CoverMediaUrl"),
            CoverAlt = GetNullableString(reader, "CoverAlt"),

            CanonicalUrl = GetNullableString(reader, "CanonicalUrl"),
            MetaTitle = GetNullableString(reader, "MetaTitle"),
            MetaDescription = GetNullableString(reader, "MetaDescription"),

            PublishedAtUtc = GetNullableDateTime(reader, "PublishedAtUtc"),
            UpdatedAtUtc = reader.GetDateTime(reader.GetOrdinal("UpdatedAtUtc")),

            ViewCount = GetInt64OrDefault(reader, "ViewCount"),
            LikeCount = GetInt64OrDefault(reader, "LikeCount"),
            CommentCount = GetInt64OrDefault(reader, "CommentCount"),
            PopularityScore = GetNullableDouble(reader, "PopularityScore")
        };
    }

    private static ArticleListItemResult MapArticleListItem(SqlDataReader reader)
    {
        return new ArticleListItemResult
        {
            ArticleId = reader.GetInt64(reader.GetOrdinal("ArticleId")),
            ArticlePublicId = reader.GetString(reader.GetOrdinal("ArticlePublicId")),
            Slug = GetNullableString(reader, "Slug"),

            Title = reader.GetString(reader.GetOrdinal("Title")),
            Summary = reader.GetString(reader.GetOrdinal("Summary")),

            CategoryId = GetNullableInt64(reader, "CategoryId"),
            CategoryName = GetNullableString(reader, "CategoryName"),

            AuthorUserId = GetNullableInt64(reader, "AuthorUserId"),
            AuthorDisplayName = GetNullableString(reader, "AuthorDisplayName"),

            CoverMediaId = GetNullableInt64(reader, "CoverMediaId"),
            CoverMediaUrl = GetNullableString(reader, "CoverMediaUrl"),
            CoverAlt = GetNullableString(reader, "CoverAlt"),

            PublishedAtUtc = GetNullableDateTime(reader, "PublishedAtUtc"),
            UpdatedAtUtc = reader.GetDateTime(reader.GetOrdinal("UpdatedAtUtc")),

            ViewCount = GetInt64OrDefault(reader, "ViewCount"),
            LikeCount = GetInt64OrDefault(reader, "LikeCount"),
            CommentCount = GetInt64OrDefault(reader, "CommentCount"),
            PopularityScore = GetNullableDouble(reader, "PopularityScore")
        };
    }

    private static ArticleTagResult MapArticleTag(SqlDataReader reader)
    {
        return new ArticleTagResult
        {
            TagId = reader.GetInt64(reader.GetOrdinal("TagId")),
            TagPublicId = GetNullableString(reader, "TagPublicId"),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Slug = GetNullableString(reader, "Slug")
        };
    }

    private static ArticleMediaResult MapArticleMedia(SqlDataReader reader)
    {
        return new ArticleMediaResult
        {
            MediaId = reader.GetInt64(reader.GetOrdinal("MediaId")),
            Url = reader.GetString(reader.GetOrdinal("Url")),
            Alt = GetNullableString(reader, "Alt"),
            MediaType = reader.GetString(reader.GetOrdinal("MediaType")),
            IsPrimary = reader.GetBoolean(reader.GetOrdinal("IsPrimary")),
            SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder"))
        };
    }

    private static int NormalizePageSize(int pageSize)
    {
        if (pageSize <= 0)
        {
            return 20;
        }

        return pageSize > 100 ? 100 : pageSize;
    }

    private static string GetSortBy(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return "PublishedAt";
        }

        string normalized = sort.Trim().TrimStart('-');

        return normalized.Equals("popularity", StringComparison.OrdinalIgnoreCase)
            ? "Popularity"
            : "PublishedAt";
    }

    private static string GetSortDirection(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return "DESC";
        }

        return sort.Trim().StartsWith("-", StringComparison.Ordinal)
            ? "DESC"
            : "ASC";
    }

    private static object ToDbValue(object? value) => value ?? DBNull.Value;

    private static async Task<ArticleProjectionApplyResult> ReadProjectionApplyResultAsync(
        SqlDataReader reader,
        long incomingSourceVersion,
        CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new ArticleProjectionApplyResult
            {
                Applied = false,
                Decision = "NoResult",
                IncomingSourceVersion = incomingSourceVersion
            };
        }

        int appliedOrdinal = TryGetOrdinal(reader, "Applied");
        int decisionOrdinal = TryGetOrdinal(reader, "Decision");
        int previousSourceVersionOrdinal = TryGetOrdinal(reader, "PreviousSourceVersion");
        int incomingSourceVersionOrdinal = TryGetOrdinal(reader, "IncomingSourceVersion");

        return new ArticleProjectionApplyResult
        {
            Applied = appliedOrdinal >= 0
                      && !reader.IsDBNull(appliedOrdinal)
                      && reader.GetBoolean(appliedOrdinal),
            Decision = decisionOrdinal >= 0 && !reader.IsDBNull(decisionOrdinal)
                ? reader.GetString(decisionOrdinal)
                : string.Empty,
            PreviousSourceVersion = previousSourceVersionOrdinal >= 0
                                    && !reader.IsDBNull(previousSourceVersionOrdinal)
                ? reader.GetInt64(previousSourceVersionOrdinal)
                : null,
            IncomingSourceVersion = incomingSourceVersionOrdinal >= 0
                                    && !reader.IsDBNull(incomingSourceVersionOrdinal)
                ? reader.GetInt64(incomingSourceVersionOrdinal)
                : incomingSourceVersion
        };
    }

    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        int ordinal = TryGetOrdinal(reader, columnName);
        if (ordinal < 0)
        {
            return null;
        }

        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static long? GetNullableInt64(SqlDataReader reader, string columnName)
    {
        int ordinal = TryGetOrdinal(reader, columnName);
        if (ordinal < 0)
        {
            return null;
        }

        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }

    private static long GetInt64OrDefault(SqlDataReader reader, string columnName)
    {
        int ordinal = TryGetOrdinal(reader, columnName);
        if (ordinal < 0 || reader.IsDBNull(ordinal))
        {
            return 0;
        }

        return reader.GetInt64(ordinal);
    }

    private static long GetInt64OrInt32(SqlDataReader reader, string columnName)
    {
        int ordinal = TryGetOrdinal(reader, columnName);

        if (ordinal < 0 || reader.IsDBNull(ordinal))
        {
            return 0;
        }

        Type fieldType = reader.GetFieldType(ordinal);

        if (fieldType == typeof(int))
        {
            return reader.GetInt32(ordinal);
        }

        return reader.GetInt64(ordinal);
    }

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string columnName)
    {
        int ordinal = TryGetOrdinal(reader, columnName);
        if (ordinal < 0)
        {
            return null;
        }

        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }

    private static double? GetNullableDouble(SqlDataReader reader, string columnName)
    {
        int ordinal = TryGetOrdinal(reader, columnName);
        if (ordinal < 0)
        {
            return null;
        }

        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        object value = reader.GetValue(ordinal);

        return Convert.ToDouble(value);
    }

    private static int TryGetOrdinal(SqlDataReader reader, string columnName)
    {
        for (int index = 0; index < reader.FieldCount; index++)
        {
            if (string.Equals(
                    reader.GetName(index),
                    columnName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private static ArticleDetailResult ToArticleDetailResult(
        ArticleDetailCore core,
        IReadOnlyList<ArticleTagResult> tags,
        IReadOnlyList<ArticleMediaResult> media)
    {
        return new ArticleDetailResult
        {
            ArticleId = core.ArticleId,
            ArticlePublicId = core.ArticlePublicId,
            Slug = core.Slug,

            Title = core.Title,
            Summary = core.Summary,
            Body = core.Body,

            CategoryId = core.CategoryId,
            CategoryName = core.CategoryName,

            AuthorUserId = core.AuthorUserId,
            AuthorDisplayName = core.AuthorDisplayName,

            CoverMediaId = core.CoverMediaId,
            CoverMediaUrl = core.CoverMediaUrl,
            CoverAlt = core.CoverAlt,

            CanonicalUrl = core.CanonicalUrl,
            MetaTitle = core.MetaTitle,
            MetaDescription = core.MetaDescription,

            PublishedAtUtc = core.PublishedAtUtc,
            UpdatedAtUtc = core.UpdatedAtUtc,

            ViewCount = core.ViewCount,
            LikeCount = core.LikeCount,
            CommentCount = core.CommentCount,
            PopularityScore = core.PopularityScore,

            Tags = tags,
            Media = media
        };
    }

    private sealed class ArticleDetailCore
    {
        public long ArticleId { get; init; }

        public string ArticlePublicId { get; init; } = string.Empty;

        public string? Slug { get; init; }

        public string Title { get; init; } = string.Empty;

        public string Summary { get; init; } = string.Empty;

        public string Body { get; init; } = string.Empty;

        public long? CategoryId { get; init; }

        public string? CategoryName { get; init; }

        public long? AuthorUserId { get; init; }

        public string? AuthorDisplayName { get; init; }

        public long? CoverMediaId { get; init; }

        public string? CoverMediaUrl { get; init; }

        public string? CoverAlt { get; init; }

        public string? CanonicalUrl { get; init; }

        public string? MetaTitle { get; init; }

        public string? MetaDescription { get; init; }

        public DateTime? PublishedAtUtc { get; init; }

        public DateTime UpdatedAtUtc { get; init; }

        public long ViewCount { get; init; }

        public long LikeCount { get; init; }

        public long CommentCount { get; init; }

        public double? PopularityScore { get; init; }
    }
}
