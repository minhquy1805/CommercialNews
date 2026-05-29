using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using Interaction.Application.Models.Results;
using Interaction.Application.Ports.Persistence;
using Interaction.Domain.Entities;
using Interaction.Infrastructure.Persistence.Exceptions;
using Microsoft.Data.SqlClient;

namespace Interaction.Infrastructure.Persistence.Repositories;

public sealed class ArticleInteractionTargetProjectionRepository
    : IArticleInteractionTargetProjectionRepository
{
    private const string SelectByArticlePublicIdProc =
        "[interaction].[Interaction_ArticleInteractionTargetProjection_SelectByArticlePublicId]";

    private const string ApplyProc =
        "[interaction].[Interaction_ArticleInteractionTargetProjection_Apply]";

    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly InteractionSqlExceptionTranslator _sqlExceptionTranslator;

    public ArticleInteractionTargetProjectionRepository(
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

    public async Task<ArticleInteractionTargetProjection?> GetByArticlePublicIdAsync(
        string articlePublicId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(articlePublicId);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(
                    SelectByArticlePublicIdProc,
                    cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@ArticlePublicId", SqlDbType.Char, 26)
                    {
                        Value = articlePublicId
                    });

                using SqlDataReader reader =
                    await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapProjection(reader);
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

    public async Task<ApplyArticleInteractionTargetProjectionResult> ApplyAsync(
        string articlePublicId,
        string sourceStatus,
        bool isInteractionEnabled,
        long sourceVersion,
        string sourceMessageId,
        DateTime? sourceOccurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(articlePublicId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceStatus);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceMessageId);

        SqlConnection? ownedConnection = null;
        SqlTransaction? ownedTransaction = null;

        try
        {
            SqlCommand command;

            if (_unitOfWork.HasActiveTransaction)
            {
                command = CreateTransactionalCommand(ApplyProc);
            }
            else
            {
                /*
                * The Apply procedure reads the current source version and then
                * decides whether to insert/update or ignore the incoming message.
                * A local transaction keeps that decision atomic when there is no
                * ambient Interaction transaction.
                */
                ownedConnection = _sqlConnectionFactory.CreateConnection();
                await ownedConnection.OpenAsync(cancellationToken);

                ownedTransaction =
                    (SqlTransaction)await ownedConnection.BeginTransactionAsync(
                        cancellationToken);

                command = CreateCommand(
                    ownedConnection,
                    ownedTransaction,
                    ApplyProc);
            }

            using (command)
            {
                SqlParameter applyDecisionParameter =
                    new("@ApplyDecision", SqlDbType.NVarChar, 30)
                    {
                        Direction = ParameterDirection.Output
                    };

                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticlePublicId", SqlDbType.Char, 26)
                    {
                        Value = articlePublicId
                    },
                    new SqlParameter("@SourceStatus", SqlDbType.NVarChar, 30)
                    {
                        Value = sourceStatus
                    },
                    new SqlParameter("@IsInteractionEnabled", SqlDbType.Bit)
                    {
                        Value = isInteractionEnabled
                    },
                    new SqlParameter("@SourceVersion", SqlDbType.BigInt)
                    {
                        Value = sourceVersion
                    },
                    new SqlParameter("@SourceMessageId", SqlDbType.Char, 26)
                    {
                        Value = sourceMessageId
                    },
                    new SqlParameter("@SourceOccurredAtUtc", SqlDbType.DateTime2)
                    {
                        Value = ToDbValue(sourceOccurredAtUtc),
                        Scale = 3
                    },
                    new SqlParameter("@RequiresResync", SqlDbType.Bit)
                    {
                        Value = false
                    },
                    applyDecisionParameter
                ]);

                ArticleInteractionTargetProjection? projection = null;

                using (SqlDataReader reader =
                    await command.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        projection = MapProjection(reader);
                    }
                }

                string applyDecision = GetRequiredString(
                    applyDecisionParameter,
                    "Interaction_ArticleInteractionTargetProjection_Apply did not return ApplyDecision.");

                if (ownedTransaction is not null)
                {
                    await ownedTransaction.CommitAsync(cancellationToken);
                }

                return new ApplyArticleInteractionTargetProjectionResult(
                    Projection: projection,
                    ApplyDecision: applyDecision);
            }
        }
        catch (SqlException exception)
        {
            if (ownedTransaction is not null)
            {
                await ownedTransaction.RollbackAsync(cancellationToken);
            }

            throw _sqlExceptionTranslator.Translate(exception);
        }
        catch
        {
            if (ownedTransaction is not null)
            {
                await ownedTransaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (ownedTransaction is not null)
            {
                await ownedTransaction.DisposeAsync();
            }

            if (ownedConnection is not null)
            {
                await ownedConnection.DisposeAsync();
            }
        }
    }

    private async Task<ArticleInteractionTargetProjection?>
        SelectByArticlePublicIdInternalAsync(
            string articlePublicId,
            SqlConnection connection,
            SqlTransaction? transaction,
            CancellationToken cancellationToken)
    {
        using SqlCommand command = CreateCommand(
            connection,
            transaction,
            SelectByArticlePublicIdProc);

        command.Parameters.Add(
            new SqlParameter("@ArticlePublicId", SqlDbType.Char, 26)
            {
                Value = articlePublicId
            });

        using SqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapProjection(reader);
    }

    private SqlCommand CreateTransactionalCommand(string storedProcedureName)
    {
        return CreateCommand(
            _unitOfWork.Connection,
            _unitOfWork.Transaction,
            storedProcedureName);
    }

    private async Task<(SqlCommand Command, SqlConnection? OwnedConnection)>
        CreateReadCommandAsync(
            string storedProcedureName,
            CancellationToken cancellationToken)
    {
        if (_unitOfWork.HasActiveConnection)
        {
            SqlCommand ambientCommand = CreateCommand(
                _unitOfWork.Connection,
                _unitOfWork.HasActiveTransaction
                    ? _unitOfWork.Transaction
                    : null,
                storedProcedureName);

            return (ambientCommand, null);
        }

        SqlConnection ownedConnection =
            _sqlConnectionFactory.CreateConnection();

        await ownedConnection.OpenAsync(cancellationToken);

        SqlCommand command = CreateCommand(
            ownedConnection,
            transaction: null,
            storedProcedureName);

        return (command, ownedConnection);
    }

    private static SqlCommand CreateCommand(
        SqlConnection connection,
        SqlTransaction? transaction,
        string storedProcedureName)
    {
        SqlCommand command = connection.CreateCommand();

        command.Transaction = transaction;
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        return command;
    }

    private static ArticleInteractionTargetProjection MapProjection(
        SqlDataReader reader)
    {
        return ArticleInteractionTargetProjection.Rehydrate(
            articleInteractionTargetProjectionId:
                reader.GetInt64(
                    reader.GetOrdinal("ArticleInteractionTargetProjectionId")),
            articlePublicId:
                reader.GetString(
                    reader.GetOrdinal("ArticlePublicId")),
            sourceStatus:
                reader.GetString(
                    reader.GetOrdinal("SourceStatus")),
            isInteractionEnabled:
                reader.GetBoolean(
                    reader.GetOrdinal("IsInteractionEnabled")),
            lastSourceVersion:
                reader.GetInt64(
                    reader.GetOrdinal("LastSourceVersion")),
            lastSourceMessageId:
                GetNullableString(reader, "LastSourceMessageId"),
            lastSourceOccurredAtUtc:
                GetNullableDateTime(reader, "LastSourceOccurredAtUtc"),
            lastSyncedAtUtc:
                reader.GetDateTime(
                    reader.GetOrdinal("LastSyncedAtUtc")),
            requiresResync:
                reader.GetBoolean(
                    reader.GetOrdinal("RequiresResync")),
            createdAtUtc:
                reader.GetDateTime(
                    reader.GetOrdinal("CreatedAtUtc")),
            updatedAtUtc:
                GetNullableDateTime(reader, "UpdatedAtUtc"));
    }

    private static string GetRequiredString(
        SqlParameter parameter,
        string errorMessage)
    {
        if (parameter.Value is null or DBNull)
        {
            throw new InvalidOperationException(errorMessage);
        }

        return Convert.ToString(parameter.Value)
            ?? throw new InvalidOperationException(errorMessage);
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