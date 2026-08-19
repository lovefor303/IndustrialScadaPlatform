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
        IReadOnlyList<VariableDefinition> variables)
    {
        ProjectId = projectId;
        SchemaVersion = schemaVersion;
        Name = name;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Status = status;
        Variables = variables;
    }

    public Guid ProjectId { get; }

    public int SchemaVersion { get; }

    public string Name { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    public ProjectStatus Status { get; }

    public IReadOnlyList<VariableDefinition> Variables { get; }

    public static ProjectDocument Create(
        string name,
        IEnumerable<VariableDefinition>? variables = null)
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
            schemaVersion: 1,
            name.Trim(),
            now,
            now,
            ProjectStatus.Draft,
            variableList);
    }
}
