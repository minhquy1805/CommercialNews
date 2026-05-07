using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Identity.Application.Models.QueryModels;
using Identity.Application.Ports.Persistence;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence.Exceptions;
using Microsoft.Data.SqlClient;

namespace Identity.Infrastructure.Persistence.Repositories;

public sealed class UserAccountRepository : IUserAccountRepository
{
    private const string UserAccountInsertProc = "[identity].[UserAccount_Insert]";
    private const string UserAccountSelectByIdProc = "[identity].[UserAccount_SelectById]";
    private const string UserAccountSelectByPublicIdProc = "[identity].[UserAccount_SelectByPublicId]";
    private const string UserAccountSelectByEmailNormalizedProc = "[identity].[UserAccount_SelectByEmailNormalized]";
    private const string UserAccountSelectSkipAndTakeWhereDynamicProc = "[identity].[UserAccount_SelectSkipAndTakeWhereDynamic]";
    private const string UserAccountGetRecordCountWhereDynamicProc = "[identity].[UserAccount_GetRecordCountWhereDynamic]";
    private const string UserAccountUpdateProfileProc = "[identity].[UserAccount_UpdateProfile]";
    private const string UserAccountUpdatePasswordProc = "[identity].[UserAccount_UpdatePassword]";
    private const string UserAccountUpdateLastLoginProc = "[identity].[UserAccount_UpdateLastLogin]";
    private const string UserAccountSetEmailVerifiedProc = "[identity].[UserAccount_SetEmailVerified]";
    private const string UserAccountUpdateStatusProc = "[identity].[UserAccount_UpdateStatus]";
    private const string UserAccountActivateProc = "[identity].[UserAccount_Activate]";
    private const string UserAccountDisableProc = "[identity].[UserAccount_Disable]";
    private const string UserAccountLockProc = "[identity].[UserAccount_Lock]";
    private const string UserAccountUnlockProc = "[identity].[UserAccount_Unlock]";
    private const string UserAccountMarkEmailVerifiedProc = "[identity].[UserAccount_MarkEmailVerified]";
    private const string UserAccountInsertBootstrapAdminProc = "[identity].[UserAccount_InsertBootstrapAdmin]";

    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly IdentitySqlExceptionTranslator _sqlExceptionTranslator;

