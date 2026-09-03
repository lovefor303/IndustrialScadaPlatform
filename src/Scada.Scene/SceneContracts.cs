using Scada.Core;
using System.Text.Json.Serialization;

namespace Scada.Scene;

public readonly record struct PointD(double X, double Y);

public readonly record struct RectD(double X, double Y, double Width, double Height)
{
    public static RectD FromPoints(IEnumerable<PointD> points)
    {
        var pointList = points.ToArray();
        if (pointList.Length == 0)
        {
            throw new ArgumentException("At least one point is required.", nameof(points));
        }

        var minX = pointList.Min(point => point.X);
        var minY = pointList.Min(point => point.Y);
        var maxX = pointList.Max(point => point.X);
        var maxY = pointList.Max(point => point.Y);
        return new RectD(minX, minY, maxX - minX, maxY - minY);
    }
}

public sealed record BindingDefinition(string VariableKey, string TargetProperty);

/// <summary>
/// Declarative editor-time dynamic rule. It describes how a variable may drive
/// a visual property; runtime evaluation belongs to a later runtime module.
/// </summary>
public sealed record DynamicDefinition
{
    public DynamicDefinition(
        string targetProperty,
        string variableKey,
        VariableDataType expectedDataType,
        VariableDirection expectedDirection,
        string? condition = null,
        IReadOnlyDictionary<string, string>? mapping = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetProperty);
        ArgumentException.ThrowIfNullOrWhiteSpace(variableKey);
        TargetProperty = targetProperty;
        VariableKey = variableKey;
        ExpectedDataType = expectedDataType;
        ExpectedDirection = expectedDirection;
        Condition = condition;
        Mapping = mapping ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    public string TargetProperty { get; }

    public string VariableKey { get; init; }

    public VariableDataType ExpectedDataType { get; init; }

    public VariableDirection ExpectedDirection { get; init; }

    public string? Condition { get; init; }

    public IReadOnlyDictionary<string, string> Mapping { get; init; }
}

public sealed record InteractionDefinition
{
    public InteractionDefinition(string eventName, string actionName, IReadOnlyDictionary<string, string>? parameters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);
        EventName = eventName;
        ActionName = actionName;
        Parameters = parameters ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    public string EventName { get; }

    public string ActionName { get; }

    public IReadOnlyDictionary<string, string> Parameters { get; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ControlObject), "control")]
[JsonDerivedType(typeof(PipeObject), "pipe")]
[JsonDerivedType(typeof(TextObject), "text")]
public abstract record SceneObject
{
    protected SceneObject(
        Guid id,
        string type,
        RectD bounds,
        double rotation,
        int zIndex,
        bool isVisible,
        IReadOnlyDictionary<string, string> properties,
        IReadOnlyDictionary<string, BindingDefinition> bindings,
        IReadOnlyDictionary<string, InteractionDefinition> interactions)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Scene object ID must not be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        Id = id;
        Type = type;
        Bounds = bounds;
        Rotation = rotation;
        ZIndex = zIndex;
        IsVisible = isVisible;
        Properties = properties;
        Bindings = bindings;
        Interactions = interactions;
    }

    public Guid Id { get; }

    public string Type { get; }

    public RectD Bounds { get; init; }

    public double Rotation { get; init; }

    public int ZIndex { get; init; }

    public bool IsVisible { get; init; }

    public IReadOnlyDictionary<string, string> Properties { get; init; }

    public IReadOnlyDictionary<string, BindingDefinition> Bindings { get; init; }

    public IReadOnlyDictionary<string, InteractionDefinition> Interactions { get; init; }

    public IReadOnlyDictionary<string, DynamicDefinition> Dynamics { get; init; } =
        new Dictionary<string, DynamicDefinition>(StringComparer.Ordinal);

    public int ControlVersion { get; init; } = 1;

    public static SceneObject Create(string type, RectD bounds, Guid? id = null) =>
        ControlObject.Create(type, bounds, id);
}

public sealed record ControlObject : SceneObject
{
    private ControlObject(
        Guid id,
        string type,
        RectD bounds,
        double rotation,
        int zIndex,
        bool isVisible,
        IReadOnlyDictionary<string, string> properties,
        IReadOnlyDictionary<string, BindingDefinition> bindings,
        IReadOnlyDictionary<string, InteractionDefinition> interactions)
        : base(id, type, bounds, rotation, zIndex, isVisible, properties, bindings, interactions)
    {
    }

    public new static ControlObject Create(string type, RectD bounds, Guid? id = null) =>
        new(
            id ?? Guid.NewGuid(),
            type,
            bounds,
            rotation: 0,
            zIndex: 0,
            isVisible: true,
            new Dictionary<string, string>(),
            new Dictionary<string, BindingDefinition>(),
            new Dictionary<string, InteractionDefinition>());
}

