using System.Globalization;
using System.Text;
using System.Xml;
using System.Diagnostics.CodeAnalysis;
using Scada.Controls.Rendering;
using Scada.Core;

namespace Scada.Controls.Svg;

/// <summary>
/// Serializes the platform-neutral control render contract as standalone SVG.
/// The renderer deliberately accepts only typed primitives and never includes executable markup or external assets.
/// </summary>
public sealed class SvgControlRenderer
{
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The renderer remains an instance dependency so future profiles can carry explicit options without changing callers.")]
    public string Render(ControlRenderPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var output = new StringBuilder();
        using var writer = XmlWriter.Create(output, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = false,
            OmitXmlDeclaration = true,
            NewLineHandling = NewLineHandling.None
        });

        writer.WriteStartElement("svg", "http://www.w3.org/2000/svg");
        writer.WriteAttributeString("viewBox", $"0 0 {Number(plan.DesignSize.Width)} {Number(plan.DesignSize.Height)}");
        writer.WriteAttributeString("width", Number(plan.DesignSize.Width));
        writer.WriteAttributeString("height", Number(plan.DesignSize.Height));
        writer.WriteAttributeString("role", "img");
        writer.WriteAttributeString("data-control-type", plan.TypeId);
        writer.WriteAttributeString("data-control-version", plan.Version.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("data-state", StateName(plan.State));
        writer.WriteAttributeString("data-quality", QualityName(plan));

        WriteStyles(writer);
        WriteAnchors(writer, plan.Anchors);
        foreach (var primitive in plan.Primitives)
        {
            WritePrimitive(writer, primitive, plan);
        }
        if (plan.State == ControlState.Unknown)
        {
            WriteUnknownQualityMarker(writer, plan);
        }

        writer.WriteEndElement();
        writer.Flush();
        return output.ToString();
    }

