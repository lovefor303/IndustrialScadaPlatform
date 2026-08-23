using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;

namespace Scada.Gateway;

public sealed class AuthStore
{
    private readonly string _connectionString;
    private readonly PasswordHasher<AuthUser> _passwordHasher = new();

    public AuthStore(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = false
        };
        _connectionString = builder.ToString();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Users (
                Id TEXT NOT NULL PRIMARY KEY,
                UserName TEXT NOT NULL COLLATE NOCASE UNIQUE,
                PasswordHash TEXT NOT NULL,
                Enabled INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS UserRoles (
                UserId TEXT NOT NULL PRIMARY KEY,
                Role TEXT NOT NULL,
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<AuthUser> CreateFirstAdminAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        await EnsureNoUsersAsync(cancellationToken).ConfigureAwait(false);
        return await CreateUserAsync(userName, password, AuthRole.Admin, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<AuthUser> CreateUserAsync(
        string userName,
        string password,
        AuthRole role,
        CancellationToken cancellationToken = default)
    {
        ValidateCredentials(userName, password);
        var user = new AuthUser
        {
            Id = Guid.NewGuid(),
            UserName = userName.Trim(),
            Role = role,
            Enabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var hash = _passwordHasher.HashPassword(user, password);

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            await using var insertUser = connection.CreateCommand();
            insertUser.Transaction = (SqliteTransaction)transaction;
            insertUser.CommandText = """
                INSERT INTO Users(Id, UserName, PasswordHash, Enabled, CreatedAt, UpdatedAt)
                VALUES ($id, $userName, $passwordHash, 1, $createdAt, $updatedAt);
                """;
            insertUser.Parameters.AddWithValue("$id", user.Id.ToString("D"));
            insertUser.Parameters.AddWithValue("$userName", user.UserName);
            insertUser.Parameters.AddWithValue("$passwordHash", hash);
            insertUser.Parameters.AddWithValue("$createdAt", user.CreatedAt.ToUniversalTime().ToString("O"));
            insertUser.Parameters.AddWithValue("$updatedAt", user.UpdatedAt.ToUniversalTime().ToString("O"));
            await insertUser.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            await using var insertRole = connection.CreateCommand();
            insertRole.Transaction = (SqliteTransaction)transaction;
            insertRole.CommandText = "INSERT INTO UserRoles(UserId, Role) VALUES ($userId, $role);";
            insertRole.Parameters.AddWithValue("$userId", user.Id.ToString("D"));
            insertRole.Parameters.AddWithValue("$role", role.ToString());
            await insertRole.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return user;
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException("用户名已存在。", exception);
        }
    }

    public async Task<AuthUser?> AuthenticateAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT u.Id, u.UserName, u.PasswordHash, u.Enabled, u.CreatedAt, u.UpdatedAt, r.Role
            FROM Users u JOIN UserRoles r ON r.UserId = u.Id
            WHERE u.UserName = $userName LIMIT 1;
            """;
        command.Parameters.AddWithValue("$userName", userName.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var user = ReadUser(reader);
        var hash = reader.GetString(2);
        if (!user.Enabled || _passwordHasher.VerifyHashedPassword(user, hash, password)
            == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return user;
    }

    public async Task<AuthUser?> GetUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT u.Id, u.UserName, u.PasswordHash, u.Enabled, u.CreatedAt, u.UpdatedAt, r.Role
            FROM Users u JOIN UserRoles r ON r.UserId = u.Id WHERE u.Id = $id;
            """;
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? ReadUser(reader) : null;
    }

    public async Task SetEnabledAsync(Guid id, bool enabled, CancellationToken cancellationToken = default) =>
        await ExecuteUserUpdateAsync(
            id,
            "UPDATE Users SET Enabled = $enabled, UpdatedAt = $updatedAt WHERE Id = $id;",
            command => command.Parameters.AddWithValue("$enabled", enabled ? 1 : 0),
            cancellationToken).ConfigureAwait(false);

    public async Task SetRoleAsync(Guid id, AuthRole role, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var roleCommand = connection.CreateCommand();
        roleCommand.Transaction = (SqliteTransaction)transaction;
        roleCommand.CommandText = "UPDATE UserRoles SET Role = $role WHERE UserId = $id;";
        roleCommand.Parameters.AddWithValue("$role", role.ToString());
        roleCommand.Parameters.AddWithValue("$id", id.ToString("D"));
        await roleCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        await using var userCommand = connection.CreateCommand();
        userCommand.Transaction = (SqliteTransaction)transaction;
        userCommand.CommandText = "UPDATE Users SET UpdatedAt = $updatedAt WHERE Id = $id;";
        userCommand.Parameters.AddWithValue("$updatedAt", DateTimeOffset.UtcNow.ToString("O"));
        userCommand.Parameters.AddWithValue("$id", id.ToString("D"));
        await userCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureNoUsersAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM Users LIMIT 1);";
        if (Convert.ToInt32(
                await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false),
                System.Globalization.CultureInfo.InvariantCulture) != 0)
        {
            throw new InvalidOperationException("首次管理员只能创建一次。 ");
        }
    }

    private async Task ExecuteUserUpdateAsync(
        Guid id,
        string sql,
        Action<SqliteCommand> addParameters,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        command.Parameters.AddWithValue("$updatedAt", DateTimeOffset.UtcNow.ToString("O"));
        addParameters(command);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    private static AuthUser ReadUser(SqliteDataReader reader) =>
        new()
        {
            Id = Guid.Parse(reader.GetString(0)),
            UserName = reader.GetString(1),
            Enabled = reader.GetInt64(3) != 0,
            CreatedAt = DateTimeOffset.Parse(reader.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind),
            UpdatedAt = DateTimeOffset.Parse(reader.GetString(5), null, System.Globalization.DateTimeStyles.RoundtripKind),
            Role = Enum.Parse<AuthRole>(reader.GetString(6), ignoreCase: false)
        };

    private static void ValidateCredentials(string userName, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        if (string.IsNullOrEmpty(password) || password.Length < 8)
        {
            throw new ArgumentException("密码至少需要 8 个字符。", nameof(password));
        }
    }
}