    public UserAccountRepository(
        IIdentityUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        IdentitySqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<UserAccount?> GetByIdAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return null;
        }

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(UserAccountSelectByIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@UserId", SqlDbType.BigInt) { Value = userId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapUserAccount(reader);
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

    public async Task<UserAccount?> GetByPublicIdAsync(
        string publicId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return null;
        }

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(UserAccountSelectByPublicIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@PublicId", SqlDbType.Char, 26)
                    {
                        Value = publicId.Trim()
                    });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapUserAccount(reader);
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

    public async Task<UserAccount?> GetByEmailNormalizedAsync(
        string emailNormalized,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(emailNormalized))
        {
            return null;
        }

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(UserAccountSelectByEmailNormalizedProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@EmailNormalized", SqlDbType.NVarChar, 320)
                    {
                        Value = emailNormalized.Trim()
                    });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapUserAccount(reader);
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

    public async Task<UserAccountDetailResult?> SelectDetailByIdAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return null;
        }

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(UserAccountSelectByIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@UserId", SqlDbType.BigInt) { Value = userId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapUserAccountDetailResult(reader);
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

    public async Task<PagedQueryResult<UserAccountListResultItem>> SelectSkipAndTakeAsync(
        UserAccountListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SqlConnection? ownedConnection = null;

        try
        {
            int skip = Math.Max(0, query.Skip);
            int take = query.Take <= 0 ? 20 : query.Take;
            int page = query.Page <= 0 ? 1 : query.Page;

            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(UserAccountSelectSkipAndTakeWhereDynamicProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                AddUserAccountListQueryParameters(command, query, skip, take);

                List<UserAccountListResultItem> items = [];

                using (SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        items.Add(MapUserAccountListResultItem(reader));
                    }
                }

                int totalItems = await GetRecordCountAsync(query, cancellationToken);

                return new PagedQueryResult<UserAccountListResultItem>
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

    public async Task<long> InsertAsync(
        UserAccount userAccount,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userAccount);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(UserAccountInsertProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@PublicId", SqlDbType.Char, 26) { Value = userAccount.PublicId });

                command.Parameters.Add(
                    new SqlParameter("@Email", SqlDbType.NVarChar, 320) { Value = userAccount.Email });

                command.Parameters.Add(
                    new SqlParameter("@EmailNormalized", SqlDbType.NVarChar, 320) { Value = userAccount.EmailNormalized });

                command.Parameters.Add(
                    new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 500) { Value = userAccount.PasswordHash });

                command.Parameters.Add(
                    new SqlParameter("@FullName", SqlDbType.NVarChar, 200)
                    {
                        Value = ToDbValue(userAccount.FullName)
                    });

                command.Parameters.Add(
                    new SqlParameter("@AvatarUrl", SqlDbType.NVarChar, 800)
                    {
                        Value = ToDbValue(userAccount.AvatarUrl)
                    });

                command.Parameters.Add(
                    new SqlParameter("@Status", SqlDbType.VarChar, 20)
                    {
                        Value = userAccount.Status
                    });

                SqlParameter userIdParameter = new("@UserId", SqlDbType.BigInt)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(userIdParameter);

                await command.ExecuteNonQueryAsync(cancellationToken);

                return Convert.ToInt64(userIdParameter.Value);
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

    public async Task<long> InsertBootstrapAdminAsync(
        UserAccount userAccount,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userAccount);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(UserAccountInsertBootstrapAdminProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@PublicId", SqlDbType.Char, 26) { Value = userAccount.PublicId });

                command.Parameters.Add(
                    new SqlParameter("@Email", SqlDbType.NVarChar, 320) { Value = userAccount.Email });

                command.Parameters.Add(
                    new SqlParameter("@EmailNormalized", SqlDbType.NVarChar, 320) { Value = userAccount.EmailNormalized });

                command.Parameters.Add(
                    new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 500) { Value = userAccount.PasswordHash });

                command.Parameters.Add(
                    new SqlParameter("@FullName", SqlDbType.NVarChar, 200)
                    {
                        Value = ToDbValue(userAccount.FullName)
                    });

                command.Parameters.Add(
                    new SqlParameter("@AvatarUrl", SqlDbType.NVarChar, 800)
                    {
                        Value = ToDbValue(userAccount.AvatarUrl)
                    });

                SqlParameter userIdParameter = new("@UserId", SqlDbType.BigInt)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(userIdParameter);

                await command.ExecuteNonQueryAsync(cancellationToken);

                return Convert.ToInt64(userIdParameter.Value);
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

    public async Task<bool> UpdateProfileAsync(
        long userId,
        string? fullName,
        string? avatarUrl,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return false;
        }

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(UserAccountUpdateProfileProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@UserId", SqlDbType.BigInt) { Value = userId });

                command.Parameters.Add(
                    new SqlParameter("@FullName", SqlDbType.NVarChar, 200)
                    {
                        Value = ToTrimmedDbValue(fullName)
                    });

                command.Parameters.Add(
                    new SqlParameter("@AvatarUrl", SqlDbType.NVarChar, 800)
                    {
                        Value = ToTrimmedDbValue(avatarUrl)
                    });

                return await ExecuteAffectedRowsAsync(command, cancellationToken);
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

    public async Task<bool> UpdatePasswordAsync(
        long userId,
        string passwordHash,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(UserAccountUpdatePasswordProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@UserId", SqlDbType.BigInt) { Value = userId });

                command.Parameters.Add(
                    new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 500)
                    {
                        Value = passwordHash.Trim()
                    });

                return await ExecuteAffectedRowsAsync(command, cancellationToken);
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

    public async Task<bool> UpdateLastLoginAsync(
        long userId,
        DateTime lastLoginAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return false;
        }

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(UserAccountUpdateLastLoginProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@UserId", SqlDbType.BigInt) { Value = userId });

                command.Parameters.Add(
                    new SqlParameter("@LastLoginAt", SqlDbType.DateTime2) { Value = lastLoginAtUtc });

                return await ExecuteAffectedRowsAsync(command, cancellationToken);
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

    public async Task<bool> SetEmailVerifiedAsync(
        long userId,
        DateTime verifiedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return false;
        }

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(UserAccountSetEmailVerifiedProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@UserId", SqlDbType.BigInt)
                    {
                        Value = userId
                    });

                command.Parameters.Add(
                    new SqlParameter("@EmailVerifiedAt", SqlDbType.DateTime2)
                    {
                        Value = verifiedAtUtc
                    });

                return await ExecuteAffectedRowsAsync(command, cancellationToken);
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

    public async Task<bool> UpdateStatusAsync(
        long userId,
        string status,
        DateTime? lockedUntil,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(UserAccountUpdateStatusProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@UserId", SqlDbType.BigInt) { Value = userId });

                command.Parameters.Add(
                    new SqlParameter("@Status", SqlDbType.VarChar, 20)
                    {
                        Value = status.Trim()
                    });

                command.Parameters.Add(
                    new SqlParameter("@LockedUntil", SqlDbType.DateTime2)
                    {
                        Value = ToDbValue(lockedUntil)
                    });

                return await ExecuteAffectedRowsAsync(command, cancellationToken);
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

    public async Task<bool> ActivateAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return false;
        }

        return await ExecuteUserAccountMutationAsync(
            UserAccountActivateProc,
            userId,
            cancellationToken);
    }

    public async Task<bool> DisableAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return false;
        }

        return await ExecuteUserAccountMutationAsync(
            UserAccountDisableProc,
            userId,
            cancellationToken);
    }

    public async Task<bool> LockAsync(
        long userId,
        DateTime lockedUntilUtc,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return false;
        }

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(UserAccountLockProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@UserId", SqlDbType.BigInt) { Value = userId });

                command.Parameters.Add(
                    new SqlParameter("@LockedUntil", SqlDbType.DateTime2) { Value = lockedUntilUtc });

                return await ExecuteAffectedRowsAsync(command, cancellationToken);
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

    public async Task<bool> UnlockAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return false;
        }

        return await ExecuteUserAccountMutationAsync(
            UserAccountUnlockProc,
            userId,
            cancellationToken);
    }

    public async Task<bool> MarkEmailVerifiedAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return false;
        }

        return await ExecuteUserAccountMutationAsync(
            UserAccountMarkEmailVerifiedProc,
            userId,
            cancellationToken);
    }

    private async Task<int> GetRecordCountAsync(
        UserAccountListQuery query,
        CancellationToken cancellationToken)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(UserAccountGetRecordCountWhereDynamicProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                AddUserAccountListQueryParameters(command, query);

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

    private async Task<bool> ExecuteUserAccountMutationAsync(
        string storedProcedureName,
        long userId,
        CancellationToken cancellationToken)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(storedProcedureName, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@UserId", SqlDbType.BigInt) { Value = userId });

                return await ExecuteAffectedRowsAsync(command, cancellationToken);
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

    private static async Task<bool> ExecuteAffectedRowsAsync(
        SqlCommand command,
        CancellationToken cancellationToken)
    {
        SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(affectedRowsParameter);

        await command.ExecuteNonQueryAsync(cancellationToken);

        int affectedRows = affectedRowsParameter.Value is DBNull
            ? 0
            : Convert.ToInt32(affectedRowsParameter.Value);

        return affectedRows > 0;
    }

    private static void AddUserAccountListQueryParameters(
        SqlCommand command,
        UserAccountListQuery query,
        int? skip = null,
        int? take = null)
    {
        command.Parameters.AddRange(
        [
            new SqlParameter("@FromCreatedAt", SqlDbType.DateTime2) { Value = ToDbValue(query.FromCreatedAt) },
            new SqlParameter("@ToCreatedAt", SqlDbType.DateTime2) { Value = ToDbValue(query.ToCreatedAt) },
            new SqlParameter("@Status", SqlDbType.VarChar, 20) { Value = ToTrimmedDbValue(query.Status) },
            new SqlParameter("@IsEmailVerified", SqlDbType.Bit) { Value = ToDbValue(query.IsEmailVerified) },
            new SqlParameter("@Query", SqlDbType.NVarChar, 320) { Value = ToTrimmedDbValue(query.Query) }
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

    private static UserAccount MapUserAccount(SqlDataReader reader)
    {
        return UserAccount.Rehydrate(
            userId: reader.GetInt64(reader.GetOrdinal("UserId")),
            publicId: reader.GetString(reader.GetOrdinal("PublicId")),
            email: reader.GetString(reader.GetOrdinal("Email")),
            emailNormalized: reader.GetString(reader.GetOrdinal("EmailNormalized")),
            passwordHash: reader.GetString(reader.GetOrdinal("PasswordHash")),
            fullName: ReadNullableString(reader, "FullName"),
            avatarUrl: ReadNullableString(reader, "AvatarUrl"),
            isEmailVerified: reader.GetBoolean(reader.GetOrdinal("IsEmailVerified")),
            emailVerifiedAt: ReadNullableDateTime(reader, "EmailVerifiedAt"),
            status: reader.GetString(reader.GetOrdinal("Status")),
            lockedUntil: ReadNullableDateTime(reader, "LockedUntil"),
            createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            updatedAt: reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            lastLoginAt: ReadNullableDateTime(reader, "LastLoginAt"),
            version: reader.GetInt32(reader.GetOrdinal("Version")));
    }

    private static UserAccountDetailResult MapUserAccountDetailResult(SqlDataReader reader)
    {
        return new UserAccountDetailResult
        {
            UserId = reader.GetInt64(reader.GetOrdinal("UserId")),
            PublicId = reader.GetString(reader.GetOrdinal("PublicId")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            EmailNormalized = reader.GetString(reader.GetOrdinal("EmailNormalized")),
            FullName = ReadNullableString(reader, "FullName"),
            AvatarUrl = ReadNullableString(reader, "AvatarUrl"),
            IsEmailVerified = reader.GetBoolean(reader.GetOrdinal("IsEmailVerified")),
            EmailVerifiedAt = ReadNullableDateTime(reader, "EmailVerifiedAt"),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            LockedUntil = ReadNullableDateTime(reader, "LockedUntil"),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            LastLoginAt = ReadNullableDateTime(reader, "LastLoginAt"),
            Version = reader.GetInt32(reader.GetOrdinal("Version"))
        };
    }

    private static UserAccountListResultItem MapUserAccountListResultItem(SqlDataReader reader)
    {
        return new UserAccountListResultItem
        {
            UserId = reader.GetInt64(reader.GetOrdinal("UserId")),
            PublicId = reader.GetString(reader.GetOrdinal("PublicId")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            EmailNormalized = reader.GetString(reader.GetOrdinal("EmailNormalized")),
            FullName = ReadNullableString(reader, "FullName"),
            AvatarUrl = ReadNullableString(reader, "AvatarUrl"),
            IsEmailVerified = reader.GetBoolean(reader.GetOrdinal("IsEmailVerified")),
            EmailVerifiedAt = ReadNullableDateTime(reader, "EmailVerifiedAt"),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            LockedUntil = ReadNullableDateTime(reader, "LockedUntil"),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            LastLoginAt = ReadNullableDateTime(reader, "LastLoginAt"),
            Version = reader.GetInt32(reader.GetOrdinal("Version"))
        };
    }

    private static string? ReadNullableString(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTime? ReadNullableDateTime(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }

    private static object ToDbValue(string? value)
    {
        return value is not null ? value : DBNull.Value;
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
        return !string.IsNullOrWhiteSpace(value) ? value.Trim() : DBNull.Value;
    }
}
