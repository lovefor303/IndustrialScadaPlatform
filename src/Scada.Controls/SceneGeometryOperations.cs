using Scada.Scene;

namespace Scada.Controls;

/// <summary>
/// Immutable, selection-scoped scene editing operations. Pipes are ordinary scene
/// objects: they change only when explicitly selected or when one of their own
/// endpoints is edited.
/// </summary>
public static class SceneGeometryOperations
{
    public static ScreenDocument Move(
        ScreenDocument screen,
        IEnumerable<Guid> selectedObjectIds,
        double dx,
        double dy)
    {
        ArgumentNullException.ThrowIfNull(screen);
        EnsureFinite(dx, nameof(dx));
        EnsureFinite(dy, nameof(dy));
        var selected = MaterializeSelection(screen, selectedObjectIds);

        return ReplaceSelected(screen, selected, sceneObject => sceneObject switch
        {
            PipeObject pipe => CopyPipe(
                pipe,
                Offset(pipe.Start, dx, dy),
                Offset(pipe.End, dx, dy),
                pipe.Bends.Select(point => Offset(point, dx, dy))),
            _ => sceneObject with { Bounds = Offset(sceneObject.Bounds, dx, dy) }
        });
    }

    public static ScreenDocument Nudge(
        ScreenDocument screen,
        IEnumerable<Guid> selectedObjectIds,
        GeometryNudgeDirection direction) => direction switch
        {
            GeometryNudgeDirection.Left => Move(screen, selectedObjectIds, -1, 0),
            GeometryNudgeDirection.Right => Move(screen, selectedObjectIds, 1, 0),
            GeometryNudgeDirection.Up => Move(screen, selectedObjectIds, 0, -1),
            GeometryNudgeDirection.Down => Move(screen, selectedObjectIds, 0, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };

    public static ScreenDocument Align(
        ScreenDocument screen,
        IEnumerable<Guid> selectedObjectIds,
        GeometryAlignment alignment)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var selected = MaterializeSelection(screen, selectedObjectIds);
        var objects = GetEditableObjects(screen, selected);
        EnsureAtLeastTwoEditableObjects(objects);

        var isHorizontal = alignment is GeometryAlignment.Left or GeometryAlignment.Center or GeometryAlignment.Right;
        if (!isHorizontal && alignment is not (GeometryAlignment.Top or GeometryAlignment.Middle or GeometryAlignment.Bottom))
        {
            throw new ArgumentOutOfRangeException(nameof(alignment));
        }

        var minimum = isHorizontal
            ? objects.Min(sceneObject => sceneObject.Bounds.X)
            : objects.Min(sceneObject => sceneObject.Bounds.Y);
        var maximum = isHorizontal
            ? objects.Max(sceneObject => sceneObject.Bounds.X + sceneObject.Bounds.Width)
            : objects.Max(sceneObject => sceneObject.Bounds.Y + sceneObject.Bounds.Height);
        var center = (minimum + maximum) / 2;
        var leading = alignment switch
        {
            GeometryAlignment.Left or GeometryAlignment.Top => minimum,
            GeometryAlignment.Right or GeometryAlignment.Bottom => maximum,
            _ => center
        };

        return ReplaceSelected(screen, selected, sceneObject =>
        {
            if (sceneObject is PipeObject)
            {
                return sceneObject;
            }

            var bounds = sceneObject.Bounds;
            var updated = alignment switch
            {
                GeometryAlignment.Left => bounds with { X = leading },
                GeometryAlignment.Center => bounds with { X = center - bounds.Width / 2 },
                GeometryAlignment.Right => bounds with { X = leading - bounds.Width },
                GeometryAlignment.Top => bounds with { Y = leading },
                GeometryAlignment.Middle => bounds with { Y = center - bounds.Height / 2 },
                GeometryAlignment.Bottom => bounds with { Y = leading - bounds.Height },
                _ => throw new ArgumentOutOfRangeException(nameof(alignment))
            };
            return sceneObject with { Bounds = updated };
        });
    }

    public static ScreenDocument Distribute(
        ScreenDocument screen,
        IEnumerable<Guid> selectedObjectIds,
        GeometryDistribution distribution)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var selected = MaterializeSelection(screen, selectedObjectIds);
        var objects = GetEditableObjects(screen, selected);
        EnsureAtLeastTwoEditableObjects(objects);
        if (objects.Count < 3)
        {
            return screen;
        }

        var horizontal = distribution == GeometryDistribution.Horizontal;
        if (!horizontal && distribution != GeometryDistribution.Vertical)
        {
            throw new ArgumentOutOfRangeException(nameof(distribution));
        }

