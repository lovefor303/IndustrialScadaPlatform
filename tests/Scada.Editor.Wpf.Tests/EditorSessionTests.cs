using Scada.Controls;
using Scada.Core;
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

    [Fact]
    public void DynamicsPanelAcceptsMatchingVariableAndRejectsMissingTypeOrDirection()
    {
        var feedback = VariableDefinition.Number("Tank.Level", VariableDataType.Float64, VariableDirection.Feedback, "%");
        var command = VariableDefinition.Bool("Tank.Command", VariableDirection.Command);
        var control = ControlObject.Create(ControlTypeIds.LevelBar, new RectD(0, 0, 120, 48));
        var project = ProjectDocument.Create(
            "Dynamic test",
            new[] { feedback, command },
            new[] { ScreenDocument.Create("Main", new SceneObject[] { control }) });
        var session = new EditorSession(project, "Main");
        session.SelectOnly(control.Id);
        var panel = new DynamicsPanelViewModel(session);

        var valid = new DynamicDefinition(
            "Visibility",
            feedback.Key,
            VariableDataType.Float64,
            VariableDirection.Feedback,
            condition: "> 0");
        Assert.True(panel.TrySet(valid, out var validErrors), string.Join("; ", validErrors));
        Assert.Equal(valid, Assert.IsType<ControlObject>(session.ActiveScreen.FindObject(control.Id)).Dynamics["Visibility"]);

        var missing = valid with { VariableKey = "Tank.Missing" };
        Assert.False(panel.TrySet(missing, out var missingErrors));
        Assert.Contains(missingErrors, error => error.Contains("变量", StringComparison.Ordinal));

        var wrongType = valid with { VariableKey = command.Key };
        Assert.False(panel.TrySet(wrongType, out var typeErrors));
        Assert.Contains(typeErrors, error => error.Contains("类型", StringComparison.Ordinal));

        var wrongDirection = valid with { VariableKey = command.Key, ExpectedDataType = VariableDataType.Bool };
        Assert.False(panel.TrySet(wrongDirection, out var directionErrors));
        Assert.Contains(directionErrors, error => error.Contains("方向", StringComparison.Ordinal));
    }

    [Fact]
    public void PropertyPanelUpdatesGeometryAndEventsUseChineseCatalogLabels()
    {
        var control = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(0, 0, 100, 80));
        var project = ProjectDocument.Create(
            "Panel test",
            screens: new[] { ScreenDocument.Create("Main", new SceneObject[] { control }) });
        var session = new EditorSession(project, "Main");
        session.SelectOnly(control.Id);

        var properties = new PropertyPanelViewModel(session);
        properties.SetGeometry(new RectD(20, 30, 140, 90), rotation: 15, isVisible: false);
        var updated = Assert.IsType<ControlObject>(session.ActiveScreen.FindObject(control.Id));
        Assert.Equal(new RectD(20, 30, 140, 90), updated.Bounds);
        Assert.Equal(15, updated.Rotation);
        Assert.False(updated.IsVisible);

        var events = new EventsPanelViewModel(session);
        Assert.Contains(events.Events, item => item.Id == "pointer.left.press" && item.ChineseLabel == "左键按下");
        Assert.True(events.TrySet(
            "pointer.left.press",
            "command.toggle-bool",
            new Dictionary<string, string>(),
            out var eventErrors), string.Join("; ", eventErrors));
        updated = Assert.IsType<ControlObject>(session.ActiveScreen.FindObject(control.Id));
        Assert.Equal("左键按下", events.GetEventLabel("pointer.left.press"));
        Assert.Equal("command.toggle-bool", updated.Interactions["pointer.left.press"].ActionName);
    }

    [Fact]
    public void BindingPanelValidatesRoleTypeAndDirectionBeforeChangingTheObject()
    {
        var feedback = VariableDefinition.Bool("Valve.OpenFeedback", VariableDirection.Feedback);
        var command = VariableDefinition.Bool("Valve.OpenCommand", VariableDirection.Command);
        var valve = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(0, 0, 100, 80));
        var project = ProjectDocument.Create(
            "Binding test",
            new[] { feedback, command },
            new[] { ScreenDocument.Create("Main", new SceneObject[] { valve }) });
        var session = new EditorSession(project, "Main");
        session.SelectOnly(valve.Id);
        var panel = new BindingPanelViewModel(session);

        Assert.True(panel.TrySet("OpenFeedback", feedback.Key, out var validErrors), string.Join("; ", validErrors));
        Assert.Equal(feedback.Key, Assert.IsType<ControlObject>(session.ActiveScreen.FindObject(valve.Id)).Bindings["OpenFeedback"].VariableKey);

        Assert.False(panel.TrySet("OpenFeedback", command.Key, out var directionErrors));
        Assert.Contains(directionErrors, error => error.Contains("方向", StringComparison.Ordinal));
        Assert.False(panel.TrySet("UnknownRole", feedback.Key, out var roleErrors));
        Assert.Contains(roleErrors, error => error.Contains("角色", StringComparison.Ordinal));
    }

    [Fact]
    public void UndoAndRedoRestoreProjectSnapshotsAndClearRedoAfterNewEdit()
    {
        var valve = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(10, 20, 80, 60));
        var project = ProjectDocument.Create(
            "History test",
            screens: new[] { ScreenDocument.Create("Main", new SceneObject[] { valve }) });
        var session = new EditorSession(project, "Main");
        session.SelectOnly(valve.Id);

        session.MoveSelection(10, 0);
        session.MoveSelection(0, 5);

        Assert.True(session.CanUndo);
        Assert.False(session.CanRedo);
        Assert.Equal(new RectD(20, 25, 80, 60), session.ActiveScreen.FindObject(valve.Id)!.Bounds);

        session.Undo();
        Assert.Equal(new RectD(20, 20, 80, 60), session.ActiveScreen.FindObject(valve.Id)!.Bounds);
        session.Undo();
        Assert.Equal(valve.Bounds, session.ActiveScreen.FindObject(valve.Id)!.Bounds);
        Assert.False(session.CanUndo);
        Assert.True(session.CanRedo);

        session.Redo();
        Assert.Equal(new RectD(20, 20, 80, 60), session.ActiveScreen.FindObject(valve.Id)!.Bounds);
        session.MoveSelection(2, 0);
        Assert.False(session.CanRedo);
        Assert.Equal(new RectD(22, 20, 80, 60), session.ActiveScreen.FindObject(valve.Id)!.Bounds);
    }

    [Fact]
    public void UndoRedoPreserveIndependentPipeGeometryAndStableMetadata()
    {
        var valve = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(100, 100, 80, 60));
        var pipe = PipeObject.Create(
            new PointD(20, 130),
            new PointD(100, 130),
            new[] { new PointD(60, 130) });
        var project = ProjectDocument.Create(
            "History pipe test",
            screens: new[] { ScreenDocument.Create("Main", new SceneObject[] { valve, pipe }) });
        var session = new EditorSession(project, "Main");
        session.SelectOnly(valve.Id);

        session.MoveSelection(15, -4);
        session.Undo();
        session.Redo();

        var restoredPipe = Assert.IsType<PipeObject>(session.ActiveScreen.FindObject(pipe.Id));
        Assert.Equal(pipe.Start, restoredPipe.Start);
        Assert.Equal(pipe.End, restoredPipe.End);
        Assert.Equal(pipe.Bends, restoredPipe.Bends);
        Assert.Equal(pipe.Properties, restoredPipe.Properties);
        Assert.Equal(pipe.Bindings, restoredPipe.Bindings);
        Assert.Equal(new RectD(115, 96, 80, 60), session.ActiveScreen.FindObject(valve.Id)!.Bounds);
    }

    [Fact]
    public void AlignmentAndDistributionAreSingleUndoableSessionEdits()
    {
        var first = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(0, 10, 20, 20));
        var second = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(60, 30, 40, 30));
        var third = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(180, 50, 30, 40));
        var project = ProjectDocument.Create(
            "Productivity test",
            screens: new[] { ScreenDocument.Create("Main", new SceneObject[] { first, second, third }) });
        var session = new EditorSession(project, "Main");
        session.SelectMany(new[] { first.Id, second.Id, third.Id });

        session.AlignSelection(GeometryAlignment.Top);
        Assert.Equal(10, session.ActiveScreen.FindObject(third.Id)!.Bounds.Y);
        Assert.Equal(new[] { first.Id, second.Id, third.Id }, session.SelectedObjectIds);
        session.Undo();
        Assert.Equal(50, session.ActiveScreen.FindObject(third.Id)!.Bounds.Y);

        session.DistributeSelection(GeometryDistribution.Horizontal);
        Assert.Equal(80, session.ActiveScreen.FindObject(second.Id)!.Bounds.X);
        session.Undo();
        Assert.Equal(60, session.ActiveScreen.FindObject(second.Id)!.Bounds.X);
    }

    [Fact]
    public void GroupingCanBeSelectedLaterWithoutChangingSceneObjects()
    {
        var first = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(0, 0, 40, 40));
        var second = ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(60, 0, 80, 60));
        var pipe = PipeObject.Create(new PointD(0, 20), new PointD(60, 20));
        var project = ProjectDocument.Create(
            "Group test",
            screens: new[] { ScreenDocument.Create("Main", new SceneObject[] { first, second, pipe }) });
        var session = new EditorSession(project, "Main");
        session.SelectMany(new[] { first.Id, second.Id });

        var groupId = session.GroupSelection();
        session.ClearSelection();
        session.SelectGroup(groupId);
        session.MoveSelection(10, 5);

        Assert.Equal(new RectD(10, 5, 40, 40), session.ActiveScreen.FindObject(first.Id)!.Bounds);
        Assert.Equal(new RectD(70, 5, 80, 60), session.ActiveScreen.FindObject(second.Id)!.Bounds);
        Assert.Equal(pipe, session.ActiveScreen.FindObject(pipe.Id));
        Assert.True(session.UngroupSelection());
        Assert.False(session.UngroupSelection());
    }

    [Fact]
    public void ToggleSelectionAddsAndRemovesObjectsWithoutChangingExistingSelection()
    {
        var first = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(0, 0, 40, 40));
        var second = ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(60, 0, 80, 60));
        var project = ProjectDocument.Create(
            "Selection test",
            screens: new[] { ScreenDocument.Create("Main", new SceneObject[] { first, second }) });
        var session = new EditorSession(project, "Main");

        session.SelectOnly(first.Id);
        Assert.True(session.ToggleSelection(second.Id));
        Assert.Equal(new[] { first.Id, second.Id }, session.SelectedObjectIds);
        Assert.False(session.ToggleSelection(first.Id));
        Assert.Equal(new[] { second.Id }, session.SelectedObjectIds);
    }

    [Fact]
    public void FrameSelectionSelectsIntersectingObjectsAndClearsForEmptyFrame()
    {
        var first = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(10, 10, 40, 40));
        var second = ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(100, 100, 80, 60));
        var pipe = PipeObject.Create(new PointD(0, 30), new PointD(20, 30));
        var project = ProjectDocument.Create(
            "Frame selection test",
            screens: new[] { ScreenDocument.Create("Main", new SceneObject[] { first, second, pipe }) });
        var session = new EditorSession(project, "Main");

        session.SelectIntersecting(new RectD(0, 0, 70, 70));
        Assert.Contains(first.Id, session.SelectedObjectIds);
        Assert.Contains(pipe.Id, session.SelectedObjectIds);
        Assert.DoesNotContain(second.Id, session.SelectedObjectIds);

        session.SelectIntersecting(new RectD(300, 300, 20, 20));
        Assert.Empty(session.SelectedObjectIds);
    }
}