public sealed record TextObject : SceneObject
{
    private TextObject(
        Guid id,
        string text,
        RectD bounds,
        double rotation,
        int zIndex,
        bool isVisible,
        IReadOnlyDictionary<string, string> properties,
        IReadOnlyDictionary<string, BindingDefinition> bindings,
        IReadOnlyDictionary<string, InteractionDefinition> interactions)
        : base(id, "text", bounds, rotation, zIndex, isVisible, properties, bindings, interactions)
    {
        Text = text;
    }

    public string Text { get; init; }

    public new static TextObject Create(string text, RectD bounds, Guid? id = null) =>
        new(
            id ?? Guid.NewGuid(),
            text,
            bounds,
            rotation: 0,
            zIndex: 0,
            isVisible: true,
            new Dictionary<string, string>(),
            new Dictionary<string, BindingDefinition>(),
            new Dictionary<string, InteractionDefinition>());
}

public sealed record PipeObject : SceneObject
{
    private PipeObject(
        Guid id,
        string type,
        PointD start,
        PointD end,
        IReadOnlyList<PointD> bends,
        RectD bounds,
        double rotation,
        int zIndex,
        bool isVisible,
        IReadOnlyDictionary<string, string> properties,
        IReadOnlyDictionary<string, BindingDefinition> bindings,
        IReadOnlyDictionary<string, InteractionDefinition> interactions)
        : base(id, type, bounds, rotation, zIndex, isVisible, properties, bindings, interactions)
    {
        Start = start;
        End = end;
        Bends = bends;
    }

    public PointD Start { get; init; }

    public PointD End { get; init; }

    public IReadOnlyList<PointD> Bends { get; init; }

    public static PipeObject Create(
        PointD start,
        PointD end,
        IEnumerable<PointD>? bends = null,
        Guid? id = null,
        string type = "pipe.straight")
    {
        var bendList = bends?.ToArray() ?? [];
        var points = new[] { start }.Concat(bendList).Append(end);
        return new PipeObject(
            id ?? Guid.NewGuid(),
            type,
            start,
            end,
            bendList,
            RectD.FromPoints(points),
            rotation: 0,
            zIndex: 0,
            isVisible: true,
            new Dictionary<string, string>(),
            new Dictionary<string, BindingDefinition>(),
            new Dictionary<string, InteractionDefinition>());
    }

    public PipeObject WithStart(PointD start) => CopyWithEndpoints(start, End);

    public PipeObject WithEnd(PointD end) => CopyWithEndpoints(Start, end);

    private PipeObject CopyWithEndpoints(PointD start, PointD end)
    {
        var bounds = RectD.FromPoints(new[] { start }.Concat(Bends).Append(end));
        return new PipeObject(
            Id,
            Type,
            start,
            end,
            Bends,
            bounds,
            Rotation,
            ZIndex,
            IsVisible,
            Properties,
            Bindings,
            Interactions)
        {
            ControlVersion = ControlVersion
        };
    }
}

public sealed record ScreenDocument
{
    private ScreenDocument(string name, IReadOnlyList<SceneObject> objects)
    {
        Name = name;
        Objects = objects;
    }

    public string Name { get; }

    public IReadOnlyList<SceneObject> Objects { get; }

    public ScreenDocument AddObject(SceneObject sceneObject)
    {
        ArgumentNullException.ThrowIfNull(sceneObject);
        if (Objects.Any(existing => existing.Id == sceneObject.Id))
        {
            throw new ArgumentException(
                $"Scene object '{sceneObject.Id}' already exists on screen '{Name}'.",
                nameof(sceneObject));
        }

        return Create(Name, Objects.Append(sceneObject));
    }

    public ScreenDocument RemoveObject(Guid objectId)
    {
        if (!Objects.Any(sceneObject => sceneObject.Id == objectId))
        {
            throw new KeyNotFoundException($"Scene object '{objectId}' was not found.");
        }

        return Create(Name, Objects.Where(sceneObject => sceneObject.Id != objectId));
    }

    public SceneObject? FindObject(Guid objectId) =>
        Objects.SingleOrDefault(sceneObject => sceneObject.Id == objectId);

    public ScreenDocument ReplaceObjects(IEnumerable<SceneObject> replacements)
    {
        ArgumentNullException.ThrowIfNull(replacements);
        return Create(Name, replacements);
    }

    public static ScreenDocument Create(string name, IEnumerable<SceneObject>? objects = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var objectList = objects?.ToArray() ?? [];
        var duplicateId = objectList
            .GroupBy(sceneObject => sceneObject.Id)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (duplicateId is not null)
        {
            throw new ArgumentException(
                $"Duplicate scene object ID '{duplicateId}' is not allowed.",
                nameof(objects));
        }

        return new ScreenDocument(name.Trim(), objectList);
    }

    public ScreenDocument ReplaceObject(SceneObject replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        var index = Objects.ToList().FindIndex(sceneObject => sceneObject.Id == replacement.Id);
        if (index < 0)
        {
            throw new KeyNotFoundException($"Scene object '{replacement.Id}' was not found.");
        }

        var updated = Objects.ToArray();
        updated[index] = replacement;
        return new ScreenDocument(Name, updated);
    }
}
