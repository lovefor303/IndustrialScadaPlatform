using System.Text;
using Scada.Controls;
using Scada.Controls.Geometry;
using Scada.Controls.Rendering;
using Scada.Controls.Svg;
using Scada.Core;

return await SvgPreviewProgram.RunAsync(args);

internal static class SvgPreviewProgram
{
    private static readonly ControlState[] States =
    [
        ControlState.Stopped,
        ControlState.Active,
        ControlState.Transition,
        ControlState.Fault,
        ControlState.Unknown
    ];

    public static Task<int> RunAsync(string[] args)
    {
        if (args.Length != 2 || !string.Equals(args[0], "--output", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Usage: Scada.Controls.Preview.Svg --output <absolute-directory>");
            return Task.FromResult(2);
        }

        var directory = args[1];
        if (!Path.IsPathFullyQualified(directory))
        {
            Console.Error.WriteLine("The output directory must be an absolute path.");
            return Task.FromResult(2);
        }

        Directory.CreateDirectory(directory);
        var renderer = new SvgControlRenderer();
        var documents = new List<SvgPreviewDocument>();
        foreach (var typeId in ControlTypeIds.All.OrderBy(type => type, StringComparer.Ordinal))
        {
            foreach (var state in States)
            {
                var fileName = $"{typeId}-{StateName(state)}.svg";
                var plan = ControlGeometryFactory.Build(typeId, CreateContext(state));
                File.WriteAllText(Path.Combine(directory, fileName), renderer.Render(plan), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                documents.Add(new SvgPreviewDocument(fileName, $"{typeId} / {StateName(state)}"));
            }
        }

        var html = new SvgDocumentWriter().CreateHtml("工业控件离线 SVG 预览", documents);
        File.WriteAllText(Path.Combine(directory, "index.html"), html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine($"Wrote {documents.Count} SVG files and index.html to {directory}");
        return Task.FromResult(0);
    }

    private static ControlRenderContext CreateContext(ControlState state) =>
        ControlRenderContext.ForState(state) with
        {
            Quality = state == ControlState.Unknown ? VariableQuality.Bad : VariableQuality.Good,
            NumericValues = new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["ProcessValue"] = 58.2,
                ["LevelValue"] = 62,
                ["ValvePosition"] = 45,
                ["Minimum"] = 0,
                ["Maximum"] = 100
            },
            TextValues = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Unit"] = "°C",
                ["Label"] = "执行",
                ["ActionKind"] = "write-command",
                ["ActionTarget"] = "Preview.Command"
            }
        };

    private static string StateName(ControlState state) => state switch
    {
        ControlState.Stopped => "stopped",
        ControlState.Active => "active",
        ControlState.Transition => "transition",
        ControlState.Fault => "fault",
        ControlState.Unknown => "unknown",
        _ => throw new ArgumentOutOfRangeException(nameof(state))
    };
}
