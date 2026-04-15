using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Content.Application.Models.QueryModels;
using Content.Application.Ports.Persistence;
using Content.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Content.Infrastructure.Persistence.Repositories
{
    public sealed class ArticleRevisionRepository : IArticleRevisionRepository
    {
        private const string ArticleRevisionInsertProc = "[content].[Content_ArticleRevision_Insert]";
        private const string ArticleRevisionSelectByArticleIdProc = "[content].[Content_ArticleRevision_SelectByArticleId]";
        private const string ArticleRevisionSelectByIdProc = "[content].[Content_ArticleRevision_SelectById]";

        private readonly ContentUnitOfWork _unitOfWork;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;

        public ArticleRevisionRepository(
            ContentUnitOfWork unitOfWork,
            ISqlConnectionFactory sqlConnectionFactory)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        }

        public async Task InsertAsync(
            long articleId,
            int revisionNumber,
            string titleSnapshot,
            string? summarySnapshot,
            string bodySnapshot,
            long? categoryIdSnapshot,
            string statusSnapshot,
            long? coverMediaIdSnapshot,
            DateTime changedAt,
            long? changedByUserId,
            string changeType,
            string? changeSummary,
            CancellationToken cancellationToken = default)
        {
            using SqlCommand command = CreateTransactionalCommand(ArticleRevisionInsertProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                new SqlParameter("@RevisionNumber", SqlDbType.Int) { Value = revisionNumber },
                new SqlParameter("@TitleSnapshot", SqlDbType.NVarChar, 300) { Value = titleSnapshot },
                new SqlParameter("@SummarySnapshot", SqlDbType.NVarChar, 2000) { Value = ToDbValue(summarySnapshot) },
                new SqlParameter("@ContentSnapshot", SqlDbType.NVarChar) { Value = bodySnapshot },
                new SqlParameter("@CategoryIdSnapshot", SqlDbType.BigInt) { Value = ToDbValue(categoryIdSnapshot) },
                new SqlParameter("@StatusSnapshot", SqlDbType.NVarChar, 30) { Value = statusSnapshot },
                new SqlParameter("@CoverMediaIdSnapshot", SqlDbType.BigInt) { Value = ToDbValue(coverMediaIdSnapshot) },
                new SqlParameter("@ChangedByUserId", SqlDbType.BigInt) { Value = ToDbValue(changedByUserId) },
                new SqlParameter("@ChangeType", SqlDbType.NVarChar, 30) { Value = changeType },
                new SqlParameter("@ChangeSummary", SqlDbType.NVarChar, 1000) { Value = ToDbValue(changeSummary) }
            ]);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<PagedQueryResult<ArticleRevisionListResultItem>> GetPagedByArticleIdAsync(
            ArticleRevisionListQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateReadCommandAsync(ArticleRevisionSelectByArticleIdProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = query.ArticleId });

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    List<ArticleRevisionListResultItem> allItems = [];

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        allItems.Add(new ArticleRevisionListResultItem
                        {
                            RevisionId = reader.GetInt64(reader.GetOrdinal("RevisionId")),
                            RevisionNumber = reader.GetInt32(reader.GetOrdinal("RevisionNumber")),
                            TitleSnapshot = reader.GetString(reader.GetOrdinal("TitleSnapshot")),
                            SummarySnapshot = GetNullableString(reader, "SummarySnapshot"),
                            BodySnapshot = reader.GetString(reader.GetOrdinal("ContentSnapshot")),
                            CategoryIdSnapshot = GetNullableInt64(reader, "CategoryIdSnapshot"),
                            StatusSnapshot = reader.GetString(reader.GetOrdinal("StatusSnapshot")),
                            CoverMediaIdSnapshot = GetNullableInt64(reader, "CoverMediaIdSnapshot"),
                            ChangedAt = reader.GetDateTime(reader.GetOrdinal("ChangedAt")),
                            ChangedByUserId = GetNullableInt64(reader, "ChangedByUserId"),
                            ChangeType = reader.GetString(reader.GetOrdinal("ChangeType")),
                            ChangeSummary = GetNullableString(reader, "ChangeSummary")
                        });
                    }

                    int totalItems = allItems.Count;
                    int skip = (query.Page - 1) * query.PageSize;

                    IReadOnlyCollection<ArticleRevisionListResultItem> pagedItems = allItems
                        .Skip(skip)
                        .Take(query.PageSize)
                        .ToArray();

                    return new PagedQueryResult<ArticleRevisionListResultItem>
                    {
                        Items = allItems
                            .Skip(skip)
                            .Take(query.PageSize)
                            .ToArray(),
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

        public async Task<ArticleRevisionDetailResultItem?> GetByIdAsync(
            long articleId,
            long revisionId,
            CancellationToken cancellationToken = default)
        {
            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateReadCommandAsync(ArticleRevisionSelectByIdProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.AddRange(
                    [
                        new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                        new SqlParameter("@RevisionId", SqlDbType.BigInt) { Value = revisionId }
                    ]);

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return null;
                    }

                    return new ArticleRevisionDetailResultItem
                    {
                        RevisionId = reader.GetInt64(reader.GetOrdinal("RevisionId")),
                        ArticleId = reader.GetInt64(reader.GetOrdinal("ArticleId")),
                        RevisionNumber = reader.GetInt32(reader.GetOrdinal("RevisionNumber")),
                        TitleSnapshot = reader.GetString(reader.GetOrdinal("TitleSnapshot")),
                        SummarySnapshot = GetNullableString(reader, "SummarySnapshot"),
                        BodySnapshot = reader.GetString(reader.GetOrdinal("ContentSnapshot")),
                        CategoryIdSnapshot = GetNullableInt64(reader, "CategoryIdSnapshot"),
                        StatusSnapshot = reader.GetString(reader.GetOrdinal("StatusSnapshot")),
                        CoverMediaIdSnapshot = GetNullableInt64(reader, "CoverMediaIdSnapshot"),
                        ChangedAt = reader.GetDateTime(reader.GetOrdinal("ChangedAt")),
                        ChangedByUserId = GetNullableInt64(reader, "ChangedByUserId"),
                        ChangeType = reader.GetString(reader.GetOrdinal("ChangeType")),
                        ChangeSummary = GetNullableString(reader, "ChangeSummary")
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
                ambientCommand.Transaction = _unitOfWork.HasActiveTransaction ? _unitOfWork.Transaction : null;
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
    }
}