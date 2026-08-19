using Microsoft.Data.Sqlite;
using Scada.Core;
using Scada.Scene;

namespace Scada.Storage;

public sealed class RevisionStore
{
    private readonly string _connectionString;
    private readonly JsonProjectSerializer _serializer;

    public RevisionStore(string databasePath, JsonProjectSerializer? serializer = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var fullPath = Path.GetFullPath(databasePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        }.ToString();
        _serializer = serializer ?? new JsonProjectSerializer();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Projects(
                ProjectId TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                DraftJson TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Revisions(
                ProjectId TEXT NOT NULL,
                RevisionId TEXT PRIMARY KEY,
                RevisionNumber INTEGER NOT NULL,
                Author TEXT NOT NULL,
                Json TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UNIQUE(ProjectId, RevisionNumber)
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SaveDraftAsync(
        ProjectDocument project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        var json = _serializer.Serialize(project);
        var errors = _serializer.Validate(json);
        if (errors.Count > 0)
        {
            throw new InvalidDataException(FormatErrors(errors));
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Projects(ProjectId, Name, DraftJson, UpdatedAt)
            VALUES($projectId, $name, $json, $updatedAt)
            ON CONFLICT(ProjectId) DO UPDATE SET
                Name = excluded.Name,
                DraftJson = excluded.DraftJson,
                UpdatedAt = excluded.UpdatedAt;
            """;
        command.Parameters.AddWithValue("$projectId", project.ProjectId.ToString());
        command.Parameters.AddWithValue("$name", project.Name);
        command.Parameters.AddWithValue("$json", json);
        command.Parameters.AddWithValue("$updatedAt", project.UpdatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<ProjectDocument?> LoadDraftAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT DraftJson FROM Projects WHERE ProjectId = $projectId;";
        command.Parameters.AddWithValue("$projectId", projectId.ToString());
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is string json ? _serializer.Deserialize(json) : null;
    }

    public async Task<ProjectRevision> PublishAsync(
        Guid projectId,
        string author,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(author);
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var json = await ReadDraftJsonAsync(connection, projectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Project '{projectId}' does not have a draft.");
        var errors = _serializer.Validate(json);
        if (errors.Count > 0)
        {
            throw new InvalidDataException(FormatErrors(errors));
        }

        var revisionNumber = await ReadNextRevisionNumberAsync(connection, projectId, cancellationToken);
        var revision = new ProjectRevision(
            Guid.NewGuid(),
            projectId,
            revisionNumber,
            author.Trim(),
            DateTimeOffset.UtcNow);

        await using var insert = connection.CreateCommand();
        insert.Transaction = (SqliteTransaction)transaction;
        insert.CommandText = """
            INSERT INTO Revisions(ProjectId, RevisionId, RevisionNumber, Author, Json, CreatedAt)
            VALUES($projectId, $revisionId, $revisionNumber, $author, $json, $createdAt);
            """;
        insert.Parameters.AddWithValue("$projectId", projectId.ToString());
        insert.Parameters.AddWithValue("$revisionId", revision.RevisionId.ToString());
        insert.Parameters.AddWithValue("$revisionNumber", revision.RevisionNumber);
        insert.Parameters.AddWithValue("$author", revision.Author);
        insert.Parameters.AddWithValue("$json", json);
        insert.Parameters.AddWithValue("$createdAt", revision.CreatedAt.ToString("O"));
        await insert.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return revision;
    }

    public async Task<IReadOnlyList<ProjectRevision>> ListRevisionsAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT RevisionId, RevisionNumber, Author, CreatedAt
            FROM Revisions
            WHERE ProjectId = $projectId
            ORDER BY RevisionNumber;
            """;
        command.Parameters.AddWithValue("$projectId", projectId.ToString());

        var revisions = new List<ProjectRevision>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            revisions.Add(new ProjectRevision(
                Guid.Parse(reader.GetString(0)),
                projectId,
                reader.GetInt32(1),
                reader.GetString(2),
                DateTimeOffset.Parse(reader.GetString(3), System.Globalization.CultureInfo.InvariantCulture)));
        }

        return revisions;
    }

    public async Task<ProjectDocument> LoadRevisionAsync(
        Guid projectId,
        Guid revisionId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Json FROM Revisions
            WHERE ProjectId = $projectId AND RevisionId = $revisionId;
            """;
        command.Parameters.AddWithValue("$projectId", projectId.ToString());
        command.Parameters.AddWithValue("$revisionId", revisionId.ToString());
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is string json
            ? _serializer.Deserialize(json)
            : throw new KeyNotFoundException($"Revision '{revisionId}' was not found for project '{projectId}'.");
    }

    public async Task RestoreToDraftAsync(
        Guid projectId,
        Guid revisionId,
        CancellationToken cancellationToken = default)
    {
        var project = await LoadRevisionAsync(projectId, revisionId, cancellationToken);
        await SaveDraftAsync(project, cancellationToken);
    }

    public async Task ExportAsync(
        Guid projectId,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var project = await LoadDraftAsync(projectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Project '{projectId}' does not have a draft.");
        var fullPath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(fullPath, _serializer.Serialize(project), cancellationToken);
    }

    public async Task<Guid> ImportAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var json = await File.ReadAllTextAsync(Path.GetFullPath(filePath), cancellationToken);
        var project = _serializer.Deserialize(json);
        await SaveDraftAsync(project, cancellationToken);
        return project.ProjectId;
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task<string?> ReadDraftJsonAsync(
        SqliteConnection connection,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT DraftJson FROM Projects WHERE ProjectId = $projectId;";
        command.Parameters.AddWithValue("$projectId", projectId.ToString());
        return await command.ExecuteScalarAsync(cancellationToken) as string;
    }

    private static async Task<int> ReadNextRevisionNumberAsync(
        SqliteConnection connection,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COALESCE(MAX(RevisionNumber), 0) + 1
            FROM Revisions WHERE ProjectId = $projectId;
            """;
        command.Parameters.AddWithValue("$projectId", projectId.ToString());
        return Convert.ToInt32(
            await command.ExecuteScalarAsync(cancellationToken),
            System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string FormatErrors(IEnumerable<ProjectValidationError> errors) =>
        string.Join(Environment.NewLine, errors.Select(error => $"{error.Path}: {error.Message}"));
}
