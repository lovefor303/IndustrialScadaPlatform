using Microsoft.Data.Sqlite;
using Scada.Core;
using Scada.Scene;
using Scada.Storage;
using Xunit;

namespace Scada.Storage.Tests;

public sealed class RevisionStoreTests
{
    [Fact]
    public async Task SaveDraftThenReopenReturnsSameProject()
    {
        await WithStoreAsync(async (store, _, _) =>
        {
            var project = CreateProject();

            await store.SaveDraftAsync(project);
            var reopened = await store.LoadDraftAsync(project.ProjectId);

            Assert.NotNull(reopened);
            Assert.Equal(project.ProjectId, reopened.ProjectId);
            Assert.Equal(project.Screens[0].Objects[0].Bounds, reopened.Screens[0].Objects[0].Bounds);
        });
    }

    [Fact]
    public async Task LoadLatestDraftReturnsMostRecentlyUpdatedProject()
    {
        var directory = CreateDirectory();
        try
        {
            var store = new RevisionStore(Path.Combine(directory, "latest.db"));
            await store.InitializeAsync();
            var first = ProjectDocument.Create("First", screens: new[] { ScreenDocument.Create("Main") });
            var second = ProjectDocument.Create("Second", screens: new[] { ScreenDocument.Create("Main") });
            await store.SaveDraftAsync(first);
            await Task.Delay(10);
            await store.SaveDraftAsync(second);

            var latest = await store.LoadLatestDraftAsync();

            Assert.NotNull(latest);
            Assert.Equal(second.ProjectId, latest!.ProjectId);
            Assert.Equal("Second", latest.Name);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task PublishCreatesImmutableRevisionAndListsPublishedCopy()
    {
        await WithStoreAsync(async (store, _, _) =>
        {
            var project = CreateProject();
            await store.SaveDraftAsync(project);

            var first = await store.PublishAsync(project.ProjectId, "engineer");
            var changed = CopyWithName(project, "Changed Draft");
            await store.SaveDraftAsync(changed);
            var revisions = await store.ListRevisionsAsync(project.ProjectId);
            var published = await store.LoadRevisionAsync(project.ProjectId, first.RevisionId);

            Assert.Single(revisions);
            Assert.Equal(1, first.RevisionNumber);
            Assert.Equal("engineer", first.Author);
            Assert.Equal(project.Name, published.Name);
            Assert.Equal(ProjectStatus.Published, published.Status);
            Assert.Equal("Changed Draft", (await store.LoadDraftAsync(project.ProjectId))!.Name);
        });
    }

    [Fact]
    public async Task RestoreCopiesSelectedRevisionIntoDraftWithoutChangingHistory()
    {
        await WithStoreAsync(async (store, _, _) =>
        {
            var project = CreateProject();
            await store.SaveDraftAsync(project);
            var first = await store.PublishAsync(project.ProjectId, "engineer");
            await store.SaveDraftAsync(CopyWithName(project, "Second"));
            await store.PublishAsync(project.ProjectId, "engineer");

            await store.RestoreToDraftAsync(project.ProjectId, first.RevisionId);

            var restored = await store.LoadDraftAsync(project.ProjectId);
            var revisions = await store.ListRevisionsAsync(project.ProjectId);
            Assert.Equal(project.Name, restored!.Name);
            Assert.Equal(ProjectStatus.Draft, restored.Status);
            Assert.Equal(2, revisions.Count);
            Assert.Collection(
                revisions,
                revision => Assert.Equal(1, revision.RevisionNumber),
                revision => Assert.Equal(2, revision.RevisionNumber));
        });
    }

    [Fact]
    public async Task ImportExportRoundTripPreservesGeometryAndBindings()
    {
        await WithStoreAsync(async (store, databasePath, directory) =>
        {
            var project = CreateProject();
            await store.SaveDraftAsync(project);
            var exportPath = Path.Combine(directory, "project.scada.json");
            await store.ExportAsync(project.ProjectId, exportPath);

            var importDatabasePath = Path.Combine(directory, "import.db");
            var importStore = new RevisionStore(importDatabasePath);
            await importStore.InitializeAsync();
            var importedId = await importStore.ImportAsync(exportPath);
            var imported = await importStore.LoadDraftAsync(importedId);

            Assert.NotEqual(databasePath, importDatabasePath);
            Assert.Equal(project.ProjectId, importedId);
            Assert.Equal(project.Screens[0].Objects[0].Bounds, imported!.Screens[0].Objects[0].Bounds);
            Assert.Equal(project.Screens[0].Objects[0].Bindings, imported.Screens[0].Objects[0].Bindings);
        });
    }

    [Fact]
    public async Task PublishingInvalidProjectFailsWithoutCreatingRevision()
    {
        await WithStoreAsync(async (store, databasePath, _) =>
        {
            var project = CreateProject();
            await store.SaveDraftAsync(project);
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Pooling = false
            }.ToString();
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Projects SET DraftJson = '{\"schemaVersion\":1}' WHERE ProjectId = $projectId;";
            command.Parameters.AddWithValue("$projectId", project.ProjectId.ToString());
            await command.ExecuteNonQueryAsync();

            await Assert.ThrowsAsync<InvalidDataException>(() => store.PublishAsync(project.ProjectId, "engineer"));
            Assert.Empty(await store.ListRevisionsAsync(project.ProjectId));
        });
    }

    private static async Task WithStoreAsync(
        Func<RevisionStore, string, string, Task> action)
    {
        var directory = Path.Combine(Path.GetTempPath(), "IndustrialScadaPlatform.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var databasePath = Path.Combine(directory, "project.db");

        try
        {
            var store = new RevisionStore(databasePath);
            await store.InitializeAsync();
            await action(store, databasePath, directory);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "IndustrialScadaPlatform.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static ProjectDocument CreateProject()
    {
        var control = ControlObject.Create("Valve", new RectD(20, 30, 40, 40)) with
        {
            Bindings = new Dictionary<string, BindingDefinition>
            {
                ["open"] = new("Valve.OpenFeedback", "fill")
            }
        };
        var screen = ScreenDocument.Create("Main", new[] { control });
        var variable = VariableDefinition.Bool("Valve.OpenFeedback", VariableDirection.Feedback);
        return ProjectDocument.Create("Demo", new[] { variable }, new[] { screen });
    }

    private static ProjectDocument CopyWithName(ProjectDocument project, string name) =>
        ProjectDocument.FromStorage(
            project.ProjectId,
            project.SchemaVersion,
            name,
            project.CreatedAt,
            DateTimeOffset.UtcNow,
            ProjectStatus.Draft,
            project.Variables,
            project.Screens);
}
