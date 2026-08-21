using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Scada.Editor.Wpf;

public sealed class EditorShellViewModel : INotifyPropertyChanged
{
    private string _statusText = "就绪";
    private readonly EditorCommands? _commands;
    private readonly IProjectFileDialog? _fileDialog;

    public EditorShellViewModel(
        EditorRole role = EditorRole.Developer,
        EditorCommands? commands = null,
        IProjectFileDialog? fileDialog = null)
    {
        Role = role;
        _commands = commands;
        _fileDialog = fileDialog;
        NewProjectCommand = new AsyncEditorCommand(NewProjectAsync, () => IsEngineeringEnabled);
        OpenProjectCommand = new AsyncEditorCommand(OpenProjectAsync, () => IsEngineeringEnabled);
        SaveDraftCommand = new AsyncEditorCommand(SaveDraftAsync, () => IsEngineeringEnabled);
        PublishCommand = new AsyncEditorCommand(PublishAsync, () => IsEngineeringEnabled);
        RestoreCommand = new AsyncEditorCommand(RestoreAsync, () => IsEngineeringEnabled);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public EditorRole Role { get; }

    public bool IsEngineeringEnabled => EditorAuthorization.CanEdit(Role);

    public ICommand NewProjectCommand { get; }

    public ICommand OpenProjectCommand { get; }

    public ICommand SaveDraftCommand { get; }

    public ICommand PublishCommand { get; }

    public ICommand RestoreCommand { get; }

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
