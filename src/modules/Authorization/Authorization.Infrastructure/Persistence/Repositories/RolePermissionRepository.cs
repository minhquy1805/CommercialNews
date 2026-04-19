using System.Data;
using Authorization.Application.Models.QueryModels;
using Authorization.Application.Ports.Persistence;
using Authorization.Domain.Entities;
using Authorization.Infrastructure.Persistence.Exceptions;
using Authorization.Infrastructure.Persistence.Sql;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using Microsoft.Data.SqlClient;

namespace Authorization.Infrastructure.Persistence.Repositories;

public sealed class RolePermissionRepository : IRolePermissionRepository
{
    private const string RolePermissionGrantProc = "[authorization].[RolePermission_Grant]";
    private const string RolePermissionRevokeProc = "[authorization].[RolePermission_Revoke]";
    private const string RolePermissionSelectByRoleIdAndPermissionIdProc =
        "[authorization].[RolePermission_SelectByRoleIdAndPermissionId]";
    private const string RolePermissionSelectPermissionsByRoleIdProc =
        "[authorization].[RolePermission_SelectPermissionsByRoleId]";
    private const string RolePermissionSelectRolesByPermissionIdProc =
        "[authorization].[RolePermission_SelectRolesByPermissionId]";

    private readonly AuthorizationUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly AuthorizationSqlExceptionTranslator _sqlExceptionTranslator;

