using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Microsoft.Data.SqlClient;
using Reading.Application.Models.QueryModels;
using Reading.Application.Ports.Persistence;
using Reading.Domain.Enums;

namespace Reading.Infrastructure.Persistence.Repositories;

public sealed class ReadingQueryRepository : IReadingQueryRepository
{
    private const string ArticleSelectByIdProc = "[reading].[Reading_Article_SelectById]";
    private const string ArticleSelectBySlugProc = "[reading].[Reading_Article_SelectBySlug]";
    private const string ArticleSelectSkipAndTakeProc = "[reading].[Reading_Article_SelectSkipAndTake]";
    private const string ArticleSelectRelatedProc = "[reading].[Reading_Article_SelectRelated]";
    private const string ArticleSearchProc = "[reading].[Reading_Article_Search]";

    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public ReadingQueryRepository(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
    }

    public async Task<PagedQueryResult<ReadingArticleListItem>> GetArticlesAsync(
        ReadingArticleListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection connection) =
                await CreateReadCommandAsync(ArticleSelectSkipAndTakeProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                int skip = (query.Page - 1) * query.PageSize;
                int take = query.PageSize;

                (string sortBy, string sortDirection) = ParseSort(query.Sort);

                command.Parameters.AddRange(
                [
                    new SqlParameter("@Skip", SqlDbType.Int) { Value = skip },
                    new SqlParameter("@Take", SqlDbType.Int) { Value = take },
                    new SqlParameter("@CategoryId", SqlDbType.BigInt) { Value = ToDbValue(query.CategoryId) },
                    new SqlParameter("@TagId", SqlDbType.BigInt) { Value = ToDbValue(query.TagId) },
                    new SqlParameter("@Keyword", SqlDbType.NVarChar, 300) { Value = ToDbValue(query.Q) },
                    new SqlParameter("@SortBy", SqlDbType.NVarChar, 30) { Value = sortBy },
                    new SqlParameter("@SortDirection", SqlDbType.NVarChar, 4) { Value = sortDirection },
                    new SqlParameter("@Scope", SqlDbType.VarChar, 30) { Value = "public" }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<ReadingArticleListItem> items = [];
                int totalItems = 0;

                while (await reader.ReadAsync(cancellationToken))
                {
                    if (totalItems == 0 && !reader.IsDBNull(reader.GetOrdinal("TotalCount")))
                    {
                        totalItems = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                    }

                    items.Add(MapReadingArticleListItem(reader));
                }

                return new PagedQueryResult<ReadingArticleListItem>
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

    public async Task<ReadingArticleDetailResult?> GetArticleByIdAsync(
        long articleId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection connection) =
                await CreateReadCommandAsync(ArticleSelectByIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                    new SqlParameter("@Scope", SqlDbType.VarChar, 30) { Value = "public" }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                ReadingArticleDetailResult detail = MapReadingArticleDetailResultCore(reader);

                if (await reader.NextResultAsync(cancellationToken))
                {
                    List<ReadingTagResultItem> tags = [];

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        tags.Add(new ReadingTagResultItem
                        {
                            TagId = reader.GetInt64(reader.GetOrdinal("TagId")),
                            Name = reader.GetString(reader.GetOrdinal("Name"))
                        });
                    }

                    detail.Tags = tags;
                }

                if (await reader.NextResultAsync(cancellationToken))
                {
                    List<ReadingArticleMediaResultItem> media = [];

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        media.Add(new ReadingArticleMediaResultItem
                        {
                            MediaId = reader.GetInt64(reader.GetOrdinal("MediaId")),
                            Url = reader.GetString(reader.GetOrdinal("Url")),
                            Alt = GetNullableString(reader, "Alt"),
                            IsPrimary = reader.GetBoolean(reader.GetOrdinal("IsPrimary")),
                            Order = reader.GetInt32(reader.GetOrdinal("DisplayOrder"))
                        });
                    }

                    detail.Media = media;
                }

                return detail;
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

    public async Task<ReadingArticleDetailResult?> GetArticleBySlugAsync(
        string scope,
        string slug,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection connection) =
                await CreateReadCommandAsync(ArticleSelectBySlugProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@Scope", SqlDbType.VarChar, 30) { Value = scope },
                    new SqlParameter("@Slug", SqlDbType.NVarChar, 200) { Value = slug }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                ReadingArticleDetailResult detail = MapReadingArticleDetailResultCore(reader);

                if (await reader.NextResultAsync(cancellationToken))
                {
                    List<ReadingTagResultItem> tags = [];

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        tags.Add(new ReadingTagResultItem
                        {
                            TagId = reader.GetInt64(reader.GetOrdinal("TagId")),
                            Name = reader.GetString(reader.GetOrdinal("Name"))
                        });
                    }

                    detail.Tags = tags;
                }

                if (await reader.NextResultAsync(cancellationToken))
                {
                    List<ReadingArticleMediaResultItem> media = [];

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        media.Add(new ReadingArticleMediaResultItem
                        {
                            MediaId = reader.GetInt64(reader.GetOrdinal("MediaId")),
                            Url = reader.GetString(reader.GetOrdinal("Url")),
                            Alt = GetNullableString(reader, "Alt"),
                            IsPrimary = reader.GetBoolean(reader.GetOrdinal("IsPrimary")),
                            Order = reader.GetInt32(reader.GetOrdinal("DisplayOrder"))
                        });
                    }

                    detail.Media = media;
                }

                return detail;
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

    public async Task<IReadOnlyList<ReadingArticleListItem>> GetRelatedArticlesAsync(
        ReadingRelatedArticlesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection connection) =
                await CreateReadCommandAsync(ArticleSelectRelatedProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = query.ArticleId },
                    new SqlParameter("@Limit", SqlDbType.Int) { Value = query.Limit },
                    new SqlParameter("@Scope", SqlDbType.VarChar, 30) { Value = "public" }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<ReadingArticleListItem> items = [];

                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(MapReadingArticleListItem(reader));
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

    public async Task<PagedQueryResult<ReadingArticleListItem>> SearchArticlesAsync(
        ReadingSearchArticlesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection connection) =
                await CreateReadCommandAsync(ArticleSearchProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                int skip = (query.Page - 1) * query.PageSize;
                int take = query.PageSize;

                (string sortBy, string sortDirection) = ParseSort(query.Sort);

                command.Parameters.AddRange(
                [
                    new SqlParameter("@Keyword", SqlDbType.NVarChar, 300) { Value = query.Q },
                    new SqlParameter("@Skip", SqlDbType.Int) { Value = skip },
                    new SqlParameter("@Take", SqlDbType.Int) { Value = take },
                    new SqlParameter("@SortBy", SqlDbType.NVarChar, 30) { Value = sortBy },
                    new SqlParameter("@SortDirection", SqlDbType.NVarChar, 4) { Value = sortDirection },
                    new SqlParameter("@Scope", SqlDbType.VarChar, 30) { Value = "public" }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<ReadingArticleListItem> items = [];
                int totalItems = 0;

                while (await reader.ReadAsync(cancellationToken))
                {
                    if (totalItems == 0 && !reader.IsDBNull(reader.GetOrdinal("TotalCount")))
                    {
                        totalItems = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                    }

                    items.Add(MapReadingArticleListItem(reader));
                }

                return new PagedQueryResult<ReadingArticleListItem>
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

    private async Task<(SqlCommand Command, SqlConnection Connection)> CreateReadCommandAsync(
        string storedProcedureName,
        CancellationToken cancellationToken)
    {
        SqlConnection connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        SqlCommand command = connection.CreateCommand();
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        return (command, connection);
    }

    private static ReadingArticleListItem MapReadingArticleListItem(SqlDataReader reader)
    {
        return new ReadingArticleListItem
        {
            ArticleId = reader.GetInt64(reader.GetOrdinal("ArticleId")),
            Title = reader.GetString(reader.GetOrdinal("Title")),
            Summary = GetNullableString(reader, "Summary") ?? string.Empty,
            Slug = GetNullableString(reader, "Slug") ?? string.Empty,
            PublishedAt = reader.GetDateTime(reader.GetOrdinal("PublishedAt")),
            Category = reader.IsDBNull(reader.GetOrdinal("CategoryId"))
                ? null
                : new ReadingCategoryResultItem
                {
                    CategoryId = reader.GetInt64(reader.GetOrdinal("CategoryId")),
                    Name = GetNullableString(reader, "CategoryName") ?? string.Empty
                },
            Cover = reader.IsDBNull(reader.GetOrdinal("CoverMediaId"))
                ? null
                : new ReadingArticleMediaResultItem
                {
                    MediaId = reader.GetInt64(reader.GetOrdinal("CoverMediaId")),
                    Url = GetNullableString(reader, "CoverUrl") ?? string.Empty,
                    Alt = GetNullableString(reader, "CoverAlt"),
                    IsPrimary = !reader.IsDBNull(reader.GetOrdinal("CoverIsPrimary"))
                        && reader.GetBoolean(reader.GetOrdinal("CoverIsPrimary")),
                    Order = GetNullableInt32(reader, "CoverDisplayOrder") ?? 0
                },
            Counters = new ReadingArticleCountersResult
            {
                Views = GetNullableInt64(reader, "Views") ?? 0,
                Likes = GetNullableInt64(reader, "Likes") ?? 0,
                CountersPartial = !reader.IsDBNull(reader.GetOrdinal("CountersPartial"))
                    && reader.GetBoolean(reader.GetOrdinal("CountersPartial"))
            }
        };
    }

    private static ReadingArticleDetailResult MapReadingArticleDetailResultCore(SqlDataReader reader)
    {
        return new ReadingArticleDetailResult
        {
            ArticleId = reader.GetInt64(reader.GetOrdinal("ArticleId")),
            Title = reader.GetString(reader.GetOrdinal("Title")),
            Summary = GetNullableString(reader, "Summary") ?? string.Empty,
            Body = reader.GetString(reader.GetOrdinal("Body")),
            Slug = GetNullableString(reader, "Slug") ?? string.Empty,
            PublishedAt = reader.GetDateTime(reader.GetOrdinal("PublishedAt")),
            Category = reader.IsDBNull(reader.GetOrdinal("CategoryId"))
                ? null
                : new ReadingCategoryResultItem
                {
                    CategoryId = reader.GetInt64(reader.GetOrdinal("CategoryId")),
                    Name = GetNullableString(reader, "CategoryName") ?? string.Empty
                },
            Seo = new ReadingArticleSeoResult
            {
                CanonicalUrl = GetNullableString(reader, "CanonicalUrl"),
                MetaTitle = GetNullableString(reader, "MetaTitle"),
                MetaDescription = GetNullableString(reader, "MetaDescription")
            },
            Counters = new ReadingArticleCountersResult
            {
                Views = GetNullableInt64(reader, "Views") ?? 0,
                Likes = GetNullableInt64(reader, "Likes") ?? 0,
                CountersPartial = !reader.IsDBNull(reader.GetOrdinal("CountersPartial"))
                    && reader.GetBoolean(reader.GetOrdinal("CountersPartial"))
            },
            Tags = Array.Empty<ReadingTagResultItem>(),
            Media = Array.Empty<ReadingArticleMediaResultItem>()
        };
    }

    private static (string SortBy, string SortDirection) ParseSort(string sort)
    {
        string normalized = string.IsNullOrWhiteSpace(sort)
            ? ReadingSortValues.PublishedAtDescending
            : sort.Trim();

        return normalized switch
        {
            var value when string.Equals(value, ReadingSortValues.PublishedAtAscending, StringComparison.OrdinalIgnoreCase)
                => ("PublishedAt", "ASC"),

            var value when string.Equals(value, ReadingSortValues.PublishedAtDescending, StringComparison.OrdinalIgnoreCase)
                => ("PublishedAt", "DESC"),

            var value when string.Equals(value, ReadingSortValues.PopularityAscending, StringComparison.OrdinalIgnoreCase)
                => ("Popularity", "ASC"),

            var value when string.Equals(value, ReadingSortValues.PopularityDescending, StringComparison.OrdinalIgnoreCase)
                => ("Popularity", "DESC"),

            _ => ("PublishedAt", "DESC")
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
}