        var ordered = (horizontal
                ? objects.OrderBy(sceneObject => sceneObject.Bounds.X)
                : objects.OrderBy(sceneObject => sceneObject.Bounds.Y))
            .ThenBy(sceneObject => sceneObject.Id)
            .ToArray();
        var first = ordered[0].Bounds;
        var last = ordered[^1].Bounds;
        var start = horizontal ? first.X : first.Y;
        var end = horizontal ? last.X + last.Width : last.Y + last.Height;
        var totalSize = ordered.Sum(sceneObject => horizontal ? sceneObject.Bounds.Width : sceneObject.Bounds.Height);
        var gap = (end - start - totalSize) / (ordered.Length - 1);
        var next = start;
        var positions = new Dictionary<Guid, double>();
        foreach (var sceneObject in ordered)
        {
            positions[sceneObject.Id] = next;
            next += (horizontal ? sceneObject.Bounds.Width : sceneObject.Bounds.Height) + gap;
        }

        return ReplaceSelected(screen, selected, sceneObject =>
        {
            if (sceneObject is PipeObject || !positions.TryGetValue(sceneObject.Id, out var position))
            {
                return sceneObject;
            }

            var bounds = horizontal
                ? sceneObject.Bounds with { X = position }
                : sceneObject.Bounds with { Y = position };
            return sceneObject with { Bounds = bounds };
        });
    }

    public static ScreenDocument ChangeLayer(
        ScreenDocument screen,
        IEnumerable<Guid> selectedObjectIds,
        LayerOrderOperation operation)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var selected = MaterializeSelection(screen, selectedObjectIds);
        if (selected.Count == 0)
        {
            throw new InvalidOperationException("At least one object must be selected.");
        }

        var minimum = screen.Objects.Min(sceneObject => sceneObject.ZIndex);
        var maximum = screen.Objects.Max(sceneObject => sceneObject.ZIndex);
        return ReplaceSelected(screen, selected, sceneObject =>
        {
            var zIndex = operation switch
            {
                LayerOrderOperation.BringForward => sceneObject.ZIndex + 1,
                LayerOrderOperation.SendBackward => sceneObject.ZIndex - 1,
                LayerOrderOperation.BringToFront => maximum + 1,
                LayerOrderOperation.SendToBack => minimum - 1,
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            };
            return sceneObject with { ZIndex = zIndex };
        });
    }

    public static ScreenDocument Rotate(
        ScreenDocument screen,
        IEnumerable<Guid> selectedObjectIds,
        double degrees)
    {
        ArgumentNullException.ThrowIfNull(screen);
        EnsureFinite(degrees, nameof(degrees));
        var selected = MaterializeSelection(screen, selectedObjectIds);

        return ReplaceSelected(screen, selected, sceneObject => sceneObject switch
        {
            PipeObject pipe => RotatePipe(pipe, degrees),
            _ => sceneObject with { Rotation = NormalizeDegrees(sceneObject.Rotation + degrees) }
        });
    }

    public static ScreenDocument Resize(
        ScreenDocument screen,
        ControlCatalog catalog,
        Guid objectId,
        RectD requestedBounds)
    {
        ArgumentNullException.ThrowIfNull(screen);
        ArgumentNullException.ThrowIfNull(catalog);
        EnsureBounds(requestedBounds, nameof(requestedBounds));
        var sceneObject = GetRequired(screen, objectId);
        if (sceneObject is PipeObject)
        {
            throw new InvalidOperationException("Pipe objects must use ResizePipeEndpoint.");
        }

        var bounds = sceneObject is ControlObject control
            ? ApplyResizePolicy(control.Bounds, requestedBounds, catalog.Get(control.Type, control.ControlVersion))
            : requestedBounds;
        return screen.ReplaceObject(sceneObject with { Bounds = bounds });
    }

    public static ScreenDocument ResizePipeEndpoint(
        ScreenDocument screen,
        Guid pipeId,
        PipeEndpoint endpoint,
        PointD point)
    {
        ArgumentNullException.ThrowIfNull(screen);
        EnsureFinite(point.X, nameof(point));
        EnsureFinite(point.Y, nameof(point));
        var pipe = GetRequired(screen, pipeId) as PipeObject
            ?? throw new InvalidOperationException($"Scene object '{pipeId}' is not a pipe.");

        var replacement = endpoint switch
        {
            PipeEndpoint.Start => CopyPipe(pipe, point, pipe.End, pipe.Bends),
            PipeEndpoint.End => CopyPipe(pipe, pipe.Start, point, pipe.Bends),
            _ => throw new ArgumentOutOfRangeException(nameof(endpoint))
        };
        return screen.ReplaceObject(replacement);
    }

    private static ScreenDocument ReplaceSelected(
        ScreenDocument screen,
        HashSet<Guid> selected,
        Func<SceneObject, SceneObject> transform) =>
        ScreenDocument.Create(
            screen.Name,
            screen.Objects.Select(sceneObject => selected.Contains(sceneObject.Id) ? transform(sceneObject) : sceneObject));

    private static HashSet<Guid> MaterializeSelection(ScreenDocument screen, IEnumerable<Guid> selectedObjectIds)
    {
        ArgumentNullException.ThrowIfNull(selectedObjectIds);
        var selected = new HashSet<Guid>(selectedObjectIds);
        foreach (var id in selected)
        {
            _ = GetRequired(screen, id);
        }

        return selected;
    }

    private static List<SceneObject> GetEditableObjects(ScreenDocument screen, HashSet<Guid> selected) =>
        screen.Objects
            .Where(sceneObject => selected.Contains(sceneObject.Id) && sceneObject is not PipeObject)
            .ToList();

    private static void EnsureAtLeastTwoEditableObjects(List<SceneObject> objects)
    {
        if (objects.Count < 2)
        {
            throw new InvalidOperationException("At least two non-pipe objects must be selected.");
        }
    }

    private static SceneObject GetRequired(ScreenDocument screen, Guid id) =>
        screen.Objects.SingleOrDefault(sceneObject => sceneObject.Id == id)
        ?? throw new KeyNotFoundException($"Scene object '{id}' was not found.");

    private static RectD ApplyResizePolicy(RectD current, RectD requested, ControlDefinition definition) =>
        definition.ResizePolicy switch
        {
            ResizePolicy.PreserveAspectRatio => PreserveAspectRatio(requested, definition),
            ResizePolicy.StretchHorizontal => new RectD(
                requested.X,
                requested.Y,
                Math.Max(requested.Width, definition.MinimumSize.Width),
                current.Height),
            ResizePolicy.StretchVertical => new RectD(
                requested.X,
                requested.Y,
                current.Width,
                Math.Max(requested.Height, definition.MinimumSize.Height)),
            ResizePolicy.Free => new RectD(
                requested.X,
                requested.Y,
                Math.Max(requested.Width, definition.MinimumSize.Width),
                Math.Max(requested.Height, definition.MinimumSize.Height)),
            _ => throw new ArgumentOutOfRangeException(nameof(definition))
        };

    private static RectD PreserveAspectRatio(RectD requested, ControlDefinition definition)
    {
        var scale = Math.Max(
            requested.Width / definition.DefaultSize.Width,
            requested.Height / definition.DefaultSize.Height);
        var minimumScale = Math.Max(
            definition.MinimumSize.Width / definition.DefaultSize.Width,
            definition.MinimumSize.Height / definition.DefaultSize.Height);
        scale = Math.Max(scale, minimumScale);
        return new RectD(
            requested.X,
            requested.Y,
            definition.DefaultSize.Width * scale,
            definition.DefaultSize.Height * scale);
    }

    private static PipeObject RotatePipe(PipeObject pipe, double degrees)
    {
        var center = new PointD(
            pipe.Bounds.X + pipe.Bounds.Width / 2,
            pipe.Bounds.Y + pipe.Bounds.Height / 2);
        return CopyPipe(
            pipe,
            RotatePoint(pipe.Start, center, degrees),
            RotatePoint(pipe.End, center, degrees),
            pipe.Bends.Select(point => RotatePoint(point, center, degrees)));
    }

    private static PointD RotatePoint(PointD point, PointD center, double degrees)
    {
        var radians = degrees * Math.PI / 180;
        var cosine = Math.Cos(radians);
        var sine = Math.Sin(radians);
        var x = point.X - center.X;
        var y = point.Y - center.Y;
        return new PointD(
            center.X + (x * cosine) - (y * sine),
            center.Y + (x * sine) + (y * cosine));
    }

    private static PipeObject CopyPipe(
        PipeObject source,
        PointD start,
        PointD end,
        IEnumerable<PointD> bends) =>
        PipeObject.Create(start, end, bends, source.Id, source.Type) with
        {
            Rotation = source.Rotation,
            ZIndex = source.ZIndex,
            IsVisible = source.IsVisible,
            Properties = source.Properties,
            Bindings = source.Bindings,
            Interactions = source.Interactions,
            ControlVersion = source.ControlVersion
        };

    private static RectD Offset(RectD bounds, double dx, double dy) =>
        new(bounds.X + dx, bounds.Y + dy, bounds.Width, bounds.Height);

    private static PointD Offset(PointD point, double dx, double dy) =>
        new(point.X + dx, point.Y + dy);

    private static double NormalizeDegrees(double degrees)
    {
        var normalized = degrees % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }

    private static void EnsureBounds(RectD bounds, string parameterName)
    {
        EnsureFinite(bounds.X, parameterName);
        EnsureFinite(bounds.Y, parameterName);
        EnsureFinite(bounds.Width, parameterName);
        EnsureFinite(bounds.Height, parameterName);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Bounds dimensions must be positive.");
        }
    }

    private static void EnsureFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Geometry values must be finite.");
        }
    }
}

public enum GeometryNudgeDirection
{
    Left,
    Right,
    Up,
    Down
}

public enum GeometryAlignment
{
    Left,
    Center,
    Right,
    Top,
    Middle,
    Bottom
}

public enum GeometryDistribution
{
    Horizontal,
    Vertical
}

public enum LayerOrderOperation
{
    BringForward,
    SendBackward,
    BringToFront,
    SendToBack
}

public enum PipeEndpoint
{
    Start,
    End
}
