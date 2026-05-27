using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Interaction.Application.Models.Queries;
using Interaction.Application.Models.Results;
using Interaction.Application.Ports.Persistence;
using Interaction.Infrastructure.Persistence.Exceptions;
using Microsoft.Data.SqlClient;

namespace Interaction.Infrastructure.Persistence.Repositories;

public sealed class CommentModerationCaseRepository
    : ICommentModerationCaseRepository
{
    private const string SelectPagedProc =
        "[interaction].[Interaction_CommentModerationCase_SelectPaged]";

    private const string SelectByPublicIdProc =
        "[interaction].[Interaction_CommentModerationCase_SelectByPublicId]";

    private const string DismissProc =
        "[interaction].[Interaction_CommentModerationCase_Dismiss]";

    private const string HideCommentProc =
        "[interaction].[Interaction_CommentModerationCase_HideComment]";

    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly InteractionSqlExceptionTranslator _sqlExceptionTranslator;

    public CommentModerationCaseRepository(
        IInteractionUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        InteractionSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _sqlConnectionFactory = sqlConnectionFactory
            ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));

        _sqlExceptionTranslator = sqlExceptionTranslator
            ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<PagedQueryResult<ModerationCaseListItemResult>> GetPagedAsync(
        GetModerationCasesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(
                    SelectPagedProc,
                    cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                int page = query.Page <= 0 ? 1 : query.Page;
                int pageSize = NormalizePageSize(query.PageSize);
                int skip = checked((page - 1) * pageSize);

                command.Parameters.AddRange(
                [
                    new SqlParameter("@Status", SqlDbType.NVarChar, 30)
                    {
                        Value = ToDbValue(query.Status)
                    },
                    new SqlParameter("@Priority", SqlDbType.NVarChar, 20)
                    {
                        Value = ToDbValue(query.Priority)
                    },
                    new SqlParameter("@ArticlePublicId", SqlDbType.Char, 26)
                    {
                        Value = ToDbValue(query.ArticlePublicId)
                    },
                    new SqlParameter("@CommentPublicId", SqlDbType.Char, 26)
                    {
                        Value = ToDbValue(query.CommentPublicId)
                    },
                    new SqlParameter("@AlertTriggered", SqlDbType.Bit)
                    {
                        Value = ToDbValue(query.AlertTriggered)
                    },
                    new SqlParameter("@Skip", SqlDbType.Int)
                    {
                        Value = skip
                    },
                    new SqlParameter("@Take", SqlDbType.Int)
                    {
                        Value = pageSize
                    }
                ]);

                List<ModerationCaseListItemResult> items = [];
                int totalItems = 0;

                using SqlDataReader reader =
                    await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(MapListItem(reader));

                    totalItems = GetCountAsInt(
                        reader,
                        "TotalCount");
                }

                return new PagedQueryResult<ModerationCaseListItemResult>
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

    public async Task<ModerationCaseDetailResult?> GetByPublicIdAsync(
        string casePublicId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(casePublicId);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(
                    SelectByPublicIdProc,
                    cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@CasePublicId", SqlDbType.Char, 26)
                    {
                        Value = casePublicId
                    });

                using SqlDataReader reader =
                    await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                string moderationCasePublicId =
                    reader.GetString(
                        reader.GetOrdinal("CommentModerationCasePublicId"));

                string status =
                    reader.GetString(
                        reader.GetOrdinal("CaseStatus"));

                string priority =
                    reader.GetString(
                        reader.GetOrdinal("Priority"));

                string highestSeverity =
                    reader.GetString(
                        reader.GetOrdinal("HighestSeverity"));

                DateTime? alertTriggeredAtUtc =
                    GetNullableDateTime(reader, "AlertTriggeredAtUtc");

                string? alertLevel =
                    GetNullableString(reader, "AlertLevel");

                string? alertMessageId =
                    GetNullableString(reader, "AlertMessageId");

                DateTime openedAtUtc =
                    reader.GetDateTime(
                        reader.GetOrdinal("OpenedAtUtc"));

                DateTime? resolvedAtUtc =
                    GetNullableDateTime(reader, "ResolvedAtUtc");

                long? resolvedByUserId =
                    GetNullableInt64(reader, "ResolvedByUserId");

                string? resolutionType =
                    GetNullableString(reader, "ResolutionType");

                string? resolutionReasonCode =
                    GetNullableString(reader, "ResolutionReasonCode");

                string? resolutionNote =
                    GetNullableString(reader, "ResolutionNote");

                long version =
                    reader.GetInt64(
                        reader.GetOrdinal("CaseVersion"));

                var comment = new ModerationCaseCommentDetailResult(
                    CommentPublicId:
                        reader.GetString(
                            reader.GetOrdinal("CommentPublicId")),
                    ArticlePublicId:
                        reader.GetString(
                            reader.GetOrdinal("ArticlePublicId")),
                    AuthorUserId:
                        reader.GetInt64(
                            reader.GetOrdinal("AuthorUserId")),
                    Content:
                        reader.GetString(
                            reader.GetOrdinal("Content")),
                    Status:
                        reader.GetString(
                            reader.GetOrdinal("CommentStatus")),
                    Version:
                        reader.GetInt64(
                            reader.GetOrdinal("CommentVersion")),
                    CreatedAtUtc:
                        reader.GetDateTime(
                            reader.GetOrdinal("CommentCreatedAtUtc")));

                List<ModerationCaseReportDetailResult> reports = [];

                if (await reader.NextResultAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        reports.Add(MapReportDetail(reader));
                    }
                }

                return new ModerationCaseDetailResult(
                    CommentModerationCasePublicId: moderationCasePublicId,
                    Status: status,
                    Priority: priority,
                    HighestSeverity: highestSeverity,
                    AlertTriggeredAtUtc: alertTriggeredAtUtc,
                    AlertLevel: alertLevel,
                    AlertMessageId: alertMessageId,
                    OpenedAtUtc: openedAtUtc,
                    ResolvedAtUtc: resolvedAtUtc,
                    ResolvedByUserId: resolvedByUserId,
                    ResolutionType: resolutionType,
                    ResolutionReasonCode: resolutionReasonCode,
                    ResolutionNote: resolutionNote,
                    Version: version,
                    Comment: comment,
                    Reports: reports);
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

    public async Task<DismissReportedCommentCaseResult> DismissAsync(
        string casePublicId,
        long expectedVersion,
        string historyPublicId,
        long actorUserId,
        string reasonCode,
        string? note,
        string? correlationId,
        string actorType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(casePublicId);
        ArgumentException.ThrowIfNullOrWhiteSpace(historyPublicId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorType);

        try
        {
            /*
             * DismissReportedCommentCaseUseCase opens this transaction because:
             *
             * Case Open -> Dismissed
             * + Pending Reports -> Dismissed
             * + CommentModerationActionHistory
             * + interaction.comment_reports_dismissed outbox
             *
             * must commit atomically.
             */
            using SqlCommand command = CreateTransactionalCommand(DismissProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@CasePublicId", SqlDbType.Char, 26)
                {
                    Value = casePublicId
                },
                new SqlParameter("@ExpectedVersion", SqlDbType.BigInt)
                {
                    Value = expectedVersion
                },
                new SqlParameter("@HistoryPublicId", SqlDbType.Char, 26)
                {
                    Value = historyPublicId
                },
                new SqlParameter("@ActorUserId", SqlDbType.BigInt)
                {
                    Value = actorUserId
                },
                new SqlParameter("@ReasonCode", SqlDbType.NVarChar, 40)
                {
                    Value = reasonCode
                },
                new SqlParameter("@Note", SqlDbType.NVarChar, 1000)
                {
                    Value = ToDbValue(note)
                },
                new SqlParameter("@CorrelationId", SqlDbType.Char, 26)
                {
                    Value = ToDbValue(correlationId)
                },
                new SqlParameter("@ActorType", SqlDbType.NVarChar, 30)
                {
                    Value = actorType
                }
            ]);

            using SqlDataReader reader =
                await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException(
                    "Interaction_CommentModerationCase_Dismiss did not return a mutation result row.");
            }

            return new DismissReportedCommentCaseResult(
                CommentModerationCasePublicId:
                    reader.GetString(
                        reader.GetOrdinal("CommentModerationCasePublicId")),
                CaseStatus:
                    reader.GetString(
                        reader.GetOrdinal("CaseStatus")),
                CaseVersion:
                    reader.GetInt64(
                        reader.GetOrdinal("CaseVersion")),
                ResolvedAtUtc:
                    GetNullableDateTime(reader, "ResolvedAtUtc"),
                ResolvedByUserId:
                    GetNullableInt64(reader, "ResolvedByUserId"),
                ResolutionType:
                    GetNullableString(reader, "ResolutionType"),
                ResolutionReasonCode:
                    GetNullableString(reader, "ResolutionReasonCode"));
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<HideReportedCommentResult> HideCommentAsync(
        string casePublicId,
        long expectedCaseVersion,
        long expectedCommentVersion,
        string historyPublicId,
        long actorUserId,
        string reasonCode,
        string? note,
        string? correlationId,
        string actorType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(casePublicId);
        ArgumentException.ThrowIfNullOrWhiteSpace(historyPublicId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorType);

        try
        {
            /*
             * HideReportedCommentUseCase opens this transaction because:
             *
             * Comment Visible -> Hidden
             * + Case Open -> Actioned
             * + Pending Reports -> Actioned
             * + CommentModerationActionHistory
             * + interaction.comment_hidden outbox
             *
             * must commit atomically.
             */
            using SqlCommand command = CreateTransactionalCommand(
                HideCommentProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@CasePublicId", SqlDbType.Char, 26)
                {
                    Value = casePublicId
                },
                new SqlParameter("@ExpectedCaseVersion", SqlDbType.BigInt)
                {
                    Value = expectedCaseVersion
                },
                new SqlParameter("@ExpectedCommentVersion", SqlDbType.BigInt)
                {
                    Value = expectedCommentVersion
                },
                new SqlParameter("@HistoryPublicId", SqlDbType.Char, 26)
                {
                    Value = historyPublicId
                },
                new SqlParameter("@ActorUserId", SqlDbType.BigInt)
                {
                    Value = actorUserId
                },
                new SqlParameter("@ReasonCode", SqlDbType.NVarChar, 40)
                {
                    Value = reasonCode
                },
                new SqlParameter("@Note", SqlDbType.NVarChar, 1000)
                {
                    Value = ToDbValue(note)
                },
                new SqlParameter("@CorrelationId", SqlDbType.Char, 26)
                {
                    Value = ToDbValue(correlationId)
                },
                new SqlParameter("@ActorType", SqlDbType.NVarChar, 30)
                {
                    Value = actorType
                }
            ]);

            using SqlDataReader reader =
                await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException(
                    "Interaction_CommentModerationCase_HideComment did not return a mutation result row.");
            }

            return new HideReportedCommentResult(
                CommentModerationCasePublicId:
                    reader.GetString(
                        reader.GetOrdinal("CommentModerationCasePublicId")),
                CaseStatus:
                    reader.GetString(
                        reader.GetOrdinal("CaseStatus")),
                CaseVersion:
                    reader.GetInt64(
                        reader.GetOrdinal("CaseVersion")),
                ResolvedAtUtc:
                    GetNullableDateTime(reader, "ResolvedAtUtc"),
                CommentPublicId:
                    reader.GetString(
                        reader.GetOrdinal("CommentPublicId")),
                ArticlePublicId:
                    reader.GetString(
                        reader.GetOrdinal("ArticlePublicId")),
                CommentStatus:
                    reader.GetString(
                        reader.GetOrdinal("CommentStatus")),
                CommentVersion:
                    reader.GetInt64(
                        reader.GetOrdinal("CommentVersion")),
                HiddenAtUtc:
                    GetNullableDateTime(reader, "HiddenAtUtc"),
                ResolvedReportCount:
                    reader.GetInt64(
                        reader.GetOrdinal("ResolvedReportCount")));
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    private SqlCommand CreateTransactionalCommand(
        string storedProcedureName)
    {
        SqlCommand command = _unitOfWork.Connection.CreateCommand();

        command.Transaction = _unitOfWork.Transaction;
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        return command;
    }

    private async Task<(SqlCommand Command, SqlConnection? OwnedConnection)>
        CreateReadCommandAsync(
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

        SqlConnection ownedConnection =
            _sqlConnectionFactory.CreateConnection();

        await ownedConnection.OpenAsync(cancellationToken);

        SqlCommand command = ownedConnection.CreateCommand();

        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        return (command, ownedConnection);
    }

    private static ModerationCaseListItemResult MapListItem(
        SqlDataReader reader)
    {
        DateTime? alertTriggeredAtUtc =
            GetNullableDateTime(reader, "AlertTriggeredAtUtc");

        /*
         * One report per (CommentId, ReporterUserId) is enforced in V1.
         * Therefore TotalReportCount within one case is also the number
         * of distinct reporters represented in that case.
         */
        int distinctReporterCount =
            GetCountAsInt(reader, "TotalReportCount");

        return new ModerationCaseListItemResult(
            CommentModerationCasePublicId:
                reader.GetString(
                    reader.GetOrdinal("CommentModerationCasePublicId")),
            CommentPublicId:
                reader.GetString(
                    reader.GetOrdinal("CommentPublicId")),
            ArticlePublicId:
                reader.GetString(
                    reader.GetOrdinal("ArticlePublicId")),
            Status:
                reader.GetString(
                    reader.GetOrdinal("Status")),
            Priority:
                reader.GetString(
                    reader.GetOrdinal("Priority")),
            HighestSeverity:
                reader.GetString(
                    reader.GetOrdinal("HighestSeverity")),
            PendingReportCount:
                GetCountAsInt(reader, "PendingReportCount"),
            DistinctReporterCount:
                distinctReporterCount,
            AlertTriggered:
                alertTriggeredAtUtc.HasValue,
            AlertTriggeredAtUtc:
                alertTriggeredAtUtc,
            AlertLevel:
                GetNullableString(reader, "AlertLevel"),
            OpenedAtUtc:
                reader.GetDateTime(
                    reader.GetOrdinal("OpenedAtUtc")),
            Version:
                reader.GetInt64(
                    reader.GetOrdinal("Version")));
    }

    private static ModerationCaseReportDetailResult MapReportDetail(
        SqlDataReader reader)
    {
        return new ModerationCaseReportDetailResult(
            CommentReportPublicId:
                reader.GetString(
                    reader.GetOrdinal("CommentReportPublicId")),
            ReporterUserId:
                reader.GetInt64(
                    reader.GetOrdinal("ReporterUserId")),
            ReasonCode:
                reader.GetString(
                    reader.GetOrdinal("ReasonCode")),
            Description:
                GetNullableString(reader, "Description"),
            Status:
                reader.GetString(
                    reader.GetOrdinal("Status")),
            CreatedAtUtc:
                reader.GetDateTime(
                    reader.GetOrdinal("CreatedAtUtc")),
            ResolvedAtUtc:
                GetNullableDateTime(reader, "ResolvedAtUtc"));
    }

    private static int NormalizePageSize(int pageSize)
    {
        if (pageSize <= 0)
        {
            return 20;
        }

        return pageSize > 200
            ? 200
            : pageSize;
    }

    private static int GetCountAsInt(
        SqlDataReader reader,
        string columnName)
    {
        long count = reader.GetInt64(
            reader.GetOrdinal(columnName));

        return checked((int)count);
    }

    private static object ToDbValue(object? value)
    {
        return value ?? DBNull.Value;
    }

    private static string? GetNullableString(
        SqlDataReader reader,
        string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetString(ordinal);
    }

    private static long? GetNullableInt64(
        SqlDataReader reader,
        string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetInt64(ordinal);
    }

    private static DateTime? GetNullableDateTime(
        SqlDataReader reader,
        string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetDateTime(ordinal);
    }
}