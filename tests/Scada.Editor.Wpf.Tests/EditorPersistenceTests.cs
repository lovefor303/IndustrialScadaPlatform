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
    public async Task DirtyNewProjectRequiresConfirmationAndPreservesDraftWhenDeclined()
    {
        var project = EditorCommands.CreateProject("Original", "Main");
        var session = new EditorSession(project, "Main");
        session.AddObject(ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(0, 0, 80, 60)));
        var prompt = new TestDiscardPrompt(false);
        var directory = Path.Combine(Path.GetTempPath(), "IndustrialScadaPlatform.EditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var store = new RevisionStore(Path.Combine(directory, "editor.db"));
            await store.InitializeAsync();
            var commands = new EditorCommands(session, store, "developer", prompt);

            var changed = await commands.TryNewProjectAsync("Replacement", "Main");

            Assert.False(changed);
            Assert.Equal("Original", session.Project.Name);
            Assert.True(session.IsDirty);
            Assert.True(prompt.WasAsked);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
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
            var valve = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(10, 20, 100, 80)) with
            {
                Rotation = 12,
                Properties = new Dictionary<string, string> { ["label"] = "XV101" }
            };
            var pipe = PipeObject.Create(
                new PointD(0, 60),
                new PointD(260, 60),
                new[] { new PointD(100, 60), new PointD(100, 120) });
            var project = ProjectDocument.Create(
                "Editor project",
                screens: new[] { ScreenDocument.Create("Main", new SceneObject[] { pipe, valve }) });
            var session = new EditorSession(project, "Main");
            var commands = new EditorCommands(session, store, "developer");

            session.SelectOnly(valve.Id);
            session.UpdateSelectedObject(sceneObject => sceneObject with
            {
                Bounds = new RectD(200, 300, 120, 90),
                Rotation = 27
            });
            var savedBounds = new RectD(200, 300, 120, 90);
            const double savedRotation = 27;

            await commands.SaveDraftAsync();
            Assert.False(session.IsDirty);

            var reopened = await store.LoadDraftAsync(project.ProjectId);
            Assert.NotNull(reopened);
            var reopenedSession = new EditorSession(reopened, "Main");
            var reopenedObject = Assert.IsType<ControlObject>(reopenedSession.ActiveScreen.FindObject(valve.Id));
            var reopenedPipe = Assert.IsType<PipeObject>(reopenedSession.ActiveScreen.FindObject(pipe.Id));
            Assert.Equal(valve.Id, reopenedObject.Id);
            Assert.Equal(valve.Properties, reopenedObject.Properties);
            Assert.Equal(savedBounds, reopenedObject.Bounds);
            Assert.Equal(savedRotation, reopenedObject.Rotation);
            Assert.Equal(pipe.Start, reopenedPipe.Start);
            Assert.Equal(pipe.End, reopenedPipe.End);
            Assert.Equal(pipe.Bends, reopenedPipe.Bends);

            var reopenedCommands = new EditorCommands(reopenedSession, store, "developer");
            var revision = await reopenedCommands.PublishAsync();
            Assert.Equal(1, revision.RevisionNumber);
            Assert.Single(await reopenedCommands.ListRevisionsAsync());

            reopenedSession.SelectOnly(reopenedObject.Id);
            reopenedSession.UpdateSelectedObject(sceneObject => sceneObject with
            {
                Bounds = new RectD(500, 600, 100, 80),
                Rotation = 4
            });
            await reopenedCommands.SaveDraftAsync();
            await reopenedCommands.RestoreAsync(revision.RevisionId);
            var restoredObject = Assert.IsType<ControlObject>(reopenedSession.ActiveScreen.FindObject(valve.Id));
            Assert.Equal(savedBounds, restoredObject.Bounds);
            Assert.Equal(savedRotation, restoredObject.Rotation);
            Assert.False(reopenedSession.IsDirty);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LoadLatestDraftReplacesCurrentSessionWithoutNeedingAFilePath()
    {
        var directory = Path.Combine(Path.GetTempPath(), "IndustrialScadaPlatform.EditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var store = new RevisionStore(Path.Combine(directory, "editor.db"));
            await store.InitializeAsync();
            var savedProject = EditorCommands.CreateProject("Saved draft", "Main");
            await store.SaveDraftAsync(savedProject);
            var session = new EditorSession(EditorCommands.CreateProject("Fresh session", "Main"), "Main");
            var commands = new EditorCommands(session, store, "developer");

            var loaded = await commands.TryLoadLatestDraftAsync();

            Assert.True(loaded);
            Assert.Equal(savedProject.ProjectId, session.Project.ProjectId);
            Assert.Equal("Saved draft", session.Project.Name);
            Assert.False(session.IsDirty);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class TestDiscardPrompt : IUnsavedChangesPrompt
    {
        private readonly bool _answer;

        public TestDiscardPrompt(bool answer)
        {
            _answer = answer;
        }

        public bool WasAsked { get; private set; }

        public Task<bool> ConfirmDiscardAsync(CancellationToken cancellationToken = default)
        {
            WasAsked = true;
            return Task.FromResult(_answer);
        }
    }
}
