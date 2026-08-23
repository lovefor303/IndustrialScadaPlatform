using System.Globalization;
using System.Security;
using Scada.Controls;
using Scada.Controls.Geometry;
using Scada.Controls.Rendering;
using Scada.Controls.Svg;
using Scada.Core;
using Scada.Scene;

namespace Scada.Runtime;

public sealed class RuntimeSceneProjector
{
    private readonly ControlCatalog _catalog;
    private readonly SvgControlRenderer _svgRenderer;

    public RuntimeSceneProjector(ControlCatalog? catalog = null, SvgControlRenderer? svgRenderer = null)
    {
        _catalog = catalog ?? ControlCatalog.CreateDefault();
        _svgRenderer = svgRenderer ?? new SvgControlRenderer();
    }

    public RuntimeScreenDocument Project(
        ProjectDocument project,
        IRuntimeVariableSource variables,
        string screenName,
        string layoutProfile = "desktop",
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentException.ThrowIfNullOrWhiteSpace(screenName);
        ValidateProfile(layoutProfile);

        var screen = project.Screens.SingleOrDefault(item =>
            string.Equals(item.Name, screenName, StringComparison.Ordinal));
        if (screen is null)
        {
            throw new RuntimeSourceException(
                RuntimeDiagnostics.Create(RuntimeDiagnostics.ScreenNotFound, $"画面“{screenName}”不存在。"));
        }

        var timestamp = now ?? DateTimeOffset.UtcNow;
        var diagnostics = new List<RuntimeDiagnostic>();
        var projections = screen.Objects
            .OrderBy(item => item.ZIndex)
            .ThenBy(item => item.Id)
            .Select(item => ProjectObject(item, variables, timestamp, diagnostics))
            .ToArray();

        return new RuntimeScreenDocument(
            screen.Name,
            DesignBounds(screen.Objects),
            layoutProfile,
            projections,
            diagnostics);
    }

    private RuntimeObjectProjection ProjectObject(
        SceneObject sceneObject,
        IRuntimeVariableSource variables,
        DateTimeOffset now,
        ICollection<RuntimeDiagnostic> diagnostics)
    {
        return sceneObject switch
        {
            ControlObject control => ProjectControl(control, variables, now, diagnostics),
            PipeObject pipe => ProjectPipe(pipe),
            TextObject text => ProjectText(text),
            _ => ProjectUnsupported(sceneObject, diagnostics)
        };
    }

    private RuntimeObjectProjection ProjectControl(
        ControlObject control,
        IRuntimeVariableSource variables,
        DateTimeOffset now,
        ICollection<RuntimeDiagnostic> diagnostics)
    {
        ControlDefinition definition;
        try
        {
            definition = _catalog.Get(control.Type, control.ControlVersion);
        }
        catch (KeyNotFoundException)
        {
            return ProjectUnsupported(control, diagnostics);
        }

        var roleValues = new Dictionary<string, VariableValue>(StringComparer.Ordinal);
        var numericValues = new Dictionary<string, double>(StringComparer.Ordinal);
        var textValues = new Dictionary<string, string>(control.Properties, StringComparer.Ordinal);
        var quality = VariableQuality.Good;

        foreach (var role in definition.Bindings)
        {
            var runtimeValue = control.Bindings.TryGetValue(role.Name, out var binding)
                ? variables.Read(binding.VariableKey, now)
                : new RuntimeVariableValue(
                    role.Name,
                    null,
                    role.DataTypes.First(),
                    VariableQuality.Bad,
                    now);
            var value = new VariableValue(
                runtimeValue.DataType,
                runtimeValue.Value,
                runtimeValue.Quality,
                runtimeValue.Timestamp);
            roleValues[role.Name] = value;
            quality = Worse(quality, runtimeValue.Quality);

            if (TryGetNumber(runtimeValue.Value, out var number))
            {
                numericValues[role.Name] = number;
                numericValues[role.StateRole.ToString()] = number;
                if (binding is not null)
                {
                    numericValues[binding.TargetProperty] = number;
                }
            }
        }

        AddPropertyNumber(control.Properties, numericValues, "Minimum");
        AddPropertyNumber(control.Properties, numericValues, "Maximum");

        var stateResult = ControlStateResolver.Resolve(definition, roleValues, now);
        var context = ControlRenderContext.ForState(stateResult.State) with
        {
            NumericValues = numericValues,
            TextValues = textValues,
            Quality = quality,
            ReducedMotion = false
        };
        var plan = ControlGeometryFactory.Build(control.Type, context);
        var innerSvg = _svgRenderer.Render(plan);
        var svg = WrapControlSvg(control, plan, innerSvg, quality);

        if (quality != VariableQuality.Good)
        {
            diagnostics.Add(RuntimeDiagnostics.UnknownVariable(control.Id.ToString()));
        }

        return new RuntimeObjectProjection(
            control.Id,
            control.Type,
            control.Bounds,
            control.Rotation,
            control.ZIndex,
            control.IsVisible,
            svg,
            stateResult.State,
            quality,
            ReadOnly: true,
            Diagnostics: quality == VariableQuality.Good
                ? []
                : new[] { RuntimeDiagnostics.UnknownVariable(control.Id.ToString()) });
    }

