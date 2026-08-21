using Scada.Controls;
using Scada.Editor.Wpf;
using Scada.Scene;
using Xunit;

namespace Scada.Editor.Wpf.Tests;

public sealed class EditorSessionTests
{
    [Fact]
    public void ToolboxContainsEveryPhase2ControlAndIndependentPipeEntry()
    {
        var entries = ToolboxCatalog.CreateDefault().Entries;

        Assert.Contains(entries, entry => entry.TypeId == ControlTypeIds.CentrifugalPump);
        Assert.Contains(entries, entry => entry.TypeId == ControlTypeIds.AutomatedValve);
        Assert.Contains(entries, entry => entry.TypeId == ControlTypeIds.Vessel);
        Assert.Contains(entries, entry => entry.TypeId == ControlTypeIds.StraightPipe);
        Assert.Contains(entries, entry => entry.TypeId == "text");
        Assert.Equal(ControlTypeIds.All.Count + 1, entries.Count);
        Assert.Contains(entries, entry => entry.TypeId == ControlTypeIds.StraightPipe && entry.IsPipe);
    }

    [Fact]
    public void AddingObjectPreservesExistingIdsAndMarksDraftDirty()
    {
        var existing = ControlObject.Create(
            ControlTypeIds.CentrifugalPump,
            new RectD(10, 20, 120, 80));
        var project = ProjectDocument.Create(
            "Test project",
            screens: new[] { ScreenDocument.Create("Main", new SceneObject[] { existing }) });
        var session = new EditorSession(project, "Main");
        var added = ControlObject.Create(
            ControlTypeIds.AutomatedValve,
            new RectD(200, 100, 80, 60));

        session.AddObject(added);

        Assert.True(session.IsDirty);
        Assert.Equal(
            new[] { existing.Id, added.Id },
            session.ActiveScreen.Objects.Select(sceneObject => sceneObject.Id));
    }

    [Fact]
    public void MovingValveDoesNotMutateUnselectedPipe()
    {
        var valve = ControlObject.Create(
            ControlTypeIds.AutomatedValve,
            new RectD(100, 100, 80, 60));
        var pipe = PipeObject.Create(
            new PointD(20, 130),
            new PointD(100, 130),
            new[] { new PointD(60, 130) });
        var project = ProjectDocument.Create(
            "Test project",
            screens: new[] { ScreenDocument.Create("Main", new SceneObject[] { valve, pipe }) });
        var session = new EditorSession(project, "Main");
        session.SelectOnly(valve.Id);

        session.MoveSelection(15, -4);

        var movedPipe = Assert.IsType<PipeObject>(session.ActiveScreen.FindObject(pipe.Id));
        Assert.Equal(pipe.Start, movedPipe.Start);
        Assert.Equal(pipe.End, movedPipe.End);
        Assert.Equal(pipe.Bends, movedPipe.Bends);
    }

    [Fact]
    public void ReplacingScreenMakesProjectDraftAndPreservesProjectIdentity()
    {
        var screen = ScreenDocument.Create("Main");
        var project = ProjectDocument.Create("Test project", screens: new[] { screen });
        var session = new EditorSession(project, "Main");
        var replacement = screen.AddObject(ControlObject.Create(
            ControlTypeIds.Vessel,
            new RectD(0, 0, 200, 180)));

        session.ReplaceActiveScreen(replacement);

        Assert.Equal(project.ProjectId, session.Project.ProjectId);
        Assert.Equal(Scada.Core.ProjectStatus.Draft, session.Project.Status);
        Assert.Equal(replacement, session.ActiveScreen);
        Assert.True(session.Project.UpdatedAt >= project.UpdatedAt);
    }
}
