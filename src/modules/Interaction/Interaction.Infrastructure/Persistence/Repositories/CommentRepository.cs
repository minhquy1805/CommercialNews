using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Interaction.Application.Models.Queries;
using Interaction.Application.Models.Results;
using Interaction.Application.Ports.Persistence;
using Interaction.Domain.Entities;
using Interaction.Infrastructure.Persistence.Exceptions;
using Microsoft.Data.SqlClient;

namespace Interaction.Infrastructure.Persistence.Repositories;

public sealed class CommentRepository : ICommentRepository
{
    private const string InsertVisibleProc =
        "[interaction].[Interaction_Comment_InsertVisible]";

    private const string SelectByPublicIdProc =
        "[interaction].[Interaction_Comment_SelectByPublicId]";

    private const string SelectVisibleByArticlePublicIdProc =
        "[interaction].[Interaction_Comment_SelectVisibleByArticlePublicId]";

    private const string SelectAdminPagedProc =
        "[interaction].[Interaction_Comment_SelectAdminPaged]";

    private const string GetVisibleCountByArticlePublicIdProc =
        "[interaction].[Interaction_Comment_GetVisibleCountByArticlePublicId]";

    private const string HideProc =
        "[interaction].[Interaction_Comment_Hide]";

    private const string RestoreProc =
        "[interaction].[Interaction_Comment_Restore]";

    private const string DeleteOwnProc =
        "[interaction].[Interaction_Comment_DeleteOwn]";

    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly InteractionSqlExceptionTranslator _sqlExceptionTranslator;

    public CommentRepository(
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

    public async Task<Comment> InsertVisibleAsync(
        string publicId,
        string articlePublicId,
        long authorUserId,
        string content,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicId);
        ArgumentException.ThrowIfNullOrWhiteSpace(articlePublicId);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        try
        {
            /*
             * CreateCommentUseCase opens the transaction because:
             *
             * Comment insert + interaction.comment_created outbox
             * must commit atomically.
             */
            using SqlCommand command = CreateTransactionalCommand(
                InsertVisibleProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@PublicId", SqlDbType.Char, 26)
                {
                    Value = publicId
                },
                new SqlParameter("@ArticlePublicId", SqlDbType.Char, 26)
                {
                    Value = articlePublicId
                },
                new SqlParameter("@AuthorUserId", SqlDbType.BigInt)
                {
                    Value = authorUserId
                },
                new SqlParameter("@Content", SqlDbType.NVarChar, 2000)
                {
                    Value = content
                }
            ]);

            using SqlDataReader reader =
                await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException(
                    "Interaction_Comment_InsertVisible did not return a comment row.");
            }

