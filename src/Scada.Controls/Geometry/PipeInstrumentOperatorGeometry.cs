using System.Globalization;
using Scada.Controls.Rendering;
using Scada.Core;

namespace Scada.Controls.Geometry;

public static class PipeInstrumentOperatorGeometry
{
    public static ControlRenderPlan BuildStraightPipe(ControlRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var flowing = context.State == ControlState.Active
            && context.Quality == VariableQuality.Good
            && !context.ReducedMotion;
        var primitives = new List<RenderPrimitive>
        {
            new RenderLine("pipe.body", new(0, 10), new(120, 10), PipeToken(context))
        };
        if (flowing)
        {
            primitives.Add(new RenderPath(
                "pipe.flow-marker",
                [new MoveTo(new(52, 4)), new LineTo(new(64, 10)), new LineTo(new(52, 16)), new ClosePath()],
                VisualTokens.Active,
                "pipe.flow"));
        }

        return Plan(
            ControlTypeIds.StraightPipe,
            new(120, 20),
            primitives,
            new Dictionary<string, RenderPoint>(StringComparer.Ordinal)
            {
                ["start"] = new(0, 10),
                ["end"] = new(120, 10)
            },
            context,
            flowing
                ? new HashSet<string>(["pipe.flow"], StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal));
    }

    public static ControlRenderPlan BuildElbow(ControlRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Plan(
            ControlTypeIds.PipeElbow,
            new(60, 60),
            [new RenderPath(
                "pipe.elbow-path",
                [new MoveTo(new(0, 30)), new LineTo(new(30, 30)), new LineTo(new(30, 60))],
                PipeToken(context))],
            new Dictionary<string, RenderPoint>(StringComparer.Ordinal)
            {
                ["inlet"] = new(0, 30),
                ["outlet"] = new(30, 60)
            },
            context);
    }

    public static ControlRenderPlan BuildTee(ControlRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Plan(
            ControlTypeIds.PipeTee,
            new(80, 80),
            [new RenderPath(
                "pipe.tee-path",
                [new MoveTo(new(0, 40)), new LineTo(new(80, 40)), new MoveTo(new(40, 40)), new LineTo(new(40, 0))],
                PipeToken(context))],
            new Dictionary<string, RenderPoint>(StringComparer.Ordinal)
            {
                ["inlet"] = new(0, 40),
                ["outlet"] = new(80, 40),
                ["branch"] = new(40, 0)
            },
            context);
    }

    public static ControlRenderPlan BuildNumericDisplay(ControlRenderContext context) =>
        BuildNumericInstrument(ControlTypeIds.NumericDisplay, "numeric", context, includeDial: false);

