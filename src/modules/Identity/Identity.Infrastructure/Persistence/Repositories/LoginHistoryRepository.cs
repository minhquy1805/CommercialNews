using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Identity.Application.Models.QueryModels;
using Identity.Application.Ports.Persistence;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence.Exceptions;
using Microsoft.Data.SqlClient;

namespace Identity.Infrastructure.Persistence.Repositories;

public sealed class LoginHistoryRepository : ILoginHistoryRepository
{
    private const string LoginHistoryInsertProc = "[identity].[LoginHistory_Insert]";
    private const string LoginHistorySelectByUserIdProc = "[identity].[LoginHistory_SelectByUserId]";
    private const string LoginHistorySelectRecentProc = "[identity].[LoginHistory_SelectRecent]";
    private const string LoginHistorySelectSkipAndTakeByUserIdProc = "[identity].[LoginHistory_SelectSkipAndTakeByUserId]";
    private const string LoginHistoryGetRecordCountByUserIdProc = "[identity].[LoginHistory_GetRecordCountByUserId]";

    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly IdentitySqlExceptionTranslator _sqlExceptionTranslator;

    public LoginHistoryRepository(
        IIdentityUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        IdentitySqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<long> InsertAsync(
        LoginHistory loginHistory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(loginHistory);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(LoginHistoryInsertProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                SqlParameter loginIdParameter = AddInsertParameters(command, loginHistory);

                await command.ExecuteNonQueryAsync(cancellationToken);

                return loginIdParameter.Value is DBNull
                    ? 0
                    : Convert.ToInt64(loginIdParameter.Value);
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

    public async Task<IReadOnlyList<LoginHistory>> GetByUserIdAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return [];
        }

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(LoginHistorySelectByUserIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@UserId", SqlDbType.BigInt)
                    {
                        Value = userId
                    });

                List<LoginHistory> items = [];

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(MapLoginHistory(reader));
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

    public async Task<IReadOnlyList<LoginHistory>> GetRecentAsync(
        int topN = 100,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(LoginHistorySelectRecentProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@TopN", SqlDbType.Int)
                    {
                        Value = topN <= 0 ? 100 : topN
                    });

                List<LoginHistory> items = [];

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(MapLoginHistory(reader));
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

    public async Task<PagedQueryResult<LoginHistoryListResultItem>> SelectByUserIdAsync(
        LoginHistoryByUserQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.Page <= 0 ? 1 : query.Page;
        int take = query.Take <= 0 ? 20 : query.Take;

        if (query.UserId <= 0)
        {
            return new PagedQueryResult<LoginHistoryListResultItem>
            {
                Items = [],
                Page = page,
                PageSize = take,
                TotalItems = 0
            };
        }

        SqlConnection? ownedConnection = null;

        try
        {
            int skip = Math.Max(0, query.Skip);

            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(LoginHistorySelectSkipAndTakeByUserIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                AddLoginHistoryByUserQueryParameters(command, query, skip, take);

                List<LoginHistoryListResultItem> items = [];

                using (SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        items.Add(MapLoginHistoryListResultItem(reader));
                    }
                }

                int totalItems = await GetRecordCountByUserIdAsync(query, cancellationToken);

                return new PagedQueryResult<LoginHistoryListResultItem>
                {
                    Items = items,
                    Page = page,
                    PageSize = take,
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

    private async Task<int> GetRecordCountByUserIdAsync(
        LoginHistoryByUserQuery query,
        CancellationToken cancellationToken)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(LoginHistoryGetRecordCountByUserIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                AddLoginHistoryByUserQueryParameters(command, query);

                object? scalar = await command.ExecuteScalarAsync(cancellationToken);

                long total = scalar is null or DBNull
                    ? 0L
                    : Convert.ToInt64(scalar);

                return total > int.MaxValue
                    ? int.MaxValue
                    : (int)total;
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

    private async Task<(SqlCommand Command, SqlConnection? OwnedConnection)> CreateCommandAsync(
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

    private static SqlParameter AddInsertParameters(
        SqlCommand command,
        LoginHistory loginHistory)
    {
        command.Parameters.Add(
            new SqlParameter("@UserId", SqlDbType.BigInt)
            {
                Value = ToDbValue(loginHistory.UserId)
            });

        command.Parameters.Add(
            new SqlParameter("@EmailNormalizedAttempted", SqlDbType.NVarChar, 320)
            {
                Value = ToTrimmedDbValue(loginHistory.EmailNormalizedAttempted)
            });

        command.Parameters.Add(
            new SqlParameter("@Succeeded", SqlDbType.Bit)
            {
                Value = loginHistory.Succeeded
            });

        command.Parameters.Add(
            new SqlParameter("@FailureReason", SqlDbType.NVarChar, 100)
            {
                Value = ToTrimmedDbValue(loginHistory.FailureReason)
            });

        command.Parameters.Add(
            new SqlParameter("@IpAddress", SqlDbType.NVarChar, 45)
            {
                Value = ToTrimmedDbValue(loginHistory.IpAddress)
            });

        command.Parameters.Add(
            new SqlParameter("@UserAgent", SqlDbType.NVarChar, 300)
            {
                Value = ToTrimmedDbValue(loginHistory.UserAgent)
            });

        command.Parameters.Add(
            new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100)
            {
                Value = ToTrimmedDbValue(loginHistory.CorrelationId)
            });

        SqlParameter loginIdParameter = new("@LoginId", SqlDbType.BigInt)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(loginIdParameter);

        return loginIdParameter;
    }

    private static void AddLoginHistoryByUserQueryParameters(
        SqlCommand command,
        LoginHistoryByUserQuery query,
        int? skip = null,
        int? take = null)
    {
        command.Parameters.AddRange(
        [
            new SqlParameter("@UserId", SqlDbType.BigInt) { Value = query.UserId },
            new SqlParameter("@Succeeded", SqlDbType.Bit) { Value = ToDbValue(query.Succeeded) },
            new SqlParameter("@FromAttemptedAt", SqlDbType.DateTime2) { Value = ToDbValue(query.FromAttemptedAt) },
            new SqlParameter("@ToAttemptedAt", SqlDbType.DateTime2) { Value = ToDbValue(query.ToAttemptedAt) }
        ]);

        if (skip.HasValue)
        {
            command.Parameters.Add(
                new SqlParameter("@Skip", SqlDbType.Int) { Value = skip.Value });
        }

        if (take.HasValue)
        {
            command.Parameters.Add(
                new SqlParameter("@Take", SqlDbType.Int) { Value = take.Value });
        }
    }

    private static LoginHistory MapLoginHistory(SqlDataReader reader)
    {
        return LoginHistory.Rehydrate(
            loginId: reader.GetInt64(reader.GetOrdinal("LoginId")),
            userId: ReadNullableInt64(reader, "UserId"),
            emailNormalizedAttempted: ReadNullableString(reader, "EmailNormalizedAttempted"),
            succeeded: reader.GetBoolean(reader.GetOrdinal("Succeeded")),
            failureReason: ReadNullableString(reader, "FailureReason"),
            attemptedAt: reader.GetDateTime(reader.GetOrdinal("AttemptedAt")),
            ipAddress: ReadNullableString(reader, "IpAddress"),
            userAgent: ReadNullableString(reader, "UserAgent"),
            correlationId: ReadNullableString(reader, "CorrelationId"));
    }

    private static LoginHistoryListResultItem MapLoginHistoryListResultItem(SqlDataReader reader)
    {
        return new LoginHistoryListResultItem
        {
            LoginId = reader.GetInt64(reader.GetOrdinal("LoginId")),
            UserId = ReadNullableInt64(reader, "UserId"),
            Succeeded = reader.GetBoolean(reader.GetOrdinal("Succeeded")),
            FailureReason = ReadNullableString(reader, "FailureReason"),
            AttemptedAt = reader.GetDateTime(reader.GetOrdinal("AttemptedAt")),
            IpAddress = ReadNullableString(reader, "IpAddress"),
            UserAgent = ReadNullableString(reader, "UserAgent"),
            CorrelationId = ReadNullableString(reader, "CorrelationId")
        };
    }

    private static long? ReadNullableInt64(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }

    private static string? ReadNullableString(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static object ToDbValue(long? value)
    {
        return value.HasValue ? value.Value : DBNull.Value;
    }

    private static object ToDbValue(DateTime? value)
    {
        return value.HasValue ? value.Value : DBNull.Value;
    }

    private static object ToDbValue(bool? value)
    {
        return value.HasValue ? value.Value : DBNull.Value;
    }

    private static object ToTrimmedDbValue(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : DBNull.Value;
    }
}
