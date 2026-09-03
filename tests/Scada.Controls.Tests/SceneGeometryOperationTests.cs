using Scada.Controls;
using Scada.Scene;
using Scada.Storage;
using Xunit;

namespace Scada.Controls.Tests;

public sealed class SceneGeometryOperationTests
{
    private static readonly ControlCatalog Catalog = ControlCatalog.CreateDefault();

    [Fact]
    public void MovingValveDoesNotMoveUnselectedPipe()
    {
        var valveId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var pipeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var valve = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(40, 30, 40, 40), valveId);
        var pipe = PipeObject.Create(new PointD(0, 50), new PointD(120, 50), id: pipeId);
        var screen = ScreenDocument.Create("Main", new SceneObject[] { valve, pipe });

        var moved = SceneGeometryOperations.Move(screen, new[] { valveId }, dx: 1, dy: 0);

        Assert.Equal(pipe, Assert.IsType<PipeObject>(moved.Objects.Single(item => item.Id == pipeId)));
        Assert.Equal(41, Assert.IsType<ControlObject>(moved.Objects.Single(item => item.Id == valveId)).Bounds.X);
    }

    [Fact]
    public void GroupMoveChangesOnlyExplicitIds()
    {
        var pump = ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(10, 20, 120, 72));
        var valve = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(160, 30, 80, 64));
        var pipe = PipeObject.Create(new PointD(130, 56), new PointD(160, 56));
        var label = TextObject.Create("P-101", new RectD(10, 96, 80, 24));
        var screen = ScreenDocument.Create("Main", new SceneObject[] { pump, valve, pipe, label });

        var moved = SceneGeometryOperations.Move(screen, new[] { pump.Id, label.Id }, dx: 4, dy: -2);

        Assert.Equal(pipe, moved.Objects.Single(item => item.Id == pipe.Id));
        Assert.Equal(valve, moved.Objects.Single(item => item.Id == valve.Id));
        Assert.Equal(new RectD(14, 18, 120, 72), moved.Objects.Single(item => item.Id == pump.Id).Bounds);
        Assert.Equal(new RectD(14, 94, 80, 24), moved.Objects.Single(item => item.Id == label.Id).Bounds);
    }

    [Fact]
    public void AlignmentUsesSelectedObjectEdgesAndLeavesPipeGeometryUntouched()
    {
        var left = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(20, 40, 40, 30));
        var middle = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(80, 10, 60, 50));
        var right = TextObject.Create("设备", new RectD(160, 70, 50, 20));
        var pipe = PipeObject.Create(new PointD(0, 25), new PointD(20, 25), id: Guid.NewGuid());
        var screen = ScreenDocument.Create("Main", new SceneObject[] { left, middle, right, pipe });

        var aligned = SceneGeometryOperations.Align(
            screen,
            new[] { left.Id, middle.Id, right.Id, pipe.Id },
            GeometryAlignment.Left);

        Assert.Equal(20, aligned.FindObject(left.Id)!.Bounds.X);
        Assert.Equal(20, aligned.FindObject(middle.Id)!.Bounds.X);
        Assert.Equal(20, aligned.FindObject(right.Id)!.Bounds.X);
        Assert.Equal(pipe, aligned.FindObject(pipe.Id));
        Assert.Equal(left.Rotation, aligned.FindObject(left.Id)!.Rotation);
    }

    [Fact]
    public void DistributionEqualizesGapsWithoutChangingObjectSizesOrMetadata()
    {
        var first = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(0, 10, 20, 20));
        var second = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(60, 30, 40, 30));
        var third = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(180, 50, 30, 40));
        var pipe = PipeObject.Create(new PointD(5, 5), new PointD(8, 8), id: Guid.NewGuid());
        var screen = ScreenDocument.Create("Main", new SceneObject[] { first, second, third, pipe });

        var distributed = SceneGeometryOperations.Distribute(
            screen,
            new[] { first.Id, second.Id, third.Id, pipe.Id },
            GeometryDistribution.Horizontal);

        Assert.Equal(0, distributed.FindObject(first.Id)!.Bounds.X);
        Assert.Equal(180, distributed.FindObject(third.Id)!.Bounds.X);
        Assert.Equal(80, distributed.FindObject(second.Id)!.Bounds.X);
        Assert.Equal(second.Bounds.Height, distributed.FindObject(second.Id)!.Bounds.Height);
        Assert.Equal(pipe, distributed.FindObject(pipe.Id));
    }

    [Fact]
    public void AlignmentAndDistributionRequireTwoNonPipeObjects()
    {
        var pipe = PipeObject.Create(new PointD(0, 0), new PointD(10, 0));
        var screen = ScreenDocument.Create("Main", new SceneObject[] { pipe });

        Assert.Throws<InvalidOperationException>(() =>
            SceneGeometryOperations.Align(screen, new[] { pipe.Id }, GeometryAlignment.Left));
        Assert.Throws<InvalidOperationException>(() =>
            SceneGeometryOperations.Distribute(screen, new[] { pipe.Id }, GeometryDistribution.Horizontal));
    }

    [Fact]
    public void LayerOperationsChangeOnlySelectedZOrderAndPreserveObjectGeometry()
    {
        var back = ControlObject.Create(ControlTypeIds.Vessel, new RectD(0, 0, 100, 100)) with { ZIndex = 0 };
        var middle = ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(10, 10, 80, 60)) with { ZIndex = 1 };
        var front = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(20, 20, 40, 40)) with { ZIndex = 2 };
        var pipe = PipeObject.Create(new PointD(0, 50), new PointD(20, 50), id: Guid.NewGuid()) with { ZIndex = 3 };
        var screen = ScreenDocument.Create("Main", new SceneObject[] { back, middle, front, pipe });

        var moved = SceneGeometryOperations.ChangeLayer(
            screen,
            new[] { middle.Id },
            LayerOrderOperation.BringToFront);

        Assert.Equal(4, moved.FindObject(middle.Id)!.ZIndex);
        Assert.Equal(middle.Bounds, moved.FindObject(middle.Id)!.Bounds);
        Assert.Equal(pipe, moved.FindObject(pipe.Id));

        var sentBack = SceneGeometryOperations.ChangeLayer(
            moved,
            new[] { middle.Id },
            LayerOrderOperation.SendToBack);
        Assert.Equal(-1, sentBack.FindObject(middle.Id)!.ZIndex);
        Assert.Equal(front.ZIndex, sentBack.FindObject(front.Id)!.ZIndex);
    }

    [Fact]
    public void NudgeMovesSelectedObjectsByExactlyOneSceneUnit()
    {
        var label = TextObject.Create("XV-102", new RectD(10, 20, 80, 24));
        var screen = ScreenDocument.Create("Main", new SceneObject[] { label });

        var nudged = SceneGeometryOperations.Nudge(screen, new[] { label.Id }, GeometryNudgeDirection.Right);

        Assert.Equal(new RectD(11, 20, 80, 24), nudged.Objects.Single().Bounds);
    }

    [Fact]
    public void RotationKeepsControlBoundsAndRotatesAboutObjectCenter()
    {
        var valve = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(40, 30, 100, 80));
        var screen = ScreenDocument.Create("Main", new SceneObject[] { valve });

        var rotated = SceneGeometryOperations.Rotate(screen, new[] { valve.Id }, degrees: 90);
        var result = Assert.IsType<ControlObject>(rotated.Objects.Single());

        Assert.Equal(new RectD(40, 30, 100, 80), result.Bounds);
        Assert.Equal(90, result.Rotation);
    }

    [Fact]
    public void AspectPreservingResizeUsesCatalogShapeInsteadOfSelectionRectangle()
    {
        var pump = ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(10, 20, 120, 72));
        var screen = ScreenDocument.Create("Main", new SceneObject[] { pump });

        var resized = SceneGeometryOperations.Resize(
            screen,
            Catalog,
            pump.Id,
            new RectD(10, 20, 240, 72));

        Assert.Equal(new RectD(10, 20, 240, 144), resized.Objects.Single().Bounds);
    }

    [Fact]
    public void StraightPipeResizeUsesFreeEndpointWithoutChangingUnselectedObject()
    {
        var pipe = PipeObject.Create(new PointD(10, 20), new PointD(100, 20), [new PointD(50, 20)]);
        var label = TextObject.Create("P-101", new RectD(0, 40, 80, 24));
        var screen = ScreenDocument.Create("Main", new SceneObject[] { pipe, label });

        var resized = SceneGeometryOperations.ResizePipeEndpoint(
            screen,
            pipe.Id,
            PipeEndpoint.End,
            new PointD(160, 50));
        var updatedPipe = Assert.IsType<PipeObject>(resized.Objects.Single(item => item.Id == pipe.Id));

        Assert.Equal(new PointD(10, 20), updatedPipe.Start);
        Assert.Equal(new PointD(160, 50), updatedPipe.End);
        Assert.Equal(RectD.FromPoints([new PointD(10, 20), new PointD(50, 20), new PointD(160, 50)]), updatedPipe.Bounds);
        Assert.Equal(label, resized.Objects.Single(item => item.Id == label.Id));
    }

    [Fact]
    public void GeometryOperationsRejectNonFiniteCoordinatesNegativeSizesAndMissingObjects()
    {
        var pump = ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(10, 20, 120, 72));
        var screen = ScreenDocument.Create("Main", new SceneObject[] { pump });

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SceneGeometryOperations.Move(screen, new[] { pump.Id }, double.NaN, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SceneGeometryOperations.Resize(screen, Catalog, pump.Id, new RectD(0, 0, -1, 10)));
        Assert.Throws<KeyNotFoundException>(() =>
            SceneGeometryOperations.Move(screen, new[] { Guid.Parse("33333333-3333-3333-3333-333333333333") }, 1, 0));
    }

    [Fact]
    public void GeometrySurvivesProjectSerializationWithoutDrift()
    {
        var pipe = PipeObject.Create(new PointD(10.25, 20.5), new PointD(100.75, 30.125), [new PointD(50.5, 22.25)]);
        var screen = ScreenDocument.Create("Main", new SceneObject[] { pipe });
        var edited = SceneGeometryOperations.ResizePipeEndpoint(
            SceneGeometryOperations.Move(screen, new[] { pipe.Id }, 0.125, -0.25),
            pipe.Id,
            PipeEndpoint.End,
            new PointD(160.875, 40.625));
        var project = ProjectDocument.Create("Geometry", screens: new[] { edited });

        var restored = new JsonProjectSerializer().Deserialize(new JsonProjectSerializer().Serialize(project));
        var restoredPipe = Assert.IsType<PipeObject>(restored.Screens.Single().Objects.Single());
        var expected = Assert.IsType<PipeObject>(edited.Objects.Single());

        Assert.Equal(expected.Id, restoredPipe.Id);
        Assert.Equal(expected.Start, restoredPipe.Start);
        Assert.Equal(expected.End, restoredPipe.End);
        Assert.Equal(expected.Bends, restoredPipe.Bends);
        Assert.Equal(expected.Bounds, restoredPipe.Bounds);
    }
}
