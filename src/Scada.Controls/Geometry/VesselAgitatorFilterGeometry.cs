using System.Globalization;
using Scada.Controls.Rendering;
using Scada.Core;

namespace Scada.Controls.Geometry;

/// <summary>
/// Builds the first reusable vessel, agitator and filter silhouettes.
/// Geometry is deliberately independent: composition and placement belong to the scene.
/// </summary>
public static class VesselAgitatorFilterGeometry
{
    public static ControlRenderPlan BuildVessel(ControlRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        const double width = 180;
        const double height = 240;
        const double shellLeft = 32;
        const double shellRight = 148;
        const double shellTop = 30;
        const double shellBottom = 198;
        const double liquidLeft = 43;
        const double liquidWidth = 94;
        const double liquidTop = 53;
        const double liquidBottom = 184;
        const double headspace = 14;

        var level = context.GetClampedNumeric("LevelValue", 0, 100);
        var availableHeight = liquidBottom - liquidTop - headspace;
        var liquidHeight = availableHeight * level / 100;
        var liquidY = liquidBottom - liquidHeight;
        var stateToken = StateToken(context);

        var primitives = new RenderPrimitive[]
        {
            new RenderPath(
                "vessel.jacket",
                [new MoveTo(new(25, 48)), new CubicTo(new(25, 36), new(32, 27), new(42, 24)),
                 new LineTo(new(138, 24)), new CubicTo(new(148, 27), new(155, 36), new(155, 48)),
                 new LineTo(new(155, 187)), new CubicTo(new(155, 199), new(148, 207), new(138, 210)),
                 new LineTo(new(42, 210)), new CubicTo(new(32, 207), new(25, 199), new(25, 187)),
                 new ClosePath()],
                VisualTokens.EquipmentDepth),
            new RenderPath(
                "vessel.shell",
                [new MoveTo(new(shellLeft, 48)), new CubicTo(new(shellLeft, 35), new(43, shellTop), new(90, shellTop)),
                 new CubicTo(new(137, shellTop), new(shellRight, 35), new(shellRight, 48)),
                 new LineTo(new(shellRight, shellBottom)), new CubicTo(new(shellRight, 210), new(130, 217), new(90, 217)),
                 new CubicTo(new(50, 217), new(shellLeft, 210), new(shellLeft, shellBottom)),
                 new ClosePath()],
                VisualTokens.EquipmentBody),
            new RenderPath(
                "vessel.shell-highlight",
                [new MoveTo(new(42, 48)), new CubicTo(new(42, 38), new(51, 33), new(90, 33)),
                 new CubicTo(new(129, 33), new(138, 38), new(138, 48))],
                VisualTokens.EquipmentDepth),
            new RenderRectangle("vessel.liquid", new(liquidLeft, liquidY, liquidWidth, liquidHeight), stateToken),
            new RenderLine("vessel.liquid-surface", new(liquidLeft, liquidY), new(liquidLeft + liquidWidth, liquidY), stateToken),
            new RenderLine("vessel.level-guide", new(153, 53), new(153, 184), VisualTokens.EquipmentDepth),
            new RenderLine("vessel.level-tick-low", new(149, 184), new(157, 184), VisualTokens.EquipmentDepth),
            new RenderLine("vessel.level-tick-high", new(149, 53), new(157, 53), VisualTokens.EquipmentDepth),
            new RenderRectangle("vessel.bottom-outlet", new(82, 214, 16, 26), VisualTokens.EquipmentDepth),
            new RenderLine("vessel.outlet-flange", new(78, 216), new(102, 216), VisualTokens.Outline),
            new RenderEllipse("vessel.state-indicator", new(78, 8, 24, 16), stateToken)
        };

        return new ControlRenderPlan(
            ControlTypeIds.Vessel,
            version: 1,
            new RenderSize(width, height),
            primitives,
            new Dictionary<string, RenderPoint>(StringComparer.Ordinal)
            {
                ["top-inlet"] = new(90, 0),
                ["bottom-outlet"] = new(90, height),
                ["level"] = new(153, 53)
            },
            context.State,
            new HashSet<string>(StringComparer.Ordinal),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["vessel.level"] = level.ToString("0.###", CultureInfo.InvariantCulture),
                ["vessel.headspace"] = headspace.ToString("0.###", CultureInfo.InvariantCulture)
            });
    }

    public static ControlRenderPlan BuildAgitator(ControlRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var activeAnimation = context.State == ControlState.Active
            && context.Quality == VariableQuality.Good
            && !context.ReducedMotion
            ? "agitator.rotate"
            : null;
        var stateToken = StateToken(context);
        var primitives = new RenderPrimitive[]
        {
            new RenderPath(
                "agitator.vessel",
                [new MoveTo(new(16, 45)), new CubicTo(new(16, 36), new(23, 31), new(50, 31)),
                 new CubicTo(new(77, 31), new(84, 36), new(84, 45)), new LineTo(new(84, 137)),
                 new CubicTo(new(84, 148), new(74, 153), new(50, 153)),
                 new CubicTo(new(26, 153), new(16, 148), new(16, 137)), new ClosePath()],
                VisualTokens.EquipmentBody),
            new RenderRectangle("agitator.motor", new(35, 8, 30, 22), VisualTokens.EquipmentBody),
            new RenderRectangle("agitator.motor-end", new(31, 12, 5, 14), VisualTokens.EquipmentDepth),
            new RenderRectangle("agitator.motor-foot", new(42, 29, 16, 6), VisualTokens.EquipmentDepth),
            new RenderLine("agitator.shaft", new(50, 30), new(50, 132), VisualTokens.Outline, activeAnimation),
            new RenderPath(
                "agitator.impeller",
                [new MoveTo(new(30, 132)), new LineTo(new(70, 132)), new LineTo(new(63, 139)),
                 new LineTo(new(54, 135)), new LineTo(new(50, 145)), new LineTo(new(46, 135)),
                 new LineTo(new(37, 139)), new ClosePath()],
                stateToken,
                activeAnimation),
            new RenderLine("agitator.liquid-line", new(25, 112), new(75, 112), VisualTokens.EquipmentDepth)
        };

        return new ControlRenderPlan(
            ControlTypeIds.Agitator,
            version: 1,
            new RenderSize(100, 160),
            primitives,
            new Dictionary<string, RenderPoint>(StringComparer.Ordinal)
            {
                ["top-drive"] = new(50, 0),
                ["bottom-shaft"] = new(50, 160)
            },
            context.State,
            activeAnimation is null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>([activeAnimation], StringComparer.Ordinal),
            new Dictionary<string, string>(StringComparer.Ordinal));
    }

    public static ControlRenderPlan BuildFilter(ControlRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var blocked = context.State == ControlState.Fault;
        var stateToken = StateToken(context);
        var primitives = new RenderPrimitive[]
        {
            new RenderLine("filter.inlet", new(0, 72), new(30, 72), VisualTokens.Outline),
            new RenderRectangle("filter.inlet-flange", new(26, 64, 6, 16), VisualTokens.EquipmentDepth),
            new RenderPath(
                "filter.housing",
                [new MoveTo(new(30, 24)), new CubicTo(new(39, 16), new(61, 16), new(70, 24)),
                 new LineTo(new(70, 124)), new CubicTo(new(61, 132), new(39, 132), new(30, 124)),
                 new ClosePath()],
                VisualTokens.EquipmentBody),
            new RenderEllipse("filter.cover", new(30, 15, 40, 18), VisualTokens.EquipmentDepth),
            new RenderRectangle("filter.element", new(41, 38, 18, 75), stateToken),
            new RenderLine("filter.outlet", new(70, 72), new(100, 72), VisualTokens.Outline),
            new RenderRectangle("filter.outlet-flange", new(68, 64, 6, 16), VisualTokens.EquipmentDepth),
            new RenderEllipse("filter.blocked-indicator", new(42, 139, 16, 8), blocked ? VisualTokens.Fault : stateToken)
        };

        return new ControlRenderPlan(
            ControlTypeIds.Filter,
            version: 1,
            new RenderSize(100, 160),
            primitives,
            new Dictionary<string, RenderPoint>(StringComparer.Ordinal)
            {
                ["inlet"] = new(0, 72),
                ["outlet"] = new(100, 72)
            },
            context.State,
            new HashSet<string>(StringComparer.Ordinal),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["filter.condition"] = blocked ? "blocked" : "normal"
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
