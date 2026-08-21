using Scada.Core;
using Scada.Controls;
using Scada.Editor.Wpf;
using Scada.Scene;
using Scada.Storage;
using Xunit;

namespace Scada.Editor.Wpf.Tests;

public sealed class EditorPersistenceTests
{
    [Fact]
    public async Task CommandsSaveReopenPublishAndRestoreWithoutLosingSceneMetadata()
    {
        var directory = Path.Combine(Path.GetTempPath(), "IndustrialScadaPlatform.EditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var databasePath = Path.Combine(directory, "editor.db");
            var store = new RevisionStore(databasePath);
            await store.InitializeAsync();
            var project = EditorCommands.CreateProject("Editor project", "Main");
            var session = new EditorSession(project, "Main");
            var valve = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(10, 20, 100, 80)) with
            {
                Properties = new Dictionary<string, string> { ["label"] = "XV101" }
            };
            session.AddObject(valve);
            var commands = new EditorCommands(session, store, "developer");

            await commands.SaveDraftAsync();
            Assert.False(session.IsDirty);

            var reopened = await store.LoadDraftAsync(project.ProjectId);
            Assert.NotNull(reopened);
            var reopenedSession = new EditorSession(reopened, "Main");
            var reopenedObject = Assert.IsType<ControlObject>(reopenedSession.ActiveScreen.Objects.Single());
            Assert.Equal(valve.Id, reopenedObject.Id);
            Assert.Equal(valve.Properties, reopenedObject.Properties);

            var reopenedCommands = new EditorCommands(reopenedSession, store, "developer");
            var revision = await reopenedCommands.PublishAsync();
            Assert.Equal(1, revision.RevisionNumber);
            Assert.Single(await reopenedCommands.ListRevisionsAsync());

            reopenedSession.SelectOnly(reopenedObject.Id);
            reopenedSession.UpdateSelectedObject(sceneObject => sceneObject with
            {
                Bounds = new RectD(200, 300, 100, 80)
            });
            await reopenedCommands.SaveDraftAsync();
            await reopenedCommands.RestoreAsync(revision.RevisionId);
            var restoredObject = Assert.IsType<ControlObject>(reopenedSession.ActiveScreen.Objects.Single());
            Assert.Equal(valve.Bounds, restoredObject.Bounds);
            Assert.False(reopenedSession.IsDirty);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
