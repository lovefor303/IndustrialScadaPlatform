namespace Scada.Gateway;

public enum AuthRole
{
    Viewer,
    Operator,
    Engineer,
    Admin
}

public sealed class AuthUser
{
    public Guid Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public AuthRole Role { get; init; }
    public bool Enabled { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