    private static RuntimeObjectProjection ProjectPipe(PipeObject pipe)
    {
        var points = new[] { pipe.Start }.Concat(pipe.Bends).Append(pipe.End).ToArray();
        var path = string.Join(
            " ",
            points.Select((point, index) =>
                $"{(index == 0 ? "M" : "L")} {Number(point.X)} {Number(point.Y)}"));
        var svg = $"<g data-object-id=\"{pipe.Id}\" data-control-type=\"{SecurityElement.Escape(pipe.Type)}\" data-state=\"neutral\" data-quality=\"good\"><path data-part=\"pipe.runtime\" d=\"{path}\" fill=\"none\" stroke=\"#D79B2B\" stroke-width=\"4\" stroke-linecap=\"round\" stroke-linejoin=\"round\" /></g>";
        return new RuntimeObjectProjection(
            pipe.Id,
            pipe.Type,
            pipe.Bounds,
            pipe.Rotation,
            pipe.ZIndex,
            pipe.IsVisible,
            svg,
            ControlState.Neutral,
            VariableQuality.Good,
            ReadOnly: true,
            Diagnostics: [],
            pipe.Start,
            pipe.End,
            pipe.Bends);
    }

    private static RuntimeObjectProjection ProjectText(TextObject text)
    {
        var escaped = SecurityElement.Escape(text.Text) ?? string.Empty;
        var svg = $"<g data-object-id=\"{text.Id}\" data-control-type=\"text\" data-state=\"neutral\" data-quality=\"good\"><text x=\"{Number(text.Bounds.X)}\" y=\"{Number(text.Bounds.Y + 16)}\" fill=\"#E6ECEF\" font-family=\"Segoe UI, Microsoft YaHei, sans-serif\" font-size=\"18\">{escaped}</text></g>";
        return new RuntimeObjectProjection(
            text.Id,
            text.Type,
            text.Bounds,
            text.Rotation,
            text.ZIndex,
            text.IsVisible,
            svg,
            ControlState.Neutral,
            VariableQuality.Good,
            ReadOnly: true,
            Diagnostics: []);
    }

