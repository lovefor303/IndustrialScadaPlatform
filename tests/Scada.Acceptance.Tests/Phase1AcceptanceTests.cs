using Scada.Core;
using Scada.Scene;
using Scada.Simulator;
using Scada.Storage;
using Xunit;

namespace Scada.Acceptance.Tests;

public sealed class Phase1AcceptanceTests
{
    [Fact]
    public async Task ProjectCompletesThePhaseOneOfflineLifecycle()
    {
        var directory = Path.Combine(Path.GetTempPath(), "IndustrialScadaPlatform.Acceptance", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var command = VariableDefinition.Bool("Valve.OpenCommand", VariableDirection.Command);
            var feedback = VariableDefinition.Bool("Valve.OpenFeedback", VariableDirection.Feedback);
            var control = ControlObject.Create("Valve", new RectD(120, 240, 48, 48)) with
            {
                Bindings = new Dictionary<string, BindingDefinition>
                {
                    ["open"] = new(feedback.Key, "fill")
                }
            };
            var pipe = PipeObject.Create(
                new PointD(20, 264),
                new PointD(360, 264),
                new[] { new PointD(80, 264), new PointD(80, 320) });
            var screen = ScreenDocument.Create("Main", new SceneObject[] { pipe, control });
            var project = ProjectDocument.Create("Acceptance Project", new[] { command, feedback }, new[] { screen });

            var sourceStore = new RevisionStore(Path.Combine(directory, "source.db"));
            await sourceStore.InitializeAsync();
            await sourceStore.SaveDraftAsync(project);
            var exportPath = Path.Combine(directory, "project.scada.json");
            await sourceStore.ExportAsync(project.ProjectId, exportPath);

            var targetStore = new RevisionStore(Path.Combine(directory, "target.db"));
            await targetStore.InitializeAsync();
            var importedId = await targetStore.ImportAsync(exportPath);
            var revision = await targetStore.PublishAsync(importedId, "acceptance");
            var revisions = await targetStore.ListRevisionsAsync(importedId);
            await targetStore.RestoreToDraftAsync(importedId, revision.RevisionId);
            var restored = await targetStore.LoadDraftAsync(importedId);

            Assert.NotNull(restored);
            Assert.Equal(project.ProjectId, restored.ProjectId);
            Assert.Equal(project.SchemaVersion, restored.SchemaVersion);
            Assert.Equal(project.Screens[0].Objects[0].Bounds, restored.Screens[0].Objects[0].Bounds);
            var restoredPipe = Assert.IsType<PipeObject>(restored.Screens[0].Objects[0]);
            Assert.Equal(pipe.Start, restoredPipe.Start);
            Assert.Equal(pipe.End, restoredPipe.End);
            Assert.Equal(pipe.Bends, restoredPipe.Bends);
            var restoredControl = Assert.IsType<ControlObject>(restored.Screens[0].Objects[1]);
            Assert.Equal(control.Bindings, restoredControl.Bindings);
            Assert.Single(revisions);
            Assert.Equal(1, revision.RevisionNumber);

            var simulator = new OfflineSimulator(restored.Variables, seed: 20260819);
            var now = new DateTimeOffset(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);
            Assert.Equal(VariableQuality.Bad, simulator.Read(command.Key, now).Quality);
            Assert.Equal(VariableQuality.Good, simulator.Read(feedback.Key, now).Quality);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