    public RolePermissionRepository(
        AuthorizationUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        AuthorizationSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<RolePermission?> GetByRoleIdAndPermissionIdAsync(
        long roleId,
        long permissionId,
        CancellationToken cancellationToken = default)
    {
        if (roleId <= 0 || permissionId <= 0)
        {
            return null;
        }

        SqlConnection? ownedConnection = null;

        try
        {
            var (command, connection) = await CreateCommandAsync(
                RolePermissionSelectByRoleIdAndPermissionIdProc,
                cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@RoleId", SqlDbType.BigInt)
                    {
                        Value = roleId
                    });

                command.Parameters.Add(
                    new SqlParameter("@PermissionId", SqlDbType.BigInt)
                    {
                        Value = permissionId
                    });

                using var reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapRolePermission(reader);
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

    public async Task<RolePermission> InsertAsync(
        RolePermission rolePermission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rolePermission);

        SqlConnection? ownedConnection = null;

        try
        {
            var (command, connection) = await CreateCommandAsync(
                RolePermissionGrantProc,
                cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@RoleId", SqlDbType.BigInt)
                    {
                        Value = rolePermission.RoleId
                    });

                command.Parameters.Add(
                    new SqlParameter("@PermissionId", SqlDbType.BigInt)
                    {
                        Value = rolePermission.PermissionId
                    });

                command.Parameters.Add(
                    new SqlParameter("@GrantedByUserId", SqlDbType.BigInt)
                    {
                        Value = (object?)rolePermission.GrantedByUserId ?? DBNull.Value
                    });

                var affectedRowsParameter = new SqlParameter("@AffectedRows", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(affectedRowsParameter);

                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            var createdOrExisting = await GetByRoleIdAndPermissionIdAsync(
                rolePermission.RoleId,
                rolePermission.PermissionId,
                cancellationToken);

            if (createdOrExisting is null)
            {
                throw new InvalidOperationException(
                    "RolePermission_Grant completed but the role-permission grant could not be reloaded.");
            }

            return createdOrExisting;
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

    public async Task<bool> RevokeAsync(
        long roleId,
        long permissionId,
        CancellationToken cancellationToken = default)
    {
        if (roleId <= 0 || permissionId <= 0)
        {
            return false;
        }

        SqlConnection? ownedConnection = null;

        try
        {
            var (command, connection) = await CreateCommandAsync(
                RolePermissionRevokeProc,
                cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@RoleId", SqlDbType.BigInt)
                    {
                        Value = roleId
                    });

                command.Parameters.Add(
                    new SqlParameter("@PermissionId", SqlDbType.BigInt)
                    {
                        Value = permissionId
                    });

                var affectedRowsParameter = new SqlParameter("@AffectedRows", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(affectedRowsParameter);

                await command.ExecuteNonQueryAsync(cancellationToken);

                var affectedRows = affectedRowsParameter.Value is DBNull
                    ? 0
                    : Convert.ToInt32(affectedRowsParameter.Value);

                return affectedRows > 0;
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

    public async Task<IReadOnlyList<RolePermissionListResultItem>> GetPermissionsByRoleIdAsync(
        long roleId,
        CancellationToken cancellationToken = default)
    {
        if (roleId <= 0)
        {
            return Array.Empty<RolePermissionListResultItem>();
        }

        List<RolePermissionListResultItem> result = [];
        SqlConnection? ownedConnection = null;

        try
        {
            var (command, connection) = await CreateCommandAsync(
                RolePermissionSelectPermissionsByRoleIdProc,
                cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@RoleId", SqlDbType.BigInt)
                    {
                        Value = roleId
                    });

                using var reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    result.Add(MapRolePermissionListResultItem(reader));
                }

                return result;
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

    public async Task<IReadOnlyList<PermissionRoleListResultItem>> GetRolesByPermissionIdAsync(
        long permissionId,
        CancellationToken cancellationToken = default)
    {
        if (permissionId <= 0)
        {
            return Array.Empty<PermissionRoleListResultItem>();
        }

        List<PermissionRoleListResultItem> result = [];
        SqlConnection? ownedConnection = null;

        try
        {
            var (command, connection) = await CreateCommandAsync(
                RolePermissionSelectRolesByPermissionIdProc,
                cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@PermissionId", SqlDbType.BigInt)
                    {
                        Value = permissionId
                    });

                using var reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    result.Add(MapPermissionRoleListResultItem(reader));
                }

                return result;
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
            var ambientCommand = _unitOfWork.Connection.CreateCommand();
            ambientCommand.Transaction = _unitOfWork.HasActiveTransaction
                ? _unitOfWork.Transaction
                : null;
            ambientCommand.CommandText = storedProcedureName;
            ambientCommand.CommandType = CommandType.StoredProcedure;

            return (ambientCommand, null);
        }

        var ownedConnection = _sqlConnectionFactory.CreateConnection();
        await ownedConnection.OpenAsync(cancellationToken);

        var command = ownedConnection.CreateCommand();
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        return (command, ownedConnection);
    }

    private static RolePermission MapRolePermission(SqlDataReader reader)
    {
        return RolePermission.Rehydrate(
            roleId: reader.GetInt64(reader.GetOrdinal("RoleId")),
            permissionId: reader.GetInt64(reader.GetOrdinal("PermissionId")),
            grantedAt: reader.GetDateTime(reader.GetOrdinal("GrantedAt")),
            grantedByUserId: ReadNullableInt64(reader, "GrantedByUserId"));
    }

    private static RolePermissionListResultItem MapRolePermissionListResultItem(SqlDataReader reader)
    {
        return new RolePermissionListResultItem
        {
            PermissionId = reader.GetInt64(reader.GetOrdinal("PermissionId")),
            PublicId = reader.GetString(reader.GetOrdinal("PermissionPublicId")),
            Key = reader.GetString(reader.GetOrdinal("PermissionKey")),
            KeyNormalized = reader.GetString(reader.GetOrdinal("PermissionKeyNormalized")),
            Description = ReadNullableString(reader, "PermissionDescription"),
            Module = ReadNullableString(reader, "PermissionModule"),
            Action = ReadNullableString(reader, "PermissionAction"),
            IsSystem = reader.GetBoolean(reader.GetOrdinal("PermissionIsSystem")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("PermissionIsActive")),
            GrantedAt = reader.GetDateTime(reader.GetOrdinal("GrantedAt")),
            GrantedByUserId = ReadNullableInt64(reader, "GrantedByUserId")
        };
    }

    private static PermissionRoleListResultItem MapPermissionRoleListResultItem(SqlDataReader reader)
    {
        return new PermissionRoleListResultItem
        {
            RoleId = reader.GetInt64(reader.GetOrdinal("RoleId")),
            PublicId = reader.GetString(reader.GetOrdinal("RolePublicId")),
            Name = reader.GetString(reader.GetOrdinal("RoleName")),
            NameNormalized = reader.GetString(reader.GetOrdinal("RoleNameNormalized")),
            DisplayName = ReadNullableString(reader, "RoleDisplayName"),
            Description = ReadNullableString(reader, "RoleDescription"),
            IsSystem = reader.GetBoolean(reader.GetOrdinal("RoleIsSystem")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("RoleIsActive")),
            GrantedAt = reader.GetDateTime(reader.GetOrdinal("GrantedAt")),
            GrantedByUserId = ReadNullableInt64(reader, "GrantedByUserId")
        };
    }

    private static string? ReadNullableString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static long? ReadNullableInt64(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }
}