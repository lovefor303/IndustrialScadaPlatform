using System.Text.Json;
using Scada.Core;
using Scada.Scene;
using Scada.Storage;

namespace Scada.Runtime;

public sealed class JsonRuntimeProjectSource : IRuntimeProjectSource
{
    private readonly string _path;
    private readonly JsonProjectSerializer _serializer;

    public JsonRuntimeProjectSource(string path, JsonProjectSerializer? serializer = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = Path.GetFullPath(path);
        _serializer = serializer ?? new JsonProjectSerializer();
    }

    public async Task<ProjectDocument> LoadAsync(CancellationToken cancellationToken = default)
    {
        string json;
        try
        {
            json = await File.ReadAllTextAsync(_path, cancellationToken);
        }
        catch (FileNotFoundException exception)
        {
            throw CreateException(RuntimeDiagnostics.ProjectMissing, "运行时项目文件不存在。", exception);
        }
        catch (DirectoryNotFoundException exception)
        {
            throw CreateException(RuntimeDiagnostics.ProjectMissing, "运行时项目目录不存在。", exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw CreateException(RuntimeDiagnostics.ProjectInvalid, "运行时项目文件无法读取。", exception);
        }

        ProjectDocument project;
        try
        {
            project = _serializer.Deserialize(json);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException or ArgumentException)
        {
            throw CreateException(RuntimeDiagnostics.ProjectInvalid, "运行时项目文件格式无效。", exception);
        }

        EnsurePublished(project);
        return project;
    }

    private static void EnsurePublished(ProjectDocument project)
    {
        if (project.Status != ProjectStatus.Published)
        {
            throw CreateException(RuntimeDiagnostics.ProjectNotPublished, "项目尚未发布，不能进入运行时。");
        }

        if (project.Screens.Count == 0)
        {
            throw CreateException(RuntimeDiagnostics.ProjectInvalid, "已发布项目没有可运行的画面。");
        }
    }

    private static RuntimeSourceException CreateException(
        string code,
        string message,
        Exception? innerException = null) =>
        new(RuntimeDiagnostics.Create(code, message), innerException);
}