            return MapComment(reader);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<Comment?> GetByPublicIdAsync(
        string commentPublicId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commentPublicId);

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
                    new SqlParameter("@CommentPublicId", SqlDbType.Char, 26)
                    {
                        Value = commentPublicId
                    });

                using SqlDataReader reader =
                    await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapComment(reader);
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

    public async Task<PagedQueryResult<PublicCommentItemResult>>
        GetVisibleByArticlePublicIdAsync(
            GetPublicCommentsQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(
                    SelectVisibleByArticlePublicIdProc,
                    cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                int page = query.Page <= 0 ? 1 : query.Page;
                int pageSize = NormalizePageSize(query.PageSize);
                int skip = checked((page - 1) * pageSize);

                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticlePublicId", SqlDbType.Char, 26)
                    {
                        Value = query.ArticlePublicId
                    },
                    new SqlParameter("@Skip", SqlDbType.Int)
                    {
                        Value = skip
                    },
                    new SqlParameter("@Take", SqlDbType.Int)
                    {
                        Value = pageSize
                    },
                    new SqlParameter("@SortDirection", SqlDbType.NVarChar, 4)
                    {
                        Value = query.SortDirection
                    }
                ]);

                List<PublicCommentItemResult> items = [];
                int? totalItemsFromPage = null;

                using (SqlDataReader reader =
                       await command.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        items.Add(MapPublicCommentItem(reader));

                        totalItemsFromPage ??= GetCountAsInt(
                            reader,
                            "TotalCount");
                    }
                }

                /*
                 * The public query uses COUNT_BIG OVER().
                 * When a requested page is empty, it has no row from which
                 * TotalCount can be read. In that case use the existing
                 * visible-count procedure to preserve correct paging metadata.
                 */
                int totalItems = totalItemsFromPage
                    ?? checked((int)await GetVisibleCountByArticlePublicIdAsync(
                        query.ArticlePublicId,
                        cancellationToken));

                return new PagedQueryResult<PublicCommentItemResult>
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

    public async Task<PagedQueryResult<AdminCommentResult>> GetAdminPagedAsync(
        GetAdminCommentsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(
                    SelectAdminPagedProc,
                    cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                int page = query.Page <= 0 ? 1 : query.Page;
                int pageSize = NormalizePageSize(query.PageSize);
                int skip = checked((page - 1) * pageSize);

                command.Parameters.AddRange(
                [
                    new SqlParameter("@Status", SqlDbType.NVarChar, 20)
                    {
                        Value = ToDbValue(query.Status)
                    },
                    new SqlParameter("@ArticlePublicId", SqlDbType.Char, 26)
                    {
                        Value = ToDbValue(query.ArticlePublicId)
                    },
                    new SqlParameter("@AuthorUserId", SqlDbType.BigInt)
                    {
                        Value = ToDbValue(query.AuthorUserId)
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

                List<AdminCommentResult> items = [];
                int totalItems = 0;

                using SqlDataReader reader =
                    await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(MapAdminCommentResult(reader));

                    totalItems = GetCountAsInt(
                        reader,
                        "TotalCount");
                }

                return new PagedQueryResult<AdminCommentResult>
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

    public async Task<long> GetVisibleCountByArticlePublicIdAsync(
        string articlePublicId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(articlePublicId);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(
                    GetVisibleCountByArticlePublicIdProc,
                    cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@ArticlePublicId", SqlDbType.Char, 26)
                    {
                        Value = articlePublicId
                    });

                object? scalar =
                    await command.ExecuteScalarAsync(cancellationToken);

                return scalar is null or DBNull
                    ? 0L
                    : Convert.ToInt64(scalar);
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

    public async Task<CommentModerationResult> HideAsync(
        string commentPublicId,
        long expectedVersion,
        string historyPublicId,
        long actorUserId,
        string reasonCode,
        string? note,
        string? correlationId,
        string actorType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commentPublicId);
        ArgumentException.ThrowIfNullOrWhiteSpace(historyPublicId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorType);

        try
        {
            /*
             * HideCommentUseCase opens the transaction because:
             *
             * Comment Visible -> Hidden
             * + CommentModerationActionHistory
             * + interaction.comment_hidden outbox
             *
             * must commit atomically.
             */
            using SqlCommand command = CreateTransactionalCommand(HideProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@CommentPublicId", SqlDbType.Char, 26)
                {
                    Value = commentPublicId
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
                    "Interaction_Comment_Hide did not return a moderation result row.");
            }

            return MapCommentModerationResult(reader);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<CommentModerationResult> RestoreAsync(
        string commentPublicId,
        long expectedVersion,
        string historyPublicId,
        long actorUserId,
        string? note,
        string? correlationId,
        string actorType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commentPublicId);
        ArgumentException.ThrowIfNullOrWhiteSpace(historyPublicId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorType);

        try
        {
            /*
             * RestoreCommentUseCase opens the transaction because:
             *
             * Comment Hidden -> Visible
             * + CommentModerationActionHistory
             * + interaction.comment_restored outbox
             *
             * must commit atomically.
             */
            using SqlCommand command = CreateTransactionalCommand(RestoreProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@CommentPublicId", SqlDbType.Char, 26)
                {
                    Value = commentPublicId
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
                    "Interaction_Comment_Restore did not return a moderation result row.");
            }

            return MapCommentModerationResult(reader);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<DeleteOwnCommentMutationResult> DeleteOwnAsync(
        string commentPublicId,
        long authorUserId,
        long? expectedVersion,
        string? caseCloseHistoryPublicId,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commentPublicId);

        try
        {
            /*
             * DeleteOwnCommentUseCase opens the transaction because:
             *
             * Comment -> Deleted
             * + optional open-case closure/history
             * + interaction.comment_deleted_by_author outbox
             *
             * must commit atomically when there is an effective deletion.
             */
            using SqlCommand command = CreateTransactionalCommand(DeleteOwnProc);

            SqlParameter changedParameter =
                CreateOutputBitParameter("@Changed");

            SqlParameter closedOpenCaseParameter =
                CreateOutputBitParameter("@ClosedOpenCase");

            SqlParameter wasVisibleParameter =
                CreateOutputBitParameter("@WasVisible");

            command.Parameters.AddRange(
            [
                new SqlParameter("@CommentPublicId", SqlDbType.Char, 26)
                {
                    Value = commentPublicId
                },
                new SqlParameter("@AuthorUserId", SqlDbType.BigInt)
                {
                    Value = authorUserId
                },
                new SqlParameter("@ExpectedVersion", SqlDbType.BigInt)
                {
                    Value = ToDbValue(expectedVersion)
                },
                new SqlParameter("@CaseCloseHistoryPublicId", SqlDbType.Char, 26)
                {
                    Value = ToDbValue(caseCloseHistoryPublicId)
                },
                new SqlParameter("@CorrelationId", SqlDbType.Char, 26)
                {
                    Value = ToDbValue(correlationId)
                },
                changedParameter,
                closedOpenCaseParameter,
                wasVisibleParameter
            ]);

            string returnedCommentPublicId;
            string returnedArticlePublicId;
            long returnedAuthorUserId;
            string status;
            long version;
            DateTime? deletedAtUtc;

            using (SqlDataReader reader =
                   await command.ExecuteReaderAsync(cancellationToken))
            {
                if (!await reader.ReadAsync(cancellationToken))
                {
                    throw new InvalidOperationException(
                        "Interaction_Comment_DeleteOwn did not return a mutation result row.");
                }

                returnedCommentPublicId =
                    reader.GetString(reader.GetOrdinal("PublicId"));

                returnedArticlePublicId =
                    reader.GetString(reader.GetOrdinal("ArticlePublicId"));

                returnedAuthorUserId =
                    reader.GetInt64(reader.GetOrdinal("AuthorUserId"));

                status =
                    reader.GetString(reader.GetOrdinal("Status"));

                version =
                    reader.GetInt64(reader.GetOrdinal("Version"));

                deletedAtUtc =
                    GetNullableDateTime(reader, "DeletedAtUtc");
            }

            return new DeleteOwnCommentMutationResult(
                CommentPublicId: returnedCommentPublicId,
                ArticlePublicId: returnedArticlePublicId,
                AuthorUserId: returnedAuthorUserId,
                Status: status,
                Version: version,
                DeletedAtUtc: deletedAtUtc,
                Changed: GetRequiredBoolean(
                    changedParameter,
                    "Interaction_Comment_DeleteOwn did not return Changed."),
                ClosedOpenCase: GetRequiredBoolean(
                    closedOpenCaseParameter,
                    "Interaction_Comment_DeleteOwn did not return ClosedOpenCase."),
                WasVisible: GetRequiredBoolean(
                    wasVisibleParameter,
                    "Interaction_Comment_DeleteOwn did not return WasVisible."));
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

    private static Comment MapComment(SqlDataReader reader)
    {
        return Comment.Rehydrate(
            commentId:
                reader.GetInt64(
                    reader.GetOrdinal("CommentId")),
            publicId:
                reader.GetString(
                    reader.GetOrdinal("PublicId")),
            articlePublicId:
                reader.GetString(
                    reader.GetOrdinal("ArticlePublicId")),
            authorUserId:
                reader.GetInt64(
                    reader.GetOrdinal("AuthorUserId")),
            parentCommentId:
                GetNullableInt64(reader, "ParentCommentId"),
            content:
                reader.GetString(
                    reader.GetOrdinal("Content")),
            status:
                reader.GetString(
                    reader.GetOrdinal("Status")),
            version:
                reader.GetInt64(
                    reader.GetOrdinal("Version")),
            createdAtUtc:
                reader.GetDateTime(
                    reader.GetOrdinal("CreatedAtUtc")),
            updatedAtUtc:
                GetNullableDateTime(reader, "UpdatedAtUtc"),
            deletedAtUtc:
                GetNullableDateTime(reader, "DeletedAtUtc"));
    }

    private static PublicCommentItemResult MapPublicCommentItem(
        SqlDataReader reader)
    {
        return new PublicCommentItemResult(
            CommentPublicId:
                reader.GetString(
                    reader.GetOrdinal("PublicId")),
            ArticlePublicId:
                reader.GetString(
                    reader.GetOrdinal("ArticlePublicId")),
            AuthorUserId:
                reader.GetInt64(
                    reader.GetOrdinal("AuthorUserId")),
            Content:
                reader.GetString(
                    reader.GetOrdinal("Content")),
            CreatedAtUtc:
                reader.GetDateTime(
                    reader.GetOrdinal("CreatedAtUtc")));
    }

    private static AdminCommentResult MapAdminCommentResult(
        SqlDataReader reader)
    {
        return new AdminCommentResult(
            CommentPublicId:
                reader.GetString(
                    reader.GetOrdinal("PublicId")),
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
                    reader.GetOrdinal("Status")),

            /*
             * V1 creates top-level comments only.
             * The SQL result contains ParentCommentId, not a public id.
             * Admin API contract keeps this field null until reply support
             * is implemented explicitly.
             */
            ParentCommentPublicId: null,

            CreatedAtUtc:
                reader.GetDateTime(
                    reader.GetOrdinal("CreatedAtUtc")),
            UpdatedAtUtc:
                GetNullableDateTime(reader, "UpdatedAtUtc"),
            DeletedAtUtc:
                GetNullableDateTime(reader, "DeletedAtUtc"),
            Version:
                reader.GetInt64(
                    reader.GetOrdinal("Version")));
    }

    private static CommentModerationResult MapCommentModerationResult(
        SqlDataReader reader)
    {
        return new CommentModerationResult(
            CommentPublicId:
                reader.GetString(
                    reader.GetOrdinal("PublicId")),
            ArticlePublicId:
                reader.GetString(
                    reader.GetOrdinal("ArticlePublicId")),
            Status:
                reader.GetString(
                    reader.GetOrdinal("Status")),
            Version:
                reader.GetInt64(
                    reader.GetOrdinal("Version")),
            UpdatedAtUtc:
                reader.GetDateTime(
                    reader.GetOrdinal("UpdatedAtUtc")));
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
