using System.Text;
using Scada.Controls.SampleProject;
using Scada.Controls.Svg;

return await SvgPreviewProgram.RunAsync(args);

internal static class SvgPreviewProgram
{
    private static readonly string[] ScenarioNames = ["stopped", "active", "transition", "fault", "unknown"];

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
        foreach (var scenarioName in ScenarioNames)
        {
            foreach (var renderable in SampleProjectFactory.BuildRenderables(scenarioName))
            {
                var fileName = $"{renderable.Control.Type}-{renderable.Control.Id:N}-{scenarioName}.svg";
                File.WriteAllText(
                    Path.Combine(directory, fileName),
                    renderer.Render(renderable.Plan),
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                var label = renderable.Control.Properties.TryGetValue("Label", out var value) ? value : renderable.Control.Type;
                documents.Add(new SvgPreviewDocument(fileName, $"{label} / {scenarioName}"));
            }
        }

        var html = new SvgDocumentWriter().CreateHtml("工业控件离线 SVG 预览", documents);
        File.WriteAllText(Path.Combine(directory, "index.html"), html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine($"Wrote {documents.Count} SVG files and index.html to {directory}");
        return Task.FromResult(0);
    }

}
