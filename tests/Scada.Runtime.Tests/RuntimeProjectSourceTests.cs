using Scada.Core;
using Scada.Scene;
using Scada.Storage;
using Xunit;

namespace Scada.Runtime.Tests;

public sealed class RuntimeProjectSourceTests
{
    [Fact]
    public async Task JsonSourceLoadsPublishedProject()
    {
        var directory = CreateDirectory();
        try
        {
            var path = Path.Combine(directory, "published.json");
            await File.WriteAllTextAsync(path, Serialize(CreateProject(ProjectStatus.Published)));

            var project = await new JsonRuntimeProjectSource(path).LoadAsync();

            Assert.Equal(ProjectStatus.Published, project.Status);
            Assert.Single(project.Screens);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task JsonSourceRejectsDraftWithStableDiagnostic()
    {
        var directory = CreateDirectory();
        try
        {
            var path = Path.Combine(directory, "draft.json");
            await File.WriteAllTextAsync(path, Serialize(CreateProject(ProjectStatus.Draft)));

            var exception = await Assert.ThrowsAsync<RuntimeSourceException>(
                () => new JsonRuntimeProjectSource(path).LoadAsync());

            Assert.Equal(RuntimeDiagnostics.ProjectNotPublished, exception.Diagnostic.Code);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task JsonSourceReportsMissingAndMalformedFiles()
    {
        var directory = CreateDirectory();
        try
        {
            var missing = await Assert.ThrowsAsync<RuntimeSourceException>(
                () => new JsonRuntimeProjectSource(Path.Combine(directory, "missing.json")).LoadAsync());
            Assert.Equal(RuntimeDiagnostics.ProjectMissing, missing.Diagnostic.Code);

            var malformedPath = Path.Combine(directory, "malformed.json");
            await File.WriteAllTextAsync(malformedPath, "{not-json");
            var malformed = await Assert.ThrowsAsync<RuntimeSourceException>(
                () => new JsonRuntimeProjectSource(malformedPath).LoadAsync());
            Assert.Equal(RuntimeDiagnostics.ProjectInvalid, malformed.Diagnostic.Code);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RevisionSourceLoadsPublishedRevisionAndRejectsMissingRevision()
    {
        var directory = CreateDirectory();
        try
        {
            var store = new RevisionStore(Path.Combine(directory, "runtime.db"));
            await store.InitializeAsync();
            var project = CreateProject(ProjectStatus.Draft);
            await store.SaveDraftAsync(project);
            var revision = await store.PublishAsync(project.ProjectId, "runtime-test");

            var loaded = await new RevisionRuntimeProjectSource(store, project.ProjectId, revision.RevisionId).LoadAsync();
            Assert.Equal(ProjectStatus.Published, loaded.Status);

            var missing = await Assert.ThrowsAsync<RuntimeSourceException>(
                () => new RevisionRuntimeProjectSource(store, project.ProjectId, Guid.NewGuid()).LoadAsync());
            Assert.Equal(RuntimeDiagnostics.ProjectMissing, missing.Diagnostic.Code);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string Serialize(ProjectDocument project) => new JsonProjectSerializer().Serialize(project);

    private static ProjectDocument CreateProject(ProjectStatus status) =>
        ProjectDocument.FromStorage(
            Guid.NewGuid(),
            ProjectFormat.CurrentVersion,
            "Runtime Test",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            status,
            screens: new[] { ScreenDocument.Create("Main") });

    private static string CreateDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "IndustrialScadaPlatform.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