    private static RuntimeObjectProjection ProjectUnsupported(
        SceneObject sceneObject,
        ICollection<RuntimeDiagnostic> diagnostics)
    {
        var diagnostic = RuntimeDiagnostics.Create(
            RuntimeDiagnostics.ControlUnsupported,
            $"控件类型“{sceneObject.Type}”暂不支持，已显示占位符。",
            sceneObject.Id,
            RuntimeDiagnosticSeverity.Warning);
        diagnostics.Add(diagnostic);
        var svg = $"<g data-object-id=\"{sceneObject.Id}\" data-control-type=\"unsupported\" data-state=\"unknown\" data-quality=\"bad\"><rect x=\"{Number(sceneObject.Bounds.X)}\" y=\"{Number(sceneObject.Bounds.Y)}\" width=\"{Number(sceneObject.Bounds.Width)}\" height=\"{Number(sceneObject.Bounds.Height)}\" /><text x=\"{Number(sceneObject.Bounds.X + 4)}\" y=\"{Number(sceneObject.Bounds.Y + 16)}\">不支持</text></g>";
        return new RuntimeObjectProjection(
            sceneObject.Id,
            sceneObject.Type,
            sceneObject.Bounds,
            sceneObject.Rotation,
            sceneObject.ZIndex,
            sceneObject.IsVisible,
            svg,
            ControlState.Unknown,
            VariableQuality.Bad,
            ReadOnly: true,
            Diagnostics: new[] { diagnostic });
    }

    private static string WrapControlSvg(
        ControlObject control,
        ControlRenderPlan plan,
        string innerSvg,
        VariableQuality quality)
    {
        var scaleX = control.Bounds.Width / plan.DesignSize.Width;
        var scaleY = control.Bounds.Height / plan.DesignSize.Height;
        var visibility = control.IsVisible ? "visible" : "hidden";
        return $"<g xmlns=\"http://www.w3.org/2000/svg\" data-object-id=\"{control.Id}\" data-control-type=\"{control.Type}\" data-state=\"{StateName(plan.State)}\" data-quality=\"{QualityName(quality)}\" visibility=\"{visibility}\" transform=\"translate({Number(control.Bounds.X)} {Number(control.Bounds.Y)}) rotate({Number(control.Rotation)}) scale({Number(scaleX)} {Number(scaleY)})\">{innerSvg}</g>";
    }

    private static RectD DesignBounds(IEnumerable<SceneObject> objects)
    {
        var points = new List<PointD>();
        foreach (var sceneObject in objects)
        {
            if (sceneObject is PipeObject pipe)
            {
                points.Add(pipe.Start);
                points.AddRange(pipe.Bends);
                points.Add(pipe.End);
                continue;
            }

            var bounds = sceneObject.Bounds;
            points.Add(new(bounds.X, bounds.Y));
            points.Add(new(bounds.X + bounds.Width, bounds.Y + bounds.Height));
        }

        return points.Count == 0 ? new RectD(0, 0, 1920, 1080) : RectD.FromPoints(points);
    }

    private static void AddPropertyNumber(
        IReadOnlyDictionary<string, string> properties,
        Dictionary<string, double> values,
        string key)
    {
        if (properties.TryGetValue(key, out var text)
            && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            && double.IsFinite(value))
        {
            values[key] = value;
        }
    }

    private static bool TryGetNumber(object? value, out double number)
    {
        if (value is null or bool)
        {
            number = 0;
            return false;
        }

        try
        {
            number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return double.IsFinite(number);
        }
        catch (FormatException)
        {
            number = 0;
            return false;
        }
        catch (InvalidCastException)
        {
            number = 0;
            return false;
        }
    }

    private static VariableQuality Worse(VariableQuality first, VariableQuality second) =>
        (VariableQuality)Math.Max((int)first, (int)second);

    private static void ValidateProfile(string profile)
    {
        if (profile is not ("desktop" or "tablet" or "phone"))
        {
            throw new ArgumentException("运行时布局必须是 desktop、tablet 或 phone。", nameof(profile));
        }
    }

    private static string StateName(ControlState state) => state switch
    {
        ControlState.Neutral => "neutral",
        ControlState.Stopped => "stopped",
        ControlState.Active => "active",
        ControlState.Transition => "transition",
        ControlState.Fault => "fault",
        ControlState.Unknown => "unknown",
        _ => "unknown"
    };

    private static string QualityName(VariableQuality quality) => quality switch
    {
        VariableQuality.Good => "good",
        VariableQuality.Uncertain => "uncertain",
        VariableQuality.Bad => "bad",
        _ => "bad"
    };

    private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
