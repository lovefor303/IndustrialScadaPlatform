using Scada.Scene;

namespace Scada.Editor.Wpf;

/// <summary>
/// Bounded immutable project snapshots for engineering edits. Viewport state is
/// intentionally outside this history because it is not part of the project.
/// </summary>
internal sealed class EditHistory
{
    private readonly int _capacity;
    private readonly Stack<ProjectDocument> _undo = new();
    private readonly Stack<ProjectDocument> _redo = new();

    public EditHistory(ProjectDocument initial, int capacity = 100)
    {
        ArgumentNullException.ThrowIfNull(initial);
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "History capacity must be positive.");
        }

        Current = initial;
        _capacity = capacity;
    }

    public ProjectDocument Current { get; private set; }

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public void Record(ProjectDocument next)
    {
        ArgumentNullException.ThrowIfNull(next);
        _undo.Push(Current);
        while (_undo.Count > _capacity)
        {
            RemoveOldest(_undo);
        }

        Current = next;
        _redo.Clear();
    }

    public bool Undo()
    {
        if (_undo.Count == 0)
        {
            return false;
        }

        _redo.Push(Current);
        Current = _undo.Pop();
        return true;
    }

    public bool Redo()
    {
        if (_redo.Count == 0)
        {
            return false;
        }

        _undo.Push(Current);
        while (_undo.Count > _capacity)
        {
            RemoveOldest(_undo);
        }

        Current = _redo.Pop();
        return true;
    }

    private static void RemoveOldest(Stack<ProjectDocument> stack)
    {
        var retained = stack.Reverse().Skip(1).Reverse().ToArray();
        stack.Clear();
        foreach (var item in retained)
        {
            stack.Push(item);
        }
    }
}
