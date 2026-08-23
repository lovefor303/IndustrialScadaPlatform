using Scada.Core;
using Scada.Scene;
using Scada.Storage;

namespace Scada.Runtime;

public sealed class RevisionRuntimeProjectSource : IRuntimeProjectSource
{
    private readonly RevisionStore _store;
    private readonly Guid _projectId;
    private readonly Guid _revisionId;

    public RevisionRuntimeProjectSource(RevisionStore store, Guid projectId, Guid revisionId)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _projectId = projectId == Guid.Empty ? throw new ArgumentException("项目 ID 不能为空。", nameof(projectId)) : projectId;
        _revisionId = revisionId == Guid.Empty ? throw new ArgumentException("版本 ID 不能为空。", nameof(revisionId)) : revisionId;
    }

    public async Task<ProjectDocument> LoadAsync(CancellationToken cancellationToken = default)
    {
        ProjectDocument project;
        try
        {
            project = await _store.LoadRevisionAsync(_projectId, _revisionId, cancellationToken);
        }
        catch (KeyNotFoundException exception)
        {
            throw new RuntimeSourceException(
                RuntimeDiagnostics.Create(RuntimeDiagnostics.ProjectMissing, "指定的已发布版本不存在。"),
                exception);
        }
        catch (Exception exception) when (exception is InvalidDataException or System.Text.Json.JsonException or ArgumentException)
        {
            throw new RuntimeSourceException(
                RuntimeDiagnostics.Create(RuntimeDiagnostics.ProjectInvalid, "已发布版本内容无效。"),
                exception);
        }

        if (project.Status != ProjectStatus.Published)
        {
            throw new RuntimeSourceException(
                RuntimeDiagnostics.Create(RuntimeDiagnostics.ProjectNotPublished, "指定版本不是已发布项目。"));
        }

        if (project.Screens.Count == 0)
        {
            throw new RuntimeSourceException(
                RuntimeDiagnostics.Create(RuntimeDiagnostics.ProjectInvalid, "已发布项目没有可运行的画面。"));
        }

        return project;
    }
}
