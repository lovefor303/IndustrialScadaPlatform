using Scada.Core;
using Scada.Scene;
using Scada.Storage;
using System.IO;

namespace Scada.Editor.Wpf;

public interface IProjectFileDialog
{
    Task<string?> PickOpenPathAsync(CancellationToken cancellationToken = default);

    Task<string?> PickSavePathAsync(CancellationToken cancellationToken = default);
}

public interface IUnsavedChangesPrompt
{
    Task<bool> ConfirmDiscardAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Persistence command adapter for the editor shell. It deliberately depends
/// only on RevisionStore and never owns PLC/runtime side effects.
/// </summary>
public sealed class EditorCommands
{
    private readonly EditorSession _session;
    private readonly RevisionStore _store;
    private readonly string _author;
    private readonly IUnsavedChangesPrompt? _discardPrompt;

    public EditorCommands(
        EditorSession session,
        RevisionStore store,
        string author,
        IUnsavedChangesPrompt? discardPrompt = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        ArgumentException.ThrowIfNullOrWhiteSpace(author);
        _author = author.Trim();
        _discardPrompt = discardPrompt;
    }

    public static ProjectDocument CreateProject(string name, string screenName = "Main") =>
        ProjectDocument.Create(name, screens: new[] { ScreenDocument.Create(screenName) });

    public void NewProject(string name, string screenName = "Main")
    {
        var project = CreateProject(name, screenName);
        _session.LoadProject(project, screenName);
    }

    public async Task<bool> TryNewProjectAsync(
        string name,
        string screenName = "Main",
        CancellationToken cancellationToken = default)
    {
        if (!await CanDiscardAsync(cancellationToken))
        {
            return false;
        }

        NewProject(name, screenName);
        return true;
    }

    public async Task OpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!await CanDiscardAsync(cancellationToken))
        {
            return;
        }

        var projectId = await _store.ImportAsync(filePath, cancellationToken);
        var project = await _store.LoadDraftAsync(projectId, cancellationToken)
            ?? throw new InvalidOperationException("打开项目后未找到草稿。");
        if (project.Screens.Count == 0)
        {
            throw new InvalidDataException("项目至少需要包含一个画面。");
        }

        var screenName = project.Screens[0].Name;
        _session.LoadProject(project, screenName);
    }

    public async Task<bool> TryOpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!await CanDiscardAsync(cancellationToken))
        {
            return false;
        }

        var projectId = await _store.ImportAsync(filePath, cancellationToken);
        var project = await _store.LoadDraftAsync(projectId, cancellationToken)
            ?? throw new InvalidOperationException("打开项目后未找到草稿。");
        if (project.Screens.Count == 0)
        {
            throw new InvalidDataException("项目至少需要包含一个画面。");
        }

        _session.LoadProject(project, project.Screens[0].Name);
        return true;
    }

    public async Task<bool> TryLoadLatestDraftAsync(CancellationToken cancellationToken = default)
    {
        if (!await CanDiscardAsync(cancellationToken))
        {
            return false;
        }

        var project = await _store.LoadLatestDraftAsync(cancellationToken);
        if (project is null)
        {
            return false;
        }

        if (project.Screens.Count == 0)
        {
            throw new InvalidDataException("草稿至少需要包含一个画面。");
        }

        _session.LoadProject(project, project.Screens[0].Name);
        return true;
    }

    public Task ExportAsync(string filePath, CancellationToken cancellationToken = default) =>
        _store.ExportAsync(_session.Project.ProjectId, filePath, cancellationToken);

    public async Task SaveDraftAsync(CancellationToken cancellationToken = default)
    {
        await _store.SaveDraftAsync(_session.Project, cancellationToken);
        _session.MarkSaved();
    }

    public async Task<ProjectRevision> PublishAsync(CancellationToken cancellationToken = default)
    {
        if (_session.IsDirty)
        {
            await SaveDraftAsync(cancellationToken);
        }

        return await _store.PublishAsync(_session.Project.ProjectId, _author, cancellationToken);
    }

    public Task<IReadOnlyList<ProjectRevision>> ListRevisionsAsync(CancellationToken cancellationToken = default) =>
        _store.ListRevisionsAsync(_session.Project.ProjectId, cancellationToken);

    public async Task RestoreAsync(Guid revisionId, CancellationToken cancellationToken = default)
    {
        await _store.RestoreToDraftAsync(_session.Project.ProjectId, revisionId, cancellationToken);
        var restored = await _store.LoadDraftAsync(_session.Project.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException("恢复版本后未找到草稿。");
        _session.LoadProject(restored, _session.ActiveScreenName);
    }

    private async Task<bool> CanDiscardAsync(CancellationToken cancellationToken)
    {
        if (!_session.IsDirty)
        {
            return true;
        }

        return _discardPrompt is not null
            && await _discardPrompt.ConfirmDiscardAsync(cancellationToken);
    }
}