    public static ControlRenderPlan BuildLevelBar(ControlRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var raw = GetRawValue(context);
        var display = Math.Clamp(raw, 0, 100);
        const double trackTop = 8;
        const double trackHeight = 104;
        var fillHeight = trackHeight * display / 100;
        var fillTop = trackTop + trackHeight - fillHeight;
        var primitives = new List<RenderPrimitive>
        {
            new RenderRectangle("level.track", new(14, trackTop, 24, trackHeight), VisualTokens.EquipmentBody),
            new RenderRectangle("level.fill", new(18, fillTop, 16, fillHeight), StateToken(context)),
            new RenderText("instrument.value", new(50, 55), FormatNumber(display), VisualTokens.EquipmentBody),
            new RenderText("instrument.unit", new(89, 55), "%", VisualTokens.EquipmentDepth)
        };
        AddQualityMarker(primitives, context, new(104, 8));

        return Plan(
            ControlTypeIds.LevelBar,
            new(120, 120),
            primitives,
            new Dictionary<string, RenderPoint>(StringComparer.Ordinal),
            context,
            diagnostics: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["instrument.family"] = "level",
                ["raw.value"] = FormatNumber(raw),
                ["display.percent"] = FormatNumber(display)
            });
    }

    public static ControlRenderPlan BuildTemperatureIndicator(ControlRenderContext context) =>
        BuildNumericInstrument(ControlTypeIds.TemperatureIndicator, "temperature", context, includeDial: true);

    public static ControlRenderPlan BuildPressureIndicator(ControlRenderContext context) =>
        BuildNumericInstrument(ControlTypeIds.PressureIndicator, "pressure", context, includeDial: true);

    public static ControlRenderPlan BuildFlowIndicator(ControlRenderContext context) =>
        BuildNumericInstrument(ControlTypeIds.FlowIndicator, "flow", context, includeDial: true);

    public static ControlRenderPlan BuildCommandButton(ControlRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var label = context.TextValues.TryGetValue("Label", out var requestedLabel)
            ? requestedLabel
            : "执行";
        var actionKind = context.TextValues.TryGetValue("ActionKind", out var requestedActionKind)
            ? requestedActionKind
            : "write-command";
        var actionTarget = context.TextValues.TryGetValue("ActionTarget", out var requestedTarget)
            ? requestedTarget
            : string.Empty;
        var pending = context.State == ControlState.Transition;
        var primitives = new RenderPrimitive[]
        {
            new RenderRectangle("button.body", new(1, 1, 118, 38), pending ? VisualTokens.Transition : VisualTokens.EquipmentBody),
            new RenderText("button.label", new(12, 24), label, VisualTokens.Outline),
            new RenderEllipse("button.pending", new(101, 13, 10, 10), pending ? VisualTokens.Transition : VisualTokens.Stopped)
        };

        return Plan(
            ControlTypeIds.CommandButton,
            new(120, 40),
            primitives,
            new Dictionary<string, RenderPoint>(StringComparer.Ordinal),
            context,
            diagnostics: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["action.kind"] = actionKind,
                ["action.target"] = actionTarget,
                ["action.state"] = pending ? "pending" : "idle"
            });
    }

    private static ControlRenderPlan BuildNumericInstrument(
        string typeId,
        string family,
        ControlRenderContext context,
        bool includeDial)
    {
        ArgumentNullException.ThrowIfNull(context);

        var raw = GetRawValue(context);
        var minimum = GetNumeric(context, "Minimum", 0);
        var maximum = GetNumeric(context, "Maximum", 100);
        if (minimum > maximum)
        {
            (minimum, maximum) = (maximum, minimum);
        }

        var display = Math.Clamp(raw, minimum, maximum);
        var unit = context.TextValues.TryGetValue("Unit", out var requestedUnit) ? requestedUnit : string.Empty;
        var primitives = new List<RenderPrimitive>
        {
            new RenderRectangle("instrument.body", new(1, 1, 118, 46), VisualTokens.EquipmentBody)
        };
        if (includeDial)
        {
            primitives.Add(new RenderEllipse("instrument.dial", new(6, 7, 34, 34), VisualTokens.EquipmentDepth));
            var ratio = maximum == minimum ? 0 : (display - minimum) / (maximum - minimum);
            var needleX = 23 + ((ratio - 0.5) * 22);
            primitives.Add(new RenderLine("instrument.needle", new(23, 24), new(needleX, 13), StateToken(context)));
        }

        primitives.Add(new RenderText("instrument.value", new(includeDial ? 48 : 10, 28), FormatNumber(display), VisualTokens.Outline));
        primitives.Add(new RenderText("instrument.unit", new(92, 28), unit, VisualTokens.EquipmentDepth));
        AddQualityMarker(primitives, context, new(108, 6));

        return Plan(
            typeId,
            new(120, 48),
            primitives,
            new Dictionary<string, RenderPoint>(StringComparer.Ordinal),
            context,
            diagnostics: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["instrument.family"] = family,
                ["raw.value"] = FormatNumber(raw),
                ["display.value"] = FormatNumber(display),
                ["display.unit"] = unit
            });
    }

    private static ControlRenderPlan Plan(
        string typeId,
        RenderSize size,
        IReadOnlyList<RenderPrimitive> primitives,
        IReadOnlyDictionary<string, RenderPoint> anchors,
        ControlRenderContext context,
        IReadOnlySet<string>? activeAnimations = null,
        IReadOnlyDictionary<string, string>? diagnostics = null) =>
        new(
            typeId,
            version: 1,
            size,
            primitives,
            anchors,
            context.State,
            activeAnimations ?? new HashSet<string>(StringComparer.Ordinal),
            diagnostics ?? new Dictionary<string, string>(StringComparer.Ordinal));

    private static void AddQualityMarker(List<RenderPrimitive> primitives, ControlRenderContext context, RenderPoint position)
    {
        if (context.Quality != VariableQuality.Good || context.State == ControlState.Unknown)
        {
            primitives.Add(new RenderEllipse("quality.unknown", new(position.X, position.Y, 8, 8), VisualTokens.Unknown));
        }
    }

    private static double GetRawValue(ControlRenderContext context) => GetNumeric(context, "ProcessValue", 0);

    private static double GetNumeric(ControlRenderContext context, string key, double fallback) =>
        context.NumericValues.TryGetValue(key, out var value) && double.IsFinite(value) ? value : fallback;

    private static string FormatNumber(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string PipeToken(ControlRenderContext context) =>
        context.Quality != VariableQuality.Good ? VisualTokens.Unknown : StateToken(context);

    private static string StateToken(ControlRenderContext context) =>
        context.Quality != VariableQuality.Good
            ? VisualTokens.Unknown
            : context.State switch
            {
                ControlState.Active => VisualTokens.Active,
                ControlState.Transition => VisualTokens.Transition,
                ControlState.Fault => VisualTokens.Fault,
                ControlState.Unknown => VisualTokens.Unknown,
                ControlState.Stopped => VisualTokens.Stopped,
                _ => VisualTokens.Outline
            };
}

