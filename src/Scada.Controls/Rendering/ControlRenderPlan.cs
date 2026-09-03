using Scada.Core;

namespace Scada.Controls.Rendering;

public sealed record ControlRenderContext
{
    private ControlRenderContext(
        ControlState state,
        IReadOnlyDictionary<string, double> numericValues,
        IReadOnlyDictionary<string, string> textValues,
        VariableQuality quality,
        bool reducedMotion)
    {
        State = state;
        NumericValues = numericValues;
        TextValues = textValues;
        Quality = quality;
        ReducedMotion = reducedMotion;
    }

    public ControlState State { get; init; }

    public IReadOnlyDictionary<string, double> NumericValues { get; init; }

    public IReadOnlyDictionary<string, string> TextValues { get; init; }

    public VariableQuality Quality { get; init; }

    public bool ReducedMotion { get; init; }

    public static ControlRenderContext ForState(ControlState state) =>
        new(
            state,
            new Dictionary<string, double>(StringComparer.Ordinal),
            new Dictionary<string, string>(StringComparer.Ordinal),
            VariableQuality.Good,
            reducedMotion: false);

    public double GetClampedNumeric(string key, double minimum, double maximum, double fallback = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!double.IsFinite(minimum) || !double.IsFinite(maximum) || minimum > maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(minimum), "Numeric clamp bounds must be finite and ordered.");
        }

        var value = NumericValues.TryGetValue(key, out var candidate) && double.IsFinite(candidate)
            ? candidate
            : fallback;
        return Math.Clamp(value, minimum, maximum);
    }
}

public sealed record ControlRenderPlan
{
    public ControlRenderPlan(
        string typeId,
        int version,
        RenderSize designSize,
        IReadOnlyList<RenderPrimitive> primitives,
        IReadOnlyDictionary<string, RenderPoint> anchors,
        ControlState state,
        IReadOnlySet<string> activeAnimations,
        IReadOnlyDictionary<string, string> diagnostics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeId);
        ArgumentOutOfRangeException.ThrowIfLessThan(version, 1);
        ArgumentNullException.ThrowIfNull(primitives);
        ArgumentNullException.ThrowIfNull(anchors);
        ArgumentNullException.ThrowIfNull(activeAnimations);
        ArgumentNullException.ThrowIfNull(diagnostics);

        var partIds = Flatten(primitives).Select(primitive => primitive.PartId).ToArray();
        EnsureUnique(partIds, "primitive part ID");
        foreach (var pair in anchors)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pair.Key);
            if (!double.IsFinite(pair.Value.X) || !double.IsFinite(pair.Value.Y)
                || pair.Value.X < 0 || pair.Value.X > designSize.Width
                || pair.Value.Y < 0 || pair.Value.Y > designSize.Height)
            {
                throw new ArgumentOutOfRangeException(nameof(anchors),
                    $"Anchor '{pair.Key}' must be inside the design bounds.");
            }
        }

        TypeId = typeId;
        Version = version;
        DesignSize = designSize;
        Primitives = primitives;
        Anchors = anchors;
        State = state;
        ActiveAnimations = activeAnimations;
        Diagnostics = diagnostics;
    }

    public string TypeId { get; }

    public int Version { get; }

    public RenderSize DesignSize { get; }

    public IReadOnlyList<RenderPrimitive> Primitives { get; }

    public IReadOnlyDictionary<string, RenderPoint> Anchors { get; }

    public ControlState State { get; }

    public IReadOnlySet<string> ActiveAnimations { get; }

    public IReadOnlyDictionary<string, string> Diagnostics { get; }

    private static IEnumerable<RenderPrimitive> Flatten(IEnumerable<RenderPrimitive> primitives)
    {
        foreach (var primitive in primitives)
        {
            yield return primitive;
            if (primitive is RenderGroup group)
            {
                foreach (var child in Flatten(group.Children))
                {
                    yield return child;
                }
            }
        }
    }

    private static void EnsureUnique(IEnumerable<string> values, string description)
    {
        var duplicate = values
            .GroupBy(value => value, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"Duplicate {description} '{duplicate.Key}' is not allowed.");
        }
    }
}
