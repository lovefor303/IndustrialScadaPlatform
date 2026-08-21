using Scada.Controls;
using Scada.Scene;

namespace Scada.Editor.Wpf;

/// <summary>
/// Selection-scoped immutable editing state. WPF controls bind to this state;
/// persistence is deliberately owned by the shell command layer.
/// </summary>
public sealed class EditorSession
{
    private readonly HashSet<Guid> _selectedObjectIds = new();

    public EditorSession(ProjectDocument project, string activeScreenName)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(activeScreenName);
        if (project.Screens.All(screen => !string.Equals(
                screen.Name,
                activeScreenName,
                StringComparison.Ordinal)))
        {
            throw new KeyNotFoundException($"Screen '{activeScreenName}' was not found.");
        }

        Project = project;
        ActiveScreenName = activeScreenName;
    }

    public ProjectDocument Project { get; private set; }

    public string ActiveScreenName { get; private set; }

    public ScreenDocument ActiveScreen => Project.Screens.Single(screen =>
        string.Equals(screen.Name, ActiveScreenName, StringComparison.Ordinal));

    public IReadOnlyCollection<Guid> SelectedObjectIds => _selectedObjectIds;

    public bool IsDirty { get; private set; }

    public void SelectOnly(Guid objectId)
    {
        if (ActiveScreen.FindObject(objectId) is null)
        {
            throw new KeyNotFoundException($"Scene object '{objectId}' was not found.");
        }

        _selectedObjectIds.Clear();
        _selectedObjectIds.Add(objectId);
    }

    public void ClearSelection() => _selectedObjectIds.Clear();

    public void AddObject(SceneObject sceneObject)
    {
        ReplaceActiveScreen(ActiveScreen.AddObject(sceneObject));
        SelectOnly(sceneObject.Id);
    }

    public void ReplaceActiveScreen(ScreenDocument replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        if (!string.Equals(replacement.Name, ActiveScreenName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The replacement screen must retain the active screen name.");
        }

        Project = Project.ReplaceScreen(replacement);
        IsDirty = true;
    }

    public void MoveSelection(double dx, double dy)
    {
        ReplaceActiveScreen(SceneGeometryOperations.Move(
            ActiveScreen,
            _selectedObjectIds,
            dx,
            dy));
    }
}