public static class ControlGeometryFactory
{
    public static ControlRenderPlan Build(string typeId, ControlRenderContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeId);
        ArgumentNullException.ThrowIfNull(context);

        return typeId switch
        {
            ControlTypeIds.CentrifugalPump => PumpAndValveGeometry.BuildPump(context),
            ControlTypeIds.AutomatedValve => PumpAndValveGeometry.BuildValve(context),
            ControlTypeIds.Vessel => VesselAgitatorFilterGeometry.BuildVessel(context),
            ControlTypeIds.Agitator => VesselAgitatorFilterGeometry.BuildAgitator(context),
            ControlTypeIds.Filter => VesselAgitatorFilterGeometry.BuildFilter(context),
            ControlTypeIds.StraightPipe => PipeInstrumentOperatorGeometry.BuildStraightPipe(context),
            ControlTypeIds.PipeElbow => PipeInstrumentOperatorGeometry.BuildElbow(context),
            ControlTypeIds.PipeTee => PipeInstrumentOperatorGeometry.BuildTee(context),
            ControlTypeIds.NumericDisplay => PipeInstrumentOperatorGeometry.BuildNumericDisplay(context),
            ControlTypeIds.LevelBar => PipeInstrumentOperatorGeometry.BuildLevelBar(context),
            ControlTypeIds.TemperatureIndicator => PipeInstrumentOperatorGeometry.BuildTemperatureIndicator(context),
            ControlTypeIds.PressureIndicator => PipeInstrumentOperatorGeometry.BuildPressureIndicator(context),
            ControlTypeIds.FlowIndicator => PipeInstrumentOperatorGeometry.BuildFlowIndicator(context),
            ControlTypeIds.CommandButton => PipeInstrumentOperatorGeometry.BuildCommandButton(context),
            _ => throw new NotSupportedException($"Control geometry type '{typeId}' is not supported.")
        };
    }
}
