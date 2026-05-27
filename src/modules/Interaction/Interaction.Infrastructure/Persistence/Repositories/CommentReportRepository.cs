using System.Data;
using Interaction.Application.Models.Results;
using Interaction.Application.Ports.Persistence;
using Interaction.Infrastructure.Persistence.Exceptions;
using Microsoft.Data.SqlClient;

namespace Interaction.Infrastructure.Persistence.Repositories;

public sealed class CommentReportRepository : ICommentReportRepository
{
    private const string CreateProc =
        "[interaction].[Interaction_CommentReport_Create]";

    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly InteractionSqlExceptionTranslator _sqlExceptionTranslator;

    public CommentReportRepository(
        IInteractionUnitOfWork unitOfWork,
        InteractionSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _sqlExceptionTranslator = sqlExceptionTranslator
            ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<CreateCommentReportMutationResult> CreateAsync(
        string reportPublicId,
        string newCasePublicId,
        string commentPublicId,
        long reporterUserId,
        string reasonCode,
        string? description,
        string evaluatedSeverity,
        int normalAlertThreshold,
        string alertMessageIdCandidate,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPublicId);
        ArgumentException.ThrowIfNullOrWhiteSpace(newCasePublicId);
        ArgumentException.ThrowIfNullOrWhiteSpace(commentPublicId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(evaluatedSeverity);
        ArgumentException.ThrowIfNullOrWhiteSpace(alertMessageIdCandidate);

        try
        {
            /*
             * CreateCommentReportUseCase opens the transaction because:
             *
             * - CommentReport insert
             * - create/join CommentModerationCase
             * - optional alert-trigger metadata
             * - interaction.comment_reported outbox
             * - optional interaction.comment_report_alert_triggered outbox
             *
             * must commit atomically.
             */
            using SqlCommand command = CreateTransactionalCommand(CreateProc);

            SqlParameter alertTriggeredParameter =
                CreateOutputBitParameter("@AlertTriggered");

            SqlParameter createdNewCaseParameter =
                CreateOutputBitParameter("@CreatedNewCase");

            command.Parameters.AddRange(
            [
                new SqlParameter("@ReportPublicId", SqlDbType.Char, 26)
                {
                    Value = reportPublicId
                },
                new SqlParameter("@NewCasePublicId", SqlDbType.Char, 26)
                {
                    Value = newCasePublicId
                },
                new SqlParameter("@CommentPublicId", SqlDbType.Char, 26)
                {
                    Value = commentPublicId
                },
                new SqlParameter("@ReporterUserId", SqlDbType.BigInt)
                {
                    Value = reporterUserId
                },
                new SqlParameter("@ReasonCode", SqlDbType.NVarChar, 40)
                {
                    Value = reasonCode
                },
                new SqlParameter("@Description", SqlDbType.NVarChar, 1000)
                {
                    Value = ToDbValue(description)
                },
                new SqlParameter("@EvaluatedSeverity", SqlDbType.NVarChar, 20)
                {
                    Value = evaluatedSeverity
                },
                new SqlParameter("@NormalAlertThreshold", SqlDbType.Int)
                {
                    Value = normalAlertThreshold
                },
                new SqlParameter("@AlertMessageIdCandidate", SqlDbType.Char, 26)
                {
                    Value = alertMessageIdCandidate
                },
                alertTriggeredParameter,
                createdNewCaseParameter
            ]);

            string returnedCommentReportPublicId;
            string returnedCommentPublicId;
            string returnedArticlePublicId;
            long returnedReporterUserId;
            string returnedReasonCode;
            string? returnedDescription;
            string reportStatus;
            DateTime createdAtUtc;
            string moderationCasePublicId;
            string caseStatus;
            string priority;
            string highestSeverity;
            long distinctReporterCount;
            DateTime? alertTriggeredAtUtc;
            string? alertLevel;
            long caseVersion;

            using (SqlDataReader reader =
                   await command.ExecuteReaderAsync(cancellationToken))
            {
                if (!await reader.ReadAsync(cancellationToken))
                {
                    throw new InvalidOperationException(
                        "Interaction_CommentReport_Create did not return a mutation result row.");
                }

                returnedCommentReportPublicId =
                    reader.GetString(
                        reader.GetOrdinal("CommentReportPublicId"));

                returnedCommentPublicId =
                    reader.GetString(
                        reader.GetOrdinal("CommentPublicId"));

                returnedArticlePublicId =
                    reader.GetString(
                        reader.GetOrdinal("ArticlePublicId"));

                returnedReporterUserId =
                    reader.GetInt64(
                        reader.GetOrdinal("ReporterUserId"));

                returnedReasonCode =
                    reader.GetString(
                        reader.GetOrdinal("ReasonCode"));

                returnedDescription =
                    GetNullableString(reader, "Description");

                reportStatus =
                    reader.GetString(
                        reader.GetOrdinal("ReportStatus"));

                createdAtUtc =
                    reader.GetDateTime(
                        reader.GetOrdinal("CreatedAtUtc"));

                moderationCasePublicId =
                    reader.GetString(
                        reader.GetOrdinal("CommentModerationCasePublicId"));

                caseStatus =
                    reader.GetString(
                        reader.GetOrdinal("CaseStatus"));

                priority =
                    reader.GetString(
                        reader.GetOrdinal("Priority"));

                highestSeverity =
                    reader.GetString(
                        reader.GetOrdinal("HighestSeverity"));

                distinctReporterCount =
                    reader.GetInt64(
                        reader.GetOrdinal("DistinctReporterCount"));

                alertTriggeredAtUtc =
                    GetNullableDateTime(reader, "AlertTriggeredAtUtc");

                alertLevel =
                    GetNullableString(reader, "AlertLevel");

                caseVersion =
                    reader.GetInt64(
                        reader.GetOrdinal("CaseVersion"));
            }

            /*
             * Output parameter values are read only after the data reader
             * has been closed/disposed.
             */
            bool alertTriggered = GetRequiredBoolean(
                alertTriggeredParameter,
                "Interaction_CommentReport_Create did not return AlertTriggered.");

            bool createdNewCase = GetRequiredBoolean(
                createdNewCaseParameter,
                "Interaction_CommentReport_Create did not return CreatedNewCase.");

            return new CreateCommentReportMutationResult(
                CommentReportPublicId: returnedCommentReportPublicId,
                CommentPublicId: returnedCommentPublicId,
                ArticlePublicId: returnedArticlePublicId,
                ReporterUserId: returnedReporterUserId,
                ReasonCode: returnedReasonCode,
                Description: returnedDescription,
                ReportStatus: reportStatus,
                CreatedAtUtc: createdAtUtc,
                CommentModerationCasePublicId: moderationCasePublicId,
                CaseStatus: caseStatus,
                Priority: priority,
                HighestSeverity: highestSeverity,
                DistinctReporterCount: distinctReporterCount,
                AlertTriggeredAtUtc: alertTriggeredAtUtc,
                AlertLevel: alertLevel,
                CaseVersion: caseVersion,
                AlertTriggered: alertTriggered,
                CreatedNewCase: createdNewCase);
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

    private static SqlParameter CreateOutputBitParameter(
        string name)
    {
        return new SqlParameter(name, SqlDbType.Bit)
        {
            Direction = ParameterDirection.Output
        };
    }

    private static bool GetRequiredBoolean(
        SqlParameter parameter,
        string errorMessage)
    {
        if (parameter.Value is null or DBNull)
        {
            throw new InvalidOperationException(errorMessage);
        }

        return Convert.ToBoolean(parameter.Value);
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