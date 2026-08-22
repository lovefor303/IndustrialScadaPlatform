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
    private EditHistory _history;

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
        _history = new EditHistory(project);
    }

    public ProjectDocument Project { get; private set; }

    public string ActiveScreenName { get; private set; }

    public ScreenDocument ActiveScreen => Project.Screens.Single(screen =>
        string.Equals(screen.Name, ActiveScreenName, StringComparison.Ordinal));

    public IReadOnlyCollection<Guid> SelectedObjectIds => _selectedObjectIds;

    public bool IsDirty { get; private set; }

    public bool CanUndo => _history.CanUndo;

    public bool CanRedo => _history.CanRedo;

    public void SelectOnly(Guid objectId)
    {
        if (ActiveScreen.FindObject(objectId) is null)
        {
            throw new KeyNotFoundException($"Scene object '{objectId}' was not found.");
        }

        _selectedObjectIds.Clear();
        _selectedObjectIds.Add(objectId);
    }

    public void SelectMany(IEnumerable<Guid> objectIds)
    {
        ArgumentNullException.ThrowIfNull(objectIds);
        var ids = objectIds.Distinct().ToArray();
        foreach (var objectId in ids)
        {
            if (ActiveScreen.FindObject(objectId) is null)
            {
                throw new KeyNotFoundException($"Scene object '{objectId}' was not found.");
            }
        }

        _selectedObjectIds.Clear();
        _selectedObjectIds.UnionWith(ids);
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

        var updatedProject = Project.ReplaceScreen(replacement);
        _history.Record(updatedProject);
        Project = updatedProject;
        IsDirty = true;
    }

    public void MarkSaved()
    {
        IsDirty = false;
    }

    public void LoadProject(ProjectDocument project, string activeScreenName)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(activeScreenName);
        if (project.Screens.All(screen => !string.Equals(screen.Name, activeScreenName, StringComparison.Ordinal)))
        {
            throw new KeyNotFoundException($"Screen '{activeScreenName}' was not found.");
        }

        Project = project;
        ActiveScreenName = activeScreenName;
        _history = new EditHistory(project);
        _selectedObjectIds.Clear();
        IsDirty = false;
    }

    public bool Undo()
    {
        if (!_history.Undo())
        {
            return false;
        }

        Project = _history.Current;
        KeepExistingSelection();
        IsDirty = true;
        return true;
    }

    public bool Redo()
    {
        if (!_history.Redo())
        {
            return false;
        }

        Project = _history.Current;
        KeepExistingSelection();
        IsDirty = true;
        return true;
    }

    public void MoveSelection(double dx, double dy)
    {
        ReplaceActiveScreen(SceneGeometryOperations.Move(
            ActiveScreen,
            _selectedObjectIds,
            dx,
            dy));
    }

    public void AlignSelection(GeometryAlignment alignment)
    {
        ReplaceActiveScreen(SceneGeometryOperations.Align(
            ActiveScreen,
            _selectedObjectIds,
            alignment));
    }

    public void DistributeSelection(GeometryDistribution distribution)
    {
        ReplaceActiveScreen(SceneGeometryOperations.Distribute(
            ActiveScreen,
            _selectedObjectIds,
            distribution));
    }

    public void UpdateSelectedObject(Func<SceneObject, SceneObject> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        if (_selectedObjectIds.Count != 1)
        {
            throw new InvalidOperationException("Exactly one scene object must be selected for this edit.");
        }

        var selectedId = _selectedObjectIds.Single();
        var selected = ActiveScreen.FindObject(selectedId)
            ?? throw new KeyNotFoundException($"Scene object '{selectedId}' was not found.");
        var replacement = update(selected)
            ?? throw new InvalidOperationException("An object update must return a scene object.");
        if (replacement.Id != selected.Id)
        {
            throw new InvalidOperationException("An object update must preserve the scene object ID.");
        }

        ReplaceActiveScreen(ActiveScreen.ReplaceObjects(
            ActiveScreen.Objects.Select(sceneObject =>
                sceneObject.Id == selectedId ? replacement : sceneObject)));
    }

    private void KeepExistingSelection()
    {
        _selectedObjectIds.RemoveWhere(id => ActiveScreen.FindObject(id) is null);
    }
}
