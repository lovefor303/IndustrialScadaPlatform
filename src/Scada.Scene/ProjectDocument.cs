using Scada.Core;

namespace Scada.Scene;

public sealed record ProjectDocument
{
    private ProjectDocument(
        Guid projectId,
        int schemaVersion,
        string name,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        ProjectStatus status,
        IReadOnlyList<VariableDefinition> variables,
        IReadOnlyList<ScreenDocument> screens)
    {
        ProjectId = projectId;
        SchemaVersion = schemaVersion;
        Name = name;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Status = status;
        Variables = variables;
        Screens = screens;
    }

    public Guid ProjectId { get; }

    public int SchemaVersion { get; }

    public string Name { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    public ProjectStatus Status { get; }

    public IReadOnlyList<VariableDefinition> Variables { get; }

    public IReadOnlyList<ScreenDocument> Screens { get; }

    public ProjectDocument ReplaceScreen(ScreenDocument replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        var index = Screens.ToList().FindIndex(screen =>
            string.Equals(screen.Name, replacement.Name, StringComparison.Ordinal));
        if (index < 0)
        {
            throw new KeyNotFoundException($"Screen '{replacement.Name}' was not found.");
        }

        var screens = Screens.ToArray();
        screens[index] = replacement;
        return FromStorage(
            ProjectId,
            SchemaVersion,
            Name,
            CreatedAt,
            DateTimeOffset.UtcNow,
            ProjectStatus.Draft,
            Variables,
            screens);
    }

    public ProjectDocument ReplaceVariables(IEnumerable<VariableDefinition> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);
        return FromStorage(
            ProjectId,
            SchemaVersion,
            Name,
            CreatedAt,
            DateTimeOffset.UtcNow,
            ProjectStatus.Draft,
            variables,
            Screens);
    }

    public ProjectDocument TouchDraft() =>
        FromStorage(
            ProjectId,
            SchemaVersion,
            Name,
            CreatedAt,
            DateTimeOffset.UtcNow,
            ProjectStatus.Draft,
            Variables,
            Screens);

    public static ProjectDocument Create(
        string name,
        IEnumerable<VariableDefinition>? variables = null,
        IEnumerable<ScreenDocument>? screens = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var variableList = variables?.ToArray() ?? [];
        var duplicateKey = variableList
            .GroupBy(variable => variable.Key, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (duplicateKey is not null)
        {
            throw new ArgumentException(
                $"Duplicate variable key '{duplicateKey}' is not allowed.",
                nameof(variables));
        }

        var now = DateTimeOffset.UtcNow;
        return new ProjectDocument(
            Guid.NewGuid(),
            schemaVersion: ProjectFormat.CurrentVersion,
            name.Trim(),
            now,
            now,
            ProjectStatus.Draft,
            variableList,
            screens?.ToArray() ?? []);
    }

    public static ProjectDocument FromStorage(
        Guid projectId,
        int schemaVersion,
        string name,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        ProjectStatus status,
        IEnumerable<VariableDefinition>? variables = null,
        IEnumerable<ScreenDocument>? screens = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project ID must not be empty.", nameof(projectId));
        }

        var variableList = variables?.ToArray() ?? [];
        var duplicateKey = variableList
            .GroupBy(variable => variable.Key, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicateKey is not null)
        {
            throw new ArgumentException(
                $"Duplicate variable key '{duplicateKey}' is not allowed.",
                nameof(variables));
        }

        return new ProjectDocument(
            projectId,
            schemaVersion,
            name.Trim(),
            createdAt,
            updatedAt,
            status,
            variableList,
            screens?.ToArray() ?? []);
    }
}
