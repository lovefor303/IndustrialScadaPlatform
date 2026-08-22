using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Scada.Editor.Wpf;

public sealed class EditorShellViewModel : INotifyPropertyChanged
{
    private string _statusText = "就绪";
    private readonly EditorCommands? _commands;
    private readonly IProjectFileDialog? _fileDialog;
    private readonly EditorSession? _session;

    public EditorShellViewModel(
        EditorRole role = EditorRole.Developer,
        EditorCommands? commands = null,
        IProjectFileDialog? fileDialog = null,
        EditorSession? session = null)
    {
        Role = role;
        _commands = commands;
        _fileDialog = fileDialog;
        _session = session;
        PropertyPanel = session is null ? null : new PropertyPanelViewModel(session);
        BindingPanel = session is null ? null : new BindingPanelViewModel(session);
        DynamicsPanel = session is null ? null : new DynamicsPanelViewModel(session);
        EventsPanel = session is null ? null : new EventsPanelViewModel(session);
        if (_session is not null)
        {
            _session.SelectionChanged += OnSessionStateChanged;
            _session.ProjectChanged += OnSessionStateChanged;
            PropertyPanel?.Refresh();
        }
        NewProjectCommand = new AsyncEditorCommand(NewProjectAsync, () => IsEngineeringEnabled);
        OpenProjectCommand = new AsyncEditorCommand(OpenProjectAsync, () => IsEngineeringEnabled);
        SaveDraftCommand = new AsyncEditorCommand(SaveDraftAsync, () => IsEngineeringEnabled);
        PublishCommand = new AsyncEditorCommand(PublishAsync, () => IsEngineeringEnabled);
        RestoreCommand = new AsyncEditorCommand(RestoreAsync, () => IsEngineeringEnabled);
        UndoCommand = new EditorCommand(Undo, () => IsEngineeringEnabled && _session?.CanUndo == true);
        RedoCommand = new EditorCommand(Redo, () => IsEngineeringEnabled && _session?.CanRedo == true);
        AlignLeftCommand = new EditorCommand(() => Align(Scada.Controls.GeometryAlignment.Left), CanAlign);
        AlignCenterCommand = new EditorCommand(() => Align(Scada.Controls.GeometryAlignment.Center), CanAlign);
        AlignRightCommand = new EditorCommand(() => Align(Scada.Controls.GeometryAlignment.Right), CanAlign);
        AlignTopCommand = new EditorCommand(() => Align(Scada.Controls.GeometryAlignment.Top), CanAlign);
        AlignMiddleCommand = new EditorCommand(() => Align(Scada.Controls.GeometryAlignment.Middle), CanAlign);
        AlignBottomCommand = new EditorCommand(() => Align(Scada.Controls.GeometryAlignment.Bottom), CanAlign);
        DistributeHorizontalCommand = new EditorCommand(() => Distribute(Scada.Controls.GeometryDistribution.Horizontal), CanAlign);
        DistributeVerticalCommand = new EditorCommand(() => Distribute(Scada.Controls.GeometryDistribution.Vertical), CanAlign);
        GroupCommand = new EditorCommand(Group, () => IsEngineeringEnabled && _session?.SelectedObjectIds.Count >= 2);
        UngroupCommand = new EditorCommand(Ungroup, () => IsEngineeringEnabled && _session?.HasSelectedGroup == true);
        BringToFrontCommand = new EditorCommand(() => ChangeLayer(Scada.Controls.LayerOrderOperation.BringToFront), CanLayer);
        SendToBackCommand = new EditorCommand(() => ChangeLayer(Scada.Controls.LayerOrderOperation.SendToBack), CanLayer);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public EditorRole Role { get; }

    public bool IsEngineeringEnabled => EditorAuthorization.CanEdit(Role);

    public ICommand NewProjectCommand { get; }

    public PropertyPanelViewModel? PropertyPanel { get; }
    public BindingPanelViewModel? BindingPanel { get; }
    public DynamicsPanelViewModel? DynamicsPanel { get; }
    public EventsPanelViewModel? EventsPanel { get; }

    public ICommand OpenProjectCommand { get; }

    public ICommand SaveDraftCommand { get; }

    public ICommand PublishCommand { get; }

    public ICommand RestoreCommand { get; }

    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand AlignLeftCommand { get; }
    public ICommand AlignCenterCommand { get; }
    public ICommand AlignRightCommand { get; }
    public ICommand AlignTopCommand { get; }
    public ICommand AlignMiddleCommand { get; }
    public ICommand AlignBottomCommand { get; }
    public ICommand DistributeHorizontalCommand { get; }
    public ICommand DistributeVerticalCommand { get; }
    public ICommand GroupCommand { get; }
    public ICommand UngroupCommand { get; }
    public ICommand BringToFrontCommand { get; }
    public ICommand SendToBackCommand { get; }

    public string UndoCommandLabel => Role == EditorRole.Operator ? "撤销" : "撤销";
    public string RedoCommandLabel => Role == EditorRole.Operator ? "重做" : "重做";
    public string AlignLeftCommandLabel => Role == EditorRole.Operator ? "左对齐" : "左对齐";
    public string DistributeHorizontalCommandLabel => Role == EditorRole.Operator ? "水平等距" : "水平等距";

    public string StatusText
    {
        get => _statusText;
        set
        {
            if (string.Equals(_statusText, value, StringComparison.Ordinal))
            {
                return;
            }

            _statusText = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private async Task NewProjectAsync()
    {
        if (_commands is null)
        {
            StatusText = "尚未打开项目";
        }
        else
        {
            var changed = await _commands.TryNewProjectAsync("未命名项目");
            StatusText = changed ? "已新建项目草稿" : "已取消新建项目";
        }
    }

    private async Task OpenProjectAsync()
    {
        if (_commands is null || _fileDialog is null)
        {
            StatusText = "尚未配置项目文件对话框";
            return;
        }

        var path = await _fileDialog.PickOpenPathAsync();
        if (path is null)
        {
            return;
        }

        await _commands.OpenAsync(path);
        StatusText = "项目已打开";
    }

    private async Task SaveDraftAsync()
    {
        if (_commands is null)
        {
            StatusText = "尚未打开项目";
            return;
        }

        await _commands.SaveDraftAsync();
        StatusText = "草稿已保存";
    }

    private async Task PublishAsync()
    {
        if (_commands is null)
        {
            StatusText = "尚未打开项目";
            return;
        }

        var revision = await _commands.PublishAsync();
        StatusText = $"已发布版本 {revision.RevisionNumber}";
    }

    private async Task RestoreAsync()
    {
        if (_commands is null)
        {
            StatusText = "尚未打开项目";
            return;
        }

        var revisions = await _commands.ListRevisionsAsync();
        if (revisions.Count == 0)
        {
            StatusText = "暂无可恢复版本";
            return;
        }

        var revision = revisions[^1];
        await _commands.RestoreAsync(revision.RevisionId);
        StatusText = $"已恢复版本 {revision.RevisionNumber}";
    }

    private bool CanAlign() => IsEngineeringEnabled && _session?.SelectedObjectIds.Count >= 2
        && _session.SelectedObjectIds.Any(id => _session.ActiveScreen.FindObject(id) is not Scada.Scene.PipeObject);

    private bool CanLayer() => IsEngineeringEnabled && _session?.SelectedObjectIds.Count > 0;

    private void OnSessionStateChanged(object? sender, EventArgs e)
    {
        PropertyPanel?.Refresh();
        BindingPanel?.Refresh();
        DynamicsPanel?.Refresh();
        EventsPanel?.Refresh();
        RefreshCommand(UndoCommand);
        RefreshCommand(RedoCommand);
        RefreshCommand(AlignLeftCommand);
        RefreshCommand(AlignCenterCommand);
        RefreshCommand(AlignRightCommand);
        RefreshCommand(AlignTopCommand);
        RefreshCommand(AlignMiddleCommand);
        RefreshCommand(AlignBottomCommand);
        RefreshCommand(DistributeHorizontalCommand);
        RefreshCommand(DistributeVerticalCommand);
        RefreshCommand(GroupCommand);
        RefreshCommand(UngroupCommand);
        RefreshCommand(BringToFrontCommand);
        RefreshCommand(SendToBackCommand);
    }

    private static void RefreshCommand(ICommand command)
    {
        if (command is EditorCommand editorCommand)
        {
            editorCommand.Refresh();
        }
    }

    private void Undo()
    {
        if (_session?.Undo() == true)
        {
            StatusText = "已撤销";
        }
    }

    private void Redo()
    {
        if (_session?.Redo() == true)
        {
            StatusText = "已重做";
        }
    }

    private void Align(Scada.Controls.GeometryAlignment alignment)
    {
        _session?.AlignSelection(alignment);
        StatusText = "已完成对齐";
    }

    private void Distribute(Scada.Controls.GeometryDistribution distribution)
    {
        _session?.DistributeSelection(distribution);
        StatusText = "已完成分布";
    }

    private void Group()
    {
        _session?.GroupSelection();
        OnSessionStateChanged(this, EventArgs.Empty);
        StatusText = "已组合";
    }

    private void Ungroup()
    {
        if (_session?.UngroupSelection() == true)
        {
            OnSessionStateChanged(this, EventArgs.Empty);
            StatusText = "已取消组合";
        }
    }

    private void ChangeLayer(Scada.Controls.LayerOrderOperation operation)
    {
        _session?.ChangeLayer(operation);
        StatusText = operation == Scada.Controls.LayerOrderOperation.BringToFront ? "已置于顶层" : "已置于底层";
    }
}

internal sealed class EditorCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool> _canExecute;

    public EditorCommand(Action execute, Func<bool> canExecute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute();

    public void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            _execute();
        }
    }

    public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

internal sealed class AsyncEditorCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool> _canExecute;
    private bool _running;

    public AsyncEditorCommand(Func<Task> execute, Func<bool> canExecute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_running && _canExecute();

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _running = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try
        {
            await _execute();
        }
        finally
        {
            _running = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
