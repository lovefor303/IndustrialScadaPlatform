namespace Scada.Editor.Wpf;

public enum EditorRole
{
    Developer,
    Engineer,
    Operator,
    Viewer
}

public static class EditorAuthorization
{
    public static bool CanEdit(EditorRole role) =>
        role is EditorRole.Developer or EditorRole.Engineer;
}
