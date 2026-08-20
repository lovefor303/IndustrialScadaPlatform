using System.Globalization;
using Scada.Controls.Rendering;
using Scada.Core;

namespace Scada.Controls.Geometry;

public static class PumpAndValveGeometry
{
    public static ControlRenderPlan BuildPump(ControlRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var stateToken = StateToken(context);
        var activeAnimation = context.State == ControlState.Active
            && context.Quality == VariableQuality.Good
            && !context.ReducedMotion
            ? "pump.rotate"
            : null;
        var primitives = new RenderPrimitive[]
        {
            new RenderLine("pump.suction-pipe", new(0, 38), new(29, 38), VisualTokens.Outline),
            new RenderRectangle("pump.suction-flange", new(24, 30, 6, 16), VisualTokens.EquipmentDepth),
            new RenderPath(
                "pump.casing",
                [new MoveTo(new(29, 38)), new CubicTo(new(29, 20), new(43, 12), new(57, 15)),
                 new CubicTo(new(69, 17), new(76, 28), new(74, 39)),
                 new CubicTo(new(72, 52), new(59, 59), new(46, 56)),
                 new CubicTo(new(35, 53), new(29, 47), new(29, 38)), new ClosePath()],
                VisualTokens.EquipmentBody),
            new RenderEllipse("pump.state-accent", new(36, 25, 25, 25), stateToken),
            new RenderEllipse("pump.impeller", new(43, 32, 11, 11), VisualTokens.Outline, activeAnimation),
            new RenderPath(
                "pump.discharge-neck",
                [new MoveTo(new(56, 16)), new LineTo(new(65, 13)), new LineTo(new(74, 17)),
                 new LineTo(new(74, 23)), new LineTo(new(65, 23)), new ClosePath()],
                VisualTokens.EquipmentBody),
            new RenderRectangle("pump.discharge-flange", new(72, 14, 5, 12), VisualTokens.EquipmentDepth),
            new RenderLine("pump.discharge-pipe", new(77, 20), new(120, 20), VisualTokens.Outline),
            new RenderRectangle("pump.coupling", new(72, 32, 12, 12), VisualTokens.EquipmentDepth),
            new RenderRectangle("pump.motor", new(84, 25, 27, 26), VisualTokens.EquipmentBody),
            new RenderPath(
                "pump.motor-end-cap",
                [new MoveTo(new(111, 27)), new CubicTo(new(118, 29), new(118, 47), new(111, 49)),
                 new ClosePath()],
                VisualTokens.EquipmentDepth),
            new RenderLine("pump.motor-fin-1", new(90, 28), new(90, 48), VisualTokens.EquipmentDepth),
            new RenderLine("pump.motor-fin-2", new(96, 28), new(96, 48), VisualTokens.EquipmentDepth),
            new RenderLine("pump.motor-fin-3", new(102, 28), new(102, 48), VisualTokens.EquipmentDepth),
            new RenderLine("pump.shaft", new(54, 38), new(86, 38), VisualTokens.Outline, activeAnimation),
            new RenderRectangle("pump.front-foot", new(38, 53, 12, 5), VisualTokens.EquipmentDepth),
            new RenderRectangle("pump.motor-foot", new(91, 50, 13, 8), VisualTokens.EquipmentDepth),
            new RenderRectangle("pump.base", new(22, 58, 96, 5), VisualTokens.EquipmentDepth)
        };

        return new ControlRenderPlan(
            ControlTypeIds.CentrifugalPump,
            version: 1,
            new RenderSize(120, 72),
            primitives,
            new Dictionary<string, RenderPoint>(StringComparer.Ordinal)
            {
                ["suction"] = new(0, 38),
                ["discharge"] = new(120, 20)
            },
            context.State,
            activeAnimation is null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>([activeAnimation], StringComparer.Ordinal),
            new Dictionary<string, string>(StringComparer.Ordinal));
    }

    public static ControlRenderPlan BuildValve(ControlRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var position = context.GetClampedNumeric("ValvePosition", 0, 100);
        var positionText = position.ToString("0.###", CultureInfo.InvariantCulture);
        var activeAnimation = context.State is ControlState.Transition or ControlState.Active
            && context.Quality == VariableQuality.Good
            && !context.ReducedMotion
            ? "valve.travel"
            : null;
        var stemEndY = 26 + (position * 0.12);
        var primitives = new RenderPrimitive[]
        {
            new RenderLine("valve.inlet-pipe", new(0, 40), new(35, 40), VisualTokens.Outline),
            new RenderPath(
                "valve.body",
                [new MoveTo(new(35, 40)), new LineTo(new(50, 29)), new LineTo(new(65, 40)),
                 new LineTo(new(50, 51)), new ClosePath()],
                VisualTokens.EquipmentBody),
            new RenderRectangle("valve.body-accent", new(42, 35, 16, 10), StateToken(context)),
            new RenderLine("valve.stem", new(50, 26), new(50, stemEndY), VisualTokens.Outline, activeAnimation),
            new RenderRectangle("valve.actuator", new(42, 8, 16, 18), VisualTokens.EquipmentBody),
            new RenderLine("valve.outlet-pipe", new(65, 40), new(100, 40), VisualTokens.Outline),
            new RenderEllipse("valve.state-indicator", new(45, 11, 10, 10), StateToken(context))
        };

        return new ControlRenderPlan(
            ControlTypeIds.AutomatedValve,
            version: 1,
            new RenderSize(100, 80),
            primitives,
            new Dictionary<string, RenderPoint>(StringComparer.Ordinal)
            {
                ["inlet"] = new(0, 40),
                ["outlet"] = new(100, 40)
            },
            context.State,
            activeAnimation is null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>([activeAnimation], StringComparer.Ordinal),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["valve.position"] = positionText
            });
    }

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
