namespace Scada.Core;

public enum ProjectStatus
{
    Draft,
    Published,
    Archived
}

public sealed record ProjectIdentity(Guid ProjectId, string Name);

public sealed record ProjectRevision(
    Guid RevisionId,
    Guid ProjectId,
    int RevisionNumber,
    string Author,
    DateTimeOffset CreatedAt);

public sealed record ProjectValidationError(
    string Path,
    string Code,
    string Message);