    private static void WriteAnchors(XmlWriter writer, IReadOnlyDictionary<string, RenderPoint> anchors)
    {
        writer.WriteStartElement("metadata");
        foreach (var anchor in anchors.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            writer.WriteStartElement("anchor");
            writer.WriteAttributeString("data-anchor", anchor.Key);
            writer.WriteAttributeString("x", Number(anchor.Value.X));
            writer.WriteAttributeString("y", Number(anchor.Value.Y));
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteUnknownQualityMarker(XmlWriter writer, ControlRenderPlan plan)
    {
        writer.WriteStartElement("ellipse");
        writer.WriteAttributeString("data-part", "control.quality-unknown");
        writer.WriteAttributeString("class", "scada-shape token-unknown");
        writer.WriteAttributeString("cx", Number(Math.Max(0, plan.DesignSize.Width - 6)));
        writer.WriteAttributeString("cy", "6");
        writer.WriteAttributeString("rx", "4");
        writer.WriteAttributeString("ry", "4");
        writer.WriteEndElement();
    }

    private static void WriteStyles(XmlWriter writer)
    {
        writer.WriteStartElement("style");
        writer.WriteString(
            ".scada-line{fill:none;stroke-linecap:round;stroke-linejoin:round;}" +
            ".scada-shape{stroke:#343B40;stroke-width:1.5;stroke-linejoin:round;}" +
            ".scada-text{font-family:'Segoe UI',sans-serif;font-size:12px;}" +
            ".stroke-pipe{stroke-width:4;}" +
            ".token-canvas{fill:#202428;stroke:#202428;}" +
            ".token-equipment-body{fill:#D4D9DC;stroke:#D4D9DC;}" +
            ".token-equipment-depth{fill:#929BA1;stroke:#929BA1;}" +
            ".token-outline{fill:#343B40;stroke:#343B40;}" +
            ".token-stopped{fill:#7E878D;stroke:#7E878D;}" +
            ".token-active{fill:#2E9D62;stroke:#2E9D62;}" +
            ".token-transition{fill:#D79B2B;stroke:#D79B2B;}" +
            ".token-fault{fill:#D64045;stroke:#D64045;}" +
            ".token-unknown{fill:#697278;stroke:#697278;}" +
            "@keyframes scada-spin{to{transform:rotate(360deg);}}" +
            "@keyframes scada-pulse{50%{opacity:.45;}}" +
            ".motion-pump-rotate,.motion-agitator-rotate{animation:scada-spin .9s linear infinite;transform-origin:center;transform-box:fill-box;}" +
            ".motion-valve-travel,.motion-pipe-flow{animation:scada-pulse .9s ease-in-out infinite;}" +
            "@media (prefers-reduced-motion:reduce){.motion-pump-rotate,.motion-agitator-rotate,.motion-valve-travel,.motion-pipe-flow{animation:none;}}"
        );
        writer.WriteEndElement();
    }

    private static void WritePrimitive(XmlWriter writer, RenderPrimitive primitive, ControlRenderPlan plan)
    {
        switch (primitive)
        {
            case RenderLine line:
                writer.WriteStartElement("line");
                WritePartAttributes(writer, line, plan, "scada-line", isPipe: line.PartId.StartsWith("pipe.", StringComparison.Ordinal));
                writer.WriteAttributeString("x1", Number(line.Start.X));
                writer.WriteAttributeString("y1", Number(line.Start.Y));
                writer.WriteAttributeString("x2", Number(line.End.X));
                writer.WriteAttributeString("y2", Number(line.End.Y));
                writer.WriteEndElement();
                break;
            case RenderRectangle rectangle:
                writer.WriteStartElement("rect");
                WritePartAttributes(writer, rectangle, plan, "scada-shape");
                writer.WriteAttributeString("x", Number(rectangle.Bounds.X));
                writer.WriteAttributeString("y", Number(rectangle.Bounds.Y));
                writer.WriteAttributeString("width", Number(rectangle.Bounds.Width));
                writer.WriteAttributeString("height", Number(rectangle.Bounds.Height));
                writer.WriteEndElement();
                break;
            case RenderEllipse ellipse:
                writer.WriteStartElement("ellipse");
                WritePartAttributes(writer, ellipse, plan, "scada-shape");
                writer.WriteAttributeString("cx", Number(ellipse.Bounds.X + ellipse.Bounds.Width / 2));
                writer.WriteAttributeString("cy", Number(ellipse.Bounds.Y + ellipse.Bounds.Height / 2));
                writer.WriteAttributeString("rx", Number(ellipse.Bounds.Width / 2));
                writer.WriteAttributeString("ry", Number(ellipse.Bounds.Height / 2));
                writer.WriteEndElement();
                break;
            case RenderText text:
                writer.WriteStartElement("text");
                WritePartAttributes(writer, text, plan, "scada-text");
                writer.WriteAttributeString("x", Number(text.Position.X));
                writer.WriteAttributeString("y", Number(text.Position.Y));
                writer.WriteString(text.Text);
                writer.WriteEndElement();
                break;
            case RenderPath path:
                writer.WriteStartElement("path");
                WritePartAttributes(writer, path, plan, path.PartId.StartsWith("pipe.", StringComparison.Ordinal) ? "scada-line" : "scada-shape", path.PartId.StartsWith("pipe.", StringComparison.Ordinal));
                writer.WriteAttributeString("d", PathData(path.Commands));
                writer.WriteEndElement();
                break;
            case RenderGroup group:
                writer.WriteStartElement("g");
                WritePartAttributes(writer, group, plan, string.Empty);
                foreach (var child in group.Children)
                {
                    WritePrimitive(writer, child, plan);
                }

                writer.WriteEndElement();
                break;
            default:
                throw new NotSupportedException($"Unsupported render primitive '{primitive.GetType().Name}'.");
        }
    }

    private static void WritePartAttributes(
        XmlWriter writer,
        RenderPrimitive primitive,
        ControlRenderPlan plan,
        string baseClass,
        bool isPipe = false)
    {
        var classes = new List<string>();
        if (!string.IsNullOrEmpty(baseClass))
        {
            classes.Add(baseClass);
        }

        classes.Add(TokenClass(primitive.Token));
        if (isPipe)
        {
            classes.Add("stroke-pipe");
        }

        if (primitive.AnimationName is { } animation && plan.ActiveAnimations.Contains(animation))
        {
            classes.Add(MotionClass(animation));
            writer.WriteAttributeString("data-animation", animation);
        }

        writer.WriteAttributeString("data-part", primitive.PartId);
        if (classes.Count > 0)
        {
            writer.WriteAttributeString("class", string.Join(" ", classes));
        }
    }

    private static string PathData(IReadOnlyList<PathCommand> commands)
    {
        var builder = new StringBuilder();
        foreach (var command in commands)
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            switch (command)
            {
                case MoveTo move:
                    builder.Append("M ").Append(Number(move.Point.X)).Append(' ').Append(Number(move.Point.Y));
                    break;
                case LineTo line:
                    builder.Append("L ").Append(Number(line.Point.X)).Append(' ').Append(Number(line.Point.Y));
                    break;
                case CubicTo cubic:
                    builder.Append("C ").Append(Number(cubic.Control1.X)).Append(' ').Append(Number(cubic.Control1.Y)).Append(' ')
                        .Append(Number(cubic.Control2.X)).Append(' ').Append(Number(cubic.Control2.Y)).Append(' ')
                        .Append(Number(cubic.Point.X)).Append(' ').Append(Number(cubic.Point.Y));
                    break;
                case ClosePath:
                    builder.Append('Z');
                    break;
                default:
                    throw new NotSupportedException($"Unsupported path command '{command.GetType().Name}'.");
            }
        }

        return builder.ToString();
    }

    private static string TokenClass(string token) => token switch
    {
        VisualTokens.Canvas => "token-canvas",
        VisualTokens.EquipmentBody => "token-equipment-body",
        VisualTokens.EquipmentDepth => "token-equipment-depth",
        VisualTokens.Outline => "token-outline",
        VisualTokens.Stopped => "token-stopped",
        VisualTokens.Active => "token-active",
        VisualTokens.Transition => "token-transition",
        VisualTokens.Fault => "token-fault",
        VisualTokens.Unknown => "token-unknown",
        "transparent" => "",
        _ => throw new NotSupportedException($"Unsupported visual token '{token}'.")
    };

    private static string MotionClass(string animation) => animation switch
    {
        "pump.rotate" => "motion-pump-rotate",
        "agitator.rotate" => "motion-agitator-rotate",
        "valve.travel" => "motion-valve-travel",
        "pipe.flow" => "motion-pipe-flow",
        _ => throw new NotSupportedException($"Unsupported animation '{animation}'.")
    };

    private static string StateName(ControlState state) => state switch
    {
        ControlState.Neutral => "neutral",
        ControlState.Stopped => "stopped",
        ControlState.Active => "active",
        ControlState.Transition => "transition",
        ControlState.Fault => "fault",
        ControlState.Unknown => "unknown",
        _ => throw new ArgumentOutOfRangeException(nameof(state))
    };

    private static string QualityName(ControlRenderPlan plan) =>
        plan.State == ControlState.Unknown || ContainsUnknownQualityMarker(plan.Primitives)
            ? "bad"
            : "good";

    private static bool ContainsUnknownQualityMarker(IEnumerable<RenderPrimitive> primitives) => primitives.Any(primitive =>
        primitive.PartId == "quality.unknown"
        || primitive is RenderGroup group && ContainsUnknownQualityMarker(group.Children));

    private static string Number(double value) => value.ToString("0.###############", CultureInfo.InvariantCulture);
}
